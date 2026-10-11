# QuestLog

[![CI](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CI.yml/badge.svg)](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CI.yml)
[![CD](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CD.yml/badge.svg)](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CD.yml)
[![CodeQL](https://github.com/marcin-przywoski/QuestLog/actions/workflows/codeql.yml/badge.svg)](https://github.com/marcin-przywoski/QuestLog/actions/workflows/codeql.yml)
[![Stable release](https://img.shields.io/github/v/release/marcin-przywoski/QuestLog?label=master)](https://github.com/marcin-przywoski/QuestLog/releases/latest)
[![Dev release](https://img.shields.io/github/v/release/marcin-przywoski/QuestLog?include_prereleases&label=dev)](https://github.com/marcin-przywoski/QuestLog/releases)
[![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4)](https://dotnet.microsoft.com/)
[![Avalonia 11](https://img.shields.io/badge/Avalonia-11-8B44AC)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/platform-macOS-lightgrey)](https://www.apple.com/macos/)
[![Commit activity](https://img.shields.io/github/commit-activity/m/marcin-przywoski/QuestLog)](https://github.com/marcin-przywoski/QuestLog/commits)
[![Repo size](https://img.shields.io/github/repo-size/marcin-przywoski/QuestLog)]()
[![Downloads](https://img.shields.io/github/downloads/marcin-przywoski/QuestLog/total)](https://github.com/marcin-przywoski/QuestLog/releases)

A macOS desktop email client for Microsoft Outlook, built with [Avalonia UI](https://avaloniaui.net/) and .NET 6. QuestLog reads your Outlook inbox through AppleScript automation and presents it in a clean, native-feeling interface.

## Features

- **Inbox browsing** — lists emails from every Outlook folder named "Inbox", with sender, subject, date, and a body preview
- **Reading pane** — full subject, sender, received date, and body for the selected message
- **Unread filter** — toggle to show only unread messages; unread items are rendered in bold
- **Mark as read** — flag selected messages as read directly in Outlook
- **Status feedback** — loading indicator and status bar messages for all operations
- **Auto-updates** — [Velopack](https://velopack.io/)-powered: checks on startup, shows release notes in a banner, one-click restart to apply

## Requirements

| Requirement | Notes |
|---|---|
| macOS | Outlook integration runs through `/usr/bin/osascript`; the app throws `PlatformNotSupportedException` on other OSes when loading mail |
| Microsoft Outlook for Mac | Must be installed and configured with at least one account |
| .NET 6 SDK | Required to build from source |

The first time QuestLog queries Outlook, macOS will prompt for Automation permission — grant it to allow AppleScript access.

## Getting Started

```bash
# Restore dependencies
dotnet restore QuestLog.slnx

# Run the app
dotnet run --project QuestLog.GUI

# Build a Release binary
dotnet build QuestLog.slnx --configuration Release
```

## Usage

1. Click **Load Emails** to fetch the latest messages (up to 50 by default).
2. Select a message in the left pane to read it in the detail pane.
3. Check **Show Unread Only** to filter the list to unread messages.
4. Select a message and click **Mark as Read** to flag it as read in Outlook.

### Updates

- Use **Check for updates** in the toolbar to trigger a manual check.
- When a newer release is found, it downloads in the background and a yellow banner with the release notes appears above the email list — it never hides your mail.
- Click **Restart to update** to apply the downloaded update and relaunch the app.
- Update checks only run for Velopack-installed builds; builds run from source (`dotnet run`) report that updates are unavailable.

## Architecture

```
QuestLog.GUI/
├── Program.cs                    # Entry point — Velopack bootstrap + Avalonia startup
├── App.axaml(.cs)                # Application definition and main window wiring
├── ViewLocator.cs                # ViewModel → View resolution
├── Views/
│   └── MainWindow.axaml          # Toolbar, update banner, email list, detail pane, status bar
├── ViewModels/
│   └── MainWindowViewModel.cs    # LoadEmails / MarkAsRead / CheckForUpdates / ApplyUpdate commands (CommunityToolkit.Mvvm)
├── Models/
│   ├── Email.cs                  # Observable email model
│   ├── AvailableUpdate.cs        # Downloaded update (version + release notes)
│   └── DesignData.cs             # Sample data for the XAML designer
├── Interfaces/
│   ├── IEmailService.cs          # Email provider abstraction
│   └── IUpdateService.cs         # Update provider abstraction
├── Services/
│   ├── AppleScriptOutlookService.cs  # Outlook access via osascript
│   ├── VelopackUpdateService.cs      # Update checks/downloads via Velopack + GitHub releases
│   └── DesignTime*Service.cs         # Designer-only fakes returning DesignData samples
├── Converters/
│   └── BoolToFontWeightConverter.cs  # Unread → bold rendering
└── Resources/AppleScripts/       # Embedded .applescript templates
    ├── GetEmails.applescript
    ├── GetEmailById.applescript
    └── MarkAsRead.applescript
```

**How Outlook integration works:** `AppleScriptOutlookService` loads embedded AppleScript templates, substitutes parameters (message count, unread filter, message id), and executes them via `/usr/bin/osascript -e`. Scripts return `||`-delimited records separated by `<<EMAIL>>`, which the service parses into `Email` models. `IEmailService` keeps the provider swappable — a Windows implementation (e.g. Outlook COM interop or Microsoft Graph) can be added without touching the UI.

**How updates work:** `VelopackUpdateService` wraps a Velopack `UpdateManager` backed by `GithubSource`, which reads the releases published by the CD pipeline. On demand it checks the feed for the channel the install was packed with, downloads the update (delta packages when available), and maps it into an `AvailableUpdate` shown in the banner. **Restart to update** applies the downloaded package and relaunches.

**Key packages:** Avalonia 11 (Fluent theme, Inter font), CommunityToolkit.Mvvm 8.2 (source-generated `[ObservableProperty]`/`[RelayCommand]`), Velopack 0.0.556.

## Versioning & Releases

Versioning is driven by [GitVersion](https://gitversion.net/) (`GitVersion.yml`, GitHub Flow):

- `master` — stable releases
- `development` — patch increments, with a `dev` prerelease label
- `feature/*` and `enhancement/*` — minor increments
- `fix/*` — inherits its increment from `develop`
- `hotfix/*` — patch increments from `master`

Local tools: `dotnet tool restore` installs GitVersion.Tool and GitReleaseManager.Tool (see `dotnet-tools.json`).

### CI (`.github/workflows/CI.yml`)

On pushes/PRs to `master`, `development`, `feature/**`, `enhancement/**`, `fix/**`, `hotfix/**`:

1. `dotnet format --verify-no-changes` lint gate
2. GitVersion computes SemVer (tag pushes are verified against it in CD)
3. `dotnet restore` + `dotnet build` (Release)

### CD (`.github/workflows/CD.yml`)

On pushes to `master`/`development` and `*.*.*` tags (Windows runner):

1. GitVersion computes the version; tag builds must match the tag's version
2. `dotnet publish -r win-x64` builds the application; `vpk pack` builds a Velopack package, using the previous release on the same channel for deltas when available
3. Release notes are generated via `gh api releases/generate-notes`; `dotnet gitreleasemanager create` creates a **draft** release (`--pre` for prereleases)
4. Velopack assets are uploaded with `vpk upload github --merge`, then `dotnet gitreleasemanager publish` publishes the release

**Update channels:** stable releases feed the default `win` channel; prereleases feed `win-{label}` (`development` builds → `win-dev`). Installed apps only receive updates from the channel their package was built for — stable installs never see `dev` builds and vice versa.

A `dev-sync.yml` workflow automatically opens a `master` → `development` sync PR after pushes to `master` (e.g. hotfixes).

## Contributing

- Follow the existing `.editorconfig` style — CI enforces `dotnet format`.
- Branch from `development` for features (`feature/*`, `enhancement/*`) and fixes (`fix/*`, `hotfix/*`); hotfixes branch from `master`.
- Keep the `IEmailService` contract intact when adding providers.
