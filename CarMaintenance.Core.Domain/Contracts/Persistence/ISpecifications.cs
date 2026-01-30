using System.Linq.Expressions;
using CarMaintenance.Core.Domain.Models.Base;

namespace CarMaintenance.Core.Domain.Contracts.Persistence
{
    public interface ISpecifications<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
    {
        Expression<Func<TEntity, bool>>? Criteria { get; set; }
        List<Expression<Func<TEntity, object>>> Includes { get; set; }
        Expression<Func<TEntity, object>>? OrderBy { get; set; }
        Expression<Func<TEntity, object>>? OrderByDesc { get; set; }
        int Skip { get; set; }
        int Take { get; set; }
        bool IsPaginationEnabled { get; set; }
    }
}