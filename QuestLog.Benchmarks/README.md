# QuestLog.Benchmarks

BenchmarkDotNet harness for measuring allocation/memory behavior of QuestLog's hot paths — currently the AppleScript wire-format parser (`AppleScriptOutlookService.ParseEmails`) and its per-record `ParseDate` fallback chain.

This project is intentionally **not** part of `QuestLog.slnx`: `CD.yml` runs a bare `dotnet publish` on the solution, and a benchmark exe in the solution would be published and packed into the Velopack installer.

## Run

```bash
# All benchmarks, Release build (required — Debug is rejected by BenchmarkDotNet)
dotnet run --project QuestLog.Benchmarks -c Release

# Filter to one class / one method
dotnet run --project QuestLog.Benchmarks -c Release -- --filter '*EmailParsing*'
dotnet run --project QuestLog.Benchmarks -c Release -- --filter '*ParseDate_Valid*'

# Fast smoke run (single short iteration, results are not statistically valid)
dotnet run --project QuestLog.Benchmarks -c Release -- --job Dry
```

Every benchmark class uses `[MemoryDiagnoser]`, so the report includes `Allocated` bytes and `Gen0/1/2` collections per operation in addition to timings. Full reports land in `QuestLog.Benchmarks/BenchmarkDotNet.Artifacts/` (gitignored).

## Coverage notes

- `ParseEmails` is parameterized over email count (`50/500/2000`) and body size (`1 KiB/64 KiB`) — the dominant allocations are `string.Split` arrays, per-field `Trim`, and the `Email` objects themselves.
- `ParseDate` covers the hit-on-first-`TryParse` path and the worst case: an unparseable string walks all cultures × explicit formats before returning `DateTime.MinValue`.
- `IEmailService`'s osascript calls are macOS-only and not benchmarkable cross-platform; parsing is the platform-neutral memory-relevant surface.
