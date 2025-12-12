namespace CarMaintenance.APIs.Controllers.Errors
{
    public class ApiValidationErrorResponse : ApiResponse
    {

        public required IEnumerable<ValidationError>  Errors { get; set; }
        public ApiValidationErrorResponse(string? message = null)
            : base(400 , message)
        {
            
        }

        public class ValidationError
        {
            public required string Field { get; set; }
            public required IEnumerable<string> Errors { get; set; }
        }

    }
}
