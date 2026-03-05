using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Bookings;
using CarMaintenance.Core.Service.Abstraction.Services.Reviews;
using CarMaintenance.Core.Service.Abstraction.Services.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services.Vehicles;

namespace CarMaintenance.Core.Service.Abstraction.Services
{
    public interface IServiceManager
    {
        public IServiceService ServiceService { get; }
        public IAuthService  AuthService { get; }
        public IVehicleService  VehicleService { get; }

        public ITechniciansService TechniciansService { get; }
        public IBookingService BookingService { get; }
        public IAiTechnicianService AiTechnicianService { get; }
        public IReviewService ReviewService { get; }

    }
}
