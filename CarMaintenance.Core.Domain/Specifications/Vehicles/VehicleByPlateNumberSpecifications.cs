using CarMaintenance.Core.Domain.Models.Data;

namespace CarMaintenance.Core.Domain.Specifications.Vehicles
{
    public class VehicleByPlateNumberSpecifications : BaseSpecifications<Vehicle, int>
    {
        // Check if PlateNumber exists (للتحقق من التكرار)
        public VehicleByPlateNumberSpecifications(string plateNumber)
            : base(v => v.PlateNumber == plateNumber)
        {
        }

        // Check if PlateNumber exists for another vehicle (للتحديث)
        public VehicleByPlateNumberSpecifications(string plateNumber, int excludeId)
            : base(v => v.PlateNumber == plateNumber && v.Id != excludeId)
        {
        }
    }
}