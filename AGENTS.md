# Repository Guidelines

## Project Overview

QuestLog is a desktop email reader for **macOS Outlook**, built with **Avalonia 11 / .NET 6** using MVVM (CommunityToolkit.Mvvm source generators). It reads mail by executing embedded AppleScript templates through `/usr/bin/osascript`. Velopack provides auto-updates.

**Critical:** the app is macOS-only at runtime (`OperatingSystem.IsMacOS()` guard → `PlatformNotSupportedException` elsewhere), but builds and launches on Windows — service calls fail gracefully into the status bar.

## Architecture & Data Flow

Single-project MVVM app — `QuestLog.slnx` contains only `QuestLog.GUI/QuestLog.GUI.csproj`.

```
Toolbar command → IEmailService → embedded .applescript (__TOKEN__ substitution)
  → /usr/bin/osascript -e <script> → stdout (<<EMAIL>> records, "||" fields)
  → ParseEmails → ObservableCollection<Email> → compiled bindings
```

- **MVVM via CommunityToolkit.Mvvm 8.2.1 source generators** — NOT ReactiveUI. `[ObservableProperty]` on `_camelCase` fields → `PascalCase` properties; `[RelayCommand]` on `async Task XxxAsync()` → `XxxCommand`; change hooks via `partial void OnXxxChanged(...)`. VMs must be `partial` and inherit `ViewModelBase : ObservableObject`.
- **ViewLocator convention** (`ViewLocator.cs`): `QuestLog.GUI.ViewModels.FooViewModel` ↔ `QuestLog.GUI.Views.FooView` — name-symmetric or the view won't resolve (fallback renders `TextBlock "Not Found: …"`). Annotated `[RequiresUnreferencedCode]` (trimming trap).
- **No DI container.** `App.axaml.cs` news up `MainWindowViewModel`; its parameterless ctor news up `AppleScriptOutlookService`. An `IEmailService` ctor overload exists for testability. Follow ctor-overload injection; don't half-wire a container.
- **Compiled bindings on by default** (`AvaloniaUseCompiledBindingsByDefault=true`); views declare `x:DataType` on the root and per-`DataTemplate`.
- **Startup order matters** (`Program.cs`): `VelopackApp.Build().Run()` runs first (may exit/restart for updates), then `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`. `BuildAvaloniaApp` is also used by the visual designer — keep it. `App.axaml.cs` also removes `DataAnnotationsValidationPlugin` to avoid double validation with CommunityToolkit.

## Key Directories

|Path|Purpose|
|---|---|
|`QuestLog.GUI/ViewModels/`|VMs (`MainWindowViewModel`, `ViewModelBase`)|
|`QuestLog.GUI/Views/`|AXAML views (`MainWindow.axaml` + trivial code-behind)|
|`QuestLog.GUI/Models/`|`Email : ObservableObject` (only `IsRead` uses `SetProperty`; other props are plain)|
|`QuestLog.GUI/Services/`|`AppleScriptOutlookService` — sole `IEmailService` impl (process exec, wire parsing, token substitution)|
|`QuestLog.GUI/Interfaces/`|`IEmailService` (4 async methods; `GetEmailByIdAsync` currently unused)|
|`QuestLog.GUI/Resources/AppleScripts/`|Embedded `*.applescript` templates (auto-embedded by csproj glob)|
|`QuestLog.GUI/Converters/`|`BoolToFontWeightConverter` (unread = bold), registered app-wide|
|`QuestLog.GUI/Assets/`|`AvaloniaResource` glob (app icon)|
|`scripts/`|`Invoke-CodeQlScan.ps1` — local CodeQL runner|
|`.github/workflows/`|`CI.yml`, `CD.yml`, `codeql.yml`, `dev-sync.yml`|

## Development Commands

```bash
dotnet restore QuestLog.slnx                          # restore (CI adds --locked-mode)
dotnet run --project QuestLog.GUI                     # run (Debug); --project REQUIRED, bare `dotnet run` fails
dotnet build QuestLog.slnx --configuration Release    # CI build
dotnet format --verify-no-changes QuestLog.slnx       # lint gate (whitespace only — same as CI)
dotnet tool restore && dotnet gitversion              # SemVer via GitVersion 6.8.2
pwsh scripts/Invoke-CodeQlScan.ps1                    # local CodeQL → artifacts/codeql/*.sarif
```

**SDK requirement:** `QuestLog.slnx` is the XML solution format — `dotnet`/MSBuild **≥9.0.200** (or VS 17.10+) is required to parse it, even though the project targets net6.0. On SDK ≤8 every command fails with MSB4068; `dotnet format` crashes. There is no `global.json` — local SDK resolves to whatever is installed.

