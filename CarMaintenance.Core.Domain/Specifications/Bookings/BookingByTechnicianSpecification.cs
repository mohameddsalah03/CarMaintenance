using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public class BookingByTechnicianSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByTechnicianSpecification(string technicianId)
            : base(b => b.TechnicianId == technicianId)
        {
        }
    }
}