using System;
using System.ComponentModel.DataAnnotations;

namespace UniMart_App.Models
{
    public class SupportTicketReply
    {
        public int Id { get; set; }
        public int SupportTicketId { get; set; }
        public SupportTicket SupportTicket { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string SenderRole { get; set; } // "Admin" or "User"
    }
}
