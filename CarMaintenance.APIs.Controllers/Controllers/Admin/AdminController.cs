using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Admin;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMaintenance.APIs.Controllers.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminController(IServiceManager _serviceManager) : BaseApiController
    {
        // GET: /api/admin/dashboard-stats
        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
            => Ok(await _serviceManager.AdminService.GetDashboardStatsAsync());

        // GET: /api/admin/bookings/{id}
        [HttpGet("bookings/{id:int}")]
        public async Task<ActionResult<BookingDetailsDto>> GetBookingDetails(int id)
            => Ok(await _serviceManager.AdminService.GetBookingDetailsAsync(id));

        // GET /api/admin/customers
        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllCustomers()
            => Ok(await _serviceManager.AdminService.GetAllCustomersAsync());

    }
}