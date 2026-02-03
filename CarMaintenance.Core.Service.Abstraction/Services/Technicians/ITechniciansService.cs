using CarMaintenance.Shared.DTOs.Technicians;

namespace CarMaintenance.Core.Service.Abstraction.Services
{
    public interface ITechniciansService
    {
        Task<IEnumerable<TechniciansDto>> GetAllTechniciansAsync();
        Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync();
        Task<TechniciansDto?> GetTechnicianByIdAsync(string id);

        // انتقل من AuthService
        Task<TechniciansDto> CreateTechnicianAsync(CreateTechnicianDto createDto);

        Task<TechniciansDto> UpdateTechnicianAsync(string id, TechnicianUpdateDto updateDto);
        Task DeleteTechnicianAsync(string id);
        Task<TechniciansDto> ToggleAvailabilityAsync(string id);
    }
}