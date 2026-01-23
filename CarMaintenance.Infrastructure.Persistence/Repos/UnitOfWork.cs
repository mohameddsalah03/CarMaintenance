using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Base;
using CarMaintenance.Infrastructure.Persistence.Data;

namespace CarMaintenance.Infrastructure.Persistence.Repos
{
    public class UnitOfWork(CarDbContext _context) : IUnitOfWork
    {
        Dictionary<string, object> _repos = new Dictionary<string, object>();

        public IGenericRepository<TEntity, Tkey> GetRepo<TEntity, Tkey>() where TEntity : BaseEntity<Tkey>
        {
            var name = typeof(TEntity).Name;
            if (_repos.ContainsKey(name))
            {
                return (IGenericRepository<TEntity, Tkey>) _repos[name];
            }
            var repo = new GenericRepo<TEntity, Tkey>(_context);
            _repos[name] = repo;
            return repo;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
