using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using QuestLog.GUI.Models;
using QuestLog.GUI.Services;

namespace QuestLog.Benchmarks
{
    [MemoryDiagnoser]
    public class EmailParsingBenchmarks
    {
        private string _payload = string.Empty;

        [Params(50, 500, 2000)]
        public int EmailCount { get; set; }

        [Params(1024, 64 * 1024)]
        public int BodySizeBytes { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _payload = BuildWirePayload(EmailCount, BodySizeBytes);
        }

        [Benchmark]
        public int ParseEmails()
        {
            // ParseEmails eagerly materializes a List<Email>; returning a scalar
            // keeps BenchmarkDotNet from flagging a deferred IEnumerable.
            return ((List<Email>)AppleScriptOutlookService.ParseEmails(_payload)).Count;
        }

        // Replicates the <<EMAIL>>-separated, ||-delimited wire format produced by
        // the embedded AppleScript templates (Id|Subject|Sender|SenderEmail|ReceivedDate|IsRead|Body).
        private static string BuildWirePayload(int count, int bodySizeBytes)
        {
            var body = new string('x', bodySizeBytes);
            var builder = new StringBuilder((bodySizeBytes + 128) * count);

            for (int i = 0; i < count; i++)
            {
                builder.Append(i);
                builder.Append("||Subject ").Append(i);
                builder.Append("||Sender ").Append(i);
                builder.Append("||sender").Append(i).Append("@example.com");
                builder.Append("||2026-10-12 10:30:00");
                builder.Append(i % 2 == 0 ? "||true" : "||false");
                builder.Append("||").Append(body);
                builder.Append("<<EMAIL>>");
            }

            return builder.ToString();
        }
    }
}
