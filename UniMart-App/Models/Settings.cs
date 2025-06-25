using System.ComponentModel.DataAnnotations;

namespace UniMart_App.Models
{
    public class Settings
    {
        [Key]
        public int Id { get; set; }

        [Range(0, 100)]
        public decimal MerchantFeePercentage { get; set; }

        [Range(0, 100)]
        public decimal UserRewardPercentage { get; set; }

        [Range(0, 100)]
        public decimal UserFeePercentage { get; set; }
    }
}
