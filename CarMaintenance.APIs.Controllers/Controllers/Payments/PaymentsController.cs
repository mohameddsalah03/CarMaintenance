using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services.Payments;
using CarMaintenance.Shared.DTOs.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarMaintenance.APIs.Controllers.Controllers.Payments
{
    public class PaymentsController(IPaymentService _paymentService) : BaseApiController
    {
        // POST: /api/payments/initiate
        [Authorize(Roles = "Customer")]
        [HttpPost("initiate")]
        public async Task<ActionResult<PaymentInitiatedDto>> InitiatePayment(
            [FromBody] InitiatePaymentDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _paymentService.InitiatePaymentAsync(dto, userId);
            return Ok(result);
        }

        // POST: /api/payments/callback
        // Paymob calls this — must be anonymous and always return 200
        [AllowAnonymous]
        [HttpPost("callback")]
        public async Task<ActionResult> HandleCallback()
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();
            var hmac = Request.Query["hmac"].ToString();

            await _paymentService.HandleCallbackAsync(rawBody, hmac);

            // Paymob expects 200 always
            return Ok();
        }
    }
}