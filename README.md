# QuestLog

[![CI](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CI.yml/badge.svg)](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CI.yml)
[![CD](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CD.yml/badge.svg)](https://github.com/marcin-przywoski/QuestLog/actions/workflows/CD.yml)
[![CodeQL](https://github.com/marcin-przywoski/QuestLog/actions/workflows/codeql.yml/badge.svg)](https://github.com/marcin-przywoski/QuestLog/actions/workflows/codeql.yml)
[![.NET 6](https://img.shields.io/badge/.NET-6.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-macOS-lightgrey)](https://www.apple.com/macos/)
[![Downloads](https://img.shields.io/github/downloads/marcin-przywoski/QuestLog/total)](https://github.com/marcin-przywoski/QuestLog/releases)

A macOS desktop email client for Microsoft Outlook, built with [Avalonia UI](https://avaloniaui.net/) and .NET 6. QuestLog reads your Outlook inbox through AppleScript automation and presents it in a clean, native-feeling interface.

## Features

- **Inbox browsing** — lists emails from every Outlook folder named "Inbox", with sender, subject, date, and a body preview
- **Reading pane** — full subject, sender, received date, and body for the selected message
- **Unread filter** — toggle to show only unread messages; unread items are rendered in bold
- **Mark as read** — flag selected messages as read directly in Outlook
- **Status feedback** — loading indicator and status bar messages for all operations
- **Auto-updates** — [Velopack](https://velopack.io/) integration for seamless in-app updates

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

## Architecture

```
QuestLog.GUI/
├── Program.cs                    # Entry point — Velopack bootstrap + Avalonia startup
├── App.axaml(.cs)                # Application definition and main window wiring
├── ViewLocator.cs                # ViewModel → View resolution
├── Views/
│   └── MainWindow.axaml          # Toolbar, email list, detail pane, status bar
├── ViewModels/
│   └── MainWindowViewModel.cs    # LoadEmails / MarkAsRead commands (CommunityToolkit.Mvvm)
├── Models/
│   └── Email.cs                  # Observable email model
├── Interfaces/
│   └── IEmailService.cs          # Email provider abstraction
├── Services/
│   └── AppleScriptOutlookService.cs  # Outlook access via osascript
├── Converters/
│   └── BoolToFontWeightConverter.cs  # Unread → bold rendering
└── Resources/AppleScripts/       # Embedded .applescript templates
    ├── GetEmails.applescript
    ├── GetEmailById.applescript
    └── MarkAsRead.applescript
```

**How Outlook integration works:** `AppleScriptOutlookService` loads embedded AppleScript templates, substitutes parameters (message count, unread filter, message id), and executes them via `/usr/bin/osascript -e`. Scripts return `||`-delimited records separated by `<<EMAIL>>`, which the service parses into `Email` models. `IEmailService` keeps the provider swappable — a Windows implementation (e.g. Outlook COM interop or Microsoft Graph) can be added without touching the UI.

**Key packages:** Avalonia 11 (Fluent theme, Inter font), CommunityToolkit.Mvvm 8.2 (source-generated `[ObservableProperty]`/`[RelayCommand]`), Velopack 0.0.556.

## Versioning & Releases

Versioning is driven by [GitVersion](https://gitversion.net/) (`GitVersion.yml`, GitHub Flow):

- `master` — continuous deployment
- `feature/*` — minor increments
- `fix/*` — inherits from `develop`
- `hotfix/*` — patch increments from `master`

Local tool: `dotnet tool restore` installs `GitVersion.Tool` (see `dotnet-tools.json`).

### CI (`.github/workflows/CI.yml`)

On pushes/PRs to `master`, `development`, `feature/**`, `fix/**`:

1. `dotnet format --verify-no-changes` lint gate
2. GitVersion computes SemVer (tag pushes are verified against the computed version)
3. `dotnet restore` + `dotnet build` (Release)

### CD (`.github/workflows/CD.yml`)

On `*.*.*` tag pushes (Windows runner):

1. `dotnet publish -r win-x64`
2. `vpk pack` builds a Velopack package (with delta updates against the previous release)
3. GitHub release created via `gh release create`, Velopack assets uploaded with `vpk upload github`

## Contributing

- Follow the existing `.editorconfig` style — CI enforces `dotnet format`.
- Branch from `development` for features (`feature/*`) and fixes (`fix/*`); hotfixes branch from `master`.
- Keep the `IEmailService` contract intact when adding providers.
