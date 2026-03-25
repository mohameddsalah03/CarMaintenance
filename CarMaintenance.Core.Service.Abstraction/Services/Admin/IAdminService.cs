using CarMaintenance.Shared.DTOs.Admin;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;

namespace CarMaintenance.Core.Service.Abstraction.Services.Admin
{
    public interface IAdminService
    {
        // Dashboard statistics for the admin home screen
        Task<DashboardStatsDto> GetDashboardStatsAsync();

        // Full booking details - admin can see any booking
        Task<BookingDetailsDto> GetBookingDetailsAsync(int bookingId);
    }
}