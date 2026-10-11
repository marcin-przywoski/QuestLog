using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Models;
using QuestLog.GUI.Services;
using Velopack;

namespace QuestLog.GUI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IEmailService _emailService;
        private readonly IUpdateService _updateService;

        [ObservableProperty]
        private ObservableCollection<Email> _emails = new();

        [ObservableProperty]
        private Email? _selectedEmail;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _showUnreadOnly;

        [ObservableProperty]
        private AvailableUpdate? _availableUpdate;

        [ObservableProperty]
        private bool _isCheckingForUpdates;

        public MainWindowViewModel()
        {
            // Design-time / previewer path: sample data only - never real Outlook or Velopack.
            _emailService = new DesignTimeEmailService();
            _updateService = new DesignTimeUpdateService();
        }

        public MainWindowViewModel(IEmailService emailService, IUpdateService updateService)
        {
            _emailService = emailService;
            _updateService = updateService;
        }

        [RelayCommand]
        private async Task LoadEmailsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading emails from Outlook...";

                var emails = ShowUnreadOnly
                    ? await _emailService.GetUnreadEmailsAsync(50)
                    : await _emailService.GetEmailsAsync(50);

                Emails.Clear();
                foreach (var email in emails)
                {
                    Emails.Add(email);
                }

                StatusMessage = $"Loaded {Emails.Count} email(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task MarkAsReadAsync()
        {
            if (SelectedEmail == null)
            {
                return;
            }

            try
            {
                var success = await _emailService.MarkAsReadAsync(SelectedEmail.Id);
                if (!success)
                {
                    StatusMessage = "Failed to mark email as read";
                    return;
                }

                var selectedEmail = SelectedEmail;
                selectedEmail.IsRead = true;

                if (ShowUnreadOnly)
                {
                    var selectedIndex = Emails.IndexOf(selectedEmail);
                    Emails.Remove(selectedEmail);
                    SelectedEmail = Emails.Count == 0
                        ? null
                        : Emails[Math.Min(selectedIndex, Emails.Count - 1)];
                    StatusMessage = "Email marked as read and removed from unread list";
                    return;
                }

                StatusMessage = "Email marked as read";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                IsCheckingForUpdates = true;
                StatusMessage = "Checking for updates...";

                // Hide the stale banner (and its restart button) while re-checking.
                AvailableUpdate = null;

                UpdateInfo? update = await _updateService.CheckForUpdatesAsync();
                if (update == null)
                {
                    StatusMessage = "App is up to date";
                    return;
                }

                string version = update.TargetFullRelease?.Version?.ToString() ?? "unknown";
                StatusMessage = $"Update {version} available - downloading...";
                await _updateService.DownloadUpdateAsync(update);

                AvailableUpdate = new AvailableUpdate
                {
                    Version = version,
                    NotesMarkdown = _updateService.PendingUpdateNotes,
                };
                StatusMessage = $"Update {version} ready - restart to update";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        [RelayCommand]
        private async Task ApplyUpdateAsync()
        {
            try
            {
                StatusMessage = "Applying update...";
                await _updateService.ApplyUpdateAndRestartAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        partial void OnShowUnreadOnlyChanged(bool value)
        {
            _ = LoadEmailsAsync();
        }
    }
}
