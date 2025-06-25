using System.ComponentModel.DataAnnotations;

namespace UniMart_App.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
        
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 