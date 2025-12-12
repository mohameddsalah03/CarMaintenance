using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Contracts.Persistence
{
    public interface IUnitOfWork
    {

        public IGenericRepository<TEntity, Tkey> GetRepo<TEntity, Tkey>() where TEntity : BaseEntity<Tkey>;


        Task<int> SaveChangesAsync();

    }
}
