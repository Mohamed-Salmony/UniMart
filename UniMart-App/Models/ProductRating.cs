namespace UniMart_App.Models
{
    public class ProductRating
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int RatingValue { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Added alias property for Rating to fix missing definition error
        public int Rating
        {
            get { return RatingValue; }
            set { RatingValue = value; }
        }
    }
}
