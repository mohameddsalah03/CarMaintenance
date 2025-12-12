using CarMaintenance.APIs.Controllers.Errors;
using CarMaintenance.Shared.Exceptions;

namespace CarMaintenance.APIs.Middlewares
{
    public class ExceptionHandlerMiddleware 
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlerMiddleware(
            RequestDelegate next ,
            ILogger<ExceptionHandlerMiddleware> logger,
            IWebHostEnvironment environment
            )
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }


        public async Task InvokeAsync(HttpContext context)
        {

            try
            {
                //Logic Will Be Executed For The Request 
                await _next(context); // Go To Next Middleware
                //Logic Will Be Executed For The Response
                //
            }
            catch (Exception ex)
            {

                #region Logging ToDo With Serial Package

                if (_environment.IsDevelopment())
                {
                    // Log In Console If Environmnet in development
                    _logger.LogError(ex, ex.Message, ex.StackTrace!.ToString());
                }
                else
                {
                    // Log Exception In External Resourece Like (Database) or File(text || json)

                }


                #endregion

                await HandleExceptionsAsync(context, ex);

            }
        }

        private async Task HandleExceptionsAsync(HttpContext context, Exception ex)
        {
            ApiResponse response;
            switch (ex)
            {
                case NotFoundException:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response = new ApiResponse(StatusCodes.Status404NotFound, ex.Message);
                    break;

                    /// 
                case ValidationException validationException:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response = new ApiValidationErrorResponse(ex.Message)
                    { Errors = (IEnumerable<ApiValidationErrorResponse.ValidationError>)validationException.Errors };
                    break;

                case BadRequestException:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response = new ApiResponse(StatusCodes.Status404NotFound, ex.Message);
                    break;

                case UnauthorizedException:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response = new ApiResponse(StatusCodes.Status401Unauthorized, ex.Message);
                    break;




                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response = _environment.IsDevelopment() ?
                        new ApiExceptionResponse(StatusCodes.Status500InternalServerError, ex.Message, ex.StackTrace!.ToString()) :
                        new ApiExceptionResponse(StatusCodes.Status500InternalServerError, ex.Message);
                    break;

            }

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
