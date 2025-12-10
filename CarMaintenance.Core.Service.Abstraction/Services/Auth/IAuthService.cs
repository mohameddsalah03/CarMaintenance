using CarMaintenance.Shared.DTOs.Auth;

namespace CarMaintenance.Core.Service.Abstraction.Services.Auth
{
    public interface IAuthService
    {
        Task<UserDto> LoginAysnc(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> EmailExists(string email);
    }
}
