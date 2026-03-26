namespace CarMaintenance.Shared.Exceptions
{
    public class BadRequestException : ApplicationException
    {
        public BadRequestException()
            : base("Bad request.")
        {
        }

        public BadRequestException(string message)
            : base(message)
        {
        }
    }
}