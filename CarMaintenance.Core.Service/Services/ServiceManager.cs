using AutoMapper;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Core.Service.Abstraction.Services.Admin;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Bookings;
using CarMaintenance.Core.Service.Abstraction.Services.Notifications;
using CarMaintenance.Core.Service.Abstraction.Services.Payments;
using CarMaintenance.Core.Service.Abstraction.Services.Reviews;
using CarMaintenance.Core.Service.Abstraction.Services.Technicians;
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
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<IServiceService> _serviceService;
        private readonly Lazy<IAuthService> _authService;
        private readonly Lazy<IVehicleService> _vehicleService;
        private readonly Lazy<ITechniciansService> _technicianService;
        private readonly Lazy<IBookingService> _bookingService;
        private readonly Lazy<IAiTechnicianService> _aiTechnicianService;
        private readonly Lazy<IReviewService> _reviewService;
        private readonly Lazy<INotificationService> _notificationService;
        private readonly Lazy<IAdminService> _adminService;
        private readonly Lazy<IPaymentService> _paymentService;
        private readonly Lazy<IAiDiagnosisService> _aiDiagnosisService;

        public ServiceManager(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _serviceProvider = serviceProvider;

            _serviceService = new Lazy<IServiceService>(() => _serviceProvider.GetRequiredService<IServiceService>());

            _vehicleService = new Lazy<IVehicleService>(() =>new VehicleService(_unitOfWork, _mapper));

            _bookingService = new Lazy<IBookingService>(() =>_serviceProvider.GetRequiredService<IBookingService>());

            _authService = new Lazy<IAuthService>(() => _serviceProvider.GetRequiredService<IAuthService>());

            _technicianService = new Lazy<ITechniciansService>(() =>_serviceProvider.GetRequiredService<ITechniciansService>());

            _aiTechnicianService = new Lazy<IAiTechnicianService>(() => _serviceProvider.GetRequiredService<IAiTechnicianService>());
            
            _reviewService = new Lazy<IReviewService>(() => _serviceProvider.GetRequiredService<IReviewService>());

            _notificationService = new Lazy<INotificationService>(() => _serviceProvider.GetRequiredService<INotificationService>());
            
            _adminService = new Lazy<IAdminService>(() => _serviceProvider.GetRequiredService<IAdminService>());
            _paymentService = new Lazy<IPaymentService>(() => _serviceProvider.GetRequiredService<IPaymentService>());
            _aiDiagnosisService = new Lazy<IAiDiagnosisService>(() => _serviceProvider.GetRequiredService<IAiDiagnosisService>());

        }

        public IServiceService ServiceService => _serviceService.Value;
        public IAuthService AuthService => _authService.Value;
        public IVehicleService VehicleService => _vehicleService.Value;
        public ITechniciansService TechniciansService => _technicianService.Value;
        public IBookingService BookingService => _bookingService.Value;
        public IAiTechnicianService AiTechnicianService => _aiTechnicianService.Value;
        public IReviewService ReviewService => _reviewService.Value;

        public INotificationService NotificationService => _notificationService.Value;

        public IAdminService AdminService => _adminService.Value;

        public IPaymentService PaymentService => _paymentService.Value;

        public IAiDiagnosisService AiDiagnosisService => _aiDiagnosisService.Value;
    }
}