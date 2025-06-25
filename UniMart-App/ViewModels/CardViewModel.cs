using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using UniMart_App.Models;

namespace UniMart_App.ViewModels
{
    public class CardViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Cardholder name is required")]
        [Display(Name = "Cardholder Name")]
        [StringLength(100)]
        public string CardholderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Card number is required")]
        [Display(Name = "Card Number")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Card number must be 16 digits")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Card number must contain only digits")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required")]
        [Display(Name = "Expiry Date")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/([0-9]{2})$", ErrorMessage = "Expiry date must be in MM/YY format")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV is required")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV must be 3 or 4 digits")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must contain only digits")]
        public string CVV { get; set; } = string.Empty;

        public string CardType { get; set; } = string.Empty;

        [Display(Name = "Set as Default")]
        public bool IsDefault { get; set; }
    }

    public class CardListViewModel
    {
        public List<CardDisplayModel> Cards { get; set; } = new();
        public CardViewModel? NewCard { get; set; }
    }

    public class CardDisplayModel
    {
        public int Id { get; set; }
        public string CardholderName { get; set; } = string.Empty;
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}

