using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Base;
using CarMaintenance.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace CarMaintenance.Infrastructure.Persistence.Repos
{
    public class GenericRepo<TEntity,TKey>(CarDbContext _context) : 
        IGenericRepository<TEntity, TKey> where TEntity : BaseEntity<TKey>
    {
        public  async Task AddAsync(TEntity entity)
            => await _context.Set<TEntity>().AddAsync(entity);
        

        public void Delete(TEntity entity)
           => _context.Set<TEntity>().Remove(entity);
        

        public async Task<IEnumerable<TEntity>> GetAllAsync(bool withTracking = false)
            => await _context.Set<TEntity>().ToListAsync();


        public async Task<TEntity?> GetByIdAsync(TKey id)
            => await _context.Set<TEntity>().FindAsync(id);
        

        public void Update(TEntity entity)
            => _context.Set<TEntity>().Update(entity);
        
    }
}
