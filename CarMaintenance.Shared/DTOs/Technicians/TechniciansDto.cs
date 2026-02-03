namespace CarMaintenance.Shared.DTOs.Technicians
{
    public class TechniciansDto
    {
        public string Id { get; set; } = null!;
        public string Specialization { get; set; } = null!;
        public decimal Rating { get; set; }
        public bool IsAvailable { get; set; }

        // User Info
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
    }
}