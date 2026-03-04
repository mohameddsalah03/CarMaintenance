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
        [Authorize]
        [HttpGet] // GET: /api/Vehicles
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetUserVehicles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicles = await serviceManager.VehicleService.GetUserVehicleAsync(userId!);
            return Ok(vehicles);
        }

        [Authorize]
        [HttpGet("{id}")] // GET: /api/Vehicles/9
        public async Task<ActionResult<VehicleDto>> GetVehicle([FromRoute] int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicle = await serviceManager.VehicleService.GetVehicleByIdAsync(id, userId!);
            return Ok(vehicle);
        }

        [Authorize]
        [HttpPost] // POST: /api/Vehicles
        public async Task<ActionResult<VehicleDto>> AddVehicle([FromBody] CreateVehicleDto createDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var vehicle = await serviceManager.VehicleService.AddVehicleAsync(createDto, userId!);
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }

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

        [Authorize]
        [HttpDelete("{id}")] // DELETE: /api/Vehicles/1
        public async Task<ActionResult> DeleteVehicle([FromRoute] int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await serviceManager.VehicleService.DeleteVehicleAsync(id, userId!);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")] // GET: /api/Vehicles/all
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles()
        {
            var vehicles = await serviceManager.VehicleService.GetAllVehiclesAsync();
            return Ok(vehicles);
        }
    }
}