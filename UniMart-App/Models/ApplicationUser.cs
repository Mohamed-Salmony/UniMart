using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace UniMart_App.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        public string ProfileImageUrl { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int? FacultyId { get; set; }
        public Faculty Faculty { get; set; }

        public int? AcademicYearId { get; set; }
        public AcademicYear AcademicYear { get; set; }

        // Navigation properties for user's data
        public ICollection<Product> Products { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
        public ICollection<ProductRating> Ratings { get; set; }
        public ICollection<SupportTicket> SupportTickets { get; set; }
        public ICollection<Notification> Notifications { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public MerchantStatus MerchantStatus { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime? LastSeen { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public ApplicationUser()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            Products = new HashSet<Product>();
            Orders = new HashSet<Order>();
            Favorites = new HashSet<Favorite>();
            Ratings = new HashSet<ProductRating>();
            SupportTickets = new HashSet<SupportTicket>();
            Notifications = new HashSet<Notification>();
        }
    }
}
