using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    /// <summary>
    /// Returns ALL bookings for a vehicle (all statuses).
    /// Used in UpdateBookingStatus to count completed vs total
    /// for the progress notification ("تم إكمال خدمة 2/5").
    /// </summary>
    public class BookingByVehicleAllSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByVehicleAllSpecification(int vehicleId)
            : base(b => b.VehicleId == vehicleId)
        {
        }
    }
}