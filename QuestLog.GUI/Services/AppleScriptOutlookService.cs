using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestLog.GUI.Interfaces;
using QuestLog.GUI.Models;

namespace QuestLog.GUI.Services
{
    public class AppleScriptOutlookService : IEmailService
    {
        private const string GetEmailsScriptResource = "QuestLog.GUI.Resources.AppleScripts.GetEmails.applescript";
        private const string GetEmailByIdScriptResource = "QuestLog.GUI.Resources.AppleScripts.GetEmailById.applescript";
        private const string MarkAsReadScriptResource = "QuestLog.GUI.Resources.AppleScripts.MarkAsRead.applescript";
        private const string AppleScriptPath = "/usr/bin/osascript";

        public async Task<IEnumerable<Email>> GetEmailsAsync(int count = 50)
        {
            var script = BuildGetEmailsScript(count, unreadOnly: false);
            var result = await ExecuteAppleScriptAsync(script);
            return ParseEmails(result);
        }

        public async Task<IEnumerable<Email>> GetUnreadEmailsAsync(int count = 50)
        {
            var script = BuildGetEmailsScript(count, unreadOnly: true);
            var result = await ExecuteAppleScriptAsync(script);
            return ParseEmails(result);
        }

        public async Task<Email?> GetEmailByIdAsync(string id)
        {
            var script = BuildGetEmailByIdScript(id);
            var result = await ExecuteAppleScriptAsync(script);
            var emails = ParseEmails(result);
            return emails.FirstOrDefault();
        }

        public async Task<bool> MarkAsReadAsync(string id)
        {
            var script = BuildMarkAsReadScript(id);
            var result = await ExecuteAppleScriptAsync(script);
            return result.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildGetEmailsScript(int count, bool unreadOnly)
        {
            var filterClause = unreadOnly ? "whose is read is false" : "";

            return LoadScript(GetEmailsScriptResource)
                .Replace("__COUNT__", count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("__FILTER_CLAUSE__", filterClause, StringComparison.Ordinal);
        }

        private static string BuildGetEmailByIdScript(string id)
        {
            return LoadScript(GetEmailByIdScriptResource)
                .Replace("__MESSAGE_ID__", id, StringComparison.Ordinal);
        }

        private static string BuildMarkAsReadScript(string id)
        {
            return LoadScript(MarkAsReadScriptResource)
                .Replace("__MESSAGE_ID__", id, StringComparison.Ordinal);
        }

        private static string LoadScript(string resourceName)
        {
            var assembly = typeof(AppleScriptOutlookService).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded AppleScript resource '{resourceName}' was not found.");
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }

        private static async Task<string> ExecuteAppleScriptAsync(string script)
        {
            EnsureAppleScriptIsSupported();

            var processInfo = new ProcessStartInfo
            {
                FileName = AppleScriptPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            processInfo.ArgumentList.Add("-e");
            processInfo.ArgumentList.Add(script);

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var message = string.IsNullOrWhiteSpace(error) ? output : error;
                throw new InvalidOperationException($"AppleScript execution failed with exit code {process.ExitCode}: {message.Trim()}");
            }

            return output;
        }

        private static void EnsureAppleScriptIsSupported()
        {
            if (!OperatingSystem.IsMacOS())
            {
                throw new PlatformNotSupportedException("Outlook integration requires macOS.");
            }

            if (!File.Exists(AppleScriptPath))
            {
                throw new FileNotFoundException("Outlook integration requires /usr/bin/osascript.", AppleScriptPath);
            }
        }

        private static IEnumerable<Email> ParseEmails(string rawOutput)
        {
            var emails = new List<Email>();

            if (string.IsNullOrWhiteSpace(rawOutput))
            {
                return emails;
            }

            var emailRecords = rawOutput.Split(new[] { "<<EMAIL>>" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var record in emailRecords)
            {
                var parts = record.Split(new[] { "||" }, 7, StringSplitOptions.None);

                if (parts.Length >= 7)
                {
                    var email = new Email
                    {
                        Id = parts[0].Trim(),
                        Subject = parts[1].Trim(),
                        Sender = parts[2].Trim(),
                        SenderEmail = parts[3].Trim(),
                        ReceivedDate = ParseDate(parts[4].Trim()),
                        IsRead = parts[5].Trim().Equals("true", StringComparison.OrdinalIgnoreCase),
                        Body = parts[6].Trim(),
                        Folder = "Inbox"
                    };

                    emails.Add(email);
                }
            }

            return emails;
        }

        private static DateTime ParseDate(string dateStr)
        {
            var localizedCultures = new[]
            {
                CultureInfo.CurrentCulture,
                CultureInfo.GetCultureInfo("pl-PL")
            };

            foreach (var culture in localizedCultures)
            {
                if (DateTime.TryParse(dateStr, culture, DateTimeStyles.AllowWhiteSpaces, out var localizedResult))
                {
                    return localizedResult;
                }
            }

            string[] formats = {
                "EEEE, MMMM d, yyyy 'at' h:mm:ss a",
                "dddd, d MMMM yyyy 'o' HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "M/d/yyyy h:mm:ss tt",
                "d MMMM yyyy HH:mm:ss",
                "MMMM d, yyyy h:mm:ss tt"
            };

            foreach (var format in formats)
            {
                foreach (var culture in localizedCultures)
                {
                    if (DateTime.TryParseExact(dateStr, format, culture,
                        DateTimeStyles.AllowWhiteSpaces, out var result))
                    {
                        return result;
                    }
                }
            }

            return DateTime.MinValue;
        }
    }
}
