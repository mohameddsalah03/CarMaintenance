using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Reviews
{
    public class ReviewSpecification : BaseSpecifications<Review,int>
    {
        public ReviewSpecification(int bookingId) : 
            base(r=>r.BookingId == bookingId) 
        {
            AddIncludes();
        }

        public ReviewSpecification(string technicianId, bool byTechnician)
            : base(r => r.TechnicianId == technicianId)
        {
            AddIncludes();
            AddOrderByDesc(r => r.CreatedAt);
        }

        public ReviewSpecification()
            : base()
        {
            AddIncludes();
            AddOrderByDesc(r => r.CreatedAt);
        }


        protected override void AddIncludes()
        {
            base.AddIncludes();
            Includes.Add(r => r.Booking);
            Includes.Add(r => r.User);
            Includes.Add(r => r.Technician);
            AddThenInclude($"{nameof(Review.Technician)}.{nameof(Technician.User)}");
        }

    }
}
