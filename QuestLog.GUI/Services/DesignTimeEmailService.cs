using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Models;

namespace QuestLog.GUI.Services
{
    /// <summary>
    /// <see cref="IEmailService"/> used by the design-time view model: returns
    /// <see cref="DesignData"/> samples instead of executing AppleScript against Outlook.
    /// </summary>
    public class DesignTimeEmailService : IEmailService
    {
        public Task<IEnumerable<Email>> GetEmailsAsync(int count = 50)
        {
            return Task.FromResult(DesignData.Emails);
        }

        public Task<IEnumerable<Email>> GetUnreadEmailsAsync(int count = 50)
        {
            return Task.FromResult(DesignData.Emails);
        }

        public Task<Email?> GetEmailByIdAsync(string id)
        {
            return Task.FromResult(DesignData.Emails.FirstOrDefault(email => email.Id == id));
        }

        public Task<bool> MarkAsReadAsync(string id)
        {
            return Task.FromResult(true);
        }
    }
}
