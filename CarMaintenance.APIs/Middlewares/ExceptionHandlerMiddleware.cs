using CarMaintenance.Shared.DTOs.Auth;
using CarMaintenance.Shared.Exceptions;

namespace CarMaintenance.APIs.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;

        public ExceptionHandlerMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
                await HandleNotFoundEndpointAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted) return;

            context.Response.Clear();
            context.Response.ContentType = "application/json";

            context.Response.StatusCode = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedException => StatusCodes.Status401Unauthorized,  
                ForbiddenException => StatusCodes.Status403Forbidden,    
                ValidationException ve => StatusCodes.Status400BadRequest,
                BadRequestException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError,
            };

            object response;

            if (ex is ValidationException validationEx)
            {
                response = new
                {
                    context.Response.StatusCode,
                    ErrorMessage = validationEx.Message,
                    validationEx.Errors
                };
            }
            else
            {
                response = new ErrorToReturn
                {
                    StatusCode = context.Response.StatusCode,
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message
                };
            }

            await context.Response.WriteAsJsonAsync(response);
        }

        private static async Task HandleNotFoundEndpointAsync(HttpContext context)
        {
            if (context.Response.HasStarted) return;

            var endpoint = context.GetEndpoint();
            if (endpoint is null && context.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                context.Response.ContentType = "application/json";
                var response = new ErrorToReturn
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = $"Endpoint '{context.Request.Path}' was not found."
                };
                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}