using CarMaintenance.Core.Domain.Models.Identity;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Auth.Email;
using CarMaintenance.Shared.DTOs.Auth;
using CarMaintenance.Shared.Exceptions;
using CarMaintenance.Shared.Settings;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ValidationException = CarMaintenance.Shared.Exceptions.ValidationException;

namespace CarMaintenance.Core.Service.Services.Auth
{
    public class AuthService(
        UserManager<ApplicationUser> _userManager ,
        SignInManager<ApplicationUser> _signInManager,
        IOptions<JwtSettings> _jwtSettings,
        IEmailService _emailService
        ) : IAuthService
    {
        private readonly JwtSettings _jwtSettings = _jwtSettings.Value;
      
        public async Task<UserDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user is null) throw new UnauthorizedException("Invalid Login");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password ,lockoutOnFailure:true);

            if (result.IsNotAllowed) throw new UnauthorizedException("Account Not Confirmed Yet.");
            if (result.IsLockedOut) throw new UnauthorizedException("Account Is Locked.");
            if (!result.Succeeded) throw new UnauthorizedException("Invalid Login."); // Must in Last

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

            var result = await _userManager.CreateAsync(user , registerDto.Password);

            if(!result.Succeeded) throw new ValidationException() 
            {
                Errors = result.Errors.Select(E=>E.Description)
            };

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
            => await _userManager.FindByEmailAsync(email!) is not null;


        private async Task<string> GenerateTokenAsync(ApplicationUser user)
        {

            List<Claim> ClaimList = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!)
            };
            
            var roles = await _userManager.GetRolesAsync(user);
            
            foreach (var role in roles)
                ClaimList.Add(new Claim(ClaimTypes.Role, role));
            
            var secretKey = _jwtSettings.Key;
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var signInCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            
            var tokenObj = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(30),
                claims: ClaimList,
                signingCredentials: signInCredentials
                );
            
            return new JwtSecurityTokenHandler().WriteToken(tokenObj);

        }

        public async Task<UserDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.IdToken);

                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user is null)
                {
                    
                    user = new ApplicationUser
                    {
                        DisplayName = payload.Name,
                        Email = payload.Email,
                        UserName = payload.Email,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        throw new ValidationException()
                        {
                            Errors = createResult.Errors.Select(e => e.Description)
                        };
                }

                return new UserDto()
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email!,
                    Token = await GenerateTokenAsync(user),
                };
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedException("Invalid Google token");
            }
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user is null)
                return false; // لا نخبر المستخدم أن البريد غير موجود لأسباب أمنية

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // رابط إعادة تعيين كلمة المرور (يمكنك تخصيصه حسب الـ Frontend)
            var resetUrl = $"https://your-frontend-url/reset-password?email={user.Email}&token={encodedToken}";

            var emailBody = $@"
                <h2>إعادة تعيين كلمة المرور</h2>
                <p>مرحباً {user.DisplayName},</p>
                <p>لقد طلبت إعادة تعيين كلمة المرور الخاصة بك.</p>
                <p>يرجى الضغط على الرابط التالي لإعادة تعيين كلمة المرور:</p>
                <a href='{resetUrl}'>إعادة تعيين كلمة المرور</a>
                <p>إذا لم تطلب ذلك، يرجى تجاهل هذا البريد.</p>
            ";

            await _emailService.SendEmailAsync(user.Email!, "إعادة تعيين كلمة المرور", emailBody);

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user is null)
                throw new BadRequestException("Invalid request");

            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetPasswordDto.Token));

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);

            if (!result.Succeeded)
                throw new ValidationException()
                {
                    Errors = result.Errors.Select(e => e.Description)
                };

            return true;
        }
    }
}
