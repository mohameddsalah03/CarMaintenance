using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Core.Domain.Specifications.Bookings.Admin
{
    public class BookingStatsSpecification : BaseSpecifications<Booking, int>
    {
        // All bookings for counting
        public BookingStatsSpecification() : base() { }

        // Bookings for a specific date (range comparison — uses index)
        public BookingStatsSpecification(DateTime date)
            : base(BuildDateRangeCriteria(date))
        { }

        // Bookings by status
        public BookingStatsSpecification(BookingStatus status)
            : base(b => b.Status == status)
        { }

        // Bookings for a technician on a specific date
        public BookingStatsSpecification(string technicianId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);
            Criteria = b => b.TechnicianId == technicianId &&
                            b.ScheduledDate >= dayStart &&
                            b.ScheduledDate < dayEnd;
        }

        // Completed bookings for revenue calculation
        public static BookingStatsSpecification ForRevenue()
            => new BookingStatsSpecification(BookingStatus.Completed);

        // Today's completed bookings for today's revenue
        public static BookingStatsSpecification ForTodayRevenue(DateTime today)
        {
            var dayStart = today.Date;
            var dayEnd = dayStart.AddDays(1);
            var spec = new BookingStatsSpecification();
            spec.Criteria = b =>
                b.Status == BookingStatus.Completed &&
                b.ScheduledDate >= dayStart &&
                b.ScheduledDate < dayEnd;
            return spec;
        }

        private static System.Linq.Expressions.Expression<System.Func<Booking, bool>>
            BuildDateRangeCriteria(DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);
            return b => b.ScheduledDate >= dayStart && b.ScheduledDate < dayEnd;
        }
    }
}