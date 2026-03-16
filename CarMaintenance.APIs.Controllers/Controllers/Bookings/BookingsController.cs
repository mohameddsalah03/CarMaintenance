using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Bookings;
using CarMaintenance.Shared.DTOs.Bookings.Additionallssues;
using CarMaintenance.Shared.DTOs.Bookings.CreateBooking;
using CarMaintenance.Shared.DTOs.Bookings.Invoice;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto;
using CarMaintenance.Shared.DTOs.Bookings.ReturnDto.BookingDetails;
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

        [Authorize(Roles = "Customer")]
        [HttpPost] // POST: /api/Bookings
        public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingDto createDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _serviceManager.BookingService.CreateBookingAsync(createDto, userId!);
            return Ok(booking);
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("my-bookings")] // GET: /api/Bookings/my-bookings
        public async Task<ActionResult<Pagination<BookingDto>>> GetMyBookings([FromQuery] BookingSpecParams specParams)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _serviceManager.BookingService.GetMyBookingsAsync(specParams, userId!);
            return Ok(bookings);
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("{id:int}")] // GET: /api/Bookings/{id}
        public async Task<ActionResult<BookingDetailsDto>> GetBookingDetails(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _serviceManager.BookingService.GetBookingDetailsAsync(id, userId!);
            return Ok(booking);
        }

        [Authorize(Roles = "Customer")]
        [HttpPatch("{id:int}/cancel")] // PATCH: /api/Bookings/{id}/cancel
        public async Task<ActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _serviceManager.BookingService.CancelBookingAsync(id, userId!);
            return Ok();
        }

        [Authorize(Roles = "Customer")]
        [HttpPatch("additional-issues/{issueId:int}/approve")]
        public async Task<ActionResult> ApproveAdditionalIssue(int issueId,[FromBody] ApproveAdditionalIssueDto approveDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            approveDto.IssueId = issueId;
            await _serviceManager.BookingService.ApproveAdditionalIssueAsync(approveDto, userId);

            var message = approveDto.IsApproved? "تمت الموافقة على المشكلة الإضافية بنجاح": "تم رفض المشكلة الإضافية";
            return Ok(new { message });
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("{id:int}/invoice")]
        public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var invoice = await _serviceManager.BookingService.GetBookingInvoiceAsync(id, userId!);
            return Ok(invoice);
        }

        #endregion

        #region Admin Endpoints

        [Authorize(Roles = "Admin")]
        [HttpGet("all")] // GET: /api/Bookings/all
        public async Task<ActionResult<Pagination<BookingDto>>> GetAllBookings([FromQuery] BookingSpecParams specParams)
            =>  Ok(await _serviceManager.BookingService.GetAllBookingsAsync(specParams));
        

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/assign-technician")]
        public async Task<ActionResult<BookingDto>> AssignTechnician(int id)
            => Ok(await _serviceManager.BookingService.AssignTechnicianAsync(id));
        

        #endregion

        #region Technician Endpoints

        [Authorize(Roles = "Technician")]
        [HttpGet("my-assignments")] // GET: /api/Bookings/my-assignments
        public async Task<ActionResult<Pagination<BookingDto>>> GetMyAssignedBookings([FromQuery] BookingSpecParams specParams)
        {
            var technicianId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bookings = await _serviceManager.BookingService.GetMyAssignedBookingsAsync(specParams, technicianId!);
            return Ok(bookings);
        }

        [Authorize(Roles = "Technician")]
        [HttpPatch("{id:int}/update-status")]
        public async Task<ActionResult<BookingDto>> UpdateBookingStatus(int id,[FromBody] UpdateBookingStatusDto statusDto)
        {
            var technicianId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var booking = await _serviceManager.BookingService.UpdateBookingStatusAsync(id, statusDto, technicianId!);
            return Ok(booking);
        }

        [Authorize(Roles = "Technician")]
        [HttpPost("{id:int}/additional-issues")]
        public async Task<ActionResult<AdditionalIssueDto>> AddAdditionalIssue(int id,[FromBody] AddAdditionalIssueDto issueDto)
        {
            var technicianId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            issueDto.BookingId = id;
            var issue = await _serviceManager.BookingService.AddAdditionalIssueAsync(id, issueDto, technicianId!);
            return Ok(issue);
        }

        #endregion
    }
}
