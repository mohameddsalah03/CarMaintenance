using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Core.Service.Abstraction.Services.Admin;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Bookings;
using CarMaintenance.Core.Service.Abstraction.Services.Notifications;
using CarMaintenance.Core.Service.Abstraction.Services.Payments;
using CarMaintenance.Core.Service.Abstraction.Services.Reviews;
using CarMaintenance.Core.Service.Abstraction.Services.Technicians;
using CarMaintenance.Core.Service.Abstraction.Services.Vehicles;

namespace CarMaintenance.Core.Service.Abstraction.Services
{
    public interface IServiceManager
    {
        IServiceService ServiceService { get; }
        IAuthService AuthService { get; }
        IVehicleService VehicleService { get; }
        ITechniciansService TechniciansService { get; }
        IBookingService BookingService { get; }
        IAiTechnicianService AiTechnicianService { get; }
        IAiDiagnosisService AiDiagnosisService { get; }
        IReviewService ReviewService { get; }

        INotificationService NotificationService { get; }
        IAdminService AdminService { get; }
        IPaymentService PaymentService { get; }

    }
}