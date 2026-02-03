using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMaintenance.APIs.Controllers.Controllers.Technicians
{
    public class TechniciansController(IServiceManager serviceManager) : BaseApiController
    {
        // Get all technicians (Admin/Public)
        [HttpGet] // GET: /api/Technicians
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAllTechnicians()
        {
            var result = await serviceManager.TechniciansService.GetAllTechniciansAsync();
            return Ok(result);
        }

        // Get only available technicians
        [HttpGet("available")] // GET: /api/Technicians/available
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAvailableTechnicians()
        {
            var result = await serviceManager.TechniciansService.GetAvailableTechniciansAsync();
            return Ok(result);
        }

        // Get technician by ID
        [HttpGet("{id}")] // GET: /api/Technicians/{id}
        public async Task<ActionResult<TechniciansDto>> GetTechnician([FromRoute] string id)
        {
            var result = await serviceManager.TechniciansService.GetTechnicianByIdAsync(id);
            return Ok(result);
        }

        // Admin Only - Create new technician
        [Authorize(Roles = "Admin")]
        [HttpPost] // POST: /api/Technicians
        public async Task<ActionResult<TechniciansDto>> CreateTechnician([FromBody] CreateTechnicianDto createDto)
        {
            var result = await serviceManager.TechniciansService.CreateTechnicianAsync(createDto);
            return CreatedAtAction(nameof(GetTechnician), new { id = result.Id }, result);
        }

        // Admin/Technician - Update technician info
        [Authorize(Roles = "Admin,Technician")]
        [HttpPut("{id}")] // PUT: /api/Technicians/{id}
        public async Task<ActionResult<TechniciansDto>> UpdateTechnician(
            [FromRoute] string id,
            [FromBody] TechnicianUpdateDto updateDto)
        {
            var result = await serviceManager.TechniciansService.UpdateTechnicianAsync(id, updateDto);
            return Ok(result);
        }

        // Admin Only - Delete technician
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")] // DELETE: /api/Technicians/{id}
        public async Task<ActionResult> DeleteTechnician([FromRoute] string id)
        {
            await serviceManager.TechniciansService.DeleteTechnicianAsync(id);
            return NoContent();
        }

        // Admin/Technician - Toggle availability status
        [Authorize(Roles = "Admin,Technician")]
        [HttpPatch("{id}/toggle-availability")] // PATCH: /api/Technicians/{id}/toggle-availability
        public async Task<ActionResult<TechniciansDto>> ToggleAvailability([FromRoute] string id)
        {
            var result = await serviceManager.TechniciansService.ToggleAvailabilityAsync(id);
            return Ok(result);
        }
    }
}