using CarMaintenance.Infrastructure.Persistence.Identity;
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
            //services.AddScoped(typeof(IUnitOfWork), typeof(UnitOfWork.UnitOfWork));


            #region IdentityContext And IdentityInitializer

            services.AddDbContext<CarIdentityDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("IdentityContext"));
            });

            //
            //services.AddScoped(typeof(IStoreIdentityDbInitializer), typeof(StoreIdentityDbInitializer));

            #endregion

           


             
            return services;
        }
    
    }
}
