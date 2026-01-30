using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Vehicles
{
    public class VehicleSpecifications : BaseSpecifications<Vehicle, int>
    {

        // Get All Vehicles for specific User
        public VehicleSpecifications(string userId)
            : base(v => v.UserId == userId)
        {
            AddIncludes();
            AddOrderByDesc(v => v.Id);  // الأحدث أولاً
        }

        // Get Vehicle by ID for specific User (Security Check)
        public VehicleSpecifications(int id, string userId)
            : base(v => v.Id == id && v.UserId == userId)
        {
            AddIncludes();
        }

        #region Admin Only

        // Get All Vehicles 
        public VehicleSpecifications()
        {
            AddIncludes();
            AddOrderByDesc(v => v.Id);
        }

        // Get Vehicle by ID (No User can Check)
        public VehicleSpecifications(int id)
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