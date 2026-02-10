using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Bookings;
using System.Linq.Expressions;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public class BookingSpecifications : BaseSpecifications<Booking, int>

    {

        public BookingSpecifications(BookingSpecParams specParams) 
            : base(BuildCriteria(specParams))
        {
            ApplySorting(specParams.Sort);
            AddIncludes();
            if (specParams.PageSize > 0)
                AddPagination(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);
        }


        // Get user's bookings
        public BookingSpecifications(string userId, bool isCustomer = true)
            : base(b => isCustomer ? b.UserId == userId : b.TechnicianId == userId)
        {
            AddIncludes();
            AddOrderByDesc(b => b.ScheduledDate);
        }

        public BookingSpecifications(int id) : base(id) 
        {
            AddIncludes();
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

        private void ApplySorting(string? sort)
        {
            switch (sort?.ToLower())
            {
                case "dateasc":
                    AddOrderBy(b => b.ScheduledDate);
                    break;
                case "datedesc":
                default:
                    AddOrderByDesc(b => b.ScheduledDate);
                    break;
            }
        }

        protected override void AddIncludes()
        {
            Includes.Add(e => e.User);
            Includes.Add(e => e.AdditionalIssues);
            Includes.Add(e => e.AssignedTechnician!);
            Includes.Add(e => e.BookingServices);
            Includes.Add(e => e.Review!);
            Includes.Add(e => e.Vehicle);

        }

    }
}
