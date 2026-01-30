namespace CarMaintenance.Shared.DTOs.Vehicles
{
    public class VehicleDto
    {
        public int Id { get; set; }
        public string Model { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public int Year { get; set; }
        public string PlateNumber { get; set; } = null!;

        // Foreign Key
        public string UserId { get; set; } = null!;

        public string OwnerName { get; set; } = null!; // user.DisplayName
    }
}
