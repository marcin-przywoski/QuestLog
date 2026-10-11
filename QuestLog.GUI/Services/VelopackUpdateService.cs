using System;
using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using Velopack;

namespace QuestLog.GUI.Services
{
    public class VelopackUpdateService : IUpdateService
    {
        private const string DefaultFeedUrl = "https://github.com/marcin-przywoski/QuestLog/releases.atom";

        private readonly UpdateManager _updateManager;
        private VelopackAsset? _pendingUpdate;

        public VelopackUpdateService()
            : this(DefaultFeedUrl)
        {
        }

        public VelopackUpdateService(string feedUrlOrPath)
        {
            _updateManager = new UpdateManager(feedUrlOrPath);
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            // Updates only work for Velopack-installed builds (not dev runs from source).
            if (!_updateManager.IsInstalled)
            {
                return null;
            }

            var update = await _updateManager.CheckForUpdatesAsync();
            _pendingUpdate = update?.TargetFullRelease;
            return update;
        }

        public async Task DownloadUpdateAsync(UpdateInfo update)
        {
            await _updateManager.DownloadUpdatesAsync(update, null, true);
        }

        public Task ApplyUpdateAndRestartAsync()
        {
            if (_pendingUpdate == null)
            {
                throw new InvalidOperationException("No update has been downloaded.");
            }

            _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
            return Task.CompletedTask;
        }
    }
}
