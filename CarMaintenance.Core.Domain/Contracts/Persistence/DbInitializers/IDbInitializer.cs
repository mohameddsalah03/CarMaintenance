namespace CarMaintenance.Core.Domain.Contracts.Persistence.DbInitializers
{
    public interface IDbInitializer
    {
        Task InitializeAsync(); 
        Task SeedAsync();
    }
}
