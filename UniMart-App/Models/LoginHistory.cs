using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniMart_App.Models
{
    public class LoginHistory
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public string Device { get; set; }
        public string Location { get; set; }
    }
}
