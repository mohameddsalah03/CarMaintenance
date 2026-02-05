using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Domain.Models.Data;
using CarMaintenance.Core.Service.Abstraction.Services.Auth;
using CarMaintenance.Core.Service.Abstraction.Services.Auth.Email;
using CarMaintenance.Shared.DTOs.Auth;
using CarMaintenance.Shared.DTOs.Technicians;
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
        IEmailService _emailService,
        IOptions<AppSettings> _appSettings ,
        IUnitOfWork _unitOfWork
        ) : IAuthService
    {
        private readonly JwtSettings _jwtSettings = _jwtSettings.Value;
        private readonly AppSettings _appSettings = _appSettings.Value;

      
        public async Task<UserDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user is null) throw new UnauthorizedException("Invalid Login");

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password ,lockoutOnFailure:true);

            if (result.IsNotAllowed) throw new UnauthorizedException("Account Not Confirmed Yet.");
            if (result.IsLockedOut) throw new UnauthorizedException("Account Is Locked.");
            if (!result.Succeeded) throw new UnauthorizedException("Invalid Login."); // Must in Last
            
            // Generate both tokens
            var (accessToken, refreshToken) = await GenerateTokensAsync(user);

            var response = new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = accessToken, // Access Token
                RefreshToken = refreshToken, // Refresh Token
                TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes)
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

            if (!result.Succeeded)
            {
                throw new ValidationException(
                    "Registration failed",
                    result.Errors.Select(e => e.Description)
                );
            }
            // adding role for customer after register 
            await _userManager.AddToRoleAsync(user, "Customer");

            //  Generate both tokens
            var (accessToken, refreshToken) = await GenerateTokensAsync(user);

            var response = new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = accessToken,
                RefreshToken = refreshToken,
                TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes)
            };
            return response;    
        }

       
        public async Task<bool> EmailExists(string email)
            => await _userManager.FindByEmailAsync(email!) is not null;

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
                    {
                        throw new ValidationException(
                            "Failed to create user account",
                            createResult.Errors.Select(e => e.Description)
                        );
                    }
                }

                // adding role for customer after register 
                await _userManager.AddToRoleAsync(user, "Customer");

                //  Generate both tokens
                var (accessToken, refreshToken) = await GenerateTokensAsync(user);

                return new UserDto()
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email!,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes)
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

            //  استخدم TrimEnd عشان تتأكد مفيش مشاكل
            var resetUrl = $"{_appSettings.FrontendUrl.TrimEnd('/')}/reset-password?email={user.Email}&token={encodedToken}";

            // للتأكد (اختياري - شيله بعد التيست)
            //Console.WriteLine($"Reset URL: {resetUrl}");

            var emailBody = $@"
                             <h2>إعادة تعيين كلمة المرور</h2>
                             <p>مرحباً {user.DisplayName},</p>
                             <p>لقد طلبت إعادة تعيين كلمة المرور الخاصة بك.</p>
                             <p>يرجى الضغط على الرابط التالي لإعادة تعيين كلمة المرور:</p>
                             <a href='{resetUrl}'>إعادة تعيين كلمة المرور</a>
                             <p>هذا الرابط صالح لمدة ساعة واحدة فقط.</p>
                            <p>إذا لم تطلب ذلك، يرجى تجاهل هذا البريد.</p>
                        ";

            await _emailService.SendEmailAsync(user.Email!, "إعادة تعيين كلمة المرور", emailBody);
        }
        public async Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user is null)
                throw new BadRequestException("Invalid request");

            try
            {
                var decodedToken = Encoding.UTF8.GetString(
                    WebEncoders.Base64UrlDecode(resetPasswordDto.Token));

                var result = await _userManager.ResetPasswordAsync(
                    user, decodedToken, resetPasswordDto.NewPassword);

                if (!result.Succeeded)
                {
                    throw new ValidationException(
                        "Failed to reset password",
                        result.Errors.Select(e => e.Description)
                    );
                }
            }
            catch (FormatException)
            {
                throw new BadRequestException("Invalid or expired token");
            }
        }

        #region Helper Methods 

        //  إضافة Method لتوليد Refresh Token
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }


        //  تعديل GenerateTokenAsync عشان ترجع Access Token + Refresh Token
        private async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user)
        {
            // Generate Access Token (نفس الكود القديم)
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
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes), // 15 دقيقة
                claims: ClaimList,
                signingCredentials: signInCredentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(tokenObj);

            // Generate Refresh Token
            var refreshToken = GenerateRefreshToken();

            // Save Refresh Token in Database
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDurationInDays); // 7 أيام
            await _userManager.UpdateAsync(user);

            return (accessToken, refreshToken);
        }



        #endregion

        public async Task<UserDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            // 1. Validate Access Token (بدون التحقق من Expiry)
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(refreshTokenDto.Token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false, // ✅ مش مهم لو منتهي
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // 2. Extract user email from token
            var userEmail = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                throw new UnauthorizedException("Invalid token");

            // 3. Get user from database
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user is null)
                throw new UnauthorizedException("Invalid token");

            // 4. Validate Refresh Token
            if (user.RefreshToken != refreshTokenDto.RefreshToken)
                throw new UnauthorizedException("Invalid refresh token");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedException("Refresh token expired");

            // 5. Generate new tokens
            var (newAccessToken, newRefreshToken) = await GenerateTokensAsync(user);

            return new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes)
            };
        }

        public async Task<UserDto> CreateTechnicianAsync(CreateTechnicianDto technicianDto)
        {
            // 1. Create User Account
            var user = new ApplicationUser()
            {
                DisplayName = technicianDto.DisplayName,
                Email = technicianDto.Email,
                UserName = technicianDto.UserName,
                PhoneNumber = technicianDto.PhoneNumber,
            };

            var result = await _userManager.CreateAsync(user, technicianDto.Password);

            if (!result.Succeeded)
            {
                throw new ValidationException(
                    "Failed to create technician account",
                    result.Errors.Select(e => e.Description)
                );
            }

            // 2. Assign Technician Role
            await _userManager.AddToRoleAsync(user, "Technician");
            
            // 3. Create Technician Record
            var technician = new Technician()
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Specialization = technicianDto.Specialization,
                Rating = 0,
                IsAvailable = true
            };

            await _unitOfWork.GetRepo<Technician, string>().AddAsync(technician);
            await _unitOfWork.SaveChangesAsync();

            // 4. Send Email with Login Credentials
            var emailBody = $@"
                <h2>مرحباً {user.DisplayName}</h2>
                <p>تم إنشاء حساب فني صيانة لك في نظام إدارة صيانة السيارات.</p>
                <h3>بيانات الدخول:</h3>
                <p><strong>البريد الإلكتروني:</strong> {user.Email}</p>
                <p><strong>اسم المستخدم:</strong> {user.UserName}</p>
                <p><strong>كلمة المرور:</strong> {technicianDto.Password}</p>
                <p><strong>التخصص:</strong> {technicianDto.Specialization}</p>
                <br>
                <p>يرجى تسجيل الدخول وتغيير كلمة المرور من الإعدادات.</p>
                <p>رابط تسجيل الدخول: {_appSettings.FrontendUrl}/login</p>
            ";

            await _emailService.SendEmailAsync(user.Email!, "حساب فني صيانة جديد", emailBody);
            
            // 5. Generate Tokens
            var (accessToken, refreshToken) = await GenerateTokensAsync(user);

            return new UserDto()
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email!,
                Token = accessToken,
                RefreshToken = refreshToken,
                TokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes)
            };


        }
    }
}
