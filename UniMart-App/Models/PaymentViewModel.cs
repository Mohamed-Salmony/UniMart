using System.ComponentModel.DataAnnotations;

namespace UniMart_App.Models
{
    public class PaymentViewModel
    {
        [Required]
        [Display(Name = "Cardholder Name")]
        public string CardholderName { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{13,19}$", ErrorMessage = "Card number must be 13-19 digits")]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Expiry must be MM/YY")] 
        [Display(Name = "Expiry (MM/YY)")]
        public string Expiry { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "Invalid CVV")]
        [Display(Name = "CVV")]
        public string CVV { get; set; }
    }
}
