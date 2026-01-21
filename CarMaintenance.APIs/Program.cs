using CarMaintenance.APIs.Extensions;
using CarMaintenance.APIs.Middlewares;
using CarMaintenance.Core.Domain.Contracts.Persistence;
using CarMaintenance.Core.Service;
using CarMaintenance.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace CarMaintenance.APIs
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            #region Configure Services [Container For DI]


            builder.Services.AddControllers()
                .AddApplicationPart(typeof(Controllers.AssemblyInformation).Assembly)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

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
                        Name = "Graduation FIXORA"
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


            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(allowedOrigins!) // من appsettings.json
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });


            //Extensions Services Layers 
            builder.Services.AddPersistenceServices(builder.Configuration);
            builder.Services.AddApplicationServices(builder.Configuration);

         
            // register for Identity [user manager]
            builder.Services.AddIdentityServices(builder.Configuration);


            #endregion


            var app = builder.Build();

            var scope = app.Services.CreateScope();
            var seed = scope.ServiceProvider.GetRequiredService<IDataSeeding>();
            await seed.DataSeedAsync();

            #region Configure Kestral Middlewares (Pipelines)

            app.UseMiddleware<ExceptionHandlerMiddleware>();

            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseCors("AllowFrontend"); 
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            #endregion

            app.Run();


        }
    }
}
