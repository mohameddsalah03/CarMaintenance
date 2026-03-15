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
using Microsoft.EntityFrameworkCore.Migrations;

namespace CarMaintenance.Core.Service.Services.Bookings
{
    internal class BookingService(
        IUnitOfWork _unitOfWork,
        IMapper _mapper,
        IAiTechnicianService _aiTechnicianService
    ) : IBookingService
    {
        #region Customer

        public async Task<Pagination<BookingDto>> GetMyBookingsAsync(BookingSpecParams specParams, string userId)
        {
            specParams.UserId = userId;
            var spec = new BookingSpecification(specParams.UserId, true);
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
                throw new UnauthorizedException("ليس لديك صلاحية لعرض هذا الحجز");

            var result = _mapper.Map<BookingDetailsDto>(booking);

            if (!string.IsNullOrEmpty(booking.TechnicianId))
                result.TechnicianAvailableSlots = await GetTechnicianAvailableSlotsAsync(booking.TechnicianId);

            return result;
        }

        public async Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto, string userId)
        {
            // 1. Handle vehicle
            var vehicle = await _unitOfWork.GetRepo<Vehicle, int>().GetByIdAsync(createBookingDto.VehicleId);
            if (vehicle == null || vehicle.UserId != userId)
                throw new BadRequestException("السيارة غير موجودة أو لا تخصك");

            // 2. Validate scheduled date
            if (createBookingDto.ScheduledDate < DateTime.UtcNow)
                throw new BadRequestException("تاريخ الحجز يجب أن يكون في المستقبل");

            // 3. Handle Services
            var servicesIdsInput = createBookingDto.Services.Select(s => s.ServiceId).ToList();
            var services = new List<Domain.Models.Data.Service>();

            foreach (var serviceId in servicesIdsInput)
            {
                var service = await _unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetByIdAsync(serviceId);
                if (service is null)
                    throw new BadRequestException($"الخدمة برقم {serviceId} غير موجودة");
                services.Add(service);
            }

            // 4. Calculate total cost
            decimal totalCost = services.Sum(s => s.BasePrice);

            // 5. Generate booking number ✅ Fix
            var bookingNumber = GenerateBookingNumber();

            // 6. Parse payment method
            if (!Enum.TryParse<PaymentMethod>(createBookingDto.PaymentMethod, true, out var paymentMethod))
                throw new BadRequestException("طريقة دفع غير صحيحة");

            // 7. Create Booking
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

            // 9. Auto-Assign Technician via AI
            await TryAutoAssignTechnicianAsync(booking, services);

            // 10. Get full booking details
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
                throw new UnauthorizedException("ليس لديك صلاحية لإلغاء هذا الحجز");

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
            var issue = await _unitOfWork.GetRepo<AdditionalIssue, int>()
                .GetByIdAsync(approveAdditionalIssue.IssueId);

            if (issue is null)
                throw new NotFoundException(nameof(AdditionalIssue), approveAdditionalIssue.IssueId);

            var spec = new BookingSpecification(issue.BookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking is null || booking.UserId != userId)
                throw new UnauthorizedException("ليس لديك صلاحية للموافقة على هذه المشكلة");

            // WaitingClientApproval
            if (booking.Status != BookingStatus.WaitingClientApproval)
                throw new BadRequestException("لا يوجد ما يستدعي الموافقة حالياً");

            issue.IsApproved = approveAdditionalIssue.IsApproved;
            _unitOfWork.GetRepo<AdditionalIssue, int>().Update(issue);

            if (approveAdditionalIssue.IsApproved)
            {
                booking.TotalCost += issue.EstimatedCost;
            }

            // ✅ بعد رد العميل موافقة أو رفض → ارجع للـ InProgress
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
                throw new UnauthorizedException("ليس لديك صلاحية لعرض هذه الفاتورة");

            if (booking.Status == BookingStatus.Pending)
                throw new BadRequestException("الفاتورة غير متاحة - الحجز لم يبدأ بعد");

            if (booking.Status == BookingStatus.Cancelled)
                throw new BadRequestException("الفاتورة غير متاحة - الحجز ملغى");

            decimal servicesCost = booking.BookingServices.Sum(bs => bs.Service?.BasePrice ?? 0);
            decimal additionalCost = booking.AdditionalIssues
                .Where(ai => ai.IsApproved)
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
                ApprovedIssues = _mapper.Map<List<InvoiceAdditionalIssueDto>>(booking.AdditionalIssues),
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
            var technician = await _unitOfWork.GetRepo<Technician, string>()
                .GetWithSpecAsync(techSpec);

            if (technician is null)
                throw new UnauthorizedException("لست فنياً معتمداً في النظام");

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

        public async Task<AdditionalIssueDto> AddAdditionalIssueAsync(int bookingId, AddAdditionalIssueDto issueDto, string userId)
        {
            var techSpec = new TechnicianSpecification(userId, byUserId: true);
            var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

            if (technician is null)
                throw new UnauthorizedException("لست فنياً معتمداً في النظام");

            var spec = new BookingSpecification(bookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (booking.TechnicianId != technician.Id)
                throw new UnauthorizedException("ليس لديك صلاحية لإضافة مشاكل إضافية لهذا الحجز");

            if (booking.Status != BookingStatus.InProgress)
                throw new BadRequestException("لا يمكن إضافة مشكلة إلا أثناء تنفيذ الحجز");

            var additionalIssue = new AdditionalIssue
            {
                Title = issueDto.Title,
                Description = issueDto.Description,
                EstimatedCost = issueDto.EstimatedCost,
                BookingId = bookingId,
                IsApproved = false
            };

            await _unitOfWork.GetRepo<AdditionalIssue, int>().AddAsync(additionalIssue);

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
                throw new UnauthorizedException("لست فنياً معتمداً في النظام");

            var spec = new BookingSpecification(id);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), id);

            if (booking.TechnicianId != technician.Id)
                throw new UnauthorizedException("ليس لديك صلاحية لتحديث هذا الحجز");

            // ✅ مش يعدّل لو في انتظار موافقة العميل
            if (booking.Status == BookingStatus.WaitingClientApproval)
                throw new BadRequestException("الحجز في انتظار موافقة العميل على المشكلة الإضافية");

            if (!Enum.TryParse<BookingStatus>(statusDto.Status, true, out var newStatus))
                throw new BadRequestException("حالة غير صحيحة");

            if (booking.Status == BookingStatus.Pending && newStatus == BookingStatus.InProgress)
            {
                booking.Status = BookingStatus.InProgress;
            }
            else if (booking.Status == BookingStatus.InProgress && newStatus == BookingStatus.Completed)
            {
                booking.Status = BookingStatus.Completed;
                booking.PaymentStatus = PaymentStatus.Paid;
            }
            else
            {
                throw new BadRequestException("تحديث الحالة غير صحيح");
            }

            _unitOfWork.GetRepo<Booking, int>().Update(booking);
            await _unitOfWork.SaveChangesAsync();

            var updatedSpec = new BookingSpecification(id);
            var updatedBooking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(updatedSpec);
            return _mapper.Map<BookingDto>(updatedBooking!);
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
                var technician = await _unitOfWork.GetRepo<Technician, string>()
                    .GetWithSpecAsync(techSpec);

                if (technician != null && technician.IsAvailable)
                    technicianId = technician.Id;
            }

