using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Services;

namespace QuestLog.GUI.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IEmailService _emailService;


        public MainWindowViewModel()
        {
            _emailService = new AppleScriptOutlookService();
        }

        public MainWindowViewModel(IEmailService emailService)
        {
            _emailService = emailService;
        }

    }
}
