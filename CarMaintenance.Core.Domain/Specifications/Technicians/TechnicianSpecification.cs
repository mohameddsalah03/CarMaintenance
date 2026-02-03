using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Technicians
{
    public class TechnicianSpecification : BaseSpecifications<Technician, string>
    {
        // Get All Technicians
        public TechnicianSpecification() : base()
        {
            AddIncludes();
            AddOrderByDesc(t => t.Rating);
        }

        // Get Available Technicians Only
        public TechnicianSpecification(bool isAvailable) : base(t => t.IsAvailable == isAvailable)
        {
            AddIncludes();
            AddOrderByDesc(t => t.Rating);
        }

        // Get Technician by ID
        public TechnicianSpecification(string id) : base(id)
        {
            AddIncludes();
        }

        protected override void AddIncludes()
        {
            Includes.Add(t => t.User);
        }
    }
}