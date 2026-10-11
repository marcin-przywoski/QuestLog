using System;
using System.Collections.Generic;

namespace QuestLog.GUI.Models
{
    /// <summary>
    /// Sample data for the design-time view model path (parameterless ctor). Keeps the
    /// previewer realistic without ever touching Outlook or the Velopack update feed.
    /// </summary>
    public static class DesignData
    {
        public static Email KickoffEmail { get; } = new()
        {
            Id = "email-001",
            Subject = "Project kickoff and next steps",
            Sender = "Elena Rodriguez",
            SenderEmail = "elena.rodriguez@example.com",
            ReceivedDate = new DateTime(2026, 8, 5, 9, 24, 0),
            Body = "Thanks for joining the kickoff. I have attached the initial scope and outlined the next steps for the team. Please share any questions before Friday.",
            IsRead = false,
            Folder = "Inbox"
        };

        public static Email DesignReviewEmail { get; } = new()
        {
            Id = "email-002",
            Subject = "Design review notes",
            Sender = "Marcus Chen",
            SenderEmail = "marcus.chen@example.com",
            ReceivedDate = new DateTime(2026, 8, 4, 16, 10, 0),
            Body = "The updated flow looks good. I added a few notes about the empty state and the loading experience for us to review.",
            IsRead = false,
            Folder = "Inbox"
        };

        public static Email WeeklyUpdateEmail { get; } = new()
        {
            Id = "email-003",
            Subject = "Weekly team update",
            Sender = "Priya Shah",
            SenderEmail = "priya.shah@example.com",
            ReceivedDate = new DateTime(2026, 8, 4, 8, 45, 0),
            Body = "A quick update from this week: the integration is on track, and the remaining tasks are ready for validation.",
            IsRead = true,
            Folder = "Inbox"
        };

        public static IEnumerable<Email> Emails => new List<Email> { KickoffEmail, DesignReviewEmail, WeeklyUpdateEmail };

        public static AvailableUpdate SampleUpdate { get; } = new()
        {
            Version = "0.2.0",
            NotesMarkdown = "## What's new\n\n* Update banner with release notes\n* Auto-check for updates on startup\n\n### Fixes\n\n* Fixed status bar errors not clearing"
        };
    }
}
