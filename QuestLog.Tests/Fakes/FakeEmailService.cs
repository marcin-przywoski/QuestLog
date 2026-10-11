using System.Collections.Generic;
using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Models;

namespace QuestLog.Tests.Fakes
{
    /// <summary>Scriptable <see cref="IEmailService"/> for view-model tests.</summary>
    public class FakeEmailService : IEmailService
    {
        public IEnumerable<Email> EmailsResult { get; set; } = new List<Email>();
        public IEnumerable<Email> UnreadEmailsResult { get; set; } = new List<Email>();
        public bool MarkAsReadResult { get; set; } = true;
        public string? LastMarkedId { get; private set; }
        public int GetUnreadCallCount { get; private set; }

        public Task<IEnumerable<Email>> GetEmailsAsync(int count = 50)
        {
            return Task.FromResult(EmailsResult);
        }

        public Task<IEnumerable<Email>> GetUnreadEmailsAsync(int count = 50)
        {
            GetUnreadCallCount++;
            return Task.FromResult(UnreadEmailsResult);
        }

        public Task<Email?> GetEmailByIdAsync(string id)
        {
            return Task.FromResult<Email?>(null);
        }

        public Task<bool> MarkAsReadAsync(string id)
        {
            LastMarkedId = id;
            return Task.FromResult(MarkAsReadResult);
        }
    }
}
