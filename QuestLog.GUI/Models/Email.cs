using System;

namespace QuestLog.GUI.Models
{
    public class Email
    {
        public string Id { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string Folder { get; set; } = string.Empty;
    }
}