using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    
    public class BookingByNumberSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByNumberSpecification(string bookingNumber)
            : base(b => b.BookingNumber == bookingNumber)
        {
            Includes.Add(b => b.User);
            Includes.Add(b => b.BookingServices);
            AddThenInclude("BookingServices.Service");
            Includes.Add(b => b.AdditionalIssues);
        }
    }
}