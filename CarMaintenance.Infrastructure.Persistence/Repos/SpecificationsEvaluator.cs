using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace CarMaintenance.Infrastructure.Persistence.Repos
{
    public static class SpecificationsEvaluator<TEntity, TKey>
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        public static IQueryable<TEntity> GetQuery(
            IQueryable<TEntity> dbset,
            ISpecifications<TEntity, TKey> spec)
        {
            var query = dbset;

            // Step 1: Filtration (WHERE)
            if (spec.Criteria is not null)
                query = query.Where(spec.Criteria);

            //  Step 2 — Apply Includes BEFORE OrderBy and Pagination.
            // EF Core translates this to proper JOINs in the main query,
            // avoiding the "split query" warning and ensuring correct pagination.
            query = spec.Includes.Aggregate(query, (currentQuery, includeExpression) =>
                currentQuery.Include(includeExpression));

            // String-based ThenIncludes (e.g. "BookingServices.Service")
            query = spec.ThenIncludeStrings.Aggregate(query, (currentQuery, includeString) =>
                currentQuery.Include(includeString));

            // Step 3: Ordering
            if (spec.OrderByDesc is not null)
                query = query.OrderByDescending(spec.OrderByDesc);
            else if (spec.OrderBy is not null)
                query = query.OrderBy(spec.OrderBy);

            // Step 4: Pagination (SKIP / TAKE — always last)
            if (spec.IsPaginationEnabled)
                query = query.Skip(spec.Skip).Take(spec.Take);

            return query;
        }
    }
}