using UniMart_App.Models;
using System.Collections.Generic;

namespace UniMart_App.ViewModels
{
    public class UserViewModel
    {
        public ApplicationUser User { get; set; }
        public List<string> Roles { get; set; }
        public int? ProductCount { get; set; }
    }
} 