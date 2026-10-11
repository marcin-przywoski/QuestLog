using System;
using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using Velopack;

namespace QuestLog.Tests.Fakes
{
    /// <summary>Scriptable <see cref="IUpdateService"/> for view-model tests.</summary>
    public class FakeUpdateService : IUpdateService
    {
        /// <summary>When set, thrown by every method to simulate service failures.</summary>
        public Exception? CheckException { get; set; }
        public Exception? ApplyException { get; set; }
        public int CheckCallCount { get; private set; }
        public int ApplyCallCount { get; private set; }

        public string PendingUpdateNotes { get; set; } = string.Empty;

        public Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            CheckCallCount++;
            if (CheckException != null)
            {
                throw CheckException;
            }

            return Task.FromResult<UpdateInfo?>(null);
        }

        public Task DownloadUpdateAsync(UpdateInfo update)
        {
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
