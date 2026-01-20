using CarMaintenance.Core.Domain.Models.Identity;
using CarMaintenance.Infrastructure.Persistence.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CarMaintenance.Infrastructure.Persistence.Identity
{
    public class CarIdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public CarIdentityDbContext(DbContextOptions<CarIdentityDbContext> options)
            : base(options)
        {
            
        }


       
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Must Keep It

            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Ignore<IdentityUserClaim<string>>();
            builder.Ignore<IdentityUserToken<string>>();
            builder.Ignore<IdentityUserLogin<string>>();
            builder.Ignore<IdentityRoleClaim<string>>();


            //for Config
            builder.ApplyConfigurationsFromAssembly(typeof(CarIdentityDbContext).Assembly,
                type => type.GetCustomAttribute<DbContextTypeAttribute>()?.DbContextType == typeof(CarIdentityDbContext));


        }

    }
}
