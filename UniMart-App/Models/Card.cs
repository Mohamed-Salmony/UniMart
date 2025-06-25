using System.ComponentModel.DataAnnotations;

namespace UniMart_App.Models
{
    public class Card
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [Required]
        [StringLength(100)]
        public string CardholderName { get; set; } = default!;

        [Required]
        public string CardNumber { get; set; } = default!;

        [Required]
        public string ExpiryDate { get; set; } = default!;

        [Required]
        public string CVV { get; set; } = default!;

        [Required]
        public string CardType { get; set; } = default!;

        public bool IsDefault { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
