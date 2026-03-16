using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Core.Service.Abstraction.Services.Bookings;
using CarMaintenance.Shared.DTOs.AI.Request;
using CarMaintenance.Shared.DTOs.Bookings;
using CarMaintenance.Shared.DTOs.Bookings.Additionallssues;
using CarMaintenance.Shared.DTOs.Bookings.CreateBooking;
using CarMaintenance.Shared.DTOs.Bookings.Invoice;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.Exceptions;

namespace CarMaintenance.Core.Service.Services.Bookings
{
    internal class BookingService(
        IUnitOfWork _unitOfWork,
        IMapper _mapper,
        IAiTechnicianService _aiTechnicianService
    ) : IBookingService
    {
        #region Customer

        public async Task<Pagination<BookingDto>> GetMyBookingsAsync(
            BookingSpecParams specParams, string userId)
        {
            specParams.UserId = userId;
            var spec = new BookingSpecification(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);

            var specCount = new BookingWithFiltrationForCountSpecification(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(specCount);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize, count)
            {
                Data = data
            };
        }

        public async Task<BookingDetailsDto> GetBookingDetailsAsync(int id, string userId)
        {
            var spec = new BookingSpecification(id);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), id);

            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لعرض هذا الحجز");

            var result = _mapper.Map<BookingDetailsDto>(booking);

            if (!string.IsNullOrEmpty(booking.TechnicianId))
                result.TechnicianAvailableSlots =
                    await GetTechnicianAvailableSlotsAsync(booking.TechnicianId);

