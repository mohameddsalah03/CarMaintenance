using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Core.Domain.Specifications.Bookings.Admin
{
    // Fetches all bookings with minimal includes for stats counting
    // We only need Status, TotalCost, ScheduledDate - no heavy navigation loading
    public class BookingStatsSpecification : BaseSpecifications<Booking, int>
    {
        //  All bookings for counting
        public BookingStatsSpecification() : base()
        {
        }

        //  Bookings for a specific date (today's stats)
        public BookingStatsSpecification(DateTime date)
            : base(b => b.ScheduledDate.Date == date.Date)
        {
        }

        // Bookings by status
        public BookingStatsSpecification(BookingStatus status)
            : base(b => b.Status == status)
        {
        }

        //  Completed bookings for revenue calculation
        public static BookingStatsSpecification ForRevenue()
            => new BookingStatsSpecification(BookingStatus.Completed);

        //  Today's completed bookings for today's revenue
        public static BookingStatsSpecification ForTodayRevenue(DateTime today)
        {
            var spec = new BookingStatsSpecification();
            spec.Criteria = b =>
                b.Status == BookingStatus.Completed &&
                b.ScheduledDate.Date == today.Date;
            return spec;
        }
    }
}