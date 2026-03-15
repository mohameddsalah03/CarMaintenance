using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarMaintenance.APIs.Controllers.Controllers.Reviews
{
    public class ReviewsController(IServiceManager serviceManager) : BaseApiController
    {
        [Authorize(Roles = "Customer")]
        [HttpPost("{bookingId}")]
        public async Task<ActionResult<ReviewDto>> CreateReview(int bookingId,[FromBody] CreateReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            dto.BookingId = bookingId;
            return Ok(await serviceManager.ReviewService.CreateReviewAsync(dto, userId!));
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("{bookingId}")]
        public async Task<ActionResult<ReviewDto?>> GetBookingReview(int bookingId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await serviceManager.ReviewService.GetBookingReviewAsync(bookingId, userId!));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAllReviews()
            => Ok(await serviceManager.ReviewService.GetAllReviewsAsync());
      
        [Authorize(Roles = "Technician")]
        [HttpGet("my-reviews")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetMyReceivedReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reviews = await serviceManager.ReviewService.GetMyReceivedReviewsAsync(userId!);
            return Ok(reviews);
        }


    }
}