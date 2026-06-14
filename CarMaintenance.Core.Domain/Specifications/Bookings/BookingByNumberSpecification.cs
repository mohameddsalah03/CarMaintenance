using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    /// <summary>
    /// Finds a single booking by its human-readable BookingNumber (e.g. "BK-20260501-A1B2C3").
    /// Used by PaymentService when processing Paymob callbacks.
    /// Includes User, BookingServices, and AdditionalIssues for payment calculation.
    /// </summary>
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