using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Contracts.Persistence
{
    public interface IUnitOfWork : IAsyncDisposable
    {

        IGenericRepository<TEntity,TKey> GetRepository<TEntity, TKey>()
            where TEntity : BaseEntity<TKey>
            where TKey : IEquatable<TKey>;

        Task<int> CompleteAsync();

    }
}