            if (technicianId == null)
            {
                var availableSpec = new TechnicianSpecification(isAvailable: true);
                var available = await _unitOfWork.GetRepo<Technician, string>()
                    .GetAllWithSpecAsync(availableSpec);
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

        private async Task TryAutoAssignTechnicianAsync(Booking booking, List<Domain.Models.Data.Service> services)
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

                Console.WriteLine($" AI Result: {aiResult?.RecommendedTechnicianId ?? "NULL"}");

                string? technicianId = null;

                if (aiResult?.RecommendedTechnicianId != null)
                {
                    var techSpec = new TechnicianSpecification(aiResult.RecommendedTechnicianId);
                    var technician = await _unitOfWork.GetRepo<Technician, string>().GetWithSpecAsync(techSpec);

                    Console.WriteLine($" Technician Found: {technician?.Id ?? "NULL"}, IsAvailable: {technician?.IsAvailable}");

                    if (technician != null && technician.IsAvailable)
                        technicianId = technician.Id;
                }

                if (technicianId == null)
                {
                    Console.WriteLine(" AI failed or unavailable → trying fallback");
                    var availableSpec = new TechnicianSpecification(isAvailable: true);
                    var available = await _unitOfWork.GetRepo<Technician, string>().GetAllWithSpecAsync(availableSpec);
                    var fallback = available.OrderByDescending(t => t.Rating).FirstOrDefault();
                    Console.WriteLine($" Fallback Technician: {fallback?.Id ?? "NONE"}");
                    technicianId = fallback?.Id;
                }

                if (technicianId != null)
                {
                    booking.TechnicianId = technicianId;
                    _unitOfWork.GetRepo<Booking, int>().Update(booking);
                    await _unitOfWork.SaveChangesAsync();
                    Console.WriteLine($"Assigned Technician: {technicianId}");
                }
                else
                {
                    Console.WriteLine(" No technician available at all");
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
            // → "BK-20260308-A3F9C2"
        }

        private async Task<TechnicianAvailableSlotsDto> GetTechnicianAvailableSlotsAsync(string technicianId)
        {
            var techSpec = new TechnicianSpecification(technicianId);
            var technician = await _unitOfWork.GetRepo<Technician, string>()
                .GetWithSpecAsync(techSpec);

            if (technician is null)
                return new TechnicianAvailableSlotsDto { TechnicianId = technicianId };

            var bookingSpec = new BookingByTechnicianActiveSpecification(technicianId);
            var activeBookings = (await _unitOfWork.GetRepo<Booking, int>()
                .GetAllWithSpecAsync(bookingSpec)).ToList();

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