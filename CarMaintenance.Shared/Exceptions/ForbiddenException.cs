namespace CarMaintenance.Shared.Exceptions
{
    public class ForbiddenException : ApplicationException
    {
        public ForbiddenException()
            : base("You do not have permission to perform this action.")
        {
        }

        public ForbiddenException(string message)
            : base(message)
        {
        }
    }
}