using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
   
    public class BookingByVehicleAllSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByVehicleAllSpecification(int vehicleId)
            : base(b => b.VehicleId == vehicleId)
        {
        }
    }
}