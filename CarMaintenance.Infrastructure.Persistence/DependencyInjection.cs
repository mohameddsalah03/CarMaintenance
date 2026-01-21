using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Contracts.Persistence.DbInitializers;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Data;
using CarMaintenance.Infrastructure.Persistence.Repos;
using Microsoft.AspNetCore.Identity;
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


            #region IdentityContext And IdentityInitializer

           

            services.AddDbContext<CarDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("MainContext"));
            });

            services.AddIdentityCore<ApplicationUser>()
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<CarDbContext>();


            #endregion





            return services;
        }
    
    }
}
