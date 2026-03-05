using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Vehicles
{
    public class VehicleSpecification : BaseSpecifications<Vehicle, int>
    {

        public VehicleSpecification(string userId)
            : base(v => v.UserId == userId)
        {
            AddIncludes();
            AddOrderByDesc(v => v.Id);  
        }

        public VehicleSpecification(int id, string userId)
            : base(v => v.Id == id && v.UserId == userId)
        {
            AddIncludes();
        }

        #region Admin Only

        public VehicleSpecification()
        {
            AddIncludes();
            AddOrderByDesc(v => v.Id);
        }

        public VehicleSpecification(int id)
            : base(id)
        {
            AddIncludes();
        }

        #endregion
        protected override void AddIncludes()
        {
            Includes.Add(v => v.Owner);  
        }
    }
}