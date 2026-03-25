namespace CarMaintenance.Shared.DTOs.Notifications
{
    public class NotificationSpecParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageIndex { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            // Prevent frontend from requesting too many records at once
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        // Optional filter: only unread
        public bool? IsRead { get; set; }
    }
}