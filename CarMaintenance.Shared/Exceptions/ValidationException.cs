namespace CarMaintenance.Shared.Exceptions
{
    public class ValidationException : BadRequestException
    {
        public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();

        public ValidationException()
            : base("One or more validation errors occurred.")
        {
        }

        public ValidationException(string message)
            : base(message)
        {
        }

        public ValidationException(IEnumerable<string> errors)
            : base("One or more validation errors occurred.")
        {
            Errors = errors;
        }

        public ValidationException(string message, IEnumerable<string> errors)
            : base(message)
        {
            Errors = errors;
        }
    }
}