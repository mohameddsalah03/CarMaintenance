using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Helper.JsonConverterDataSeeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CarMaintenance.Infrastructure.Persistence.Data
{
    public class DataSeeding(CarDbContext _context,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole> _roleManager) : IDataSeeding
    {
        private readonly string _seedsPath = @"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds";

        public async Task InitializeAsync()
        {
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
                await _context.Database.MigrateAsync();
        }

        public async Task DataSeedAsync()
        {
            try
            {
                // 1. Seed Roles
                await SeedRolesAsync();

                // 2. Seed Users
                await SeedUsersAsync();

                // 3. Seed Technicians
                await SeedTechniciansAsync();

                // 4. Seed Vehicles
                await SeedVehiclesAsync();

                // 5. Seed Services
                await SeedServicesAsync();

                // 6. Seed Bookings
                await SeedBookingsAsync();

                // 7. Seed BookingServices
                await SeedBookingServicesAsync();

                // 8. Seed AdditionalIssues
                await SeedAdditionalIssuesAsync();

                // 9. Seed Notifications
                await SeedNotificationsAsync();

                // 10. Seed Reviews
                await SeedReviewsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in DataSeeding: {ex.Message}", ex);
            }
        }

        private async Task SeedRolesAsync()
        {
            if (!_roleManager.Roles.Any())
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
                await _roleManager.CreateAsync(new IdentityRole("Customer"));
                await _roleManager.CreateAsync(new IdentityRole("Technician"));
            }
        }

        private async Task SeedUsersAsync()
        {
            if (!_userManager.Users.Any())
            {
                var user1 = new ApplicationUser()
                {
                    Id = "9aa803f4-3c24-49a7-bffa-22e0a5d7c1bf", // Fixed ID للـ Admin
                    Email = "eslam@gmail.com",
                    DisplayName = "Eslam Ayman",
                    UserName = "EslamAyman",
                    PhoneNumber = "1234567890",
                };
                var user2 = new ApplicationUser()
                {
                    Id = "fd767dae-db14-48d6-8fc9-2d690fb441dc", // Fixed ID للـ Customer
                    Email = "MoSalah@gmail.com",
                    DisplayName = "Mo Salah",
                    UserName = "MoSalah",
                    PhoneNumber = "01150734483",
                };

                await _userManager.CreateAsync(user1, "P@ssW0rd");
                await _userManager.CreateAsync(user2, "P@ssW0rd");

                await _userManager.AddToRoleAsync(user1, "Admin");
                await _userManager.AddToRoleAsync(user2, "Customer");
            }
        }

        private async Task SeedTechniciansAsync()
        {
            if (!_context.Technicians.Any())
            {
                try
                {
                    var filePath = Path.Combine(_seedsPath, "technicians.json");

                    using var stream = File.OpenRead(filePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var technicians = await JsonSerializer.DeserializeAsync<List<Technician>>(stream, options);

                    if (technicians != null && technicians.Any())
                    {
                        // ولّد Id لكل Technician
                        foreach (var tech in technicians)
                        {
                            tech.Id = Guid.NewGuid().ToString();
                        }

                        await _context.Technicians.AddRangeAsync(technicians);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error seeding Technicians: {ex.Message}", ex);
                }
            }
        }

        private async Task SeedVehiclesAsync()
        {
            if (!_context.Vehicles.Any())
            {
                var vehiclesData = File.OpenRead(Path.Combine(_seedsPath, "vehicles.json"));
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var vehicles = await JsonSerializer.DeserializeAsync<List<Vehicle>>(vehiclesData);

                if (vehicles != null && vehicles.Any())
                {   
                    await _context.Vehicles.AddRangeAsync(vehicles);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedServicesAsync()
        {
            if (!_context.Services.Any())
            {
                var servicesData = File.OpenRead(Path.Combine(_seedsPath, "services.json"));
                var services = await JsonSerializer.DeserializeAsync<List<Service>>(servicesData);

                if (services != null && services.Any())
                {
                    await _context.Services.AddRangeAsync(services);
                    await _context.SaveChangesAsync();
                }
            }
        }


        private async Task SeedBookingsAsync()
        {
            if (!_context.Bookings.Any())
            {
                var bookingsData = File.OpenRead(Path.Combine(_seedsPath, "bookings.json"));
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                {
                    new BookingStatusJsonConverter(),
                    new PaymentMethodJsonConverter(),
                    new PaymentStatusJsonConverter()
                }
                };
                var bookings = await JsonSerializer.DeserializeAsync<List<Booking>>(bookingsData, options);

                if (bookings != null && bookings.Any())
                {
                    await _context.Bookings.AddRangeAsync(bookings);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedBookingServicesAsync()
        {
            if (!_context.BookingServices.Any())
            {
                var bookingServicesData = File.OpenRead(Path.Combine(_seedsPath, "bookingServices.json"));

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters =
                    {
                        new BookingStatusJsonConverter(),
                    }
                };
                var bookingServices = await JsonSerializer.DeserializeAsync<List<BookingService>>(bookingServicesData);

                if (bookingServices != null && bookingServices.Any())
                {
                    await _context.BookingServices.AddRangeAsync(bookingServices);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedAdditionalIssuesAsync()
        {
            if (!_context.AdditionalIssues.Any())
            {
                var additionalIssuesData = File.OpenRead(Path.Combine(_seedsPath, "additionalIssues.json"));
                var additionalIssues = await JsonSerializer.DeserializeAsync<List<AdditionalIssue>>(additionalIssuesData);

                if (additionalIssues != null && additionalIssues.Any())
                {
                    await _context.AdditionalIssues.AddRangeAsync(additionalIssues);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedNotificationsAsync()
        {
            if (!_context.Notifications.Any())
            {
                var notificationsData = File.OpenRead(Path.Combine(_seedsPath, "notifications.json"));
                var notifications = await JsonSerializer.DeserializeAsync<List<Notification>>(notificationsData);

                if (notifications != null && notifications.Any())
                {
                    await _context.Notifications.AddRangeAsync(notifications);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedReviewsAsync()
        {
            if (!_context.Reviews.Any())
            {
                var reviewsData = File.OpenRead(Path.Combine(_seedsPath, "reviews.json"));
                var reviews = await JsonSerializer.DeserializeAsync<List<Review>>(reviewsData);

                if (reviews != null && reviews.Any())
                {
                    await _context.Reviews.AddRangeAsync(reviews);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}