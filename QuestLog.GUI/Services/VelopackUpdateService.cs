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

        public string PendingUpdateNotes => _pendingUpdate?.NotesMarkdown ?? string.Empty;

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            // Updates only work for Velopack-installed builds (not dev runs from source).
            // Throw instead of returning null so callers can tell "can't update" from "no update".
            if (!_updateManager.IsInstalled && !_updateManager.IsPortable)
            {
                throw new InvalidOperationException(
                    "Updates are unavailable: this build was not installed via Velopack or the build is not portable.");
            }

            // Note: _pendingUpdate is intentionally NOT cleared here - a failed re-check must
            // leave an already-downloaded package applicable (see DownloadUpdateAsync).
            UpdateInfo? update = await _updateManager.CheckForUpdatesAsync();
            return update;
        }

        public async Task DownloadUpdateAsync(UpdateInfo update)
        {
            // ignoreDeltas left at its default (false): the CD pipeline publishes delta
            // packages, so use them to keep downloads small.
            await _updateManager.DownloadUpdatesAsync(update);

            // Only applicable once the package is actually downloaded.
            _pendingUpdate = update.TargetFullRelease
                ?? throw new InvalidOperationException("Update has no full release package.");
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
