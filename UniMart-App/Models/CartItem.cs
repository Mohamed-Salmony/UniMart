namespace UniMart_App.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public Product Product { get; set; }
        public string SelectedOptions { get; set; }
        public int Quantity { get; set; }
        public string UserId { get; set; }
        public int ProductId { get; set; }
    }
}
