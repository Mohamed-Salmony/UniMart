using System;

namespace UniMart_App.ViewModels
{
    public class SystemActivityViewModel
    {
        public string Description { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? IconClass { get; set; }
        public string? BadgeClass { get; set; }
    }
}