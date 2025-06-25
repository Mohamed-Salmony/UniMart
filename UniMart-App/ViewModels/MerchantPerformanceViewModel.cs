namespace UniMart_App.ViewModels
{
    public class MerchantPerformanceViewModel
    {
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public decimal Rating { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PerformanceScore { get; set; }
    }
}