using System;
using System.Collections.Generic;
using UniMart_App.Models;

namespace UniMart_App.ViewModels
{
    public class MerchantDetailViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public MerchantPerformanceViewModel Performance { get; set; } = null!;
        public List<Product> RecentProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<ProductRating> RecentRatings { get; set; } = new();
        public List<SystemActivityViewModel> RecentActivities { get; set; } = new();
    }
}