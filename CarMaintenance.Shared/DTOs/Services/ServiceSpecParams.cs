namespace CarMaintenance.Shared.DTOs.Services
{
    public class ServiceSpecParams
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 9;

        public int PageIndex { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? Search { get; set; }        
        public string? Category { get; set; }      
        public decimal? MinPrice { get; set; }     
        public decimal? MaxPrice { get; set; }     
        public int? MaxDuration { get; set; }      
        public string? Sort { get; set; }
    }
}