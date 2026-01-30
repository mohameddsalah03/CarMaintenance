using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace CarMaintenance.Infrastructure.Persistence.Repos
{
    public static class SpecificationsEvaluator<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        public static IQueryable<TEntity> GetQuery(IQueryable<TEntity> dbset,
                                                   ISpecifications<TEntity, TKey> spec)
        {
            var query = dbset; // _dbContext.Set<entity>();

            #region Criteria [Filtration] [Where()]

            if (spec.Criteria is not null) // P => P.Id.Equals(1)
            {
                query = query.Where(spec.Criteria); // _dbContext.Set<entity>().Where(P => P.Id.Equals(1))

            }

            #endregion

            #region Order By

            if (spec.OrderByDesc is not null)
                query = query.OrderByDescending(spec.OrderByDesc);
            else if (spec.OrderBy is not null)
                query = query.OrderBy(spec.OrderBy);


            #endregion

            #region  Pagination

            if (spec.IsPaginationEnabled)
                query = query.Skip(spec.Skip).Take(spec.Take);

            #endregion

            #region Includes [Loading Navigational Property]

            query = spec.Includes.Aggregate(query, (CurrentQuery, IncludeExpression) =>
                CurrentQuery.Include(IncludeExpression)
            );

            #endregion

            return query;

        }
    }
}