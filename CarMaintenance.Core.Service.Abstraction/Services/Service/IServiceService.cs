using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Services;
using CarMaintenance.Shared.DTOs.Services.AnalyzeProblem;

namespace CarMaintenance.Core.Service.Abstraction.Services
{
    public interface IServiceService
    {
        // Public endpoints
        Task<Pagination<ServiceDto>> GetServicesAsync(ServiceSpecParams specParams);
        Task<ServiceDto?> GetServiceByIdAsync(int id);

        // Admin endpoints
        Task<ServiceDto> CreateServiceAsync(CreateServiceDto createDto);
        Task<ServiceDto> UpdateServiceAsync(UpdateServiceDto updateDto);
        Task DeleteServiceAsync(int id);

        // Get Service Details with Technicians
        Task<ServiceDetailsDto?> GetServiceDetailsAsync(int id);
        Task<AnalyzeProblemResponseDto> AnalyzeProblemAsync(AnalyzeProblemRequestDto requestDto, string? userId);
    }
}