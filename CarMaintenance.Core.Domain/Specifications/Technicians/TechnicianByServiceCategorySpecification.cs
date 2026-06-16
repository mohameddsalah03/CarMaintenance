using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Technicians
{
    public class TechnicianByServiceCategorySpecification : BaseSpecifications<Technician, string>
    {
        public TechnicianByServiceCategorySpecification(string serviceCategory, bool onlyAvailable)
            : base(t =>
                (!onlyAvailable || t.IsAvailable) &&
                (
                    t.Specialization.Contains(serviceCategory) || // Direct match بالـ English slug
                    t.Specialization.Contains("general")
                )
            )
        {
            AddIncludes();
            AddOrderByDesc(t => t.Rating);
        }

        protected override void AddIncludes()
        {
            base.AddIncludes();
            Includes.Add(t => t.User);
        }
    }
}