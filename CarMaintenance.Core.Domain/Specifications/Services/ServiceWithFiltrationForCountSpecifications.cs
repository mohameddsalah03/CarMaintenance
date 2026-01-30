using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications;
using CarMaintenance.Shared.DTOs.Services;
using System.Linq.Expressions;

public class ServiceWithFiltrationForCountSpecifications : BaseSpecifications<Service, int>
{
    public ServiceWithFiltrationForCountSpecifications(ServiceSpecParams specParams)
        : base(BuildCriteria(specParams))
    {
    }

    private static Expression<Func<Service, bool>> BuildCriteria(ServiceSpecParams specParams)
    {
        return s =>
            (string.IsNullOrEmpty(specParams.Search) || s.Name.Contains(specParams.Search)) &&
            (string.IsNullOrEmpty(specParams.Category) || s.Category == specParams.Category) &&
            (!specParams.MinPrice.HasValue || s.BasePrice >= specParams.MinPrice.Value) &&
            (!specParams.MaxPrice.HasValue || s.BasePrice <= specParams.MaxPrice.Value) &&
            (!specParams.MaxDuration.HasValue || s.EstimatedDurationMinutes <= specParams.MaxDuration.Value);
    }
}