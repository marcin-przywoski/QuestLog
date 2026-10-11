using System.Threading.Tasks;
using Velopack;

namespace QuestLog.GUI.Interfaces
{
    public interface IUpdateService
    {
        /// <summary>Release notes (markdown) of the pending update; empty when none.</summary>
        string PendingUpdateNotes { get; }

        /// <summary>Checks the update feed; returns null when the app is up to date.</summary>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>Downloads and installs the given update locally so it can be applied on restart.</summary>
        Task DownloadUpdateAsync(UpdateInfo update);

        /// <summary>Applies the downloaded update and restarts the app.</summary>
        Task ApplyUpdateAndRestartAsync();
    }
}
