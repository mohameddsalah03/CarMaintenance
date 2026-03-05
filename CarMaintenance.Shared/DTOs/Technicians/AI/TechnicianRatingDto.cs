namespace CarMaintenance.Shared.DTOs.Technicians.AI
{
    public class TechnicianRatingDto
    {
        public string TechnicianId { get; set; } = null!;
        public double AvgRating { get; set; }
        public int TotalReviews { get; set; }
    }
}