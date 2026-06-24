using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Reviews
{
    
    public class ReviewByServiceSpecification : BaseSpecifications<Review, int>
    {
        public ReviewByServiceSpecification(int serviceId)
            : base(r => r.Booking.BookingServices.Any(bs => bs.ServiceId == serviceId))
        {
        }
    }
}