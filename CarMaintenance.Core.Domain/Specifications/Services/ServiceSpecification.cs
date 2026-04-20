using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications;
using CarMaintenance.Shared.DTOs.Services;
using System.Linq.Expressions;

public class ServiceSpecification : BaseSpecifications<Service, int>
{
    public ServiceSpecification(ServiceSpecParams specParams)
        : base(BuildCriteria(specParams))
    {
        ApplySorting(specParams.Sort);
        AddPagination(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);
    }

    public ServiceSpecification(int id) : base(id) { }

    private static Expression<Func<Service, bool>> BuildCriteria(ServiceSpecParams specParams)
    {
        return s =>
            (string.IsNullOrEmpty(specParams.Search) || s.Name.Contains(specParams.Search)) &&
            (string.IsNullOrEmpty(specParams.Category) || s.Category == specParams.Category) &&
            (!specParams.MinPrice.HasValue || s.BasePrice >= specParams.MinPrice.Value) &&
            (!specParams.MaxPrice.HasValue || s.BasePrice <= specParams.MaxPrice.Value) &&
            (!specParams.MaxDuration.HasValue || s.EstimatedDurationMinutes <= specParams.MaxDuration.Value);
    }

    private void ApplySorting(string? sort)
    {
        switch (sort?.ToLower())
        {
            case "priceasc":
                AddOrderBy(s => s.BasePrice);
                break;
            case "pricedesc":
                AddOrderByDesc(s => s.BasePrice);
                break;
            case "durationasc":
                AddOrderBy(s => s.EstimatedDurationMinutes);
                break;
            case "durationdesc":
                AddOrderByDesc(s => s.EstimatedDurationMinutes);
                break;
            default: 
                AddOrderBy(s => s.Name);
                break;
        }
    }
}