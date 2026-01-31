using CarMaintenance.Shared.DTOs.Vehicles;

namespace CarMaintenance.Core.Service.Abstraction.Services.Vehicles
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleDto>> GetUserVehicleAsync(string userId);
        Task<VehicleDto?> GetVehicleByIdAsync(int id, string userId);

        Task<VehicleDto> AddVehicleAsync(CreateVehicleDto createDto, string userId);
        Task<VehicleDto> UpdateVehicleAsync(UpdateVehicleDto updateDto, string userId);
        Task DeleteVehicleAsync(int id, string userId);


        // Admin endpoints
        Task<IEnumerable<VehicleDto>> GetAllVehiclesAsync();

    }
}
