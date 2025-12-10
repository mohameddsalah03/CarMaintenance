using CarMaintenance.Core.Domain.Models.Identity;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Shared.DTOs.Auth;
using CarMaintenance.Shared.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarMaintenance.Core.Service.Services.Auth
{
    public class AuthService(
        UserManager<ApplicationUser> userManager ,
        SignInManager<ApplicationUser> signInManager,
        IOptions<JwtSettings> jwtSettings
        ) : IAuthService
    {
        private readonly JwtSettings _jwtSettings = jwtSettings.Value;
      
        public async Task<UserDto> LoginAysnc(LoginDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            //if (user is null) throw new UnauthorizedException("Invalid Login");

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password ,lockoutOnFailure:true);
            
            //if(result.IsNotAllowed) throw new UnauthorizedException("Account Not Confirmed Yet.");
            //if(result.IsLockedOut) throw new UnauthorizedException("Account Is Locked.");
            //if(!result.Succeeded) throw new UnauthorizedException("Invalid Login."); // Must in Last

            var response = new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token =await GenerateTokenAsync(user),
            };

            return response;

        }

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            /// don't need this because called the default in identity extensions [identityOptions.User.RequireUniqueEmail = true;]
            /// if (EmailExists(registerDto.Email).Result) 
            ///    throw new BadRequestException("This Email Is Already in user"); 

            var user = new ApplicationUser()
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.UserName,  
                PhoneNumber = registerDto.PhoneNumber,
            };

            var result = await userManager.CreateAsync(user , registerDto.Password);

            //if(!result.Succeeded) throw new ValidationException() { Errors = result.Errors.Select(E=>E.Description) };

            var response = new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = await GenerateTokenAsync(user),
            };
            
            return response;    
        }

       
        public async Task<bool> EmailExists(string email)
            => await userManager.FindByEmailAsync(email!) is not null;
        
        private async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var userCliams = await userManager.GetClaimsAsync(user);
            
            var rolesAsCliams = new List<Claim>();
            var rolesClaims = await userManager.GetRolesAsync(user);
            foreach (var role in rolesClaims) 
                rolesAsCliams.Add(new Claim(ClaimTypes.Role,role.ToString()));   

            var authClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.PrimarySid, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.GivenName, user.DisplayName),
            }
            .Union(userCliams)
            .Union(rolesAsCliams);
            ;

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var signInCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var tokenObj = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                claims: authClaims,
                signingCredentials: signInCredentials
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenObj);  
        }

        public Task<UserDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            throw new NotImplementedException();
        }
    }
}
