namespace CarMaintenance.Shared.Exceptions
{
    public class BadRequestException : ApplicationException
    {
        public BadRequestException(string? message=null) 
           : base(message)
        {

        }
    }
}