            return result;
        }

        public async Task<BookingDto> CreateBookingAsync(
            CreateBookingDto createBookingDto, string userId)
        {
            // 1. Validate vehicle ownership
            var vehicle = await _unitOfWork.GetRepo<Vehicle, int>()
                .GetByIdAsync(createBookingDto.VehicleId);

            if (vehicle == null || vehicle.UserId != userId)
                throw new BadRequestException("السيارة غير موجودة أو لا تخصك");

            // 2. Validate scheduled date
            if (createBookingDto.ScheduledDate < DateTime.UtcNow)
                throw new BadRequestException("تاريخ الحجز يجب أن يكون في المستقبل");

            // Validate no duplicate service IDs in the request
            var requestedServiceIds = createBookingDto.Services.Select(s => s.ServiceId).ToList();

            if (requestedServiceIds.Count != requestedServiceIds.Distinct().Count())
                throw new BadRequestException("لا يمكن إضافة نفس الخدمة أكثر من مرة في نفس الحجز. " + "يرجى مراجعة قائمة الخدمات المختارة.");

            //Validate no active booking exists for this vehicle
            var activeBookingSpec = new BookingByVehicleActiveSpecification(createBookingDto.VehicleId);

            var activeBookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(activeBookingSpec);

            if (activeBookings.Any())
            {
                var existingBooking = activeBookings.First();
                throw new BadRequestException(
                    $"هذه السيارة لديها حجز نشط بالفعل " +
                    $"(رقم الحجز: {existingBooking.BookingNumber}, " +
                    $"الحالة: {existingBooking.Status}). " +
                    "لا يمكن إنشاء حجز جديد حتى يكتمل أو يُلغى الحجز الحالي.");
            }

            // 3. Validate and fetch services
            var services = new List<Domain.Models.Data.Service>();

            foreach (var serviceId in requestedServiceIds)
            {
                var service = await _unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetByIdAsync(serviceId);

                if (service is null)
                    throw new BadRequestException($"الخدمة برقم {serviceId} غير موجودة");

                services.Add(service);
            }

            // 4. Calculate total cost
            decimal totalCost = services.Sum(s => s.BasePrice);

            // 5. Generate booking number
            var bookingNumber = GenerateBookingNumber();

            // 6. Parse payment method
            if (!Enum.TryParse<PaymentMethod>(createBookingDto.PaymentMethod, true,out var paymentMethod))
                throw new BadRequestException(
                    $"طريقة دفع غير صحيحة: '{createBookingDto.PaymentMethod}'. " +
                    "القيم المقبولة: Cash, CreditCard");

            // 7. Create Booking entity
            var booking = new Booking
            {
                BookingNumber = bookingNumber,
                UserId = userId,
                VehicleId = createBookingDto.VehicleId,
                ScheduledDate = createBookingDto.ScheduledDate,
                Description = createBookingDto.Description,
                Status = BookingStatus.Pending,
                TotalCost = totalCost,
                PaymentMethod = paymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                TechnicianId = null
            };

            await _unitOfWork.GetRepo<Booking, int>().AddAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            // 8. Add booking services
            foreach (var serviceDto in createBookingDto.Services)
            {
                var bookingService = new Domain.Models.Data.BookingService
                {
                    BookingId = booking.Id,
                    ServiceId = serviceDto.ServiceId,
                    Duration = serviceDto.Duration,
                    Status = BookingStatus.Pending
                };
                await _unitOfWork.GetRepo<Domain.Models.Data.BookingService, int>().AddAsync(bookingService);
            }

            await _unitOfWork.SaveChangesAsync();

            // 9. Auto-assign technician via AI
            await TryAutoAssignTechnicianAsync(booking, services);

            // 10. Return full booking details
            var spec = new BookingSpecification(booking.Id);
            var createdBooking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            return _mapper.Map<BookingDto>(createdBooking!);
        }

        public async Task CancelBookingAsync(int id, string userId)
        {
            var spec = new BookingSpecification(id);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), id);

            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لإلغاء هذا الحجز");

            if (booking.Status == BookingStatus.Completed)
                throw new BadRequestException("لا يمكن إلغاء حجز مكتمل");

            if (booking.Status == BookingStatus.Cancelled)
                throw new BadRequestException("الحجز ملغى بالفعل");

            booking.Status = BookingStatus.Cancelled;
            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ApproveAdditionalIssueAsync(ApproveAdditionalIssueDto approveAdditionalIssue, string userId)
        {
            var issue = await _unitOfWork.GetRepo<AdditionalIssue, int>().GetByIdAsync(approveAdditionalIssue.IssueId);

            if (issue is null)
                throw new NotFoundException(nameof(AdditionalIssue), approveAdditionalIssue.IssueId);

            var spec = new BookingSpecification(issue.BookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking is null)
                throw new NotFoundException(nameof(Booking), issue.BookingId);

            if (booking.UserId != userId)
                throw new ForbiddenException( "ليس لديك صلاحية للموافقة على هذه المشكلة");

            if (booking.Status != BookingStatus.WaitingClientApproval)
                throw new BadRequestException(
                    $"لا يمكن الموافقة على هذه المشكلة. " +
                    $"حالة الحجز الحالية: '{booking.Status}'. " +
                    "يجب أن يكون الحجز في حالة WaitingClientApproval.");

            if (issue.IsApproved)
                throw new BadRequestException("تمت الموافقة على هذه المشكلة بالفعل.");

            issue.IsApproved = approveAdditionalIssue.IsApproved;
            _unitOfWork.GetRepo<AdditionalIssue, int>().Update(issue);

            if (approveAdditionalIssue.IsApproved)
                booking.TotalCost += issue.EstimatedCost;

            // Return to InProgress regardless of approve or reject
            booking.Status = BookingStatus.InProgress;
            _unitOfWork.GetRepo<Booking, int>().Update(booking);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<InvoiceDto> GetBookingInvoiceAsync(int bookingId, string userId)
        {
            var spec = new BookingSpecification(bookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (booking.UserId != userId)
                throw new ForbiddenException("ليس لديك صلاحية لعرض هذه الفاتورة");

            if (booking.Status == BookingStatus.Pending)
                throw new BadRequestException("الفاتورة غير متاحة - الحجز لم يبدأ بعد");

            if (booking.Status == BookingStatus.Cancelled)
                throw new BadRequestException("الفاتورة غير متاحة - الحجز ملغى");

            decimal servicesCost = booking.BookingServices.Sum(bs => bs.Service?.BasePrice ?? 0);

            decimal additionalCost = booking.AdditionalIssues.Where(ai => ai.IsApproved)
                                                              .Sum(ai => ai.EstimatedCost);

            return new InvoiceDto
            {
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
                ApprovedIssues = _mapper.Map<List<InvoiceAdditionalIssueDto>>(booking.AdditionalIssues.Where(ai => ai.IsApproved).ToList()),
                ServicesCost = servicesCost,
                AdditionalCost = additionalCost,
                TotalCost = booking.TotalCost,
                PaymentMethod = booking.PaymentMethod.ToString(),
                PaymentStatus = booking.PaymentStatus.ToString(),
            };
        }

        #endregion

        #region Technician

        public async Task<Pagination<BookingDto>> GetMyAssignedBookingsAsync(BookingSpecParams specParams, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                throw new ForbiddenException("لست فنياً معتمداً في النظام");

            specParams.TechnicianId = technician.Id;
            var spec = new BookingSpecification(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);

            var specCount = new BookingWithFiltrationForCountSpecification(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(specCount);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize, count)
            {
                Data = data
            };
        }

        // ADDED - Technician can view full details of their assigned booking
        public async Task<BookingDetailsDto> GetBookingDetailsForTechnicianAsync(int id, string userId)
        {
            // Verify caller is a valid technician
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                throw new ForbiddenException("لست فنياً معتمداً في النظام");

            var spec = new BookingSpecification(id);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), id);

            // Technician can only view bookings assigned to them
            if (booking.TechnicianId != technician.Id)
                throw new ForbiddenException( "ليس لديك صلاحية لعرض هذا الحجز. هذا الحجز غير معين عليك.");

            return _mapper.Map<BookingDetailsDto>(booking);
        }

        public async Task<AdditionalIssueDto> AddAdditionalIssueAsync(int bookingId, AddAdditionalIssueDto issueDto, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                throw new ForbiddenException("لست فنياً معتمداً في النظام");

            var spec = new BookingSpecification(bookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (booking.TechnicianId != technician.Id)
                throw new ForbiddenException( "ليس لديك صلاحية لإضافة مشاكل إضافية لهذا الحجز");

            if (booking.Status != BookingStatus.InProgress)
                throw new BadRequestException(
                    $"لا يمكن إضافة مشكلة إضافية. " +
                    $"حالة الحجز الحالية: '{booking.Status}'. " +
                    "يجب أن يكون الحجز في حالة InProgress.");

            // Prevent adding a new issue while another is still pending approval
            var hasPendingIssue = booking.AdditionalIssues.Any(ai => !ai.IsApproved);

            if (hasPendingIssue)
                throw new BadRequestException("يوجد مشكلة إضافية في انتظار موافقة العميل. " +"انتظر رد العميل قبل إضافة مشكلة جديدة.");

            var additionalIssue = new AdditionalIssue
            {
                Title = issueDto.Title,
                Description = issueDto.Description,
                EstimatedCost = issueDto.EstimatedCost,
                EstimatedDurationMinutes = issueDto.EstimatedDurationMinutes,
                CreatedAt = DateTime.UtcNow,
                BookingId = bookingId,
                IsApproved = false
            };

            await _unitOfWork.GetRepo<AdditionalIssue, int>().AddAsync(additionalIssue);

            // System automatically sets status to WaitingClientApproval
            booking.Status = BookingStatus.WaitingClientApproval;
            _unitOfWork.GetRepo<Booking, int>().Update(booking);

            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AdditionalIssueDto>(additionalIssue);
        }

        public async Task<BookingDto> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto statusDto, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                throw new ForbiddenException("لست فنياً معتمداً في النظام");

            var spec = new BookingSpecification(id);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), id);

            if (booking.TechnicianId != technician.Id)
                throw new ForbiddenException("ليس لديك صلاحية لتحديث هذا الحجز");

            // Cannot change status while waiting for customer approval
            if (booking.Status == BookingStatus.WaitingClientApproval)
                throw new BadRequestException("الحجز في انتظار موافقة العميل على المشكلة الإضافية. " +"لا يمكن تغيير الحالة حتى يرد العميل.");

            if (!Enum.TryParse<BookingStatus>(statusDto.Status, true, out var newStatus))
                throw new BadRequestException( $"حالة غير صحيحة: '{statusDto.Status}'. " +"القيم المسموحة: InProgress, Completed");

            // Prevent technician from manually setting system-controlled statuses
            if (newStatus == BookingStatus.WaitingClientApproval)
                throw new BadRequestException( "لا يمكن تعيين هذه الحالة يدوياً. " +"تُضبط تلقائياً عند إضافة مشكلة إضافية.");

            if (newStatus == BookingStatus.Cancelled)
                throw new BadRequestException( "لا يمكن للفني إلغاء الحجز.");

            // Validate allowed transitions
            var isValidTransition = (booking.Status, newStatus) switch
            {
                (BookingStatus.Pending, BookingStatus.InProgress) => true,
                (BookingStatus.InProgress, BookingStatus.Completed) => true,
                _ => false
            };

            if (!isValidTransition)
                throw new BadRequestException(
                    $"لا يمكن الانتقال من '{booking.Status}' إلى '{newStatus}'. " +
                    "الانتقالات المسموحة: Pending to InProgress, InProgress to Completed");

            booking.Status = newStatus;

            if (newStatus == BookingStatus.Completed)
            {
                booking.PaymentStatus = PaymentStatus.Paid;

                // Auto-update vehicle last maintenance date
                var vehicle = await _unitOfWork.GetRepo<Vehicle, int>().GetByIdAsync(booking.VehicleId);

                if (vehicle != null)
                {
                    vehicle.LastMaintenanceDate = DateTime.UtcNow;
                    _unitOfWork.GetRepo<Vehicle, int>().Update(vehicle);
                }
            }

            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            var updatedSpec = new BookingSpecification(id);
            var updatedBooking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(updatedSpec);

            return _mapper.Map<BookingDto>(updatedBooking!);
        }

        #endregion

        #region Admin

        public async Task<Pagination<BookingDto>> GetAllBookingsAsync(
            BookingSpecParams specParams)
        {
            var spec = new BookingSpecification(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);

            var countSpec = new BookingWithFiltrationForCountSpecification(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(countSpec);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize, count)
            {
                Data = data
            };
        }

        public async Task<BookingDto> AssignTechnicianAsync(int bookingId)
        {
            var spec = new BookingSpecification(bookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (!string.IsNullOrEmpty(booking.TechnicianId))
                throw new BadRequestException("الحجز معين لفني بالفعل");

            var aiRequest = new AiAssignmentRequestDto
            {
                BookingId = bookingId,
                ScheduledDate = booking.ScheduledDate,
                Priority = "normal",
                Services = booking.BookingServices.Select(bs => new AiServiceInfoDto
                {
                    ServiceId = bs.ServiceId,
                    ServiceName = bs.Service?.Name ?? "",
                    Category = bs.Service?.Category ?? ""
                }).ToList()
            };

            var aiResult = await _aiTechnicianService.GetTechnicianRecommendationAsync(aiRequest);
            string? technicianId = null;

            if (aiResult?.RecommendedTechnicianId != null)
            {
                var techSpec = new TechnicianSpecification(aiResult.RecommendedTechnicianId);
                var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

                if (technician != null && technician.IsAvailable)
                    technicianId = technician.Id;
            }

            if (technicianId == null)
            {
                var availableSpec = new TechnicianSpecification(isAvailable: true);
                var available = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(availableSpec);
                technicianId = available.OrderByDescending(t => t.Rating).FirstOrDefault()?.Id;
            }

            if (technicianId == null)
                throw new BadRequestException("لا يوجد فنيين متاحين حالياً");

            booking.TechnicianId = technicianId;
            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            var updatedSpec = new BookingSpecification(bookingId);
            var updatedBooking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(updatedSpec);

            return _mapper.Map<BookingDto>(updatedBooking!);
        }

        #endregion

        #region Private Helpers

        private async Task TryAutoAssignTechnicianAsync(
            Booking booking, List<Domain.Models.Data.Service> services)
        {
            try
            {
                var aiRequest = new AiAssignmentRequestDto
                {
                    BookingId = booking.Id,
                    ScheduledDate = booking.ScheduledDate,
                    Priority = "normal",
                    Services = services.Select(s => new AiServiceInfoDto
                    {
                        ServiceId = s.Id,
                        ServiceName = s.Name,
                        Category = s.Category
                    }).ToList()
                };

                var aiResult = await _aiTechnicianService.GetTechnicianRecommendationAsync(aiRequest);

                string? technicianId = null;

                if (aiResult?.RecommendedTechnicianId != null)
                {
                    var techSpec = new TechnicianSpecification(aiResult.RecommendedTechnicianId);
                    var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

                    if (technician != null && technician.IsAvailable)
                        technicianId = technician.Id;
                }

                if (technicianId == null)
                {
                    var availableSpec = new TechnicianSpecification(isAvailable: true);
                    var available = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(availableSpec);
                    technicianId = available.OrderByDescending(t => t.Rating).FirstOrDefault()?.Id;
                }

                if (technicianId != null)
                {
                    booking.TechnicianId = technicianId;
                    _unitOfWork.GetRepo<Booking, int>().Update(booking);
                    await _unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Assignment Error: {ex.Message}");
            }
        }

        private static string GenerateBookingNumber()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var unique = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"BK-{date}-{unique}";
        }

        private async Task<TechnicianAvailableSlotsDto> GetTechnicianAvailableSlotsAsync(
            string technicianId)
        {
            var techSpec = new TechnicianSpecification(technicianId);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                return new TechnicianAvailableSlotsDto { TechnicianId = technicianId };

            var bookingSpec = new BookingByTechnicianActiveSpecification(technicianId);
            var activeBookings = (await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(bookingSpec)).ToList();

            var bookedDates = activeBookings.Select(b => b.ScheduledDate).ToList();
            var workingHours = new[] { 9, 11, 13, 15, 17 };
            var availableSlots = new List<AvailableSlotDto>();
            var today = DateTime.UtcNow.Date;

            for (int day = 1; day <= 3 && availableSlots.Count < 6; day++)
            {
                var date = today.AddDays(day);

                foreach (var hour in workingHours)
                {
                    var slot = date.AddHours(hour);
                    bool isBooked = bookedDates.Any(b => Math.Abs((b - slot).TotalHours) < 2);

                    if (!isBooked)
                    {
                        availableSlots.Add(new AvailableSlotDto
                        {
                            SlotDateTime = slot,
                            Label = FormatSlotLabel(slot, today)
                        });
                    }

                    if (availableSlots.Count >= 6) break;
                }
            }

            return new TechnicianAvailableSlotsDto
            {
                TechnicianId = technicianId,
                TechnicianName = technician.User.DisplayName,
                AvailableSlots = availableSlots
            };
        }

        private static string FormatSlotLabel(DateTime slot, DateTime today)
        {
            var dayLabel = slot.Date == today.AddDays(1) ? "غداً" :
                           slot.Date == today.AddDays(2) ? "بعد غد" :
                           slot.Date.ToString("dd/MM");

            var hour12 = slot.Hour > 12 ? slot.Hour - 12 :
                         slot.Hour == 0 ? 12 : slot.Hour;
            var period = slot.Hour < 12 ? "ص" : "م";

            return $"{dayLabel} {hour12}:00 {period}";
        }

        #endregion
    }
}