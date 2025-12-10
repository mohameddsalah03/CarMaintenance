namespace CarMaintenance.Core.Service.Abstraction.Services.Auth.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
