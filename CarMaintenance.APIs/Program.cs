using CarMaintenance.APIs.Extensions;
using CarMaintenance.APIs.Middlewares;
using CarMaintenance.Core.Service;
using CarMaintenance.Core.Service.Hubs;
using CarMaintenance.Infrastructure;
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

            #region Configure Services

            builder.Services.AddControllers()
                .AddApplicationPart(typeof(Controllers.AssemblyInformation).Assembly)
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Car Maintenance API",
                    Version = "v1",
                    Description = "Car Maintenance Platform - Graduation Project"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new List<string>()
                    }
                });
            });

            var allowedOrigins = builder.Configuration
                .GetSection("AllowedOrigins")
                .Get<string[]>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins!)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        // Required for SignalR WebSocket connections
                        .AllowCredentials();
                });
            });

            builder.Services.AddPersistenceServices(builder.Configuration);
            builder.Services.AddApplicationServices(builder.Configuration);
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddIdentityServices(builder.Configuration);

            //Register SignalR
            builder.Services.AddSignalR();

            #endregion

            var app = builder.Build();

            #region Database Initializer
            await app.InitializeDbContext();
            #endregion

            #region Middleware Pipeline

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

            //  Map the SignalR Hub endpoint
            app.MapHub<NotificationHub>("/hubs/notifications");

            #endregion

            app.Run();
        }
    }
}