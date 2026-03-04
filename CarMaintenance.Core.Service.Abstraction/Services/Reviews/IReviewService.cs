using CarMaintenance.Shared.DTOs.Reviews;

namespace CarMaintenance.Core.Service.Abstraction.Services.Reviews
{
    public interface IReviewService
    {
        Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, string userId);
        Task<ReviewDto?> GetBookingReviewAsync(int bookingId, string userId);

    }
}
