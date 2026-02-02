using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Auth.Email;
using CarMaintenance.Core.Service.Mapping;
using CarMaintenance.Core.Service.Services;
using CarMaintenance.Core.Service.Services.Auth;
using CarMaintenance.Core.Service.Services.Auth.Email;
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
            IConfiguration configuration
            )
        {

            services.AddAutoMapper(typeof(MappingProfile));
            // Service Manager
            services.AddScoped(typeof(IServiceManager), typeof(ServiceManager));


            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

            
            // 
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<ITechniciansService, TechniciansService>();

            return services;
        }

    }
}
