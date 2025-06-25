using System;

namespace UniMart_App.Models
{
    public class SystemActivity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; }
        public string IconColor { get; set; }
        public string ActionUrl { get; set; }
    }

    public class SystemHealth
    {
        public string ServerStatus { get; set; }
        public string DatabaseStatus { get; set; }
        public int StorageUsage { get; set; }
        public int ActiveSessions { get; set; }
        public string LastBackup { get; set; }
    }

    public class SystemAlert
    {
        public string Message { get; set; }
        public string Icon { get; set; }
        public string Severity { get; set; }
        public string ActionUrl { get; set; }
    }
}