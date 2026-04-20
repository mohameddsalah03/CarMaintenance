using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Technicians
{
    public class TechnicianByServiceCategorySpecification : BaseSpecifications<Technician,string>
    {
        public TechnicianByServiceCategorySpecification(string serviceCategory , bool onlyAvailable) :
            base(t=>
                (!onlyAvailable || t.IsAvailable) &&
                (t.Specialization.Contains(serviceCategory) ||
                 t.Specialization.Contains("عام") ||
                 t.Specialization.Contains("شامل") ||
                 t.Specialization.Contains("كهرباء السيارات") ||
                 t.Specialization.Contains("صيانة عامة")
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
