using CarMaintenance.Core.Domain.Models.Identity;
using CarMaintenance.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace CarMaintenance.APIs.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services , IConfiguration configuration)
        {
            //services.Configure<JwtSettings>(configuration.GetSection("jwtSttings"));

             services.AddIdentity<ApplicationUser, IdentityRole>(identityOptions =>
             {
                    // For User
                    identityOptions.User.RequireUniqueEmail = true;

                    // For SignIn
                    // identityOptions.SignIn.RequireConfirmedEmail = true;
                    // identityOptions.SignIn.RequireConfirmedAccount = true;
                    // identityOptions.SignIn.RequireConfirmedPhoneNumber = true;

                    // For Password
                    // identityOptions.Password.RequireNonAlphanumeric = true;
                    // identityOptions.Password.RequiredUniqueChars = 2;
                    // identityOptions.Password.RequiredLength= 6;
                    // identityOptions.Password.RequireUppercase = true;
                    // identityOptions.Password.RequireLowercase = true;
                    // identityOptions.Password.RequireDigit = true;
             })
               .AddEntityFrameworkStores<CarIdentityDbContext>();



            ////For Authorize
            //services.AddAuthentication(authenticationOptions =>
            //{
            //    authenticationOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    authenticationOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //})
            //    .AddJwtBearer(configureOptions =>
            //    {
            //        configureOptions.TokenValidationParameters = new TokenValidationParameters()
            //        {
            //            ValidateAudience = true,
            //            ValidateIssuer = true,
            //            ValidateIssuerSigningKey = true,
            //            ValidateLifetime = true,

            //            ClockSkew = TimeSpan.FromMinutes(0), // Delay
            //            ValidIssuer = configuration["jwtSttings:Issuer"],
            //            ValidAudience = configuration["jwtSttings:Audience"],
            //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["jwtSttings:Key"]!))

            //        };


            //    });
                            
            return services;    
        }
    }
}
