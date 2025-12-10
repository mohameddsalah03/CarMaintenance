using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CarMaintenance.APIs.Controllers.Controllers.Account
{
    public class AccountController(IAuthService _authService) : BaseApiController
    {

        [HttpPost("login")] //Post: /api/account/login
        public async Task<ActionResult<UserDto>> Login(LoginDto model)
        {
            var user = await _authService.LoginAysnc(model);
            return Ok(user);
        }

        [HttpPost("register")] //Post: /api/account/register

        public async Task<ActionResult<UserDto>> Register(RegisterDto model)
        {
            var user = await _authService.RegisterAsync(model);
            return Ok(user);
        }
       
        
        [HttpGet("emailexists")] //Put: /api/account/emailexists?email= ahmed.gmail.com
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            var result = await _authService.EmailExists(email);
            return Ok(result);
        }
       


    }
}
