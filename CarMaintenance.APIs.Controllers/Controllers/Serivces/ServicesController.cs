using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMaintenance.APIs.Controllers.Controllers.Serivces
{
    public class ServicesController(IServiceManager serviceManager) : BaseApiController
    {
        #region Public Endpoints

        [AllowAnonymous]
        [HttpGet] // GET: /api/Services?category=صيانة&sort=priceAsc&pageSize=10&pageIndex=1
        public async Task<ActionResult<Pagination<ServiceDto>>> GetServices([FromQuery] ServiceSpecParams specParams)
        {
            var result = await serviceManager.ServiceService.GetServicesAsync(specParams);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{id}")] // GET: /api/Services/1
        public async Task<ActionResult<ServiceDto>> GetService(int id)
        {
            var service = await serviceManager.ServiceService.GetServiceByIdAsync(id);
            return Ok(service);
        }

        #endregion

        #region Admin Only

        [Authorize(Roles = "Admin")]
        [HttpPost] // POST: /api/Services
        public async Task<ActionResult<ServiceDto>> CreateService(CreateServiceDto createDto)
        {
            var service = await serviceManager.ServiceService.CreateServiceAsync(createDto);
            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")] // PUT: /api/Services/1
        public async Task<ActionResult<ServiceDto>> UpdateService(int id, UpdateServiceDto updateDto)
        {
            updateDto.Id = id;

            var service = await serviceManager.ServiceService.UpdateServiceAsync(updateDto);
            return Ok(service);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")] // DELETE: /api/Services/1
        public async Task<ActionResult> DeleteService(int id)
        {
            await serviceManager.ServiceService.DeleteServiceAsync(id);
            return Ok();
        }

        #endregion
    }
}