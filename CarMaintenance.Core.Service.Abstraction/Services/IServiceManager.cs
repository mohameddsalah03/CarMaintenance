using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Vehicles;

namespace CarMaintenance.Core.Service.Abstraction.Services
{
    public interface IServiceManager
    {
        public IServiceService ServiceService { get; }
        public IAuthService  AuthService { get; }
        public IVehicleService  VehicleService { get; }

        public ITechniciansService TechniciansService { get; }

    }
}
