using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public class BookingByTechnicianActiveSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByTechnicianActiveSpecification(string technicianId)
            : base(b => b.TechnicianId == technicianId &&
                        (b.Status == BookingStatus.Pending ||
                         b.Status == BookingStatus.InProgress))
        {
        }
    }
}