Always pass `QuestLog.slnx` or `--project QuestLog.GUI` explicitly. `BuildInfo.targets` at repo root is a **dead leftover** (untracked, unreferenced, would generate `namespace SharpGallery`) — ignore it.

## Code Conventions & Common Patterns

Enforced by `.editorconfig` + CI `dotnet format` gate — **whitespace/formatting only**. Every naming and style rule is suggestion-level (no build/CI failure): 4-space indent, LF endings (CRLF checkouts via `core.autocrlf` will fail the verify), block-scoped namespaces, usings outside namespace, prefer explicit types (`csharp_style_var_* = false` — convention, not enforced), `_camelCase` private fields, `PascalCase` publics, `I`-prefixed interfaces in `Interfaces/`, namespace = folder. Roslynator.Analyzers 3.1.0 + Formatting.Analyzers 1.1.0 are referenced (analyzers-only).

- **Error handling:** services throw typed exceptions (`PlatformNotSupportedException`, `FileNotFoundException`, `InvalidOperationException`, `ArgumentException`); VM commands `try/catch (Exception ex)` → `StatusMessage = $"Error: {ex.Message}"`, `IsLoading` reset in `finally`. No logging, no dialogs, no rethrow.
- **Async:** `async Task` methods suffixed `Async`; no `async void`. One deliberate fire-and-forget: `_ = ReloadEmailsAsync()` in `OnShowUnreadOnlyChanged`, guarded by `_reloadRequested`/`_isReloading` coalescing flags that await the in-flight `LoadEmailsCommand.ExecutionTask`.
- **Process exec:** `ProcessStartInfo.ArgumentList` passes the entire script as one `-e` arg (no shell quoting); stdout/stderr `ReadToEndAsync` started before `WaitForExitAsync` (deadlock-safe); 2-minute timeout kills the process tree; non-zero exit → `InvalidOperationException` with stderr.

**AppleScript conventions** (`AppleScriptOutlookService.cs`):

- New script: drop `*.applescript` in `Resources/AppleScripts/` (auto-embedded; LogicalName `QuestLog.GUI.Resources.AppleScripts.<name>.applescript`), add a `const string` resource name, load via `LoadScript`, parameterize with `__TOKEN__` + ordinal `string.Replace` (`__COUNT__`, `__FILTER_CLAUSE__`, `__MESSAGE_ID__` — the id is validated all-digits then interpolated unescaped).
- Wire format is a contract: records separated by `<<EMAIL>>`, fields by `||`, exactly 7 fields in order `Id | Subject | Sender | SenderEmail | ReceivedDate | IsRead | Body` (split capped at 7 so `||` in body is absorbed; scripts also `sanitizeText` to strip separators). Keep scripts and `ParseEmails` in sync; malformed records are silently skipped.
- `MarkAsRead` contract: script must return literal `true`/`false` (compared OrdinalIgnoreCase).
- `ParseDate` tries CurrentCulture + `pl-PL` + 6 explicit formats; unparseable → `DateTime.MinValue` (never throws). Add new Outlook-localized formats to the array.

## Important Files

- `QuestLog.GUI/Program.cs` — entry point (`[STAThread] Main`, Velopack first)
- `QuestLog.GUI/App.axaml(.cs)` — FluentTheme, ViewLocator + converter registration, manual VM wiring
- `QuestLog.GUI/ViewLocator.cs` — VM→View name convention
- `QuestLog.GUI/QuestLog.GUI.csproj` — net6.0 WinExe, `AssemblyName=QuestLog` (binary name ≠ project name), `AvaloniaUseCompiledBindingsByDefault`, AppleScript embed glob, `RestorePackagesWithLockFile`
- `QuestLog.slnx` — authoritative project list (XML format)
- `QuestLog.GUI/packages.lock.json` — **git-tracked lock file**; regenerate whenever package refs change or CI `--locked-mode` restore fails
- `GitVersion.yml` — GitHubFlow/v1; `develop` regex `^dev(elop)?(elopment)?$` matches `dev`/`develop`/`development`, label **`dev`** (channels `win-dev`, tags `0.1.0-dev.*`); `feature`→Minor, `fix`→Inherit, `hotfix`→Patch, `enhancement`→Minor from develop
- `dotnet-tools.json` — at repo root (not `.config/`): `gitversion.tool` 6.8.2, `gitreleasemanager.tool` 0.20.0
- `GitReleaseManager.yaml` + `.github/release.yml` — changelog label configs (see below)
- `.editorconfig` — style authority; scopes `[*.{cs,vb}]`/`[*.cs]` only — **nothing applies to .axaml/.yml/.ps1/.md**

## Runtime/Tooling Preferences

