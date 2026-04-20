using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Infrastructure.Persistence.Data;
using CarMaintenance.Infrastructure.Persistence.Repos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace CarMaintenance.Infrastructure.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistenceServices(this IServiceCollection services , IConfiguration configuration)
        {
            services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));
            services.AddScoped<IDataSeeding, DataSeeding>();

            #region IdentityContext 


            services.AddDbContext<CarDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("MainContext"));
            });


            #endregion

            return services;
        }
    
    }
}
