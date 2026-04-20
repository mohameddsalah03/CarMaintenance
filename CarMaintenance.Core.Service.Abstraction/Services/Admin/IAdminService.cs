using CarMaintenance.Shared.DTOs.Admin;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;

namespace CarMaintenance.Core.Service.Abstraction.Services.Admin
{
    public interface IAdminService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<BookingDetailsDto> GetBookingDetailsAsync(int bookingId);
        Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
    }
}