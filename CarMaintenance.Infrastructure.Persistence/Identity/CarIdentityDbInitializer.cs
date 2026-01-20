using Microsoft.AspNetCore.Identity;
using CarMaintenance.Core.Domain.Contracts.Persistence.DbInitializers;
using CarMaintenance.Core.Domain.Models.Identity;
using CarMaintenance.Infrastructure.Persistence.Common;

namespace CarMaintenance.Infrastructure.Persistence.Identity
{
    public class CarIdentityDbInitializer(
        CarIdentityDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    )
    : DbInitializer(dbContext), ICarIdentityDbInitializer
    {
        public override async Task SeedAsync()
        {
            // ========== 1. SEED ROLES ==========
            var roles = new[] { "Admin", "Customer", "Technician" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ========== 2. SEED ADMIN ==========
            if (!userManager.Users.Any(u => u.Email == "admin@fixora.com"))
            {
                var admin = new ApplicationUser
                {
                    Id = "admin-001",
                    DisplayName = "مدير النظام",
                    UserName = "admin",
                    Email = "admin@fixora.com",
                    PhoneNumber = "01000000000",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(admin, "Admin@123");
                await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ========== 3. SEED CUSTOMERS ==========
            var customers = new[]
            {
                new { Id = "customer-001", DisplayName = "أحمد حسن", UserName = "ahmed.hassan", Email = "ahmed@example.com", Phone = "01111111111" },
                new { Id = "customer-002", DisplayName = "فاطمة محمد", UserName = "fatma.mohamed", Email = "fatma@example.com", Phone = "01222222222" },
                new { Id = "customer-003", DisplayName = "عمر خالد", UserName = "omar.khaled", Email = "omar@example.com", Phone = "01333333333" }
            };

            foreach (var customer in customers)
            {
                if (!userManager.Users.Any(u => u.Email == customer.Email))
                {
                    var user = new ApplicationUser
                    {
                        Id = customer.Id,
                        DisplayName = customer.DisplayName,
                        UserName = customer.UserName,
                        Email = customer.Email,
                        PhoneNumber = customer.Phone,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user, "Customer@123");
                    await userManager.AddToRoleAsync(user, "Customer");
                }
            }

            // ========== 4. SEED TECHNICIANS ==========
            var technicians = new[]
            {
                new { Id = "tech-001", DisplayName = "أحمد محمد", UserName = "ahmed.tech", Email = "ahmed.tech@fixora.com", Phone = "01444444444" },
                new { Id = "tech-002", DisplayName = "خالد مصطفي", UserName = "khaled.tech", Email = "khaled.tech@fixora.com", Phone = "01555555555" },
                new { Id = "tech-003", DisplayName = "يوسف مصطفي", UserName = "youssef.tech", Email = "youssef.tech@fixora.com", Phone = "01666666666" }
            };

            foreach (var tech in technicians)
            {
                if (!userManager.Users.Any(u => u.Email == tech.Email))
                {
                    var user = new ApplicationUser
                    {
                        Id = tech.Id,
                        DisplayName = tech.DisplayName,
                        UserName = tech.UserName,
                        Email = tech.Email,
                        PhoneNumber = tech.Phone,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(user, "Tech@123");
                    await userManager.AddToRoleAsync(user, "Technician");
                }
            }
        }
    }
}