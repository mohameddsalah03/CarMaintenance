namespace CarMaintenance.APIs.Controllers.Errors
{
    public class ApiValidationErrorResponse : ApiResponse
    {
        public IEnumerable<string> Errors { get; set; }

        public ApiValidationErrorResponse()
            : base(400, "One or more validation errors occurred.")
        {
            Errors = Enumerable.Empty<string>();
        }

        public ApiValidationErrorResponse(IEnumerable<string> errors)
            : base(400, "One or more validation errors occurred.")
        {
            Errors = errors;
        }

        public ApiValidationErrorResponse(string message, IEnumerable<string> errors)
            : base(400, message)
        {
            Errors = errors;
        }
    }
}