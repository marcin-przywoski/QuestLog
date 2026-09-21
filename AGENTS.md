# Repository Guidelines

## Project Overview

QuestLog is a desktop email reader for **macOS Outlook**, built with **Avalonia 11 / .NET 6** using MVVM (CommunityToolkit.Mvvm). It reads mail by executing embedded AppleScript templates through `/usr/bin/osascript`. Velopack provides auto-updates.

**Critical:** the app is macOS-only at runtime (`OperatingSystem.IsMacOS()` guard → `PlatformNotSupportedException` elsewhere), but builds and launches on Windows — service calls fail gracefully into the status bar.

## Architecture & Data Flow

Single-project MVVM app — `QuestLog.slnx` contains only `QuestLog.GUI/QuestLog.GUI.csproj`.

```
Toolbar command → IEmailService → embedded .applescript (__TOKEN__ substitution)
  → /usr/bin/osascript -e → stdout (<<EMAIL>> records, "||" fields)
  → ParseEmails → ObservableCollection<Email> → compiled bindings
```

- **MVVM via CommunityToolkit.Mvvm 8.2.1 source generators** — NOT ReactiveUI. `[ObservableProperty]` on `_camelCase` fields → `PascalCase` properties; `[RelayCommand]` on `async Task XxxAsync()` → `XxxCommand`; change hooks via `partial void OnXxxChanged(...)`.
- **ViewLocator convention** (`ViewLocator.cs`): `QuestLog.GUI.ViewModels.FooViewModel` ↔ `QuestLog.GUI.Views.FooView` — name-symmetric or the view won't resolve (fallback renders `TextBlock "Not Found: …"`). VMs must inherit `ViewModelBase : ObservableObject`.
- **No DI container.** `App.axaml.cs` news up `MainWindowViewModel`; its parameterless ctor news up `AppleScriptOutlookService`. An `IEmailService` ctor overload exists for testability. Follow ctor-overload injection; don't half-wire a container.
- **Compiled bindings on by default** (`AvaloniaUseCompiledBindingsByDefault=true`); views declare `x:DataType`.
- **Startup order matters** (`Program.cs`): `VelopackApp.Build().Run()` runs first (may exit/restart for updates), then `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`. `BuildAvaloniaApp` is also used by the visual designer — keep it.

## Key Directories

| Path | Purpose |
|---|---|
| `QuestLog.GUI/ViewModels/` | VMs (`MainWindowViewModel`, `ViewModelBase`) |
| `QuestLog.GUI/Views/` | AXAML views (`MainWindow.axaml` + trivial code-behind) |
| `QuestLog.GUI/Models/` | `Email : ObservableObject` (`IsRead` uses `SetProperty`) |
| `QuestLog.GUI/Services/` | `AppleScriptOutlookService` — sole `IEmailService` impl |
| `QuestLog.GUI/Interfaces/` | `IEmailService` (4 async methods) |
| `QuestLog.GUI/Resources/AppleScripts/` | Embedded `*.applescript` templates (auto-embedded by csproj glob) |
| `QuestLog.GUI/Converters/` | `BoolToFontWeightConverter` (unread = bold) |
| `scripts/` | `Invoke-CodeQlScan.ps1` — local CodeQL runner |
| `.github/workflows/` | `CI.yml`, `CD.yml`, `codeql.yml` |

## Development Commands

```bash
dotnet restore QuestLog.slnx                          # restore
dotnet run --project QuestLog.GUI                     # run (Debug)
dotnet build QuestLog.slnx --configuration Release    # CI build
dotnet format --verify-no-changes QuestLog.slnx       # lint gate (same as CI)
dotnet tool restore && dotnet gitversion              # SemVer via GitVersion 6.8.2
pwsh scripts/Invoke-CodeQlScan.ps1                    # local CodeQL → artifacts/codeql/*.sarif
```

**Always pass `QuestLog.slnx` or `QuestLog.GUI/QuestLog.GUI.csproj` explicitly.**. Bare `dotnet build`/`publish`/`format` at repo root silently picks it up. `BuildInfo.targets` is likewise dead (generates `namespace SharpGallery`).

## Code Conventions & Common Patterns

Enforced by `.editorconfig` + CI `dotnet format` gate (style rules are suggestion-level; whitespace/formatting is hard-enforced):

