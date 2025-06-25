namespace UniMart_App.Models
{
    public class Product
    {
        public Product()
        {
            Name = string.Empty;
            Description = string.Empty;
            Specifications = string.Empty;
            UserId = string.Empty;
            Favorites = new HashSet<Favorite>();
            Ratings = new HashSet<ProductRating>();
            OrderItems = new HashSet<OrderItem>();
            ProductImages = new HashSet<ProductImage>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int StockQuantity { get; set; }
        public string Specifications { get; set; }
        public decimal? Rating { get; set; }

        public int FacultyId { get; set; }
        public Faculty? Faculty { get; set; }

        // Product approval fields
        public bool IsApproved { get; set; } = false;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public ApplicationUser? ApprovedByUser { get; set; }

        // Added missing properties
        public decimal AverageRating
        {
            get
            {
                if (Ratings == null || !Ratings.Any())
                    return 0;
                return (decimal)Ratings.Average(r => r.RatingValue);
            }
        }

        public void UpdateAverageRating(decimal averageRating)
        {
            Rating = averageRating;
        }

        public string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public ICollection<Favorite> Favorites { get; set; }
        public ICollection<ProductRating> Ratings { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; } = new HashSet<ProductImage>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
