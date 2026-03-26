using CarMaintenance.Shared.DTOs.Technicians;
using CarMaintenance.Shared.DTOs.Technicians.AI;

namespace CarMaintenance.Core.Service.Abstraction.Services.Technicians
{
    public interface ITechniciansService
    {
        Task<IEnumerable<TechniciansDto>> GetAllTechniciansAsync();
        Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync();
        Task<TechniciansDto?> GetTechnicianByIdAsync(string id);
        Task<TechniciansDto> CreateTechnicianAsync(CreateTechnicianDto createDto);
        Task<TechniciansDto> UpdateTechnicianAsync(string id, TechnicianUpdateDto updateDto);
        Task DeleteTechnicianAsync(string id);
        Task<TechniciansDto> ToggleAvailabilityAsync(string id);


        // AI
        Task<TechnicianStatsDto> GetTechnicianStatsAsync(string id);
        Task<TechnicianWorkloadDto> GetTechnicianWorkloadAsync(string id);
        Task<TechnicianRatingDto> GetTechnicianRatingAsync(string id);
    }
}