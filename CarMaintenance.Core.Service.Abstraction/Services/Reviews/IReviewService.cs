using CarMaintenance.Shared.DTOs.Reviews;

namespace CarMaintenance.Core.Service.Abstraction.Services.Reviews
{
    public interface IReviewService
    {
        // Customer
        Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, string userId);
        Task<ReviewDto?> GetBookingReviewAsync(int bookingId, string userId);

        //Technician
        Task<IEnumerable<ReviewDto>> GetMyReceivedReviewsAsync(string userId);


        // Admin
        Task<IEnumerable<ReviewDto>> GetAllReviewsAsync();

    }
}
