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
            var processInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            processInfo.ArgumentList.Add("-e");
            processInfo.ArgumentList.Add(script);

            using var process = new Process { StartInfo = processInfo };

            try
            {
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"AppleScript error: {error}");
                }

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute AppleScript: {ex.Message}");
                return string.Empty;
            }
        }

    }
}
