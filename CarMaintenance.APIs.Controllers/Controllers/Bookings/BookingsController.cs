using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Bookings;
using CarMaintenance.Shared.DTOs.Bookings.Additionallssues;
using CarMaintenance.Shared.DTOs.Bookings.CreateBooking;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarMaintenance.APIs.Controllers.Controllers.Bookings
{
    public class BookingsController : BaseApiController
    {
        private readonly IServiceManager _serviceManager;

        public BookingsController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        #region Customer Endpoints

        // Create new booking
        [Authorize(Roles = "Customer")]
        [HttpPost] // POST: /api/Bookings
        public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingDto createDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _serviceManager.BookingService.CreateBookingAsync(createDto, userId!);
            return Ok(booking);
        }

        // Get my bookings
        [Authorize(Roles = "Customer")]
        [HttpGet("my-bookings")] // GET: /api/Bookings/my-bookings
        public async Task<ActionResult<Pagination<BookingDto>>> GetMyBookings([FromQuery] BookingSpecParams specParams)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _serviceManager.BookingService.GetMyBookingsAsync(specParams, userId!);
            return Ok(bookings);
        }

        // Get booking details
        [Authorize(Roles = "Customer")]
        [HttpGet("{id}")] // GET: /api/Bookings/5
        public async Task<ActionResult<BookingDetailsDto>> GetBookingDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _serviceManager.BookingService.GetBookingDetailsAsync(id, userId!);
            return Ok(booking);
        }

        // Cancel booking
        [Authorize(Roles = "Customer")]
        [HttpPatch("{id}/cancel")] // PATCH: /api/Bookings/5/cancel
        public async Task<ActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManager.BookingService.CancelBookingAsync(id, userId!);
            return Ok();
        }

        // Approve/Reject additional issue
        [Authorize(Roles = "Customer")]
        [HttpPatch("additional-issues/{issueId}/approve")] // PATCH: /api/Bookings/additional-issues/5/approve
        public async Task<ActionResult> ApproveAdditionalIssue(
            int issueId,
            [FromBody] ApproveAdditionalIssueDto approveDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManager.BookingService.ApproveAdditionalIssueAsync(approveDto, userId!);
            return Ok();
        }

        #endregion

        #region Manager Endpoints

        // Get all bookings
        [Authorize(Roles = "Admin")]
        [HttpGet("all")] // GET: /api/Bookings/all
        public async Task<ActionResult<Pagination<BookingDto>>> GetAllBookings([FromQuery] BookingSpecParams specParams)
        {
            var bookings = await _serviceManager.BookingService.GetAllBookingsAsync(specParams);
            return Ok(bookings);
        }

        //// Assign technician
        //[Authorize(Roles = "Admin")]
        //[HttpPatch("{id}/assign-technician")] // PATCH: /api/Bookings/5/assign-technician
        //public async Task<ActionResult<BookingDto>> AssignTechnician(
        //    [FromRoute] int id,
        //    [FromBody] AssignTechnicianDto assignDto)
        //{
        //    var booking = await _serviceManager.BookingService.AssignTechnicianAsync(id, assignDto);
        //    return Ok(booking);
        //}



        // Add additional issue
        //[Authorize(Roles = "Admin")]
        //[HttpPost("{id}/additional-issues")] // POST: /api/Bookings/5/additional-issues
        //public async Task<ActionResult<AdditionalIssueDto>> AddAdditionalIssue(
        //    int id,
        //    [FromBody] AddAdditionalIssueDto issueDto)
        //{   //int bookingId, AddAdditionalIssueDto issueDto , string technicianId
        //    var issue = await _serviceManager.BookingService.AddAdditionalIssueAsync(issueDto.BookingId , issueDto , issueDto.);
        //    return CreatedAtAction(nameof(GetBookingDetails), new { id }, issue);
        //}

        #endregion

        #region Technician Endpoints

        // Get my assigned bookings
        [Authorize(Roles = "Technician")]
        [HttpGet("my-assignments")] // GET: /api/Bookings/my-assignments
        public async Task<ActionResult<Pagination<BookingDto>>> GetMyAssignedBookings([FromQuery] BookingSpecParams specParams)
        {
            var technicianId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _serviceManager.BookingService.GetMyAssignedBookingsAsync(specParams, technicianId!);
            return Ok(bookings);
        }

        // Update booking status
        [Authorize(Roles = "Technician")]
        [HttpPatch("{id}/update-status")] // PATCH: /api/Bookings/5/update-status
        public async Task<ActionResult<BookingDto>> UpdateBookingStatus(
            int id,
            [FromBody] UpdateBookingStatusDto statusDto)
        {
            var technicianId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _serviceManager.BookingService.UpdateBookingStatusAsync(id, statusDto, technicianId!);
            return Ok(booking);
        }

        #endregion
    }
}