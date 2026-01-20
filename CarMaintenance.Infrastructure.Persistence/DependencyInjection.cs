using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Contracts.Persistence.DbInitializers;
using CarMaintenance.Infrastructure.Persistence.Data;
using CarMaintenance.Infrastructure.Persistence.Identity;
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
            
           
            //
            services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork));


            #region IdentityContext And IdentityInitializer

            services.AddDbContext<CarIdentityDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("IdentityContext"));
            });

            services.AddDbContext<CarDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("MainContext"));
            });

            // Register DbInitializers
            services.AddScoped<ICarIdentityDbInitializer, CarIdentityDbInitializer>();
            services.AddScoped<ICarDbInitializer, CarDbInitializer>();

            
            #endregion





            return services;
        }
    
    }
}
