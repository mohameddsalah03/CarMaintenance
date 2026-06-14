using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Technicians
{
    public class TechnicianByServiceCategorySpecification : BaseSpecifications<Technician,string>
    {
        public TechnicianByServiceCategorySpecification(string serviceCategory, bool onlyAvailable) :
            base(t =>
                (!onlyAvailable || t.IsAvailable) &&
                (
                    t.Specialization.Contains(serviceCategory) ||
                    t.Specialization.Contains("general") ||
                    t.Specialization.Contains("maintenance") ||
                    (serviceCategory == "تغيير الزيت" && t.Specialization.Contains("oil_change")) ||
                    (serviceCategory == "الفرامل" && t.Specialization.Contains("brakes")) ||
                    (serviceCategory == "المحرك" && t.Specialization.Contains("engine"))
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
