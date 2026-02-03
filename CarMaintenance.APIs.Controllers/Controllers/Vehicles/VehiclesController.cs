using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarMaintenance.APIs.Controllers.Controllers.Vehicles
{
    public class VehiclesController(IServiceManager serviceManager) : BaseApiController
    {
        // Get all vehicles for the logged-in user
        [Authorize]
        [HttpGet] // GET: /api/Vehicles
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetUserVehicles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicles = await serviceManager.VehicleService.GetUserVehicleAsync(userId!);
            return Ok(vehicles);
        }

        // Get specific vehicle by ID (only if it belongs to the user)
        [Authorize]
        [HttpGet("{id}")] // GET: /api/Vehicles/9
        public async Task<ActionResult<VehicleDto>> GetVehicle([FromRoute] int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicle = await serviceManager.VehicleService.GetVehicleByIdAsync(id, userId!);
            return Ok(vehicle);
        }

        // Add new vehicle for the logged-in user
        [Authorize]
        [HttpPost] // POST: /api/Vehicles
        public async Task<ActionResult<VehicleDto>> AddVehicle([FromBody] CreateVehicleDto createDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicle = await serviceManager.VehicleService.AddVehicleAsync(createDto, userId!);
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }

        // Update vehicle (only if it belongs to the user)
        [Authorize]
        [HttpPut("{id}")] // PUT: /api/Vehicles/1
        public async Task<ActionResult<VehicleDto>> UpdateVehicle(
            [FromRoute] int id,
            [FromBody] UpdateVehicleDto updateDto)
        {
            updateDto.Id = id;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicle = await serviceManager.VehicleService.UpdateVehicleAsync(updateDto, userId!);
            return Ok(vehicle);
        }

        // Delete vehicle (only if it belongs to the user)
        [Authorize]
        [HttpDelete("{id}")] // DELETE: /api/Vehicles/1
        public async Task<ActionResult> DeleteVehicle([FromRoute] int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await serviceManager.VehicleService.DeleteVehicleAsync(id, userId!);
            return NoContent();
        }

        // Admin Only - Get all vehicles from all users
        [Authorize(Roles = "Admin")]
        [HttpGet("all")] // GET: /api/Vehicles/all
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles()
        {
            var vehicles = await serviceManager.VehicleService.GetAllVehiclesAsync();
            return Ok(vehicles);
        }
    }
}