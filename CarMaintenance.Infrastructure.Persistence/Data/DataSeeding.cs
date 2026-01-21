using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarMaintenance.Infrastructure.Persistence.Data
{
    public class DataSeeding(CarDbContext _context,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole> _roleManager) : IDataSeeding
    {
        public async Task DataSeedAsync()
        {

            try
            {
                if ( (await _context.Database.GetPendingMigrationsAsync()).Any())
                {
                  await  _context.Database.MigrateAsync();

                }

                if (!_roleManager.Roles.Any())
                {
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    await _roleManager.CreateAsync(new IdentityRole("Customer"));
                    await _roleManager.CreateAsync(new IdentityRole("Technician"));

                }
                if (!_userManager.Users.Any())
                {
                    var user1 = new ApplicationUser()
                    {
                        Email = "eslam@gmail.com",
                        DisplayName = "Eslam Ayman",
                        UserName = "EslamAyman",
                        PhoneNumber = "1234567890",
                    };
                    var user2 = new ApplicationUser()
                    {
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
                if (!_context.Vehicles.Any())
                {
                    var vehiclesData = File.OpenRead(@"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds\vehicles.json");
                    var vehicles =  await JsonSerializer.DeserializeAsync<List<Vehicle>>(vehiclesData);

                    if(vehicles != null && vehicles.Any())
                    {
                        await _context.Vehicles.AddRangeAsync(vehicles);
                    }

                }
                if (!_context.AdditionalIssues.Any())
                {
                    var AdditionalIssuesData = File.OpenRead(@"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds\additionalIssues.json");
                    var AdditionalIssues = await JsonSerializer.DeserializeAsync<List<AdditionalIssue>>(AdditionalIssuesData);

                    if (AdditionalIssues != null && AdditionalIssues.Any())
                    {
                        await _context.AdditionalIssues.AddRangeAsync(AdditionalIssues);
                    }

                }
                if (!_context.Bookings.Any())
                {
                    var BookingsData = File.OpenRead(@"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds\bookings.json");
                    var Bookings = await JsonSerializer.DeserializeAsync<List<Booking>>(BookingsData);

                    if (Bookings != null && Bookings.Any())
                    {
                        await _context.Bookings.AddRangeAsync(Bookings);
                    }

                }
                if (!_context.BookingServices.Any())
                {
                    var BookingServicesData = File.OpenRead(@"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds\bookingServices.json");
                    var BookingServices = await JsonSerializer.DeserializeAsync<List<BookingService>>(BookingServicesData);

                    if (BookingServices != null && BookingServices.Any())
                    {
                        await _context.BookingServices.AddRangeAsync(BookingServices);
                    }

                }
                if (!_context.Notifications.Any())
                {
                    var NotificationsData = File.OpenRead(@"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds\notifications.json");
                    var Notifications = await JsonSerializer.DeserializeAsync<List<Notification>>(NotificationsData);

                    if (Notifications != null && Notifications.Any())
                    {
                        await _context.Notifications.AddRangeAsync(Notifications);
                    }

                }
                if (!_context.Reviews.Any())
                {
                    var ReviewsData = File.OpenRead(@"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds\reviews.json");
                    var Reviews = await JsonSerializer.DeserializeAsync<List<Review>>(ReviewsData);

                    if (Reviews != null && Reviews.Any())
                    {
                        await _context.Reviews.AddRangeAsync(Reviews);
                    }

                }
                if (!_context.Services.Any())
                {
                    var ServicesData = File.OpenRead(@"..\CarMaintenance.Infrastructure.Persistence\Data\Seeds\services.json");
                    var Services = await JsonSerializer.DeserializeAsync<List<Service>>(ServicesData);

                    if (Services != null && Services.Any())
                    {
                        await _context.Services.AddRangeAsync(Services);
                    }

                }
                
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }


        }
    }
}
