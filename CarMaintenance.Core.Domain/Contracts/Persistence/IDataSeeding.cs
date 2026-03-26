namespace CarMaintenance.Core.Domain.Contracts.Persistence
{
    public interface IDataSeeding
    {
        Task InitializeAsync();

        Task DataSeedAsync();
    }
}
