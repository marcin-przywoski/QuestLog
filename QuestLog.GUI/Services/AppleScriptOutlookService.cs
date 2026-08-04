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
