using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Base;
using CarMaintenance.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace CarMaintenance.Infrastructure.Persistence.Repos
{
    internal class GenericRepository<TEntity, TKey>(CarDbContext _dbContext) : IGenericRepository<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {

        public async Task<IEnumerable<TEntity>> GetAllAsync(bool withTracking = false)
            => withTracking ? await _dbContext.Set<TEntity>().ToListAsync()
            : await _dbContext.Set<TEntity>().AsNoTracking().ToListAsync();

        public async Task<TEntity?> GetByIdAsync(TKey id)
            => await _dbContext.Set<TEntity>().FindAsync(id);



        #region With Spec
        public async Task<IEnumerable<TEntity>> GetAllWithSpecAsync(ISpecifications<TEntity, TKey> spec, bool withTracking = false)
            => await ApplaySpecifications(spec).ToListAsync();


        public async Task<TEntity?> GetWithSpecAsync(ISpecifications<TEntity, TKey> spec)
            => await ApplaySpecifications(spec).FirstOrDefaultAsync();


        public async Task<int> GetCountAsync(ISpecifications<TEntity, TKey> spec)
            => await ApplaySpecifications(spec).CountAsync();


        #endregion

        public async Task AddAsync(TEntity entity)
            => await _dbContext.Set<TEntity>().AddAsync(entity);

        public void Delete(TEntity entity)
            => _dbContext.Set<TEntity>().Remove(entity);

        public void Update(TEntity entity)
            => _dbContext.Set<TEntity>().Update(entity);


        #region Helper Methods

        // For ApplaySpecifications With SpecicficationsEvaluator
        private IQueryable<TEntity> ApplaySpecifications(ISpecifications<TEntity, TKey> spec)
        {
            return SpecificationsEvaluator<TEntity, TKey>.GetQuery(_dbContext.Set<TEntity>(), spec);
        }

        #endregion

    }

}
