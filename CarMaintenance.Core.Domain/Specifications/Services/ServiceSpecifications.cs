using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Domain.Specifications;
using CarMaintenance.Shared.DTOs.Services;
using System.Linq.Expressions;

public class ServiceSpecifications : BaseSpecifications<Service, int>
{
    public ServiceSpecifications(ServiceSpecParams specParams)
        : base(BuildCriteria(specParams))
    {
        ApplySorting(specParams.Sort);
        AddPagination(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);
    }

    public ServiceSpecifications(int id) : base(id) { }

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
            default: // Price Asc
                AddOrderBy(s => s.Name);
                break;
        }
    }
}