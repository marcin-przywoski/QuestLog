using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuestLog.GUI.Models
{
    public class Email : ObservableObject
    {
        private bool _isRead;

        public string Id { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsRead
        {
            get => _isRead;
            set => SetProperty(ref _isRead, value);
        }

        public string Folder { get; set; } = string.Empty;
    }
}