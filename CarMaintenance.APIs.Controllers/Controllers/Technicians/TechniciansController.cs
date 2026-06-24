using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMaintenance.APIs.Controllers.Controllers.Technicians
{
    public class TechniciansController(IServiceManager serviceManager) : BaseApiController
    {
        #region Public 

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAllTechnicians()
            => Ok(await serviceManager.TechniciansService.GetAllTechniciansAsync());

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAvailableTechnicians()
            => Ok(await serviceManager.TechniciansService.GetAvailableTechniciansAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<TechniciansDto>> GetTechnician(string id)
            => Ok(await serviceManager.TechniciansService.GetTechnicianByIdAsync(id));


        #endregion

        #region Admin Only

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<TechniciansDto>> CreateTechnician([FromBody] CreateTechnicianDto createDto)
        {
            var result = await serviceManager.TechniciansService.CreateTechnicianAsync(createDto);
            return CreatedAtAction(nameof(GetTechnician), new { id = result.Id }, result);
        }

        [Authorize(Roles = "Admin,Technician")]
        [HttpPut("{id}")]
        public async Task<ActionResult<TechniciansDto>> UpdateTechnician(string id,[FromBody] TechnicianUpdateDto updateDto)
            => Ok(await serviceManager.TechniciansService.UpdateTechnicianAsync(id, updateDto));

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTechnician(string id)
        {
            await serviceManager.TechniciansService.DeleteTechnicianAsync(id);
            return NoContent();
        }

        [Authorize(Roles = "Admin,Technician")]
        [HttpPatch("{id}/toggle-availability")]
        public async Task<ActionResult<TechniciansDto>> ToggleAvailability(string id)
            => Ok(await serviceManager.TechniciansService.ToggleAvailabilityAsync(id));

        #endregion
    }
}