using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarMaintenance.APIs.Controllers.Controllers.Services
{
    public class ServicesController(IServiceManager serviceManager) : BaseApiController
    {
        #region Public Endpoints

        // Get all services with filters and pagination
        [AllowAnonymous]
        [HttpGet] // GET: /api/Services?category=صيانة&sort=priceAsc&pageSize=10&pageIndex=1
        public async Task<ActionResult<Pagination<ServiceDto>>> GetServices([FromQuery] ServiceSpecParams specParams)
        {
            var result = await serviceManager.ServiceService.GetServicesAsync(specParams);
            return Ok(result);
        }

        // Get service by ID
        [AllowAnonymous]
        [HttpGet("{id}")] // GET: /api/Services/1
        public async Task<ActionResult<ServiceDto>> GetService(int id)
        {
            var service = await serviceManager.ServiceService.GetServiceByIdAsync(id);
            return Ok(service);
        }

        //  Get service details with technicians
        [AllowAnonymous]
        [HttpGet("{id}/details")] // GET: /api/Services/1/details
        public async Task<ActionResult<ServiceDetailsDto>> GetServiceDetails(int id)
        {
            var serviceDetails = await serviceManager.ServiceService.GetServiceDetailsAsync(id);
            return Ok(serviceDetails);
        }


        #endregion

        #region Admin Only

        // Admin Only - Create new service
        [Authorize(Roles = "Admin")]
        [HttpPost] // POST: /api/Services
        public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceDto createDto)
        {
            var service = await serviceManager.ServiceService.CreateServiceAsync(createDto);
            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
        }

        // Admin Only - Update service
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")] // PUT: /api/Services/1
        public async Task<ActionResult<ServiceDto>> UpdateService(
             int id,
            [FromBody] UpdateServiceDto updateDto)
        {
            updateDto.Id = id;
            var service = await serviceManager.ServiceService.UpdateServiceAsync(updateDto);
            return Ok(service);
        }

        // Admin Only - Delete service
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")] // DELETE: /api/Services/1
        public async Task<ActionResult> DeleteService( int id)
        {
            await serviceManager.ServiceService.DeleteServiceAsync(id);
            return NoContent();
        }

        #endregion
    }
}