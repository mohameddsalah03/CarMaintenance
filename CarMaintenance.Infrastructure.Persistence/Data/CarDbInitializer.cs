using System.Text.Json;
using CarMaintenance.Core.Domain.Contracts.Persistence.DbInitializers;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Common;

namespace CarMaintenance.Infrastructure.Persistence.Data
{
    public class CarDbInitializer(CarDbContext _dbContext) :
        DbInitializer(_dbContext),
        ICarDbInitializer
    {


        public override async Task SeedAsync()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // ========== 1. Services (Independent) ==========
            if (!_dbContext.Services.Any())
            {
                var servicesData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/services.json");

                var services = JsonSerializer.Deserialize<List<Service>>(servicesData, options);

                if (services?.Any() == true)
                {
                    await _dbContext.Services.AddRangeAsync(services);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // ========== 2. Technicians (Depends on Users from IdentityDb) ==========
            if (!_dbContext.Technicians.Any())
            {
                var techsData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/technicians.json");

                var technicians = JsonSerializer.Deserialize<List<Technician>>(techsData, options);

                if (technicians?.Any() == true)
                {
                    await _dbContext.Technicians.AddRangeAsync(technicians);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // ========== 3. Vehicles (Depends on Users) ==========
            if (!_dbContext.Vehicles.Any())
            {
                var vehiclesData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/vehicles.json");

                var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(vehiclesData, options);

                if (vehicles?.Any() == true)
                {
                    await _dbContext.Vehicles.AddRangeAsync(vehicles);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // ========== 4. Bookings (Depends on Users, Vehicles, Technicians) ==========
            if (!_dbContext.Bookings.Any())
            {
                var bookingsData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/bookings.json");

                var bookings = JsonSerializer.Deserialize<List<Booking>>(bookingsData, options);

                if (bookings?.Any() == true)
                {
                    await _dbContext.Bookings.AddRangeAsync(bookings);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // ========== 5. BookingServices (Depends on Bookings, Services) ==========
            if (!_dbContext.BookingServices.Any())
            {
                var bookingServicesData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/bookingServices.json");

                var bookingServices = JsonSerializer.Deserialize<List<BookingService>>(bookingServicesData, options);

                if (bookingServices?.Any() == true)
                {
                    await _dbContext.BookingServices.AddRangeAsync(bookingServices);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // ========== 6. AdditionalIssues (Depends on Bookings) ==========
            if (!_dbContext.AdditionalIssues.Any())
            {
                var issuesData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/additionalIssues.json");

                var issues = JsonSerializer.Deserialize<List<AdditionalIssue>>(issuesData, options);

                if (issues?.Any() == true)
                {
                    await _dbContext.AdditionalIssues.AddRangeAsync(issues);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // ========== 7. Reviews (Depends on Users, Technicians, Bookings) ==========
            if (!_dbContext.Reviews.Any())
            {
                var reviewsData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/reviews.json");

                var reviews = JsonSerializer.Deserialize<List<Review>>(reviewsData, options);

                if (reviews?.Any() == true)
                {
                    await _dbContext.Reviews.AddRangeAsync(reviews);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // ========== 8. Notifications (Depends on Users) ==========
            if (!_dbContext.Notifications.Any())
            {
                var notificationsData = await File.ReadAllTextAsync(
                    @"../CarMaintenance.Infrastructure.Persistence/Data/Seeds/notifications.json");

                var notifications = JsonSerializer.Deserialize<List<Notification>>(notificationsData, options);

                if (notifications?.Any() == true)
                {
                    await _dbContext.Notifications.AddRangeAsync(notifications);
                    await _dbContext.SaveChangesAsync();
                }
            }
        }
    }
}