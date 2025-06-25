﻿namespace UniMart_App.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }

        public decimal TotalAmount
        {
            get
            {
                if (OrderItems == null || !OrderItems.Any())
                    return 0;

                return OrderItems.Sum(item => item.Quantity * item.Price);
            }
        }

        public int OrderNumber => Id;

        public string ShippingStatus { get; set; } // New field for shipping status
        public string ShippingAddress { get; set; } // Bonus: shipping address
        public string TrackingNumber { get; set; } // Bonus: tracking number
        public DateTime? DeliveryDate { get; set; } // Bonus: delivery date
        public string? PhoneNumber { get; set; } // New field for user's phone number
    }
}
