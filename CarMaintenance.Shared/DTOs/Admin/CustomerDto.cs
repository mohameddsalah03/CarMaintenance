namespace CarMaintenance.Shared.DTOs.Admin
{
    public class CustomerDto
    {
        public string Id { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string UserName { get; set; } = null!;
    }
}