using System;
using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using Velopack;
using Velopack.Sources;

namespace QuestLog.GUI.Services
{
    public class VelopackUpdateService : IUpdateService
    {
        private const string DefaultRepoUrl = "https://github.com/marcin-przywoski/QuestLog";

        private readonly UpdateManager _updateManager;
        private VelopackAsset? _pendingUpdate;

        public VelopackUpdateService()
            : this(DefaultRepoUrl)
        {
        }

        public VelopackUpdateService(string repoUrl)
        {
            // Releases are published by CD via `vpk upload github --channel <win|win-dev>`;
            // GithubSource parses those Velopack assets and UpdateManager filters by the
            // channel this install was packed with. prerelease: true so dev-channel releases
            // are visible too — stable filtering happens via channel matching.
            _updateManager = new UpdateManager(new GithubSource(repoUrl, accessToken: null, prerelease: true));
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
