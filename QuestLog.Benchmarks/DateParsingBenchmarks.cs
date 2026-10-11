using System;
using BenchmarkDotNet.Attributes;
using QuestLog.GUI.Services;

namespace QuestLog.Benchmarks
{
    [MemoryDiagnoser]
    public class DateParsingBenchmarks
    {
        // Valid ISO-ish input: parses on the first TryParse (CurrentCulture) —
        // the hot path for every ReceivedDate field in each email record.
        [Benchmark]
        public DateTime ParseDate_Valid()
        {
            return AppleScriptOutlookService.ParseDate("2026-10-12 10:30:00");
        }

        // Unparseable input: exercises the full fallback chain (2 cultures ×
        // TryParse + 6 formats × 2 cultures × TryParseExact) — the worst case.
        [Benchmark(Baseline = true)]
        public DateTime ParseDate_Invalid()
        {
            return AppleScriptOutlookService.ParseDate("not a date at all");
        }
    }
}
