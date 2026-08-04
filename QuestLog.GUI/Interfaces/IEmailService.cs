using System.Collections.Generic;
using System.Threading.Tasks;
using QuestLog.GUI.Models;

namespace QuestLog.GUI.Interfaces
{
    public interface IEmailService
    {
        Task<IEnumerable<Email>> GetEmailsAsync(int count = 50);
        Task<IEnumerable<Email>> GetUnreadEmailsAsync(int count = 50);
        Task<Email?> GetEmailByIdAsync(string id);
        Task<bool> MarkAsReadAsync(string id);
    }
}