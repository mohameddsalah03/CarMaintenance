using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services;
using CarMaintenance.Shared.DTOs.Common;
using CarMaintenance.Shared.DTOs.Services;
using CarMaintenance.Shared.DTOs.Services.AnalyzeProblem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarMaintenance.APIs.Controllers.Controllers.Services
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
        [HttpGet("{id:int}")] // GET: /api/Services/1
        public async Task<ActionResult<ServiceDto>> GetService(int id)
        {
            var service = await serviceManager.ServiceService.GetServiceByIdAsync(id);
            return Ok(service);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}/details")] // GET: /api/Services/1/details
        public async Task<ActionResult<ServiceDetailsDto>> GetServiceDetails(int id)
        {
            var serviceDetails = await serviceManager.ServiceService.GetServiceDetailsAsync(id);
            return Ok(serviceDetails);
        }


        #endregion

        #region Admin Only

        [Authorize(Roles = "Admin")]
        [HttpPost] // POST: /api/Services
        public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceDto createDto)
        {
            var service = await serviceManager.ServiceService.CreateServiceAsync(createDto);
            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")] // PUT: /api/Services/1
        public async Task<ActionResult<ServiceDto>> UpdateService(
             int id,
            [FromBody] UpdateServiceDto updateDto)
        {
            updateDto.Id = id;
            var service = await serviceManager.ServiceService.UpdateServiceAsync(updateDto);
            return Ok(service);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")] // DELETE: /api/Services/1
        public async Task<ActionResult> DeleteService( int id)
        {
            await serviceManager.ServiceService.DeleteServiceAsync(id);
            return NoContent();
        }

        #endregion


        [AllowAnonymous]
        [HttpPost("analyze-problem")]
        public async Task<ActionResult<AnalyzeProblemResponseDto>> AnalyzeProblem([FromBody] AnalyzeProblemRequestDto requestDto)
        {
            // Extract userId only if a valid JWT is present — null is safe to pass
            var userId = User.Identity?.IsAuthenticated == true? User.FindFirstValue(ClaimTypes.NameIdentifier): null;

            var result = await serviceManager.ServiceService.AnalyzeProblemAsync(requestDto, userId);
            return Ok(result);
        }
    }
}