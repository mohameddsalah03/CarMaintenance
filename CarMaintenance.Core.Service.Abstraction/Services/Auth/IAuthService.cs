using CarMaintenance.Shared.DTOs.Auth;

namespace CarMaintenance.Core.Service.Abstraction.Services.Auth
{
    public interface IAuthService
    {
        


        Task<UserDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> EmailExists(string email);
        Task<UserDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto);
        Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);

    }
}
