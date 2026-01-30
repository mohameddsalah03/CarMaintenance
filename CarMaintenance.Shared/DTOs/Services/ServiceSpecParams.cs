namespace CarMaintenance.Shared.DTOs.Services
{
    public class ServiceSpecParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageIndex { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? Search { get; set; }        // ✅ بحث بالاسم
        public string? Category { get; set; }      // ✅ فلترة بالفئة
        public decimal? MinPrice { get; set; }     // ✅ أقل سعر
        public decimal? MaxPrice { get; set; }     // ✅ أعلى سعر
        public int? MaxDuration { get; set; }      // ✅ أقصى مدة
        public string? Sort { get; set; }
    }
}