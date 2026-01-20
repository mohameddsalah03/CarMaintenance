using CarMaintenance.Core.Domain.Contracts.Persistence.DbInitializers;

namespace CarMaintenance.APIs.Extensions
{
    public static class InitializerExtensions
    {
        public static async Task<WebApplication> InitializeDbContextAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("DbInitializer");

            try
            {
                // ========== 1. Identity Database FIRST ==========
                var identityInitializer = services.GetRequiredService<ICarIdentityDbInitializer>();
                logger.LogInformation("Initializing Identity Database...");
                await identityInitializer.InitializeAsync();
                await identityInitializer.SeedAsync(); // ✅ Users يتعملوا seed الأول
                logger.LogInformation("Identity Database initialized successfully.");

                // ========== 2. Main Database SECOND ==========
                var carDbInitializer = services.GetRequiredService<ICarDbInitializer>();
                logger.LogInformation("Initializing Main Database...");
                await carDbInitializer.InitializeAsync();
                await carDbInitializer.SeedAsync(); // ✅ بعدين باقي الـ Entities
                logger.LogInformation("Main Database initialized successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }

            return app;
        }
    }
}