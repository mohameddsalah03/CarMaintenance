using CarMaintenance.APIs.Controllers.Controllers.Base;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CarMaintenance.APIs.Controllers.Controllers.Account
{
    public class AccountController(IAuthService _authService) : BaseApiController
    {
        [AllowAnonymous]
        [HttpPost("Login")] //Post: /api/account/Login
        public async Task<ActionResult<UserDto>> Login(LoginDto model)
        {
            var user = await _authService.LoginAsync(model);
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("Register")] //Post: /api/account/Register
        public async Task<ActionResult<UserDto>> Register(RegisterDto model)
        {
            var user = await _authService.RegisterAsync(model);
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpGet("EmailExists")] //Get: /api/account/EmailExists?email= ahmed.gmail.com
        public async Task<ActionResult<bool>> CheckEmailExists(string email)
        {
            var result = await _authService.EmailExists(email);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("google-login")]
        public async Task<ActionResult<UserDto>> GoogleLogin(GoogleLoginDto model)
        {
            var user = await _authService.GoogleLoginAsync(model);
            return Ok(user);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            await _authService.ForgotPasswordAsync(model);
            return Ok(new { message = "إذا كان البريد الإلكتروني موجوداً، سيتم إرسال رابط إعادة تعيين كلمة المرور" });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword(ResetPasswordDto model)
        {
            await _authService.ResetPasswordAsync(model);
            return Ok(new { message = "تم إعادة تعيين كلمة المرور بنجاح" });
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<UserDto>> RefreshToken(RefreshTokenDto model)
        {
            var user = await _authService.RefreshTokenAsync(model);
            return Ok(user);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("create-technician")] // POST: /api/Account/create-technician
        public async Task<ActionResult<UserDto>> CreateTechnician(CreateTechnicianDto model)
        {
            var technician = await _authService.CreateTechnicianAsync(model);
            return Ok(new
            {
                message = "تم إنشاء حساب الفني بنجاح وإرسال بيانات الدخول عبر البريد الإلكتروني",
                data = technician
            });
        }

    }
}