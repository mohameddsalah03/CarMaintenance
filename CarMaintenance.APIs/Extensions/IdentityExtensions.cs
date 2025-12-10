using CarMaintenance.Core.Domain.Models.Identity;
using CarMaintenance.Infrastructure.Persistence.Identity;
using CarMaintenance.Shared.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CarMaintenance.APIs.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<JwtSettings>(configuration.GetSection("jwtSttings"));

            services.AddIdentity<ApplicationUser, IdentityRole>(identityOptions =>
            {
                identityOptions.User.RequireUniqueEmail = true;
                identityOptions.Password.RequireNonAlphanumeric = true;
                identityOptions.Password.RequiredUniqueChars = 1;
                identityOptions.Password.RequiredLength = 6;
                identityOptions.Password.RequireUppercase = true;
                identityOptions.Password.RequireLowercase = true;
                identityOptions.Password.RequireDigit = true;
                identityOptions.Lockout.MaxFailedAccessAttempts = 5;
                identityOptions.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<CarIdentityDbContext>()
            .AddDefaultTokenProviders();


            var authBuilder = services.AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authenticationOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            authBuilder.AddJwtBearer(configureOptions =>
            {
                configureOptions.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = configuration["jwtSttings:Issuer"],
                    ValidAudience = configuration["jwtSttings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["jwtSttings:Key"]!))
                };
            });

            authBuilder.AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = configuration["Authentication:Google:ClientId"]!;
                googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
            });

            return services;
        }
    }
}