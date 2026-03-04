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

            return _mapper.Map<BookingDetailsDto>(booking);
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

            // 5. Generate booking number
            var bookingNumber = await GenerateBookingNumberAsync();

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

            var booking = await _unitOfWork.GetRepo<Booking, int>().GetByIdAsync(issue.BookingId);
            if (booking is null || booking.UserId != userId)
                throw new UnauthorizedException("ليس لديك صلاحية للموافقة على هذه المشكلة");

            issue.IsApproved = approveAdditionalIssue.IsApproved;

            if (approveAdditionalIssue.IsApproved)
            {
                booking.TotalCost += issue.EstimatedCost;
                _unitOfWork.GetRepo<Booking, int>().Update(booking);
            }

            _unitOfWork.GetRepo<AdditionalIssue, int>().Update(issue);
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

        public async Task<Pagination<BookingDto>> GetMyAssignedBookingsAsync(BookingSpecParams specParams, string technicianId)
        {
            specParams.TechnicianId = technicianId;
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

        public async Task<AdditionalIssueDto> AddAdditionalIssueAsync(int bookingId, AddAdditionalIssueDto issueDto, string technicianId)
        {
            var spec = new BookingSpecification(bookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            if (booking.TechnicianId != technicianId)
                throw new UnauthorizedException("ليس لديك صلاحية لإضافة مشاكل إضافية لهذا الحجز");

            var additionalIssue = new AdditionalIssue
            {
                Title = issueDto.Title,
                EstimatedCost = issueDto.EstimatedCost,
                BookingId = bookingId,
                IsApproved = false
            };

            await _unitOfWork.GetRepo<AdditionalIssue, int>().AddAsync(additionalIssue);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<AdditionalIssueDto>(additionalIssue);
        }

        public async Task<BookingDto> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto statusDto, string technicianId)
        {
            var spec = new BookingSpecification(id);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), id);

            if (booking.TechnicianId != technicianId)
                throw new UnauthorizedException("ليس لديك صلاحية لتحديث هذا الحجز");

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

        private async Task TryAutoAssignTechnicianAsync(Booking booking,List<Domain.Models.Data.Service> services)
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
            catch
            {
            }
        }

        private async Task<string> GenerateBookingNumberAsync()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(
                new BookingSpecification(new BookingSpecParams()));
            var sequence = (count + 1).ToString("D4");
            return $"BK-{date}-{sequence}";
        }

        #endregion
    }
}