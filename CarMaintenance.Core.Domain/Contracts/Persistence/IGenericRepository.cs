using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Contracts.Persistence
{
    public interface IGenericRepository<TEntity,TKey> where TEntity : BaseEntity<TKey>    
       
    {
        Task<IEnumerable<TEntity>> GetAllAsync(bool withTracking = false);
        Task<TEntity?> GetByIdAsync(TKey id);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
    }
}
