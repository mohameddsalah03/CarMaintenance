using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public class BookingByVehicleActiveSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByVehicleActiveSpecification(int vehicleId)
            : base(b => b.VehicleId == vehicleId &&
                        (b.Status == BookingStatus.Pending ||
                         b.Status == BookingStatus.InProgress ||
                         b.Status == BookingStatus.WaitingClientApproval))
        {
        }
    }
}