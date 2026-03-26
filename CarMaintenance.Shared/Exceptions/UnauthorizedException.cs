namespace CarMaintenance.Shared.Exceptions
{
    public class UnauthorizedException : ApplicationException
    {
        public UnauthorizedException()
            : base("Unauthorized access.")
        {
        }

        public UnauthorizedException(string message)
            : base(message)
        {
        }
    }
}