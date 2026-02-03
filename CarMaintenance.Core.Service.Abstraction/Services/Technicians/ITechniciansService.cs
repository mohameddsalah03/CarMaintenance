using CarMaintenance.Shared.DTOs.Technicians;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMaintenance.Core.Service.Abstraction.Services
{
    public interface ITechniciansService
    {
        Task<IEnumerable<TechniciansDto>> GetTechniciansAsync();
        Task<IEnumerable<TechniciansDto>> GetAvailableTechniciansAsync();
        //Task<TechniciansDto> AddTechniciansAsync(TechnicianCreateDto techniciansDto);
        Task<TechniciansDto> EditTechniciansAsync(string id, TechnicianUpdateDto techniciansDto);
        Task RemoveTechnicianAsync(string id);
    }
}
