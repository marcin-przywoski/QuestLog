using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuestLog.GUI.Models;
using QuestLog.GUI.ViewModels;
using QuestLog.Tests.Fakes;
using Xunit;

namespace QuestLog.Tests
{
    public class MainWindowViewModelTests
    {
        private static Email Email(string id, bool isRead = false)
        {
            return new() { Id = id, Subject = $"Subject {id}", IsRead = isRead };
        }

        private static (MainWindowViewModel vm, FakeEmailService email, FakeUpdateService update) CreateSut()
        {
            var email = new FakeEmailService();
            var update = new FakeUpdateService();
            return (new MainWindowViewModel(email, update), email, update);
        }

        [Fact]
        public async Task LoadEmails_PopulatesCollection_AndReportsCount()
        {
            var (vm, email, _) = CreateSut();
            email.EmailsResult = new List<Email> { Email("1"), Email("2") };

            await vm.LoadEmailsCommand.ExecuteAsync(null);

            Assert.Equal(2, vm.Emails.Count);
            Assert.Equal("Loaded 2 email(s)", vm.StatusMessage);
            Assert.False(vm.IsLoading);
        }

        [Fact]
        public async Task LoadEmails_ShowUnreadOnly_UsesUnreadServiceMethod()
        {
            var (vm, email, _) = CreateSut();
            email.UnreadEmailsResult = new List<Email> { Email("u1") };

            await vm.LoadEmailsCommand.ExecuteAsync(null);
            vm.ShowUnreadOnly = true;

            // ShowUnreadOnly triggers a fire-and-forget reload; wait for it to settle.
            for (int i = 0; i < 100 && email.GetUnreadCallCount == 0; i++)
            {
                await Task.Delay(10);
            }

            Assert.Equal(1, email.GetUnreadCallCount);
            Assert.Single(vm.Emails);
        }

        [Fact]
        public async Task LoadEmails_ServiceThrows_SetsErrorStatus()
        {
            var (vm, email, _) = CreateSut();
            email.MarkAsReadResult = false;
            var throwing = new ThrowingEmailService(new InvalidOperationException("osascript failed"));
            var vm2 = new MainWindowViewModel(throwing, new FakeUpdateService());

            await vm2.LoadEmailsCommand.ExecuteAsync(null);

            Assert.StartsWith("Error: ", vm2.StatusMessage);
            Assert.Contains("osascript failed", vm2.StatusMessage);
            Assert.False(vm2.IsLoading);
        }

        [Fact]
        public async Task MarkAsRead_NoSelection_IsNoOp()
        {
            var (vm, email, _) = CreateSut();
            vm.SelectedEmail = null;

            await vm.MarkAsReadCommand.ExecuteAsync(null);

            Assert.Null(email.LastMarkedId);
        }

        [Fact]
        public async Task MarkAsRead_Success_SetsIsRead()
        {
            var (vm, email, _) = CreateSut();
            var target = Email("9", isRead: false);
            email.EmailsResult = new List<Email> { target };
            await vm.LoadEmailsCommand.ExecuteAsync(null);
            vm.SelectedEmail = target;

            await vm.MarkAsReadCommand.ExecuteAsync(null);

            Assert.Equal("9", email.LastMarkedId);
            Assert.True(target.IsRead);
            Assert.Equal("Email marked as read", vm.StatusMessage);
        }

        [Fact]
        public async Task MarkAsRead_ServiceFailure_SetsFailureStatus()
        {
            var (vm, email, _) = CreateSut();
            var target = Email("9");
            email.EmailsResult = new List<Email> { target };
            email.MarkAsReadResult = false;
            await vm.LoadEmailsCommand.ExecuteAsync(null);
            vm.SelectedEmail = target;

            await vm.MarkAsReadCommand.ExecuteAsync(null);

            Assert.Equal("Failed to mark email as read", vm.StatusMessage);
            Assert.False(target.IsRead);
        }

        [Fact]
        public async Task CheckForUpdates_NoUpdate_ReportsUpToDate()
        {
            var (vm, _, _) = CreateSut();

            await vm.CheckForUpdatesCommand.ExecuteAsync(null);

            Assert.Equal("App is up to date", vm.StatusMessage);
            Assert.Null(vm.AvailableUpdate);
            Assert.False(vm.IsCheckingForUpdates);
        }

        [Fact]
        public async Task CheckForUpdates_Failure_RestoresPreviousBanner()
        {
            var (vm, _, update) = CreateSut();
            var previous = new AvailableUpdate { Version = "1.2.3", NotesMarkdown = "notes" };
            vm.AvailableUpdate = previous;
            update.CheckException = new InvalidOperationException("network down");

            await vm.CheckForUpdatesCommand.ExecuteAsync(null);

            Assert.Same(previous, vm.AvailableUpdate);
            Assert.Contains("network down", vm.StatusMessage);
            Assert.False(vm.IsCheckingForUpdates);
        }

        [Fact]
        public async Task ApplyUpdate_Failure_SetsErrorStatus()
        {
            var (vm, _, update) = CreateSut();
            update.ApplyException = new InvalidOperationException("No update has been downloaded.");

            await vm.ApplyUpdateCommand.ExecuteAsync(null);

            Assert.StartsWith("Error: ", vm.StatusMessage);
            Assert.Contains("No update", vm.StatusMessage);
        }

        private sealed class ThrowingEmailService : QuestLog.GUI.Interfaces.IEmailService
        {
            private readonly Exception _exception;

            public ThrowingEmailService(Exception exception)
            {
                _exception = exception;
            }

            public Task<IEnumerable<Email>> GetEmailsAsync(int count = 50) => Task.FromException<IEnumerable<Email>>(_exception);
            public Task<IEnumerable<Email>> GetUnreadEmailsAsync(int count = 50) => Task.FromException<IEnumerable<Email>>(_exception);
            public Task<Email?> GetEmailByIdAsync(string id) => Task.FromException<Email?>(_exception);
            public Task<bool> MarkAsReadAsync(string id) => Task.FromException<bool>(_exception);
        }
    }
}
