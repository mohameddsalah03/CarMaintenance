using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Technicians;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMaintenance.APIs.Controllers.Controllers.Technicians
{
    public class TechniciansController(IServiceManager _serviceManager) : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAllTechnicians()
        {
            var result = await _serviceManager.TechniciansService.GetTechniciansAsync();
            return Ok(result);
        }

        [HttpGet("Available")]
        public async Task<ActionResult<IEnumerable<TechniciansDto>>> GetAvailableTechnicians()
        {
            var result = await _serviceManager.TechniciansService.GetAvailableTechniciansAsync();
            return Ok(result);
        }

        //[HttpPost]
        //public async Task<ActionResult<TechniciansDto>> AddTechnician(TechnicianCreateDto techniciansDto)
        //{
        //    var result = await _serviceManager.TechniciansService.AddTechniciansAsync(techniciansDto);
        //    return Ok(result);
        //}

        [HttpPut("{id}")]
        public async Task<ActionResult<TechniciansDto>> EditTechnician(string id, TechnicianUpdateDto techniciansDto)
        {
            var result = await _serviceManager.TechniciansService.EditTechniciansAsync(id, techniciansDto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveTechnician(string id)
        {
            await _serviceManager.TechniciansService.RemoveTechnicianAsync(id);
            return Ok();
        }
    }
}