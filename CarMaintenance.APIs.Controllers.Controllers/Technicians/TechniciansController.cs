using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using Microsoft.AspNetCore.Mvc;

namespace CarMaintenance.APIs.Controllers.Controllers.Technicians
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechniciansController : BaseApiController
    {
        private readonly ITechniciansService _techniciansService;

        public TechniciansController(ITechniciansService techniciansService)
        {
            _techniciansService = techniciansService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAllTechnicians()
        {
            var result = await _techniciansService.GetTechniciansAsync();
            return Ok(result);
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAvailableTechnicians()
        {
            var result = await _techniciansService.GetAvailableTechniciansAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> AddTechnician(TechnicianCreateDto techniciansDto)
        {
            await _techniciansService.AddTechnicianAsync(techniciansDto);
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditTechnician(string id, TechniciansDto techniciansDto)
        {
            await _techniciansService.EditTechnicianAsync(id, techniciansDto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveTechnician(string id)
        {
            await _techniciansService.RemoveTechnicianAsync(id);
            return NoContent();
        }
    }
}
