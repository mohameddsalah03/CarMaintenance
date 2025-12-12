namespace CarMaintenance.Shared.Exceptions
{
    public class ValidationException : BadRequestException
    {
        public required  IEnumerable<string> Errors { get; set; }
        public ValidationException(string? message ="Validation Failed" )
            :base(message) { }
        
            
       

    }
}
