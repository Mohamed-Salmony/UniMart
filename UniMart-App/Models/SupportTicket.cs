namespace UniMart_App.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public ICollection<SupportTicketReply> Replies { get; set; } = new List<SupportTicketReply>();
    }
}
