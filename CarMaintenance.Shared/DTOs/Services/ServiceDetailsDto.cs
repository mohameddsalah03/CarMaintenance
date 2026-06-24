using CarMaintenance.Shared.DTOs.Technicians;

namespace CarMaintenance.Shared.DTOs.Services
{
    public class ServiceDetailsDto : ServiceDto
    {
        public List<TechniciansDto> AvailableTechnicians { get; set; } = new();
    }
}