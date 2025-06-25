using UniMart_App.Models;

namespace UniMart_App.ViewModels
{
    public class MerchantViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public MerchantPerformanceViewModel Performance { get; set; } = null!;
    }
}