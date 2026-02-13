using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Domain.Specifications.Vehicles;
using CarMaintenance.Core.Service.Abstraction.Services.Bookings;
using CarMaintenance.Shared.DTOs.Bookings;
using CarMaintenance.Shared.DTOs.Bookings.Additionallssues;
using CarMaintenance.Shared.DTOs.Bookings.CreateBooking;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Vehicles;
using CarMaintenance.Shared.Exceptions;
using BookingService =  CarMaintenance.Core.Domain.Models.Data.BookingService;
namespace CarMaintenance.Core.Service.Services.Bookings
{
    internal class BookingService(
        IUnitOfWork _unitOfWork ,
        IMapper _mapper
        ) : IBookingService
    {

        #region Customer
        public async Task<Pagination<BookingDto>> GetMyBookingsAsync(BookingSpecParams specParams, string userId)
        {
            //var existUser = new BookingSpecifications(userId);
            specParams.UserId = userId;
            var spec = new BookingSpecifications(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking,int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);

            var specCount = new BookingWithFiltrationForCountSpecifications(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(specCount);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize ,count)
            {
                Data = data 
            };

        }
        public async Task<BookingDetailsDto> GetBookingDetailsAsync(int id, string userId)
        {
            var spec = new BookingSpecifications(id);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);
            if (booking == null)
                throw new NotFoundException(nameof(booking) , id);

            if(booking.UserId != userId)
                throw new UnauthorizedException("ليس لديك صلاحية لعرض هذا الحجز");

            var data = _mapper.Map<BookingDetailsDto>(booking);
            return data;
        }
        
        public async Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto, string userId)
        {
            /// var specVehicle = new VehicleSpecifications(userId);
            /// var vehicle = await _unitOfWork.GetRepo<Vehicle, int>().GetAllWithSpecAsync(specVehicle);
            /// if (vehicle.Select(e=>e.Id) == createBookingDto.VehicleId) 
            
            // 1.handle vehicle
            var vehicle = await _unitOfWork.GetRepo<Vehicle, int>().GetByIdAsync(createBookingDto.VehicleId);
            if (vehicle == null || createBookingDto.VehicleId != vehicle.Id)
                throw new BadRequestException("السيارة غير موجودة أو لا تخصك");

            // 2.Validate scheduled date
            if (createBookingDto.ScheduledDate < DateTime.UtcNow)
                throw new BadRequestException("تاريخ الحجز يجب أن يكون في المستقبل");

            // 3.handle Service
            var servicesIdsInput = createBookingDto.Services.Select(s => s.ServiceId).ToList();
            var services = new List<Domain.Models.Data.Service>();

            foreach (var serviceId in servicesIdsInput)
            {
                var service = await _unitOfWork.GetRepo<Domain.Models.Data.Service, int>().GetByIdAsync(serviceId);
                if (service is null)
                    throw new BadRequestException($"الخدمة برقم {serviceId} غير موجودة");
                services.Add(service); // اتضفات السيرفس ف البوكينج؟
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
                PaymentStatus = PaymentStatus.Pending
            };

            await _unitOfWork.GetRepo<Booking,int>().AddAsync(booking);
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

                //await _unitOfWork.GetRepo<Domain.Models.Data.BookingService, (int,int))>().AddAsync(bookingService);
            }

            await _unitOfWork.SaveChangesAsync();

            // 9. Get full booking details
            var spec = new BookingSpecifications(booking.Id);
            var createdBooking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            return _mapper.Map<BookingDto>(createdBooking!);

        }


        public Task ApproveAdditionalIssueAsync(ApproveAdditionalIssueDto approveAdditionalIssue, string userId)
        {
            throw new NotImplementedException();
        }
        public Task CancelBookingAsync(int id, string userId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Technician
        public async Task<Pagination<BookingDto>> GetMyAssignedBookingsAsync(BookingSpecParams specParams, string technicianId)
        {
            specParams.TechnicianId = technicianId;
            var spec = new BookingSpecifications(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking,int>().GetAllWithSpecAsync(spec);

            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);  

            var specCount = new BookingWithFiltrationForCountSpecifications(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(spec);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize ,count)
            {
                Data = data
            };

        }
        public Task<AdditionalIssueDto> AddAdditionalIssueAsync(int bookingId, AddAdditionalIssueDto issueDto)
        {
            throw new NotImplementedException();
        }
        public Task<BookingDto> UpdateBookingStatusAsync(int id, UpdateBookingStatusDto statusDto, string technicianId)
        {
            throw new NotImplementedException();
        }


        #endregion
        
        #region Admin
        public async Task<Pagination<BookingDto>> GetAllBookingsAsync(BookingSpecParams specParams)
        {
            var spec = new BookingSpecifications(specParams);
            var bookings = await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(spec);
            var data = _mapper.Map<IEnumerable<BookingDto>>(bookings);

            var countSpec = new BookingWithFiltrationForCountSpecifications(specParams);
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(countSpec);

            return new Pagination<BookingDto>(specParams.PageIndex, specParams.PageSize, count)
            {
                Data = data
            };
        }
        public Task<BookingDto> AssignTechnicianAsync(int id, AssignTechnicianDto assignDto)
        {
            throw new NotImplementedException();
        }


        #endregion

        #region Helper Methods

        private async Task<string> GenerateBookingNumberAsync()
        {
            // Format: BK-YYYYMMDD-XXXX
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            var count = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(
                new BookingSpecifications(new BookingSpecParams()));

            var sequence = (count + 1).ToString("D4");
            return $"BK-{date}-{sequence}";
        }

        #endregion





    }
}
