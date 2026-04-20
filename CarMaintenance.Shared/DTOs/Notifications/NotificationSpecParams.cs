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
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public bool? IsRead { get; set; }
    }
}