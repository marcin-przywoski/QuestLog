using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Models;

namespace QuestLog.GUI.Services
{
    /// <summary>
    /// Windows <see cref="IEmailService"/> backed by classic desktop Outlook's COM
    /// object model. Late binding (<see cref="Activator.CreateInstance(Type)"/> +
    /// <c>dynamic</c>) avoids a PIA/version dependency; the "New Outlook" UWP-style
    /// client has no automation API and fails in <see cref="EnsureOutlookIsSupported"/>.
    /// All COM calls run on a thread-pool thread — IDispatch marshals them into
    /// Outlook's STA.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class WindowsOutlookService : IEmailService
    {
        private const string OutlookProgId = "Outlook.Application";
        private const int OlFolderInbox = 6;
        private const int OlMailItem = 43;

        public async Task<IEnumerable<Email>> GetEmailsAsync(int count = 50)
        {
            EnsureOutlookIsSupported();
            return await Task.Run(() => GetInboxEmails(count, unreadOnly: false));
        }

        public async Task<IEnumerable<Email>> GetUnreadEmailsAsync(int count = 50)
        {
            EnsureOutlookIsSupported();
            return await Task.Run(() => GetInboxEmails(count, unreadOnly: true));
        }

        public async Task<Email?> GetEmailByIdAsync(string id)
        {
            EnsureOutlookIsSupported();
            return await Task.Run(() => GetEmailByEntryId(ValidateEntryId(id)));
        }

        public async Task<bool> MarkAsReadAsync(string id)
        {
            EnsureOutlookIsSupported();
            return await Task.Run(() => MarkEntryIdAsRead(ValidateEntryId(id)));
        }

        // Mirrors GetEmails.applescript, which enumerates every folder named "Inbox"
        // across accounts: iterate MAPI stores and merge per-store Inboxes by date.
        private static IEnumerable<Email> GetInboxEmails(int count, bool unreadOnly)
        {
            var emails = new List<Email>(Math.Min(count, 256));
            Exception? firstItemError = null;
            object? app = null;
            object? session = null;
            object? stores = null;

            try
            {
                dynamic outlook = app = Activator.CreateInstance(GetOutlookComType())
                    ?? throw new InvalidOperationException("Failed to start the Outlook COM object.");
                dynamic mapi = session = outlook.GetNamespace("MAPI");
                mapi.Logon(Type.Missing, Type.Missing, false, false);

                // Namespace.Stores yields Store objects — the only type besides
                // Namespace itself that exposes GetDefaultFolder.
                dynamic sessionStores = stores = mapi.Stores;
                int storeCount = sessionStores.Count;
                int attempted = 0;
                for (int s = 1; s <= storeCount; s++)
                {
                    object? store = null;
                    object? inbox = null;
                    try
                    {
                        dynamic sessionStore = store = sessionStores.Item(s);
                        dynamic folder = inbox = sessionStore.GetDefaultFolder(OlFolderInbox);
                        attempted++;
                        CollectInboxEmails(folder, count, unreadOnly, emails, ref firstItemError);
                    }
                    catch (COMException ex)
                    {
                        // A store legitimately lacking an Inbox (archive/PST) throws
                        // inside GetDefaultFolder — skip it, don't count as failure.
                        if (store == null)
                        {
                            // Stores.Item itself failed — the store couldn't be
                            // enumerated at all; that is a real failure.
                            attempted++;
                            firstItemError ??= ex;
                        }
                        else if (inbox != null)
                        {
                            firstItemError ??= ex;
                        }
                    }
                    catch (RuntimeBinderException ex)
                    {
                        firstItemError ??= ex;
                    }
                    finally
                    {
                        Release(inbox);
                        Release(store);
                    }
                }

                // Every item failing (e.g. Outlook's programmatic-access guard blocking
                // protected properties) must surface as an error, not as an empty inbox.
                if (emails.Count == 0 && attempted > 0 && firstItemError != null)
                {
                    throw new InvalidOperationException(
                        $"Outlook denied access to message data ({firstItemError.Message}). " +
                        "If a security prompt was shown, allow programmatic access and retry.",
                        firstItemError);
                }
            }
            finally
            {
                Release(stores);
                Release(session);
                Release(app);
            }

            return emails
                .OrderByDescending(e => e.ReceivedDate)
                .Take(count)
                .ToList();
        }

        // Cap per store, not globally: the merge can draw at most `count` items from
        // any single store, but an early store must not starve later ones.
        private static void CollectInboxEmails(dynamic folder, int count, bool unreadOnly,
            List<Email> emails, ref Exception? firstItemError)
        {
            object? items = null;
            object? restrictedItems = null;

            try
            {
                dynamic allItems = items = folder.Items;
                dynamic source = allItems;
                if (unreadOnly)
                {
                    source = restrictedItems = allItems.Restrict("[UnRead] = true");
                }

                source.Sort("[ReceivedTime]", true);

                int total = source.Count;
                int collected = 0;
                for (int i = 1; i <= total && collected < count; i++)
                {
                    object? item = null;
                    try
                    {
                        dynamic mailItem = item = source.Item(i);
                        if (Convert.ToInt32(mailItem.Class) != OlMailItem)
                        {
                            continue;
                        }

                        var email = MapMailItem(mailItem);
                        if (!string.IsNullOrEmpty(email.Id))
                        {
                            emails.Add(email);
                            collected++;
                        }
                    }
                    catch (COMException ex)
                    {
                        // Item vanished or a protected property was denied; skip it like
                        // malformed records are skipped by the AppleScript parser.
                        firstItemError ??= ex;
                    }
                    catch (RuntimeBinderException ex)
                    {
                        firstItemError ??= ex;
                    }
                    finally
                    {
                        Release(item);
                    }
                }
            }
            finally
            {
                Release(restrictedItems);
                Release(items);
            }
        }

        private static Email? GetEmailByEntryId(string id)
        {
            object? app = null;
            object? session = null;
            object? item = null;

            try
            {
                dynamic outlook = app = Activator.CreateInstance(GetOutlookComType())
                    ?? throw new InvalidOperationException("Failed to start the Outlook COM object.");
                dynamic mapi = session = outlook.GetNamespace("MAPI");
                mapi.Logon(Type.Missing, Type.Missing, false, false);
                dynamic mailItem = item = mapi.GetItemFromID(id);
                // GetItemFromID resolves any item class — only map mail.
                if (Convert.ToInt32(mailItem.Class) != OlMailItem)
                {
                    return null;
                }

                return MapMailItem(mailItem);
            }
            catch (COMException)
            {
                return null;
            }
            catch (RuntimeBinderException)
            {
                return null;
            }
            finally
            {
                Release(item);
                Release(session);
                Release(app);
            }
        }

        private static bool MarkEntryIdAsRead(string id)
        {
            object? app = null;
            object? session = null;
            object? item = null;

            try
            {
                dynamic outlook = app = Activator.CreateInstance(GetOutlookComType())
                    ?? throw new InvalidOperationException("Failed to start the Outlook COM object.");
                dynamic mapi = session = outlook.GetNamespace("MAPI");
                mapi.Logon(Type.Missing, Type.Missing, false, false);
                dynamic mailItem = item = mapi.GetItemFromID(id);
                if (Convert.ToInt32(mailItem.Class) != OlMailItem)
                {
                    return false;
                }

                mailItem.UnRead = false;
                mailItem.Save();
                return true;
            }
            catch (COMException)
            {
                return false;
            }
            catch (RuntimeBinderException)
            {
                return false;
            }
            finally
            {
                Release(item);
                Release(session);
                Release(app);
            }
        }

        private static Email MapMailItem(dynamic mailItem)
        {
            return new Email
            {
                Id = Convert.ToString(mailItem.EntryID) ?? string.Empty,
                Subject = Convert.ToString(mailItem.Subject) ?? string.Empty,
                Sender = Convert.ToString(mailItem.SenderName) ?? string.Empty,
                SenderEmail = Convert.ToString(mailItem.SenderEmailAddress) ?? string.Empty,
                ReceivedDate = mailItem.ReceivedTime is DateTime received ? received : DateTime.MinValue,
                IsRead = !Convert.ToBoolean(mailItem.UnRead),
                Body = Convert.ToString(mailItem.Body) ?? string.Empty,
                Folder = "Inbox"
            };
        }

        // EntryIDs are passed to GetItemFromID as parameters — never interpolated into
        // a script — so there is no injection risk; reject only empty input.
        private static string ValidateEntryId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Message id must not be empty.", nameof(id));
            }

            return id;
        }

        private static Type GetOutlookComType()
        {
            return Type.GetTypeFromProgID(OutlookProgId)
                ?? throw new InvalidOperationException(
                    "Classic desktop Outlook is not installed or its COM automation is not registered. " +
                    "The 'New Outlook' client does not expose an automation API.");
        }

        private static void EnsureOutlookIsSupported()
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("Windows Outlook integration requires Windows.");
            }
        }

        private static void Release(object? comObject)
        {
            if (comObject == null)
            {
                return;
            }

            try
            {
                Marshal.ReleaseComObject(comObject);
            }
            catch (COMException)
            {
            }
            catch (InvalidComObjectException)
            {
            }
        }
    }
}
