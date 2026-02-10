namespace CarMaintenance.Shared.DTOs.Bookings
{
    public class BookingSpecParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageIndex { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }


        public string? Status { get; set; }        // Filter by status
        public string? UserId { get; set; }        // Filter by customer
        public string? TechnicianId { get; set; }  // Filter by technician
        public DateTime? FromDate { get; set; }    // Date range start
        public DateTime? ToDate { get; set; }      // Date range end
        public string? Sort { get; set; }          // "dateAsc", "dateDesc"

    }
}
