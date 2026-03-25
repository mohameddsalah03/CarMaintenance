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


            // All bookings (lightweight - no includes needed for counting)
            var allBookingsSpec = new BookingStatsSpecification();
            var allBookings = (await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(allBookingsSpec)).ToList();

            // Today's bookings
            var todaySpec = new BookingStatsSpecification(today);
            var todayCount = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(todaySpec);

            // Count by each status using spec
            var pendingCount = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(new BookingStatsSpecification(BookingStatus.Pending));

            var inProgressCount = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(new BookingStatsSpecification(BookingStatus.InProgress));

            var waitingCount = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(new BookingStatsSpecification(BookingStatus.WaitingClientApproval));

            var completedCount = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(new BookingStatsSpecification(BookingStatus.Completed));

            var cancelledCount = await _unitOfWork.GetRepo<Booking, int>().GetCountAsync(new BookingStatsSpecification(BookingStatus.Cancelled));

            // Technicians
            var allTechsSpec = new TechnicianSpecification();
            var totalTechs = await _unitOfWork.GetRepo<Technician, string>().GetCountAsync(allTechsSpec);

            var availableTechsSpec = new TechnicianSpecification(isAvailable: true);
            var availableTechs = await _unitOfWork.GetRepo<Technician, string>().GetCountAsync(availableTechsSpec);

            // Customers
            var customers = await _userManager.GetUsersInRoleAsync("Customer");

            // Reviews for average rating
            var allReviews = (await _unitOfWork.GetRepo<Review, int>().GetAllAsync()).ToList();

            // Revenue using specs
            var completedBookings = (await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(BookingStatsSpecification.ForRevenue())).ToList();

            var todayRevenue = (await _unitOfWork.GetRepo<Booking, int>().GetAllWithSpecAsync(BookingStatsSpecification.ForTodayRevenue(today))).Sum(b => b.TotalCost);

            return new DashboardStatsDto
            {
                TotalBookings = allBookings.Count,
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
                TotalRevenue = completedBookings.Sum(b => b.TotalCost),
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
    }
}