- **Target:** net6.0 `WinExe`; **parsing .slnx needs .NET SDK ≥9.0.200** (no `global.json`).
- NuGet via `dotnet restore`; local tools via `dotnet tool restore` (manifest at root — works fine).
- Dependencies: Avalonia 11.0.0 (+Desktop, Fluent theme, Inter font, Diagnostics **Debug-only**), CommunityToolkit.Mvvm 8.2.1, Velopack 0.0.556, Roslynator analyzers.
- Releases: push to `master`/`development` or tag `*.*.*` → CD on `windows-latest` → `dotnet publish -r win-x64` → `vpk pack` (Velopack, deltas) → `gh api releases/generate-notes` → `dotnet gitreleasemanager create` (draft; `--pre` on prereleases) → `vpk upload github --merge` → `gitreleasemanager publish`.
- Release naming: tag push → tag name (sans `v`, must equal GitVersion SemVer or job exits 1); prerelease → `SemVer` (`0.1.0-dev.N`); master → `MajorMinorPatch`. Tags are bare SemVer, no `v` prefix.
- Velopack channels: stable → `win`; prerelease → `win-{PreReleaseLabel}` (development → `win-dev`). The channel is passed to `vpk download` (**`--pre` is required there on prerelease runs** — without it only stable releases are inspected and no delta feed is found), `vpk pack` (also `-r win-x64` — vpk defaults to x86 otherwise), and `vpk upload`. `vpk` CLI is installed globally unpinned (skew risk vs Velopack 0.0.556 lib).
- Branch pushes derive release name from GitVersion; each release creates its tag, feeding the next version computation.
- Sync: `dev-sync.yml` opens/auto-merges a `master`→`development` PR after pushes to `master` (merge commit, not squash). It uses `GITHUB_TOKEN` — merges by that token do NOT trigger downstream workflows, so the synced push won't kick off CI/CD.

## Testing & QA

**No test projects exist** — no framework, no test step in CI. QA gates:

1. `dotnet format --verify-no-changes QuestLog.slnx` (CI lint gate, whitespace-only — run before committing).
2. `dotnet restore QuestLog.slnx --locked-mode` + Release build (CI).
3. CodeQL (`codeql.yml`: csharp, `security-extended` + `security-and-quality`, PRs + weekly cron; local equivalent: `pwsh scripts/Invoke-CodeQlScan.ps1 [-Configuration Debug|Release] [-ReuseDatabase] [-SkipCommunityPack]`, SARIF → `artifacts/codeql/{official,community}.sarif`, DB at `.codeql-db/`; requires `gh` CLI).

CI/CodeQL triggers are **path-filtered to `**.cs`, `**.csproj`, `**.axaml`** — docs/config/workflow-only changes never run them. CodeQL triggers on PRs only (no push) and omits `hotfix/**` (asymmetry vs CI).

**Changelog labels:** PR labels control release notes — `feature`/`enhancement`/`bugfix`/`dependencies`/`breaking` map to sections in both `.github/release.yml` and `GitReleaseManager.yaml`; `ignore-for-release`/`release` exclude; unlabeled → "Other Changes". Note: `bugfix` is a label, distinct from `fix/*` branch prefix. Dependabot targets `development` (nuget daily, actions weekly) with `dependencies` label.

## Known Quirks

- **CI/CD SDK pin is broken:** `CI.yml`/`CD.yml` pin `dotnet-version: "6"`, but SDK 6 cannot parse `.slnx` (MSB4068 / format crash, verified). Pipelines as written cannot succeed unless the pin is raised to ≥9.0.200.
- `GitVersion.yml`: the built-in `main` rule (`^master$|^main$`, Patch increment) **shadows the user `master:` block** (`mode: ContinuousDeployment`, `increment: None`) — the block is dead config.
- `CD.yml:76` comment says `win-alpha` channel — actual is `win-dev` (comment is stale, code is right).
- `Email` model: only `IsRead` raises change notification; other properties are plain auto-props.
- `GetEmailByIdAsync`/`GetEmailById.applescript` and the `IEmailService` ctor overload have no callers.
- CD ships a `win-x64` Velopack package for a macOS-only app.
- `.vscode/settings.json` hardcodes author-machine absolute paths (`axaml.selectedSolution`, dotrush); `QuestLog.GUI/.vscode/settings.json` has a stale solution path.
- Design-time `MainWindow.axaml` sample emails use non-numeric `Id`s (`email-001`) — would fail `ValidateMessageId` if MarkAsRead ran on them; harmless at design time.
- `BuildInfo.targets` — untracked dead file, would emit `namespace SharpGallery`; ignore/delete.
- `dotnet format` enforces whitespace only; every style rule (`var`, naming, braces) is suggestion-level — no gate.
