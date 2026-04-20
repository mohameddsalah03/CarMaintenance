using CarMaintenance.Core.Domain.Models.Base;
using System.Linq.Expressions;

namespace CarMaintenance.Core.Domain.Contracts.Persistence
{
    public interface IGenericRepository<TEntity,TKey> where TEntity : BaseEntity<TKey>    
       
    {
        Task<IEnumerable<TEntity>> GetAllAsync(bool withTracking = false);
        Task<TEntity?> GetByIdAsync(TKey id);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);

        // Specifications
        Task<IEnumerable<TEntity>> GetAllWithSpecAsync(ISpecifications<TEntity, TKey> spec, bool withTracking = false);
        Task<TEntity?> GetWithSpecAsync(ISpecifications<TEntity, TKey> spec);
        Task<int> GetCountAsync(ISpecifications<TEntity, TKey> spec);


        // For Sum in Admin Service
        Task<decimal> GetSumAsync(ISpecifications<TEntity, TKey> spec,Expression<Func<TEntity, decimal>> selector);
    }
}
