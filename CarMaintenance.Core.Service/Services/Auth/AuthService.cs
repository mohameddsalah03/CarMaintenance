using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Auth.Email;
using CarMaintenance.Shared.DTOs.Auth;
using CarMaintenance.Shared.Exceptions;
using CarMaintenance.Shared.Settings;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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
        IEmailService _emailService,
        IOptions<AppSettings> _appSettings 
        ) : IAuthService
    {
        private readonly JwtSettings _jwtSettings = _jwtSettings.Value;
        private readonly AppSettings _appSettings = _appSettings.Value;

        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            var displayNameTaken = await _userManager.Users.AnyAsync(u => u.DisplayName == registerDto.DisplayName);
            if (displayNameTaken)
                throw new BadRequestException($"Display name '{registerDto.DisplayName}' is already taken");

            var phoneTaken = await _userManager.Users.AnyAsync(u => u.PhoneNumber == registerDto.PhoneNumber);
            if (phoneTaken)
                throw new BadRequestException(
                    $"Phone Number '{registerDto.PhoneNumber}' is already taken");

            var userName = GenerateUserName(registerDto.Email); 
            var user = new ApplicationUser()
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = userName,
                PhoneNumber = registerDto.PhoneNumber,
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
                throw new ValidationException("Registration failed", result.Errors.Select(e => e.Description));
            

            //  Adding role for customer after register 
            await _userManager.AddToRoleAsync(user, "Customer");

            //  Generate both tokens
            var (accessToken, refreshToken) = await GenerateTokensAsync(user, rememberMe: false);

            var roles = await _userManager.GetRolesAsync(user);

            var response = new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = accessToken,
                RefreshToken = refreshToken,
                TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                RememberMe = false,
                Roles = roles 
            };

            return response;
        }

        public async Task<UserDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user is null) throw new UnauthorizedException("Invalid email or password");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true); // 5 to MaxFailedAccessAttempts

            if (result.IsNotAllowed) throw new UnauthorizedException("Account Not Confirmed Yet");
            if (result.IsLockedOut) throw new UnauthorizedException("Account Is Locked");
            if (!result.Succeeded) throw new UnauthorizedException("Invalid Login");

            //  Generate both tokens
            var (accessToken, refreshToken) = await GenerateTokensAsync(user , loginDto.RememberMe);

            var roles = await _userManager.GetRolesAsync(user);

            var response = new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = accessToken,
                RefreshToken = refreshToken,
                TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                RememberMe = loginDto.RememberMe,
                Roles = roles
            };

            return response;
        }

        public async Task<bool> EmailExists(string email)
            => await _userManager.FindByEmailAsync(email!) is not null;

        public async Task<UserDto> GoogleLoginAsync(GoogleLoginDto googleLoginDto)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDto.IdToken); // return info of user from this Idtoken 

                var user = await _userManager.FindByEmailAsync(payload.Email);

                if (user is null)
                {
                    var displayNameTaken = await _userManager.Users.AnyAsync(u => u.DisplayName == payload.Name);
                    // For Google sign-in, append a short suffix if the name is taken
                    var displayName = displayNameTaken? $"{payload.Name} {Guid.NewGuid().ToString("N")[..4]}" : payload.Name;
                   
                    var userName = GenerateUserName(payload.Email);

                    user = new ApplicationUser
                    {
                        DisplayName = payload.Name,
                        Email = payload.Email,
                        UserName = userName,      
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                        throw new ValidationException( "Failed to create user account",createResult.Errors.Select(e => e.Description));
                    

                    await _userManager.AddToRoleAsync(user, "Customer");
                }

                var (accessToken, refreshToken) = await GenerateTokensAsync(user , rememberMe: true);
                var roles = await _userManager.GetRolesAsync(user);
                // note : not create password here cause we use google

                return new UserDto
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email!,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                    RememberMe = true,
                    Roles = roles
                };
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedException("Invalid Google token");
            }
        }
        
        public async Task ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user is null)
                throw  new UnauthorizedException("This User is not Registered");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetUrl = $"{_appSettings.FrontendUrl.TrimEnd('/')}/reset-password?email={user.Email}&token={encodedToken}";


            var emailBody = $@"
                   <h2>Reset Your Password</h2>
                   <p>Hello {user.DisplayName},</p>
                   <p>You have requested to reset your password.</p>
                   <p>Please click the link below to set a new password:</p>
                   <a href='{resetUrl}'>Reset Password</a>
                   <p>This link is valid for one hour only.</p>
                   <p>If you did not request this, please ignore this email.</p>
              ";

            await _emailService.SendEmailAsync(user.Email!, "Reset Password", emailBody);
        }
        public async Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user is null)
                throw new BadRequestException("Invalid request");

            try
            {
                var decodedToken = Encoding.UTF8.GetString( WebEncoders.Base64UrlDecode(resetPasswordDto.Token));
                var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
                if (!result.Succeeded)
                    throw new ValidationException( "Failed to reset password",result.Errors.Select(e => e.Description));
            }
            catch (FormatException)
            {
                throw new BadRequestException("Invalid or expired token");
            }
        }

        public async Task<UserDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            // 1. Parse the expired Access Token to extract the User Email
            var tokenHandler = new JwtSecurityTokenHandler(); 
            var principal = tokenHandler.ValidateToken(refreshTokenDto.Token,new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = false,   
                    ClockSkew = TimeSpan.Zero
                },
             out SecurityToken validatedToken);

            // 2. extract email from claims
            var userEmail = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                throw new UnauthorizedException("Invalid token");

            // 3. Retrieve User from Database
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user is null)
                throw new UnauthorizedException("Invalid token");

            if (user.RefreshToken != refreshTokenDto.RefreshToken)
                throw new UnauthorizedException("Invalid Refresh token");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedException("Session expired, please login again");

            var isRememberMe = user.RefreshTokenExpiryTime > DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenShortDurationInDays);

            var (newAccessToken, newRefreshToken) = await GenerateTokensAsync(user, isRememberMe);

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                RememberMe = isRememberMe,
                Roles = roles
            };
        }

        #region Helper Methods 

        private async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user, bool rememberMe)
        {
            // steps to fill jwtToken
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name,user.UserName!),
                new(ClaimTypes.Email,user.Email!)
            };
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var signingCreds = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256);

            // now we can fill jwtToken 
            var jwtToken = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes),
                claims: claims,
                signingCredentials: signingCreds);

            // 1- Access Token 
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken); // Convert jwtToken json To string

            // 2- Refresh Token
            var refreshToken = GenerateRefreshToken();

            //  refreshDuration based on remember me
            var refreshDuration = rememberMe  ? _jwtSettings.RefreshTokenDurationInDays :_jwtSettings.RefreshTokenShortDurationInDays; 

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshDuration);
            await _userManager.UpdateAsync(user);

            return (accessToken, refreshToken);
        }
        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        // "ahmed@gmail.com" -> "ahmed_a3f9c2"
        private static string GenerateUserName(string email)
        {
            var localPart = email.Split('@')[0];
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..6];
            return $"{localPart}_{uniqueSuffix}".ToLower();
        }

        #endregion



    }
}
