using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Services;

namespace QuestLog.GUI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IEmailService _emailService;

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

        public MainWindowViewModel()
        {
            _emailService = new AppleScriptOutlookService();
        }

        public MainWindowViewModel(IEmailService emailService)
        {
            _emailService = emailService;
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
                return;

            try
            {
                var success = await _emailService.MarkAsReadAsync(SelectedEmail.Id);
                if (success)
                {
                    SelectedEmail.IsRead = true;
                    StatusMessage = "Email marked as read";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
        }

    }
}
