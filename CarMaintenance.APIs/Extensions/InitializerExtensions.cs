using CarMaintenance.Core.Domain.Contracts.Persistence;

namespace CarMaintenance.APIs.Extensions
{
    public static class InitializerExtensions
    {
        // generate Object From DbInitializer Explicitly
        public static async Task<WebApplication> InitializeDbContext(this WebApplication app)
        {
            using var scope = app.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            
            var CarContextInitializer = services.GetRequiredService<IDataSeeding>();

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();

            try
            {
                await CarContextInitializer.InitializeAsync();
                await CarContextInitializer.DataSeedAsync();
            }
            catch (Exception ex)
            {

                var logger = loggerFactory.CreateLogger<Program>();
                logger.LogError(ex, "An Error Has Been Occured during applying the migrations !");

            }
           
            return app;

        }
    }
}
