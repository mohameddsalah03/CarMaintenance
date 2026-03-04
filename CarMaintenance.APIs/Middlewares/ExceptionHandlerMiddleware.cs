using CarMaintenance.Shared.DTOs.Auth;
using CarMaintenance.Shared.Exceptions;


namespace CarMaintenance.APIs.Middlewares
{
    public class ExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
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
                _logger.LogError(ex, "Unhandled exception caught by middleware");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";



            context.Response.StatusCode = ex switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedException => StatusCodes.Status403Forbidden,
                BadRequestException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError,
            };

            var response = new ErrorToReturn
            {
                StatusCode = context.Response.StatusCode,
                ErrorMessage = ex.Message
            };

            await context.Response.WriteAsJsonAsync(response);
        }

        private static async Task HandleNotFoundEndpointAsync(HttpContext context)
        {
            if (context.Response.HasStarted) return;

            var endpoint = context.GetEndpoint();
            if (endpoint == null && context.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                context.Response.ContentType = "application/json";
                var response = new ErrorToReturn
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = $"End Point {context.Request.Path} is Not Found"
                };

                await context.Response.WriteAsJsonAsync(response);
                return;


            }

            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                context.Response.ContentType = "application/json";
                var response = new ErrorToReturn
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = $"End Point {context.Request.Path} is Not Found"
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}