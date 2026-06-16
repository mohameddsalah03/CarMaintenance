using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Shared.DTOs.Bookings;

namespace CarMaintenance.Core.Domain.Specifications.Bookings
{
    public class BookingSpecification : BaseSpecifications<Booking, int>

    {

        public BookingSpecification(BookingSpecParams specParams)
            : base(BookingCriteriaBuilder.Build(specParams))
        {
            ApplySorting(specParams.Sort);
            AddIncludes();
            if (specParams.PageSize > 0)
                AddPagination(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);
        }



        public BookingSpecification(int id) : base(id)
        {
            AddIncludes();
        }

        // Customer 
        public BookingSpecification(string userId)
           : base(b => b.UserId == userId)
        {
            AddIncludes();
            AddOrderByDesc(b => b.ScheduledDate);
        }

        // Technician
        public BookingSpecification(string technicianId, bool isTechnician)
            : base(b => b.TechnicianId == technicianId)
        {
            AddIncludes();
            AddOrderByDesc(b => b.ScheduledDate);
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

            AddThenInclude($"{nameof(Booking.BookingServices)}.{nameof(BookingService.Service)}");
            AddThenInclude($"{nameof(Booking.AssignedTechnician)}.{nameof(Technician.User)}");
            // for ReviewSummaryDto
            AddThenInclude($"{nameof(Booking.Review)}.{nameof(Review.User)}");
            AddThenInclude($"{nameof(Booking.Review)}.{nameof(Review.Technician)}.{nameof(Technician.User)}");
        }

    }
}