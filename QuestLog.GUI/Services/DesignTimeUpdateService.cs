using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using Velopack;

namespace QuestLog.GUI.Services
{
    /// <summary>
    /// <see cref="IUpdateService"/> used by the design-time view model: never queries the
    /// Velopack feed and never restarts anything. The banner sample comes from
    /// <see cref="Models.DesignData.SampleUpdate"/> instead.
    /// </summary>
    public class DesignTimeUpdateService : IUpdateService
    {
        public string PendingUpdateNotes => string.Empty;

        public Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            return Task.FromResult<UpdateInfo?>(null);
        }

        public Task DownloadUpdateAsync(UpdateInfo update)
        {
            return Task.CompletedTask;
        }

        public Task ApplyUpdateAndRestartAsync()
        {
            return Task.CompletedTask;
        }
    }
}
