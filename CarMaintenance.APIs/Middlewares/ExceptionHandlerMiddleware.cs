using CarMaintenance.APIs.Controllers.Errors;
using CarMaintenance.Shared.Exceptions;
using System.Text.Json;

namespace CarMaintenance.APIs.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlerMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            ApiResponse response = ex switch
            {
                ValidationException validationEx => new ApiValidationErrorResponse(
                    validationEx.Message,
                    validationEx.Errors
                ),

                NotFoundException notFoundEx => new ApiResponse(
                    StatusCodes.Status404NotFound,
                    notFoundEx.Message
                ),

                UnauthorizedException unauthorizedEx => new ApiResponse(
                    StatusCodes.Status401Unauthorized,
                    unauthorizedEx.Message
                ),

                BadRequestException badRequestEx => new ApiResponse(
                    StatusCodes.Status400BadRequest,
                    badRequestEx.Message
                ),

                _ => _environment.IsDevelopment()
                    ? new ApiExceptionResponse(
                        StatusCodes.Status500InternalServerError,
                        ex.Message,
                        ex.StackTrace?.ToString()
                    )
                    : new ApiExceptionResponse(
                        StatusCodes.Status500InternalServerError,
                        "An internal server error occurred."
                    )
            };

            context.Response.StatusCode = response.StatusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, options)
            );
        }
    }
}