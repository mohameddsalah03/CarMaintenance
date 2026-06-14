using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Reviews
{
    /// <summary>
    /// Returns all reviews linked to bookings that included a specific service.
    /// Used to calculate AverageRating and ReviewCount on ServiceDto.
    /// </summary>
    public class ReviewByServiceSpecification : BaseSpecifications<Review, int>
    {
        public ReviewByServiceSpecification(int serviceId)
            : base(r => r.Booking.BookingServices.Any(bs => bs.ServiceId == serviceId))
        {
        }
    }
}