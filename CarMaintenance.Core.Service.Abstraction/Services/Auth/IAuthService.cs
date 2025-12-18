using CarMaintenance.Shared.DTOs.Auth;

namespace CarMaintenance.Core.Service.Abstraction.Services.Auth
{
    public interface IAuthService
    {
        Task<UserDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> EmailExists(string email);
        Task<UserDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto);
        Task ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto);


        // ✅ إضافة Refresh Token method
        Task<UserDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);

    }
}
