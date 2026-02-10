using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Bookings;
using System.Linq.Expressions;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public class BookingWithFiltrationForCountSpecifications : BaseSpecifications<Booking, int>
    {
        public BookingWithFiltrationForCountSpecifications(BookingSpecParams specParams)
            : base(BuildCriteria(specParams))
        {
        }

        private static Expression<Func<Booking, bool>> BuildCriteria(BookingSpecParams specParams)
        {
            return b =>
                (string.IsNullOrEmpty(specParams.Status) || b.Status.ToString() == specParams.Status) &&
                (string.IsNullOrEmpty(specParams.UserId) || b.UserId == specParams.UserId) &&
                (string.IsNullOrEmpty(specParams.TechnicianId) || b.TechnicianId == specParams.TechnicianId) &&
                (!specParams.FromDate.HasValue || b.ScheduledDate >= specParams.FromDate.Value) &&
                (!specParams.ToDate.HasValue || b.ScheduledDate <= specParams.ToDate.Value);
        }
    }
}