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
        private readonly string _seedsPath = Path.Combine(AppContext.BaseDirectory, "Seeds");

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
                await SeedRolesAsync();
                await SeedUsersAsync();
                await SeedTechniciansAsync();
                await SeedVehiclesAsync();
                await SeedServicesAsync();
                await SeedBookingsAsync();
                await SeedBookingServicesAsync();
                await SeedAdditionalIssuesAsync();
                await SeedNotificationsAsync();
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
                    Id = "9aa803f4-3c24-49a7-bffa-22e0a5d7c1bf",
                    Email = "eslam@gmail.com",
                    DisplayName = "Eslam Ayman",
                    UserName = "EslamAyman",
                    PhoneNumber = "1234567890",
                };
                var user2 = new ApplicationUser()
                {
                    Id = "fd767dae-db14-48d6-8fc9-2d690fb441dc",
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
                var filePath = Path.Combine(_seedsPath, "technicians.json");
                if (!File.Exists(filePath)) return;

                using var stream = File.OpenRead(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var technicians = await JsonSerializer.DeserializeAsync<List<Technician>>(stream, options);

                if (technicians != null && technicians.Any())
                {
                    var userIds = _userManager.Users.Select(u => u.Id).ToHashSet();
                    var validTechnicians = technicians
                        .Where(t => !string.IsNullOrWhiteSpace(t.Id) && userIds.Contains(t.UserId))
                        .ToList();

                    if (validTechnicians.Any())
                    {
                        await _context.Technicians.AddRangeAsync(validTechnicians);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task SeedVehiclesAsync()
        {
            if (!_context.Vehicles.Any())
            {
                var vehiclesPath = Path.Combine(_seedsPath, "vehicles.json");
                if (!File.Exists(vehiclesPath)) return;

                using var vehiclesData = File.OpenRead(vehiclesPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var vehicles = await JsonSerializer.DeserializeAsync<List<Vehicle>>(vehiclesData, options);

                if (vehicles != null && vehicles.Any())
                {
                    await _context.Vehicles.AddRangeAsync(vehicles);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedServicesAsync()
        {
            try
            {
                var servicesPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "..",
                    "CarMaintenance.Infrastructure.Persistence",
                    "Data",
                    "Seeds",
                    "services.json"
                );

                servicesPath = Path.GetFullPath(servicesPath);
                Console.WriteLine($"🔍 Looking for: {servicesPath}");

                if (!File.Exists(servicesPath))
                {
                    Console.WriteLine($"❌ services.json not found at: {servicesPath}");
                    return;
                }

                Console.WriteLine("📂 Reading services.json...");

                using var servicesData = File.OpenRead(servicesPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var servicesFromFile = await JsonSerializer.DeserializeAsync<List<Service>>(servicesData, options);

                if (servicesFromFile == null || !servicesFromFile.Any())
                {
                    Console.WriteLine("⚠️ No services found in JSON file");
                    return;
                }

                Console.WriteLine($"📋 Found {servicesFromFile.Count} services in file");

                int addedCount = 0, updatedCount = 0, skippedCount = 0;

                foreach (var serviceFromFile in servicesFromFile)
                {
                    if (serviceFromFile.Id <= 0)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(serviceFromFile.Name))
                    {
                        skippedCount++;
                        continue;
                    }

                    try
                    {
                        var existingService = await _context.Services
                            .FirstOrDefaultAsync(s => s.Id == serviceFromFile.Id);

                        if (existingService != null)
                        {
                            Console.WriteLine($"🔄 Updating service: {serviceFromFile.Name}");
                            existingService.Name = serviceFromFile.Name;
                            existingService.Category = serviceFromFile.Category;
                            existingService.Description = serviceFromFile.Description;
                            existingService.BasePrice = serviceFromFile.BasePrice;
                            existingService.EstimatedDurationMinutes = serviceFromFile.EstimatedDurationMinutes;
                            existingService.IncludedItems = serviceFromFile.IncludedItems;
                            existingService.ExcludedItems = serviceFromFile.ExcludedItems;
                            existingService.Requirements = serviceFromFile.Requirements;
                            _context.Services.Update(existingService);
                            updatedCount++;
                        }
                        else
                        {
                            Console.WriteLine($"➕ Adding new service: {serviceFromFile.Name}");
                            await _context.Services.AddAsync(serviceFromFile);
                            addedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error processing service '{serviceFromFile.Name}': {ex.Message}");
                        skippedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Services seeding completed! Added: {addedCount}, Updated: {updatedCount}, Skipped: {skippedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SeedServicesAsync: {ex.Message}");
                throw;
            }
        }

        private async Task SeedBookingsAsync()
        {
            if (!_context.Bookings.Any())
            {
                var bookingsPath = Path.Combine(_seedsPath, "bookings.json");
                if (!File.Exists(bookingsPath)) return;

                using var bookingsData = File.OpenRead(bookingsPath);
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
                    var userIds = _userManager.Users.Select(u => u.Id).ToHashSet();
                    var vehicleIds = _context.Vehicles.Select(v => v.Id).ToHashSet();
                    var technicianIds = _context.Technicians.Select(t => t.Id).ToHashSet();

                    var validBookings = bookings
                        .Where(b =>
                            userIds.Contains(b.UserId) &&
                            vehicleIds.Contains(b.VehicleId) &&
                            (string.IsNullOrWhiteSpace(b.TechnicianId) || technicianIds.Contains(b.TechnicianId)))
                        .ToList();

                    if (validBookings.Any())
                    {
                        await _context.Bookings.AddRangeAsync(validBookings);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task SeedBookingServicesAsync()
        {
            if (!_context.BookingServices.Any())
            {
                var bookingServicesPath = Path.Combine(_seedsPath, "bookingServices.json");
                if (!File.Exists(bookingServicesPath)) return;

                using var bookingServicesData = File.OpenRead(bookingServicesPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new BookingStatusJsonConverter() }
                };

                var bookingServices = await JsonSerializer
                    .DeserializeAsync<List<BookingService>>(bookingServicesData, options);

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
                var path = Path.Combine(_seedsPath, "additionalIssues.json");
                if (!File.Exists(path)) return;

                using var data = File.OpenRead(path);

                // ✅ Fix: أضفنا options
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var additionalIssues = await JsonSerializer
                    .DeserializeAsync<List<AdditionalIssue>>(data, options);

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
                var path = Path.Combine(_seedsPath, "notifications.json");
                if (!File.Exists(path)) return;

                using var data = File.OpenRead(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var notifications = await JsonSerializer
                    .DeserializeAsync<List<Notification>>(data, options);

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
                var path = Path.Combine(_seedsPath, "reviews.json");
                if (!File.Exists(path)) return;

                using var data = File.OpenRead(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var reviews = await JsonSerializer
                    .DeserializeAsync<List<Review>>(data, options);

                if (reviews != null && reviews.Any())
                {
                    await _context.Reviews.AddRangeAsync(reviews);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}