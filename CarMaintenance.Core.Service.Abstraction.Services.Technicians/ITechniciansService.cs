using CarMaintenance.Shared.DTOs.Technicians;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarMaintenance.Core.Service.Abstraction.Services
{
    public interface ITechniciansService
    {
        Task<IEnumerable<TechniciansDto>> GetTechniciansAsync();
        Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync();

        // Create / Update / Delete should not return entities for write operations
        Task AddTechnicianAsync(TechnicianCreateDto techniciansDto);
        Task EditTechnicianAsync(string id, TechniciansDto techniciansDto);
        Task RemoveTechnicianAsync(string id);
    }
}
