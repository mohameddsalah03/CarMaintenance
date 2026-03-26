using CarMaintenance.Shared.DTOs.Technicians;

namespace CarMaintenance.Shared.DTOs.Services
{
    public class ServiceDetailsDto : ServiceDto
    {
        // Available Technicians for this service
        public List<TechniciansDto> AvailableTechnicians { get; set; } = new();
    }
}