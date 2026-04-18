using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Infrastructure.Persistence.Helper.JsonConverterDataSeeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarMaintenance.Infrastructure.Persistence.Data
{
    public class DataSeeding(
        CarDbContext _context,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole> _roleManager
        ) : IDataSeeding
    {
        private readonly string _seedsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Seeds");

        // Options مشترك لكل الـ JSON reads — PropertyNameCaseInsensitive عشان
        // الـ JSON بيجي camelCase بس الـ C# Properties بـ PascalCase
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // DTO داخلي لقراءة users.json — ApplicationUser مش Serializable مباشرة
        // عشان فيه Identity properties
        private sealed record UserSeedDto(
            string Id,
            string DisplayName,
            string UserName,
            string Email,
            string PhoneNumber,
            string Password,
            string Role);

        // ─────────────────────────────────────────────
        public async Task InitializeAsync()
        {
            var pending = await _context.Database.GetPendingMigrationsAsync();
            if (pending.Any())
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
                throw new Exception($"DataSeeding failed: {ex.Message}", ex);
            }
        }

        // ─── 1. Roles ────────────────────────────────
        private async Task SeedRolesAsync()
        {
            if (_roleManager.Roles.Any()) return;

            foreach (var role in new[] { "Admin", "Customer", "Technician" })
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        // ─── 2. Users ────────────────────────────────
        // يقرأ users.json — فيه admin + customers + technician users
        // بنستخدم UserManager عشان يعمل password hashing صح
        private async Task SeedUsersAsync()
        {
            if (_userManager.Users.Any()) return;

            var filePath = Path.Combine(_seedsPath, "users.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);
            var dtos = await JsonSerializer.DeserializeAsync<List<UserSeedDto>>(stream, _opts);
            if (dtos is null || !dtos.Any()) return;

            foreach (var dto in dtos)
            {
                var user = new ApplicationUser
                {
                    Id = dto.Id,
                    DisplayName = dto.DisplayName,
                    UserName = dto.UserName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    EmailConfirmed = true   // مش محتاجين email confirmation في الـ seed
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (result.Succeeded)
                    await _userManager.AddToRoleAsync(user, dto.Role);
            }
        }

        // ─── 3a. Technicians ─────────────────────────
        // string PK — مفيش IDENTITY_INSERT
        private async Task SeedTechniciansAsync()
        {
            if (_context.Technicians.Any()) return;

            var filePath = Path.Combine(_seedsPath, "technicians.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);
            var technicians = await JsonSerializer
                .DeserializeAsync<List<Technician>>(stream, _opts);
            if (technicians is null || !technicians.Any()) return;

            // تحقق إن كل UserId موجود فعلاً في جدول Users
            var validUserIds = (await _userManager.Users
                .Select(u => u.Id)
                .ToListAsync()).ToHashSet();

            var valid = technicians
                .Where(t => !string.IsNullOrWhiteSpace(t.Id)
                         && validUserIds.Contains(t.UserId))
                .ToList();

            if (valid.Any())
            {
                await _context.Technicians.AddRangeAsync(valid);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedVehiclesAsync()
        {
            if (await _context.Vehicles.AnyAsync()) return;

            var filePath = Path.Combine(_seedsPath, "vehicles.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);
            var vehicles = await JsonSerializer.DeserializeAsync<List<Vehicle>>(stream, _opts);
            if (vehicles is null || !vehicles.Any()) return;

            var validUserIds = (await _userManager.Users
                .Select(u => u.Id).ToListAsync())
                .ToHashSet();

            var valid = vehicles
                .Where(v => validUserIds.Contains(v.UserId))
                .ToList();

            if (!valid.Any()) return;

            // أهم خطوة: لازم العملية كلها تتم جوه Transaction واحدة واضحة
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. افتح الباب
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Vehicles ON");

                // 2. ضيف الداتا
                await _context.Vehicles.AddRangeAsync(valid);

                // 3. احفظ التغييرات (هنا الـ SQL بيشيك على الـ Identity)
                await _context.SaveChangesAsync();

                // 4. اقفل الباب
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Vehicles OFF");

                // 5. ثبت العملية
                await transaction.CommitAsync();
                Console.WriteLine("✅ Vehicles seeded successfully within transaction.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Vehicles seeding failed: {ex.Message}");
                throw;
            }
        }

        // ─── 3c. Services ────────────────────────────
        // int PK مع IDENTITY_INSERT — IDs 1-16 لازم تتطابق مع الـ AI SERVICES_MAP
        private async Task SeedServicesAsync()
        {
            if (await _context.Services.AnyAsync()) return;

            var filePath = Path.Combine(_seedsPath, "services.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);
            var services = await JsonSerializer.DeserializeAsync<List<Service>>(stream, _opts);
            if (services is null || !services.Any()) return;

            // البدء في Transaction صريحة لضمان بقاء الـ Connection مفتوحة
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. فتح الـ Identity Insert
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Services ON");

                // 2. إضافة البيانات
                await _context.Services.AddRangeAsync(services);

                // 3. الحفظ الفعلي
                await _context.SaveChangesAsync();

                // 4. قفل الـ Identity Insert
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Services OFF");

                // 5. تثبيت العملية
                await transaction.CommitAsync();
                Console.WriteLine("✅ Services seeded successfully!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Services seeding failed: {ex.Message}");
                throw;
            }
        }

        // ─── 4. Bookings ─────────────────────────────
        private async Task SeedBookingsAsync()
        {
            if (await _context.Bookings.AnyAsync()) return;

            var filePath = Path.Combine(_seedsPath, "bookings.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = {
            new BookingStatusJsonConverter(),
            new PaymentMethodJsonConverter(),
            new PaymentStatusJsonConverter()
        }
            };

            var bookings = await JsonSerializer.DeserializeAsync<List<Booking>>(stream, options);
            if (bookings is null || !bookings.Any()) return;

            var userIds = (await _userManager.Users.Select(u => u.Id).ToListAsync()).ToHashSet();
            var vehicleIds = (await _context.Vehicles.Select(v => v.Id).ToListAsync()).ToHashSet();
            var technicianIds = (await _context.Technicians.Select(t => t.Id).ToListAsync()).ToHashSet();

            var valid = bookings.Where(b =>
                userIds.Contains(b.UserId) &&
                vehicleIds.Contains(b.VehicleId) &&
                (string.IsNullOrEmpty(b.TechnicianId) || technicianIds.Contains(b.TechnicianId))
            ).ToList();

            if (!valid.Any()) return;

            // ─── البدء في Transaction لضمان عمل IDENTITY_INSERT ───
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Bookings ON");

                await _context.Bookings.AddRangeAsync(valid);
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Bookings OFF");

                await transaction.CommitAsync();
                Console.WriteLine("✅ Bookings seeded successfully!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Bookings seeding failed: {ex.Message}");
                throw;
            }
        }

        // ─── 5. BookingServices ──────────────────────
        private async Task SeedBookingServicesAsync()
        {
            if (await _context.BookingServices.AnyAsync()) return;

            var filePath = Path.Combine(_seedsPath, "bookingServices.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new BookingStatusJsonConverter() }
            };

            var bookingServices = await JsonSerializer.DeserializeAsync<List<BookingService>>(stream, options);
            if (bookingServices is null || !bookingServices.Any()) return;

            // Validation
            var bookingIds = (await _context.Bookings.Select(b => b.Id).ToListAsync()).ToHashSet();
            var serviceIds = (await _context.Services.Select(s => s.Id).ToListAsync()).ToHashSet();

            var valid = bookingServices.Where(bs =>
                bookingIds.Contains(bs.BookingId) &&
                serviceIds.Contains(bs.ServiceId)).ToList();

            if (!valid.Any()) return;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT BookingServices ON");
                await _context.BookingServices.AddRangeAsync(valid);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT BookingServices OFF");
                await transaction.CommitAsync();
                Console.WriteLine(" BookingServices seeded successfully!");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─── 6a. AdditionalIssues ────────────────────
        // مش محتاج IDENTITY_INSERT — الـ IDs بتتولد تلقائي
        private async Task SeedAdditionalIssuesAsync()
        {
            if (await _context.AdditionalIssues.AnyAsync()) return;

            var filePath = Path.Combine(_seedsPath, "additionalIssues.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);

            // ⚠️ التأكد من إضافة الـ Converters هنا
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters =
        { 
            // لو الـ Enum في الـ JSON مكتوب نص (String) استخدم الـ Converter الجاهز ده:
            new JsonStringEnumConverter(),
            // أو لو إنت عامل Custom Converter خاص بيه ضيفه هنا:
            // new AdditionalIssueStatusJsonConverter() 
        }
            };

            var issues = await JsonSerializer.DeserializeAsync<List<AdditionalIssue>>(stream, options);

            if (issues is null || !issues.Any()) return;

            // Validation (للتأكد إن الـ BookingId موجود فعلاً)
            var bookingIds = (await _context.Bookings.Select(b => b.Id).ToListAsync()).ToHashSet();
            var valid = issues.Where(i => bookingIds.Contains(i.BookingId)).ToList();

            if (valid.Any())
            {
                await _context.AdditionalIssues.AddRangeAsync(valid);
                await _context.SaveChangesAsync();
                Console.WriteLine("✅ AdditionalIssues seeded successfully!");
            }
        }

        // ─── 6b. Notifications ───────────────────────
        private async Task SeedNotificationsAsync()
        {
            if (await _context.Notifications.AnyAsync()) return;

            var filePath = Path.Combine(_seedsPath, "notifications.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);

            // تأكد من إضافة الـ Converter هنا
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var notifications = await JsonSerializer.DeserializeAsync<List<Notification>>(stream, options);
            if (notifications is null || !notifications.Any()) return;

            // Validation لضمان عدم وجود Foreign Key Error
            var userIds = (await _userManager.Users.Select(u => u.Id).ToListAsync()).ToHashSet();
            var valid = notifications.Where(n => userIds.Contains(n.UserId)).ToList();

            if (valid.Any())
            {
                await _context.Notifications.AddRangeAsync(valid);
                await _context.SaveChangesAsync();
                Console.WriteLine("Notifications seeded successfully!");
            }
        }

        // ─── 6c. Reviews ─────────────────────────────
        private async Task SeedReviewsAsync()
        {
            if (await _context.Reviews.AnyAsync()) return;

            var filePath = Path.Combine(_seedsPath, "reviews.json");
            if (!File.Exists(filePath)) return;

            await using var stream = File.OpenRead(filePath);
            var reviews = await JsonSerializer.DeserializeAsync<List<Review>>(stream, _opts);
            if (reviews is null || !reviews.Any()) return;

            var bookingIds = (await _context.Bookings.Select(b => b.Id).ToListAsync()).ToHashSet();
            var valid = reviews.Where(r => bookingIds.Contains(r.BookingId)).ToList();

            if (valid.Any())
            {
                await _context.Reviews.AddRangeAsync(valid);
                await _context.SaveChangesAsync();
            }
        }
    }
}