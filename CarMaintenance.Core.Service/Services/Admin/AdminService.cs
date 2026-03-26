using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;
using CarMaintenance.Core.Domain.Specifications.Bookings;
using CarMaintenance.Core.Domain.Specifications.Bookings.Admin;
using CarMaintenance.Core.Domain.Specifications.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services.Admin;
using CarMaintenance.Shared.DTOs.Admin;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;
using CarMaintenance.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace CarMaintenance.Core.Service.Services.Admin
{
    public class AdminService(
        IUnitOfWork _unitOfWork,
        IMapper _mapper,
        UserManager<ApplicationUser> _userManager
    ) : IAdminService
    {
        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;

            //  Use GetCountAsync — SQL COUNT(*) — no memory loading
            var totalBookings = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(new BookingStatsSpecification());
            var todayCount = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(new BookingStatsSpecification(today));
            var pendingCount = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(new BookingStatsSpecification(BookingStatus.Pending));
            var inProgressCount = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(new BookingStatsSpecification(BookingStatus.InProgress));
            var waitingCount = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(new BookingStatsSpecification(BookingStatus.WaitingClientApproval));
            var completedCount = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(new BookingStatsSpecification(BookingStatus.Completed));
            var cancelledCount = await _unitOfWork.GetRepo<Booking, int>()
                .GetCountAsync(new BookingStatsSpecification(BookingStatus.Cancelled));

            // Technicians
            var totalTechs = await _unitOfWork.GetRepo<Technician, string>()
                .GetCountAsync(new TechnicianSpecification());
            var availableTechs = await _unitOfWork.GetRepo<Technician, string>()
                .GetCountAsync(new TechnicianSpecification(isAvailable: true));

            // Customers
            var customers = await _userManager.GetUsersInRoleAsync("Customer");

            //  SQL-level SUM via GetSumAsync — no loading completed bookings to memory
            var totalRevenue = await _unitOfWork.GetRepo<Booking, int>()
                .GetSumAsync(BookingStatsSpecification.ForRevenue(), b => b.TotalCost);

            var todayRevenue = await _unitOfWork.GetRepo<Booking, int>()
                .GetSumAsync(BookingStatsSpecification.ForTodayRevenue(today), b => b.TotalCost);

            // Reviews — still need to load for average (no generic AverageAsync in spec yet)
            var allReviews = (await _unitOfWork.GetRepo<Review, int>().GetAllAsync()).ToList();

            return new DashboardStatsDto
            {
                TotalBookings = totalBookings,
                PendingBookings = pendingCount,
                InProgressBookings = inProgressCount,
                WaitingApprovalBookings = waitingCount,
                CompletedBookings = completedCount,
                CancelledBookings = cancelledCount,
                TodayBookings = todayCount,
                TotalCustomers = customers.Count,
                TotalTechnicians = totalTechs,
                AvailableTechnicians = availableTechs,
                AverageRating = allReviews.Any()
                    ? Math.Round((decimal)allReviews.Average(r => r.TechnicianRating), 2)
                    : 0,
                TotalReviews = allReviews.Count,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue
            };
        }
        public async Task<BookingDetailsDto> GetBookingDetailsAsync(int bookingId)
        {
            var spec = new BookingSpecification(bookingId);
            var booking = await _unitOfWork.GetRepo<Booking, int>().GetWithSpecAsync(spec);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), bookingId);

            return _mapper.Map<BookingDetailsDto>(booking);
        }

        // List All Customers 
        public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync()
        {
            var customers = await _userManager.GetUsersInRoleAsync("Customer");

            return customers.Select(u => new CustomerDto
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber ?? "",
                UserName = u.UserName ?? ""
            }).OrderBy(c => c.DisplayName);
        }
    }
}