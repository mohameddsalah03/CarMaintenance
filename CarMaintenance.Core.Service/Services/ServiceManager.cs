using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Vehicles;
using CarMaintenance.Core.Service.Services.Vehicles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarMaintenance.Core.Service.Services
{
    internal class ServiceManager : IServiceManager
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        private readonly IServiceProvider _serviceProvider; // For entity Service To Generate other DI for it
        private readonly Lazy<IServiceService> _serviceService;
        private readonly Lazy<IAuthService> _authService;
        private readonly Lazy<IVehicleService> _vehicleService;
        private readonly Lazy<ITechniciansService> _technicianService;


        public ServiceManager(
            IUnitOfWork unitOfWork ,
            IMapper mapper ,
            IConfiguration configuration,
            IServiceProvider serviceProvider
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _serviceProvider = serviceProvider;
            _serviceService = new Lazy<IServiceService>(() => new ServiceService(_unitOfWork, _mapper));
            _vehicleService = new Lazy<IVehicleService>(() => new VehicleService(_unitOfWork, _mapper));
            _authService = new Lazy<IAuthService>(() => _serviceProvider.GetRequiredService<IAuthService>());
            _technicianService = new Lazy<ITechniciansService>(() => _serviceProvider.GetRequiredService<ITechniciansService>());
        }

        

        public IServiceService ServiceService => _serviceService.Value;

        public IAuthService AuthService => _authService.Value;

        public IVehicleService VehicleService => _vehicleService.Value;

        public ITechniciansService TechniciansService => _technicianService.Value;
    }
}
