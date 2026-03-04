using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Vehicles
{
    public class VehicleByPlateNumberSpecification : BaseSpecifications<Vehicle, int>
    {
        public VehicleByPlateNumberSpecification(string plateNumber)
            : base(v => v.PlateNumber == plateNumber)
        {
        }

        public VehicleByPlateNumberSpecification(string plateNumber, int excludeId)
            : base(v => v.PlateNumber == plateNumber && v.Id != excludeId)
        {
        }
    }
}