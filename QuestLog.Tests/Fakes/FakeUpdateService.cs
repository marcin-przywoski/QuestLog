using System;
using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using Velopack;

namespace QuestLog.Tests.Fakes
{
    /// <summary>Scriptable <see cref="IUpdateService"/> for view-model tests.</summary>
    public class FakeUpdateService : IUpdateService
    {
        /// <summary>When set, thrown by <see cref="CheckForUpdatesAsync"/>.</summary>
        public Exception? CheckException { get; set; }

        /// <summary>When set, thrown by <see cref="ApplyUpdateAndRestartAsync"/>.</summary>
        public Exception? ApplyException { get; set; }

        /// <summary>Returned by <see cref="CheckForUpdatesAsync"/>; null = up to date.</summary>
        public UpdateInfo? UpdateInfoResult { get; set; }

        public int CheckCallCount { get; private set; }
        public int DownloadCallCount { get; private set; }
        public int ApplyCallCount { get; private set; }
        public UpdateInfo? LastDownloadedUpdate { get; private set; }

        public string PendingUpdateNotes { get; set; } = string.Empty;

        public Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            CheckCallCount++;
            if (CheckException != null)
            {
                throw CheckException;
            }

            return Task.FromResult<UpdateInfo?>(UpdateInfoResult);
        }

        public Task DownloadUpdateAsync(UpdateInfo update)
        {
            DownloadCallCount++;
            LastDownloadedUpdate = update;
            return Task.CompletedTask;
        }

        public Task ApplyUpdateAndRestartAsync()
        {
            ApplyCallCount++;
            if (ApplyException != null)
            {
                throw ApplyException;
            }

            return Task.CompletedTask;
        }
    }
}
