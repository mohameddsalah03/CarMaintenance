using CarMaintenance.Core.Service.Abstraction.Common.Infrastructure;
using CarMaintenance.Infrastructure.AiServices;
using CarMaintenance.Infrastructure.PaymobServices;
using CarMaintenance.Shared.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarMaintenance.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            services.Configure<AISettings>(configuration.GetSection("AISettings"));
            services.AddHttpClient<IAiTechnicianService, AiTechnicianService>();
            
            services.Configure<PaymobSettings>(configuration.GetSection("PaymobSettings"));
            services.AddHttpClient<IPaymobService, PaymobService>();

            return services;
        }
    }
}