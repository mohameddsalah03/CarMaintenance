using CarMaintenance.APIs.Extensions;
using CarMaintenance.APIs.Middlewares;
using CarMaintenance.Core.Service;
using CarMaintenance.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

namespace CarMaintenance.APIs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            #region Configure Services [Container For DI]


            builder.Services.AddControllers()
                .AddApplicationPart(typeof(Controllers.AssemblyInformation).Assembly); //Register Reqiured Services By ASP.NET Core Web APIs To DI Container 

            //Reauired Services For Swagger
            builder.Services.AddEndpointsApiExplorer();



            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Car Maintenance API",
                    Version = "v1",
                    Description = "Car Maintenance Platform - Graduation Project",
                    Contact = new OpenApiContact
                    {
                        Name = "Graduation FIX'EM"
                    }
                });

                // JWT Authentication in Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "https://your-frontend-url.com") // Frontend URLs
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); //Cookies
                });
            });


            //Extensions Services Layers 
            builder.Services.AddPersistenceServices(builder.Configuration);
            builder.Services.AddApplicationServices(builder.Configuration);

         
            // register for Identity [user manager]
            builder.Services.AddIdentityServices(builder.Configuration);


            #endregion


            var app = builder.Build();



            #region Configure Kestral Middlewares (Pipelines)

            app.UseMiddleware<ExceptionHandlerMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();


            app.UseCors("AllowFrontend"); 
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            #endregion

            app.Run();


        }
    }
}
