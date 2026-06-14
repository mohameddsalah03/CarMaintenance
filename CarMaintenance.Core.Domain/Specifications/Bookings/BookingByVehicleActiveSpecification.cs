using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    
    public class BookingByVehicleActiveSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByVehicleActiveSpecification(int vehicleId)
            : base(b => b.VehicleId == vehicleId &&
                        b.Status != BookingStatus.Completed &&
                        b.Status != BookingStatus.Cancelled)
        {
        }
    }
}