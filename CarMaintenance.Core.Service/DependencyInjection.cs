using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Mapping;
using CarMaintenance.Core.Service.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace CarMaintenance.Core.Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {

            services.AddAutoMapper(typeof(MappingProfile));

            // Service Manager
            //services.AddScoped(typeof(IServiceManager), typeof(ServiceManager));
              
            // BasketService
            services.AddScoped<IAuthService, AuthService>();

           

            return services;
        }

    }
}
