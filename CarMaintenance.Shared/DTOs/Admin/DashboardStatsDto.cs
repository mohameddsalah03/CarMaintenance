
namespace CarMaintenance.Shared.DTOs.Admin
{
    public class DashboardStatsDto  
    {
        // Booking counts
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int InProgressBookings { get; set; }
        public int WaitingApprovalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int TodayBookings { get; set; }

        // User counts
        public int TotalCustomers { get; set; }
        public int TotalTechnicians { get; set; }
        public int AvailableTechnicians { get; set; }

        // Ratings
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // Revenue
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
    }
}