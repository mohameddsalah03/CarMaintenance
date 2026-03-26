using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    // محتاجينها عشان نجيب الـ Booking من BookingNumber في الـ Callback
    public class BookingByNumberSpecification : BaseSpecifications<Booking, int>
    {
        public BookingByNumberSpecification(string bookingNumber)
            : base(b => b.BookingNumber == bookingNumber)
        {
            // محتاجين الـ User عشان نعرف لمين نبعت notification
            Includes.Add(b => b.User);
        }
    }
}