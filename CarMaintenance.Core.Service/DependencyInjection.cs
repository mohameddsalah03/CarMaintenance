using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Core.Service.Abstraction.Services.Admin;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Auth.Email;
using CarMaintenance.Core.Service.Abstraction.Services.Bookings;
using CarMaintenance.Core.Service.Abstraction.Services.Notifications;
using CarMaintenance.Core.Service.Abstraction.Services.Payments;
using CarMaintenance.Core.Service.Abstraction.Services.Reviews;
using CarMaintenance.Core.Service.Abstraction.Services.Technicians;
using CarMaintenance.Core.Service.Mapping;
using CarMaintenance.Core.Service.Services;
using CarMaintenance.Core.Service.Services.Admin;
using CarMaintenance.Core.Service.Services.Auth;
using CarMaintenance.Core.Service.Services.Auth.Email;
using CarMaintenance.Core.Service.Services.Bookings;
using CarMaintenance.Core.Service.Services.Notifications;
using CarMaintenance.Core.Service.Services.Payments;
using CarMaintenance.Core.Service.Services.Reviews;
using CarMaintenance.Core.Service.Services.Technicians;
using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarMaintenance.Core.Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddAutoMapper(m=> m.AddProfile<MappingProfile>());

            services.AddScoped(typeof(IServiceManager), typeof(ServiceManager));

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            // For Requierd Service in Service Manager
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITechniciansService, TechniciansService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IPaymentService, PaymentService>();
            
            services.AddScoped<IServiceService, ServiceService>(); 




            return services;
        }
    }
}