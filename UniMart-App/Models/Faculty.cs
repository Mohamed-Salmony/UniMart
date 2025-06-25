using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniMart_App.Models
{
    public class Faculty
    {
        public Faculty()
        {
            Users = new HashSet<ApplicationUser>();
            Products = new HashSet<Product>();
        }

        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [Range(1, 7, ErrorMessage = "Maximum academic years must be between 1 and 7")]
        public int MaxAcademicYears { get; set; }

        public virtual ICollection<ApplicationUser> Users { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