- 4-space indent, LF endings, braces always required, block-scoped namespaces, usings outside namespace.
- **Explicit types — `var` is disabled everywhere.** Predefined keywords (`int`, `string`) for locals.
- `_camelCase` private fields (required by `[ObservableProperty]`), `PascalCase` publics, `I`-prefixed interfaces in `Interfaces/`, namespace = folder.
- Prefer auto-properties, `readonly`, pattern matching, `is null`, null-propagation. Expression-bodied properties yes; methods no. Keep `using {}` blocks (simple using statement NOT preferred).
- **Error handling:** services throw typed exceptions (`PlatformNotSupportedException`, `FileNotFoundException`, `InvalidOperationException`); VM commands `try/catch (Exception ex)` → `StatusMessage = ex.Message`, `IsLoading` reset in `finally`. No logging, no dialogs, no rethrow.
- Async: `async Task` methods suffixed `Async`. One deliberate fire-and-forget: `_ = LoadEmailsAsync()` in `OnShowUnreadOnlyChanged`.

**AppleScript conventions** (`AppleScriptOutlookService.cs`):

- New script: drop `*.applescript` in `Resources/AppleScripts/` (auto-embedded; LogicalName `QuestLog.GUI.Resources.AppleScripts.<name>.applescript`), add a `const string` resource name, load via `LoadScript`, parameterize with `__TOKEN__` + ordinal `string.Replace`. Note: `id` is interpolated without escaping.
- Wire format is a contract: records separated by `<<EMAIL>>`, fields by `||`, exactly 7 fields (Id, Subject, Sender, SenderEmail, ReceivedDate, IsRead, Body). Keep scripts and `ParseEmails` in sync.
- `ParseDate` tries CurrentCulture + `pl-PL` + 6 explicit formats; unparseable → `DateTime.MinValue` (never throws).

## Important Files

- `QuestLog.GUI/Program.cs` — entry point (`[STAThread] Main`, Velopack first)
- `QuestLog.GUI/App.axaml(.cs)` — FluentTheme, ViewLocator registration, manual VM wiring
- `QuestLog.GUI/ViewLocator.cs` — VM→View name convention (`[RequiresUnreferencedCode]`)
- `QuestLog.GUI/QuestLog.GUI.csproj` — net6.0 WinExe, `AssemblyName=QuestLog` (binary name ≠ project name)
- `QuestLog.slnx` — authoritative project list (XML format; needs .NET 9+ SDK / VS 17.10+ to parse, though the project targets net6.0)
- `GitVersion.yml` — GitHubFlow/v1; `master`=ContinuousDeployment, `feature`=Minor, `hotfix`=Patch
- `dotnet-tools.json` — at repo root (not `.config/`): `gitversion.tool` 6.8.2
- `.editorconfig` — style authority

## Runtime/Tooling Preferences

- **.NET 6 SDK** (CI pins `"6"`; no `global.json`). Note: `.slnx` parsing needs newer SDK/IDE.
- NuGet via `dotnet restore`; local tools via `dotnet tool restore` (GitVersion only).
- Dependencies: Avalonia 11.0.0 (+Desktop, Fluent theme, Inter font, Diagnostics debug-only), CommunityToolkit.Mvvm 8.2.1, Velopack 0.0.556.
- Releases: tag `*.*.*` → CD on `windows-latest` → `dotnet publish -r win-x64` → `vpk pack` (Velopack, deltas) → `gh release create` → `vpk upload`.

## Testing & QA

**No test projects exist** — no framework, no test step in CI. QA gates:

1. `dotnet format --verify-no-changes` (CI lint gate — run before committing).
2. Release build of `QuestLog.slnx`.
3. CodeQL (`codeql.yml`: csharp, security-extended + security-and-quality, PRs + weekly cron; local equivalent: `scripts/Invoke-CodeQlScan.ps1`, SARIF → `artifacts/codeql/`).

## Known Quirks

- Branch-name mismatch: CI triggers on `development`, but `dependabot.yml` targets `develop` (GitVersion's regex matches both).
- CD ships a `win-x64` Velopack package for a macOS-only app.
- `.vscode/settings.json` hardcodes an absolute Windows path in `axaml.selectedSolution`.
