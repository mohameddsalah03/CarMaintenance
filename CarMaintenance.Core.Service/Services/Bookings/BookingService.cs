using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Domain.Specifications.Bookings.Admin;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services.Bookings;
using CarMaintenance.Core.Service.Abstraction.Services.Notifications;
using CarMaintenance.Shared.DTOs.Bookings;
using CarMaintenance.Shared.DTOs.Bookings.Additionallssues;
using CarMaintenance.Shared.DTOs.Bookings.AvailableSlots;
using CarMaintenance.Shared.DTOs.Bookings.CreateBooking;
using CarMaintenance.Shared.DTOs.Bookings.Invoice;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace CarMaintenance.Core.Service.Services.Bookings
{
    internal class BookingService(
        IUnitOfWork _unitOfWork,
        IMapper _mapper,
        INotificationService _notificationService,
        UserManager<ApplicationUser> _userManager
    ) : IBookingService
    {
        // Working-day constants
        private const int WorkStartMinutes = 9 * 60;   // 09:00
        private const int WorkEndMinutes = 17 * 60;    // 17:00
        private const int SlotStepMinutes = 60;        // advance cursor by 1 h per slot
        private const int MinSlotWidth = 30;           // min. gap that counts as "free"
        private const int MaxDailyMinutes = 480;       // 8 h per technician per day
        private const int MaxSlotsToReturn = 6;
        private const int MaxDaysLookAhead = 7;
        private const double MinCoverage = 0.5;        // partial-match threshold

        

        #region Customer

        public async Task<Pagination<BookingDto>> GetMyBookingsAsync(BookingSpecParams specParams, string userId)
        {
            specParams.UserId = userId;
            var spec = new BookingSpecification(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);
            var specCount = new BookingWithFiltrationForCountSpecification(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(specCount);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize, count)
            { Data = data };
        }

        public async Task<BookingDetailsDto> GetBookingDetailsAsync(int id, string userId)
        {
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(id));

            if (booking == null) throw new NotFoundException(nameof(Booking), id);
            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لعرض هذا الحجز");

            var result = _mapper.Map<BookingDetailsDto>(booking);
            if (!string.IsNullOrEmpty(booking.TechnicianId))
                result.TechnicianAvailableSlots = await BuildTechnicianSlotsAsync(booking.TechnicianId);

            return result;
        }

        public async Task<BookingDto> CreateBookingAsync(CreateBookingDto dto, string userId)
        {
            // 1. Vehicle ownership
            var vehicle = await _unitOfWork.GetRepo<Vehicle, int>().GetByIdAsync(dto.VehicleId);
            if (vehicle == null || vehicle.UserId != userId)
                throw new BadRequestException("السيارة غير موجودة أو لا تخصك");

            // 2. Future date
            if (dto.ScheduledDate < DateTime.UtcNow)
                throw new BadRequestException("تاريخ الحجز يجب أن يكون في المستقبل");

            // 3. No duplicate services
            var serviceIds = dto.Services.Select(s => s.ServiceId).ToList();
            if (serviceIds.Count != serviceIds.Distinct().Count())
                throw new BadRequestException("لا يمكن إضافة نفس الخدمة أكثر من مرة.");

            // 4. No active booking on this vehicle
            var actives = await _unitOfWork.GetRepo<Booking, int>()
                .GetAllWithSpecAsync(new BookingByVehicleActiveSpecification(dto.VehicleId));
            if (actives.Any())
            {
                var ex = actives.First();
                throw new BadRequestException(
                    $"هذه السيارة لديها حجز نشط (رقم: {ex.BookingNumber}, الحالة: {ex.Status}).");
            }

            // 5. Load services
            var services = new List<Domain.Models.Data.Service>();
            foreach (var svcId in serviceIds)
            {
                var svc = await _unitOfWork.GetRepo<Domain.Models.Data.Service, int>()
                    .GetByIdAsync(svcId);
                if (svc is null) throw new BadRequestException($"الخدمة برقم {svcId} غير موجودة");
                services.Add(svc);
            }

            // 6. Persist booking
            var booking = new Booking
            {
                BookingNumber = GenerateBookingNumber(),
                UserId = userId,
                VehicleId = dto.VehicleId,
                ScheduledDate = dto.ScheduledDate.Kind == DateTimeKind.Utc? dto.ScheduledDate: dto.ScheduledDate.ToUniversalTime(),
                Description = dto.Description,
                Status = BookingStatus.Pending,
                TotalCost = services.Sum(s => s.BasePrice),
                PaymentMethod = null,                       // Set on payment, not creation
                PaymentStatus = PaymentStatus.Pending,
                TechnicianId = null
            };

            await _unitOfWork.GetRepo<Booking, int>().AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            foreach (var svcDto in dto.Services)
            {
                await _unitOfWork.GetRepo<Domain.Models.Data.BookingService, int>().AddAsync(
                    new Domain.Models.Data.BookingService
                    {
                        BookingId = booking.Id,
                        ServiceId = svcDto.ServiceId,
                        Duration = svcDto.Duration,
                        Status = BookingStatus.Pending
                    });
            }
            await _unitOfWork.SaveChangesAsync();

            // 7. Auto-assign — never throws; failure only sends admin notification
            await TryAutoAssignAsync(booking, services);

            // 8. Re-query with all nav props for the response
            var created = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(booking.Id));

            await _notificationService.SendAsync(
                userId: userId,
                title: "تم تأكيد حجزك",
                message: $"تم إنشاء الحجز رقم {booking.BookingNumber} بنجاح.",
                type: NotificationType.BookingCreated,
                actionUrl: $"/bookings/{booking.Id}");

            await NotifyAdminsAsync(
                title: $"حجز جديد #{booking.BookingNumber}",
                message: $"العميل {created!.User.DisplayName} — " +
                         $"{created.Vehicle.Brand} {created.Vehicle.Model} — " +
                         $"{booking.TotalCost} ج.م.",
                type: NotificationType.BookingCreated,
                actionUrl: $"/admin/bookings/{booking.Id}");

            var result = _mapper.Map<BookingDto>(created!);

            if (!string.IsNullOrEmpty(created.TechnicianId))
                result.TechnicianAvailableSlots =
                    await BuildTechnicianSlotsAsync(created.TechnicianId);

            return result;
        }

        public async Task<AvailableSlotsResponseDto> GetAvailableSlotsAsync(List<int> serviceIds)
        {
            if (!serviceIds.Any())
                throw new BadRequestException("يجب اختيار خدمة واحدة على الأقل");

            // Load distinct services
            var services = new List<Domain.Models.Data.Service>();
            foreach (var id in serviceIds.Distinct())
            {
                var svc = await _unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetByIdAsync(id);
                if (svc is not null) services.Add(svc);
            }

            if (!services.Any())
                throw new BadRequestException("الخدمات المحددة غير موجودة");

            var requiredSpecs = MapCategoriesToSpecs(services.Select(s => s.Category));
            var totalDuration = services.Sum(s => s.EstimatedDurationMinutes);
            var now = DateTime.UtcNow;
            var fromDate = now.Date;  // ← من AddDays(1) إلى Date فقط


            // Load ALL available technicians ONCE
            var allAvailable = (await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(new TechnicianSpecification(isAvailable: true))).ToList();

            var results = new List<TechnicianWithSlotsDto>();

            foreach (var tech in allAvailable)
            {
                var techSpecs = NormalizeTechSpecs(tech.Specialization);
                var (isFull, ratio, _, _) = ComputeCoverage(techSpecs, requiredSpecs);

                if (ratio < MinCoverage) continue;

                // Load this technician's active bookings ONCE (for both capacity + slots)
                var activeTechSpec = new BookingByTechnicianActiveSpecification(tech.Id);
                var activeTechBookings = (await _unitOfWork.GetRepo<Booking, int>() .GetAllWithSpecAsync(activeTechSpec)).ToList();

                var slots = BuildSlots(activeTechBookings, fromDate, totalDuration, now);

                if (!slots.Any()) continue;

                results.Add(new TechnicianWithSlotsDto
                {
                    TechnicianId = tech.Id,
                    TechnicianName = tech.User.DisplayName,
                    Specialization = tech.Specialization,
                    Rating = tech.Rating,
                    ExperienceYears = tech.ExperienceYears,
                    IsFullMatch = isFull,
                    AvailableSlots = slots
                });
            }

            if (!results.Any())
                throw new BadRequestException(
                    "لا توجد مواعيد متاحة لهذه الخدمات خلال الأيام القادمة. " +
                    "يرجى التواصل مع الدعم.");

            // Full-match first → then by rating descending
            return new AvailableSlotsResponseDto
            {
                ServiceIds = serviceIds,
                TotalDurationMinutes = totalDuration,
                Technicians = results
                    .OrderByDescending(t => t.IsFullMatch)
                    .ThenByDescending(t => t.Rating)
                    .ToList()
            };
        }

        public async Task CancelBookingAsync(int id, string userId)
        {
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(id));

            if (booking == null) throw new NotFoundException(nameof(Booking), id);
            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لإلغاء هذا الحجز");
            if (booking.Status == BookingStatus.Completed)
                throw new BadRequestException("لا يمكن إلغاء حجز مكتمل");
            if (booking.Status == BookingStatus.Cancelled)
                throw new BadRequestException("الحجز ملغى بالفعل");
            if (booking.Status == BookingStatus.InProgress)
                throw new BadRequestException("لا يمكن إلغاء الحجز أثناء تنفيذه");

            if (!string.IsNullOrEmpty(booking.TechnicianId) && booking.Status == BookingStatus.WaitingClientApproval)
                await TryRestoreAvailabilityAsync(booking.TechnicianId, booking.Id);

            booking.Status = BookingStatus.Cancelled;
            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(booking.AssignedTechnician?.UserId))
                await _notificationService.SendAsync(
                    userId: booking.AssignedTechnician.UserId,
                    title: "تم إلغاء الحجز",
                    message: $"العميل {booking.User.DisplayName} ألغى الحجز {booking.BookingNumber}.",
                    type: NotificationType.BookingCancelled,
                    actionUrl: $"/technician/bookings/{booking.Id}");

            await NotifyAdminsAsync(
                title: $"إلغاء حجز #{booking.BookingNumber}",
                message: $"العميل {booking.User.DisplayName} ألغى الحجز {booking.BookingNumber}.",
                type: NotificationType.BookingCancelled,
                actionUrl: $"/admin/bookings/{booking.Id}");
        }

        public async Task ApproveAdditionalIssueAsync(ApproveAdditionalIssueDto dto, string userId)
        {
            var issue = await _unitOfWork.GetRepo<AdditionalIssue, int>().GetByIdAsync(dto.IssueId);

            if (issue is null) throw new NotFoundException(nameof(AdditionalIssue), dto.IssueId);

            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(issue.BookingId));

            if (booking is null) throw new NotFoundException(nameof(Booking), issue.BookingId);
            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية للموافقة على هذه المشكلة");
            if (booking.Status != BookingStatus.WaitingClientApproval)
                throw new BadRequestException(
                    $"حالة الحجز '{booking.Status}' — يجب WaitingClientApproval.");
            if (issue.Status != AdditionalIssueStatus.Pending)
                throw new BadRequestException("تمت معالجة هذه المشكلة بالفعل.");

            issue.Status = dto.IsApproved ? AdditionalIssueStatus.Approved : AdditionalIssueStatus.Rejected;

            _unitOfWork.GetRepo<AdditionalIssue, int>().Update(issue);

            if (dto.IsApproved)
            {
                booking.TotalCost += issue.EstimatedCost;
                booking.Status = BookingStatus.InProgress;
            }
            else if (issue.IsCritical)
            {
                // Critical rejection: record acknowledgment, resume (no blocking)
                booking.TechnicianReport =
                    (booking.TechnicianReport ?? "") +
                    $"\n[تحذير {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC] " +
                    $"رفض العميل المشكلة الحرجة: \"{issue.Title}\". استلام جزئي مقبول.";
                booking.Status = BookingStatus.InProgress;

                await NotifyAdminsAsync(
                    title: $"رفض مشكلة حرجة #{booking.BookingNumber}",
                    message: $"العميل {booking.User.DisplayName} رفض «{issue.Title}». الحجز استمر.",
                    type: NotificationType.AdditionalIssueRejected,
                    actionUrl: $"/admin/bookings/{booking.Id}");

                await _notificationService.SendAsync(
                    userId: booking.UserId,
                    title: "استلام جزئي مؤكد",
                    message: $"رفضك لإصلاح «{issue.Title}» تم تسجيله. ستدفع تكلفة ما نُفِّذ فقط.",
                    type: NotificationType.AdditionalIssueRejected,
                    actionUrl: $"/bookings/{booking.Id}");
            }
            else
            {
                booking.Status = BookingStatus.InProgress;
            }

            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            if (!string.IsNullOrEmpty(booking.AssignedTechnician?.UserId))
                await _notificationService.SendAsync(
                    userId: booking.AssignedTechnician.UserId,
                    title: dto.IsApproved
                        ? "وافق العميل على التكلفة الإضافية"
                        : (issue.IsCritical
                            ? "رفض العميل المشكلة الحرجة — تابع الخدمات الأخرى"
                            : "رفض العميل التكلفة الإضافية"),
                    message: $"العميل {booking.User.DisplayName} " +
                             $"{(dto.IsApproved ? "وافق" : "رفض")} الحجز {booking.BookingNumber}.",
                    type: dto.IsApproved
                        ? NotificationType.AdditionalIssueApproved
                        : NotificationType.AdditionalIssueRejected,
                    actionUrl: $"/technician/bookings/{booking.Id}");
        }

        public async Task<InvoiceDto> GetBookingInvoiceAsync(int bookingId, string userId)
        {
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(bookingId));

            if (booking == null) throw new NotFoundException(nameof(Booking), bookingId);
            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لعرض هذه الفاتورة");
            if (booking.Status == BookingStatus.Pending)
                throw new BadRequestException("الفاتورة غير متاحة - الحجز لم يبدأ بعد");
            if (booking.Status == BookingStatus.Cancelled)
                throw new BadRequestException("الفاتورة غير متاحة - الحجز ملغى");

            decimal svcCost = booking.BookingServices.Sum(bs => bs.Service?.BasePrice ?? 0);
            decimal addCost = booking.AdditionalIssues.Where(ai => ai.IsApproved == true).Sum(ai => ai.EstimatedCost);

            return new InvoiceDto
            {
                InvoiceNumber = $"INV-{booking.BookingNumber.Replace("BK-", "")}",
                BookingNumber = booking.BookingNumber,
                ScheduledDate = booking.ScheduledDate,
                BookingStatus = booking.Status.ToString(),
                CustomerName = booking.User?.DisplayName ?? "",
                CustomerEmail = booking.User?.Email ?? "",
                CustomerPhone = booking.User?.PhoneNumber ?? "",
                VehicleBrand = booking.Vehicle?.Brand ?? "",
                VehicleModel = booking.Vehicle?.Model ?? "",
                VehicleYear = booking.Vehicle?.Year ?? 0,
                VehiclePlateNumber = booking.Vehicle?.PlateNumber ?? "",
                TechnicianName = booking.AssignedTechnician?.User?.DisplayName!,
                TechnicianSpecialization = booking.AssignedTechnician?.Specialization!,
                Services = _mapper.Map<List<InvoiceServiceItemDto>>(booking.BookingServices),
                ApprovedIssues = _mapper.Map<List<InvoiceAdditionalIssueDto>>( booking.AdditionalIssues.Where(ai => ai.IsApproved == true).ToList()),
                ServicesCost = svcCost,
                AdditionalCost = addCost,
                TotalCost = svcCost + addCost,
                PaymentMethod = booking.PaymentMethod?.ToString() ?? "غير محدد",
                PaymentStatus = booking.PaymentStatus.ToString(),
            };
        }

        #endregion

        #region Technician

        public async Task<Pagination<BookingDto>> GetMyAssignedBookingsAsync( BookingSpecParams specParams, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);
            if (technician is null) throw new ForbiddenException("لست فنياً معتمداً");

            specParams.TechnicianId = technician.Id;
            var spec = new BookingSpecification(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);
            var specCount = new BookingWithFiltrationForCountSpecification(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(specCount);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize, count)
            { Data = data };
        }

        public async Task<BookingDetailsDto> GetBookingDetailsForTechnicianAsync( int id, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>() .GetWithSpecAsync(techSpec);
            if (technician is null) throw new ForbiddenException("لست فنياً معتمداً");

            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(id));
            if (booking == null) throw new NotFoundException(nameof(Booking), id);

            if (booking.TechnicianId != technician.Id)
                throw new ForbiddenException("هذا الحجز غير معين عليك.");

            return _mapper.Map<BookingDetailsDto>(booking);
        }

        public async Task<AdditionalIssueDto> AddAdditionalIssueAsync(int bookingId, AddAdditionalIssueDto issueDto, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);
            if (technician is null) throw new ForbiddenException("لست فنياً معتمداً");

            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(bookingId));
            if (booking == null) throw new NotFoundException(nameof(Booking), bookingId);

            if (booking.TechnicianId != technician.Id)
                throw new ForbiddenException("ليس لديك صلاحية لإضافة مشاكل لهذا الحجز");

            if (booking.Status != BookingStatus.InProgress &&
                booking.Status != BookingStatus.Pending)
                throw new BadRequestException( $"الحالة '{booking.Status}' — يجب InProgress أو Pending.");

            if (booking.AdditionalIssues.Any(ai => ai.Status == AdditionalIssueStatus.Pending))
                throw new BadRequestException("يوجد مشكلة معلقة في انتظار رد العميل.");

            var issue = new AdditionalIssue
            {
                Title = issueDto.Title,
                Description = issueDto.Description,
                EstimatedCost = issueDto.EstimatedCost,
                EstimatedDurationMinutes = issueDto.EstimatedDurationMinutes,
                CreatedAt = DateTime.UtcNow,
                BookingId = bookingId,
                Status = AdditionalIssueStatus.Pending,
                IsCritical = issueDto.IsCritical,
            };

            await _unitOfWork.GetRepo<AdditionalIssue, int>().AddAsync(issue);
            booking.Status = BookingStatus.WaitingClientApproval;
            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId: booking.UserId,
                title: issueDto.IsCritical ? "مشكلة حرجة تحتاج موافقتك" : "تكلفة إضافية مطلوبة",
                message: $"{(issueDto.IsCritical ? "مشكلة حرجة" : "مشكلة إضافية")} في الحجز " +
                         $"{booking.BookingNumber}: {issueDto.Title} — {issueDto.EstimatedCost} ج.م.",
                type: NotificationType.AdditionalIssueAdded,
                actionUrl: $"/bookings/{bookingId}");

            return _mapper.Map<AdditionalIssueDto>(issue);
        }

        public async Task<BookingDto> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto statusDto, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);
            if (technician is null) throw new ForbiddenException("لست فنياً معتمداً");

            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(id));
            if (booking == null) throw new NotFoundException(nameof(Booking), id);

            if (booking.TechnicianId != technician.Id)
                throw new ForbiddenException("ليس لديك صلاحية لتحديث هذا الحجز");

            if (booking.Status == BookingStatus.WaitingClientApproval)
                throw new BadRequestException("الحجز في انتظار موافقة العميل.");

            if (!Enum.TryParse<BookingStatus>(statusDto.Status, true, out var newStatus))
                throw new BadRequestException($"حالة غير صحيحة: '{statusDto.Status}'.");

            if (newStatus is BookingStatus.WaitingClientApproval or BookingStatus.Cancelled)
                throw new BadRequestException("لا يمكن تعيين هذه الحالة يدوياً.");

            if (newStatus == BookingStatus.Completed && booking.AdditionalIssues.Any(ai => ai.Status == AdditionalIssueStatus.Pending))
                throw new BadRequestException("يوجد مشكلة إضافية معلقة — لا يمكن الإكمال.");

            var validTransition = (booking.Status, newStatus) switch
            {
                (BookingStatus.Pending, BookingStatus.InProgress) => true,
                (BookingStatus.InProgress, BookingStatus.Completed) => true,
                _ => false
            };
            if (!validTransition)
                throw new BadRequestException($"انتقال غير مسموح: '{booking.Status}' → '{newStatus}'.");

            booking.Status = newStatus;

            if (!string.IsNullOrWhiteSpace(statusDto.TechnicianReport))
                booking.TechnicianReport = statusDto.TechnicianReport;

            if (newStatus == BookingStatus.Completed)
            {
                var veh = await _unitOfWork.GetRepo<Vehicle, int>().GetByIdAsync(booking.VehicleId);
                if (veh != null)
                {
                    veh.LastMaintenanceDate = DateTime.UtcNow;
                    _unitOfWork.GetRepo<Vehicle, int>().Update(veh);
                }
            }

            _unitOfWork.GetRepo<Booking, int>().Update(booking);

            if (!string.IsNullOrEmpty(booking.TechnicianId))
            {
                if (newStatus == BookingStatus.InProgress)
                {
                    var tech = await _unitOfWork.GetRepo<Technician, string>().GetByIdAsync(booking.TechnicianId);
                    if (tech != null)
                    {
                        tech.IsAvailable = false;
                        _unitOfWork.GetRepo<Technician, string>().Update(tech);
                    }
                }
                else if (newStatus == BookingStatus.Completed)
                    await TryRestoreAvailabilityAsync(booking.TechnicianId, booking.Id);
            }

            await _unitOfWork.SaveChangesAsync();

            if (newStatus == BookingStatus.Completed)
            {
                var allVehicle = (await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(new BookingByVehicleAllSpecification(booking.VehicleId))).ToList();
                int completedCount = allVehicle.Count(b => b.Status == BookingStatus.Completed);
                int totalCount = allVehicle.Count;

                await _notificationService.SendAsync(
                    userId: booking.UserId,
                    title: totalCount > 1 ? $"تم إكمال خدمة {completedCount}/{totalCount}" : "تم إكمال الحجز",
                    message: $"انتهت صيانة سيارتك للحجز {booking.BookingNumber}. يمكنك الآن إتمام الدفع.",
                    type: NotificationType.BookingCompleted,
                    actionUrl: $"/bookings/{booking.Id}/pay");   //
            }

            if (newStatus == BookingStatus.InProgress)
                await _notificationService.SendAsync(
                    userId: booking.UserId,
                    title: "بدأ الفني العمل",
                    message: $"الفني {booking.AssignedTechnician?.User?.DisplayName} بدأ على حجزك {booking.BookingNumber}.",
                    type: NotificationType.TechnicianAssigned,
                    actionUrl: $"/bookings/{booking.Id}");

            var updated = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(id));
            return _mapper.Map<BookingDto>(updated!);
        }

        #endregion

        #region Admin

        public async Task<Pagination<BookingDto>> GetAllBookingsAsync(BookingSpecParams specParams)
        {
            var spec = new BookingSpecification(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);
            var countSpec = new BookingWithFiltrationForCountSpecification(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(countSpec);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize, count)
            { Data = data };
        }

        public async Task<Pagination<BookingDto>> GetTodayBookingsAsync(BookingSpecParams specParams)
        {
            specParams.TodayOnly = true;
            return await GetAllBookingsAsync(specParams);
        }

        public async Task<BookingDto> AssignTechnicianAsync(int bookingId)
        {
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(bookingId));
            if (booking == null) throw new NotFoundException(nameof(Booking), bookingId);
            if (!string.IsNullOrEmpty(booking.TechnicianId))
                throw new BadRequestException("الحجز معين لفني بالفعل");

            var requiredSpecs = MapCategoriesToSpecs(booking.BookingServices.Select(bs => bs.Service?.Category));
            var totalDuration = booking.BookingServices.Sum(bs => bs.Duration);

            var techId = await SelectBestTechnicianAsync(requiredSpecs, totalDuration, booking.ScheduledDate);

            if (techId == null)
            {
                await NotifyAdminsAsync(
                    title: $"حجز يحتاج تعيين يدوي #{booking.BookingNumber}",
                    message: "لا يوجد فني متاح بالتخصص المطلوب أو ضمن الطاقة اليومية.",
                    type: NotificationType.TechnicianAssigned,
                    actionUrl: $"/admin/bookings/{bookingId}");

                throw new BadRequestException("لا يوجد فني متاح حالياً");
            }

            booking.TechnicianId = techId;
            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(new BookingSpecification(bookingId));

            await _notificationService.SendAsync(
                userId: updated!.UserId,
                title: "تم تعيين فني لحجزك",
                message: $"الفني {updated.AssignedTechnician!.User.DisplayName} للحجز {updated.BookingNumber}.",
                type: NotificationType.TechnicianAssigned,
                actionUrl: $"/bookings/{bookingId}");

            await _notificationService.SendAsync(
                userId: updated.AssignedTechnician.UserId,
                title: "تم تعيينك على حجز",
                message: $"الحجز {updated.BookingNumber} — {updated.ScheduledDate:dd/MM/yyyy HH:mm}.",
                type: NotificationType.TechnicianAssigned,
                actionUrl: $"/technician/bookings/{bookingId}");

            return _mapper.Map<BookingDto>(updated);
        }

        #endregion

        #region Private Helpers

        private async Task<string?> SelectBestTechnicianAsync( List<string> requiredSpecs, int totalDuration,DateTime scheduledDate)
        {
            var allAvailable = (await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(new TechnicianSpecification(isAvailable: true))).ToList();

            var fullMatches = new List<(Technician tech, double rating)>();
            var partialMatches = new List<(Technician tech, double coverage, double rating)>();

            foreach (var tech in allAvailable)
            {
                var techSpecs = NormalizeTechSpecs(tech.Specialization);
                var (isFull, ratio, _, _) = ComputeCoverage(techSpecs, requiredSpecs);

                if (ratio < MinCoverage) continue;

                // Daily capacity check
                var usedMins = await GetUsedMinutesOnDayAsync(tech.Id, scheduledDate);
                if (MaxDailyMinutes - usedMins < totalDuration) continue;

                if (isFull)
                    fullMatches.Add((tech, (double)tech.Rating));
                else
                    partialMatches.Add((tech, ratio, (double)tech.Rating));
            }

            if (fullMatches.Any())
                return fullMatches.OrderByDescending(t => t.rating).First().tech.Id;

            if (partialMatches.Any())
            {
                var best = partialMatches
                    .OrderByDescending(t => t.coverage)
                    .ThenByDescending(t => t.rating)
                    .First();

                await NotifyAdminsAsync(
                    title: "تعيين جزئي",
                    message: $"الفني {best.tech.User?.DisplayName} يغطي {best.coverage * 100:F0}% من الخدمات فقط.",
                    type: NotificationType.TechnicianAssigned,
                    actionUrl: null);

                return best.tech.Id;
            }

            return null;
        }

        private async Task TryAutoAssignAsync(Booking booking, List<Domain.Models.Data.Service> services)
        {
            try
            {
                var requiredSpecs = MapCategoriesToSpecs(services.Select(s => s.Category));
                var totalDuration = services.Sum(s => s.EstimatedDurationMinutes);

                var techId = await SelectBestTechnicianAsync(
                    requiredSpecs, totalDuration, booking.ScheduledDate);

                if (techId == null)
                {
                    await NotifyAdminsAsync(
                        title: $"حجز يحتاج تعيين يدوي #{booking.BookingNumber}",
                        message: "لا يوجد فني متاح — يرجى التعيين يدوياً.",
                        type: NotificationType.TechnicianAssigned,
                        actionUrl: $"/admin/bookings/{booking.Id}");
                    return;
                }

                booking.TechnicianId = techId;
                _unitOfWork.GetRepo<Booking, int>().Update(booking);
                await _unitOfWork.SaveChangesAsync();

                var assigned = await _unitOfWork.GetRepo<Technician, string>()
                    .GetWithSpecAsync(new TechnicianSpecification(techId));

                if (assigned != null)
                {
                    await _notificationService.SendAsync(
                        userId: booking.UserId,
                        title: "تم تعيين فني لحجزك",
                        message: $"الفني {assigned.User.DisplayName} للحجز {booking.BookingNumber}.",
                        type: NotificationType.TechnicianAssigned,
                        actionUrl: $"/bookings/{booking.Id}");

                    await _notificationService.SendAsync(
                        userId: assigned.UserId,
                        title: "تم تعيينك على حجز",
                        message: $"الحجز {booking.BookingNumber} — {booking.ScheduledDate:dd/MM/yyyy HH:mm}.",
                        type: NotificationType.TechnicianAssigned,
                        actionUrl: $"/technician/bookings/{booking.Id}");
                }
            }
            catch
            {
                try
                {
                    await NotifyAdminsAsync(
                        title: $"خطأ في التعيين #{booking.BookingNumber}",
                        message: "حدث خطأ في التعيين التلقائي — يرجى التعيين يدوياً.",
                        type: NotificationType.TechnicianAssigned,
                        actionUrl: $"/admin/bookings/{booking.Id}");
                }
                catch { /* never break booking creation */ }
            }
        }

        private async Task TryRestoreAvailabilityAsync(string technicianId, int excludeBookingId)
        {
            var actives = await _unitOfWork.GetRepo<Booking, int>()
                .GetAllWithSpecAsync(new BookingByTechnicianActiveSpecification(technicianId));

            if (!actives.Any(b => b.Id != excludeBookingId))
            {
                var tech = await _unitOfWork.GetRepo<Technician, string>()
                    .GetByIdAsync(technicianId);
                if (tech != null)
                {
                    tech.IsAvailable = true;
                    _unitOfWork.GetRepo<Technician, string>().Update(tech);
                }
            }
        }

        private async Task<int> GetUsedMinutesOnDayAsync(string technicianId, DateTime day)
        {
            var dayBookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(new BookingStatsSpecification(technicianId, day.Date));
            return dayBookings.Sum(b => b.BookingServices.Sum(bs => bs.Duration));
        }

        private async Task<TechnicianAvailableSlotsDto> BuildTechnicianSlotsAsync(string technicianId)
        {
            var techSpec = new TechnicianSpecification(technicianId);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                return new TechnicianAvailableSlotsDto { TechnicianId = technicianId };

            var activeBookings = (await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(new BookingByTechnicianActiveSpecification(technicianId))).ToList();

            var slots = BuildSlots(
                activeBookings,
                fromDate: DateTime.UtcNow.Date.AddDays(1),
                requiredMinutes: MinSlotWidth,
                now: DateTime.UtcNow);

            return new TechnicianAvailableSlotsDto
            {
                TechnicianId = technicianId,
                TechnicianName = technician.User.DisplayName,
                AvailableSlots = slots
            };
        }

        private async Task NotifyAdminsAsync(string title, string message, NotificationType type, string? actionUrl = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
                await _notificationService.SendAsync(admin.Id, title, message, type, actionUrl);
        }

        #endregion


        #region Static helpers

        // Normalises service categories (English slugs) to distinct lowercase keys.
        // e.g. "Oil_Change", " brakes " → ["oil_change", "brakes"]
        private static List<string> MapCategoriesToSpecs(IEnumerable<string?> categories)
            => categories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();


        // Normalises a comma-separated Specialization field (English slugs only)
        // into distinct lowercase keys.
        // e.g. "Oil_Change, BRAKES, engine" → ["oil_change", "brakes", "engine"]
        private static List<string> NormalizeTechSpecs(string specialization)
            => specialization
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.ToLowerInvariant())
                .Distinct()
                .ToList();

        // Returns (isFullMatch, coverageRatio, coveredKeys, uncoveredKeys).
        // isFullMatch = true when ratio == 1.0 (all required specs covered).
        private static (bool full, double ratio, List<string> covered, List<string> uncovered) ComputeCoverage(List<string> techSpecs, List<string> required)
        {
            if (!required.Any()) return (true, 1.0, new(), new());
            var covered = required.Where(r => techSpecs.Contains(r)).ToList();
            var uncovered = required.Except(covered).ToList();
            double ratio = (double)covered.Count / required.Count;
            return (ratio >= 1.0, ratio, covered, uncovered);
        }

        private static List<AvailableSlotDto> BuildSlots(IReadOnlyList<Booking> activeBookings, DateTime fromDate, int requiredMinutes, DateTime now)
        {
            var today = now.Date;
            var result = new List<AvailableSlotDto>();

            for (int d = 0; d < MaxDaysLookAhead && result.Count < MaxSlotsToReturn; d++)
            {
                var date = fromDate.Date.AddDays(d);

                // Build booked intervals for this day (minutes from midnight)
                // End = start + max(duration, MinSlotWidth) to handle zero-duration edge case
                var intervals = activeBookings
                    .Where(b => b.ScheduledDate.Date == date)
                    .Select(b =>
                    {
                        int s = b.ScheduledDate.Hour * 60 + b.ScheduledDate.Minute;
                        int dur = b.BookingServices.Any()
                            ? b.BookingServices.Sum(bs => bs.Duration)
                            : MinSlotWidth;
                        return (Start: s, End: s + Math.Max(dur, MinSlotWidth));
                    })
                    .OrderBy(i => i.Start)
                    .ToList();

                int cursor = WorkStartMinutes;

                while (cursor + requiredMinutes <= WorkEndMinutes &&
                       result.Count < MaxSlotsToReturn)
                {
                    var slotDateTime = date.AddMinutes(cursor);

                    // Must be in the future
                    if (slotDateTime <= now)
                    {
                        cursor += SlotStepMinutes;
                        continue;
                    }

                    // Overlap check: any interval where [cursor, cursor+required) intersects [start, end)
                    var blocking = intervals
                        .Where(i => cursor < i.End && cursor + requiredMinutes > i.Start)
                        .ToList();

                    if (!blocking.Any())
                    {
                        result.Add(new AvailableSlotDto
                        {
                            SlotDateTime = slotDateTime,
                            Label = FormatSlotLabel(slotDateTime, today)
                        });
                        cursor += SlotStepMinutes;
                    }
                    else
                    {
                        // Jump past the last blocking interval end
                        cursor = blocking.Max(i => i.End);
                    }
                }
            }

            return result;
        }

        private static string FormatSlotLabel(DateTime slotUtc, DateTime todayUtc)
        {
            var cairoTz = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var slot = TimeZoneInfo.ConvertTimeFromUtc(slotUtc, cairoTz);
            var today = TimeZoneInfo.ConvertTimeFromUtc(todayUtc, cairoTz).Date;

            var day = slot.Date == today.AddDays(1) ? "غداً" :
                      slot.Date == today.AddDays(2) ? "بعد غد" :
                      slot.Date.ToString("dd/MM");
            var h12 = slot.Hour > 12 ? slot.Hour - 12 : slot.Hour == 0 ? 12 : slot.Hour;
            return $"{day} {h12}:00 {(slot.Hour < 12 ? "ص" : "م")}";
        }

        private static string GenerateBookingNumber()
            => $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        #endregion


    }
}