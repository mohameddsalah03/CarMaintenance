using CarMaintenance.APIs.Extensions;
using CarMaintenance.APIs.Middlewares;
using CarMaintenance.Core.Service;
using CarMaintenance.Infrastructure.Persistence;

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
            builder.Services.AddSwaggerGen();

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
            
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            #endregion

            app.Run();


        }
    }
}
