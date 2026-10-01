# HuntAlerts-JP - Japanese Relay Fork

A Japanese relay fork of [HuntAlerts](https://github.com/huntsffxiv/huntalerts). Original authors: Asuna / HuntsFFXIV. Japanese relay changes: MintakaSeiran.

This repository was forked from the original GitHub repository and incorporates the history of the [current upstream on GitLab](https://projects.gamba.pro/Asuna/huntalerts). The `main` branch contains the upstream 1.4.1.7 baseline. The `jp_relay` branch contains this fork's changes. See the [changes compared with the upstream baseline](https://github.com/MintakaSeiran/HuntAlerts-JP/compare/main...jp_relay).

For this fork, use [HuntAlerts-JP Issues](https://github.com/MintakaSeiran/HuntAlerts-JP/issues). The original plugin repository is `https://puni.sh/api/repository/asuna`; its support channel is `asuna-plugins` in the [Puni.sh Discord](https://discord.gg/punishxiv).

This fork retains the upstream project's [AGPL-3.0-or-later](LICENSE) license declaration. Refer to the ECommons submodule for its license.

## Download and Installation

[Download the compiled plugin ZIP](https://github.com/MintakaSeiran/HuntAlerts-JP/releases/download/1.4.1.7_jp_min/HuntAlerts-JP-1.4.1.7_jp_min.zip) | [Release notes and checksum](https://github.com/MintakaSeiran/HuntAlerts-JP/releases/tag/1.4.1.7_jp_min)

This is a prerelease: the build and automated checks pass, but in-game behavior has not been verified.

To install through Dalamud, add this URL to **Custom Plugin Repositories**:

```text
https://raw.githubusercontent.com/MintakaSeiran/HuntAlerts-JP/jp_relay/pluginmaster.json
```

This fork retains the internal name `HuntAlerts`. Disable the original plugin and avoid installing or updating both variants simultaneously. Verify that the installed plugin's repository URL points to `MintakaSeiran/HuntAlerts-JP`. The same numeric version is used by upstream, so an existing upstream installation is not guaranteed to switch variants through an automatic update.

For manual development installation, extract the entire ZIP into a development plugin folder and register `HuntAlerts.dll` in Dalamud's development plugin locations. Keep the manifest and all bundled dependencies beside the DLL. After loading, verify that the display name includes the Japanese relay suffix. To revert, disable this fork and enable the original plugin.

## Japanese Relay Changes

Updated October 1, 2026. Based on upstream commit `69ec1140add0e5ce97553785c8ad04d04ac68224` (1.4.1.7).

InformationalVersion: `1.4.1.7_jp_min`. AssemblyVersion and the Dalamud manifest version remain `1.4.1.7`. The build targets Dalamud API 15, .NET 10, C# 14, and x64.

### Relay Message Format

Messages follow the layout used by AS Mob Plate recruitment comments. This example is translated into English for documentation; actual relay messages use Japanese names and labels where available:

```text
[S] Zona Seeker / Western Thanalan / Nearest: Horizon / DC: Meteor / Server: Yojimbo / POS: (26.8, 16.9)
```

- The DC is resolved from the alert's target world, not the player's current DC or world.
- Creature names are matched between English and Japanese BNpcName rows by row ID. Area and aetheryte names use the alert's IDs and Japanese PlaceName data, with English name matching when IDs are unavailable. Untranslated names retain their original spelling.
- The nearest aetheryte field uses the destination selected by upstream for the alert. This fork does not change the distance calculation.
- Valid coordinates appear in POS with one decimal place. Older alerts can also use coordinate strings. Missing, zero, non-finite, or negative coordinates are omitted.
- When `Add clickable map flag when relaying` is enabled and the required map ID and coordinates are available, `<flag>` is appended. POS remains visible. If setting the flag throws an exception, the message is sent without the link.
- Instance numbers of 2 or higher are included. No unconfirmed start ET is added. Unknown DC, server, and name fields are omitted.
- Train alerts use a Japanese hunt train label and expansion name, distinguishing them from S-rank alerts.
- Messages exceeding 500 UTF-8 bytes, including the channel command, are not sent. An explanation appears in the player's chat instead of silently truncating information.

### Settings

1. Open `/huntalerts settings` and find the Japanese relay checkbox under Notifications. Its Japanese label means "Send Relay in Japanese (AS Mob Plate format)." It is enabled by default. Disable it to restore the original English format.
2. Use the alert's Relay button to share through the configured channel.
3. Select `Echo (test)` from the adjacent arrow menu to check the actual message in your own chat before using a shared channel.

The only added configuration field is `JapaneseRelay=true`. It defaults to enabled even when absent from an existing configuration file. Configuration version 4, history version 2, the default relay channel, and IPC argument types remain unchanged. Updates to the original plugin do not include this fork's changes.

Development and automated verification did not overwrite installed plugins or configuration files, or send in-game chat messages.

## Building and Verification

ECommons is pinned to submodule commit `96b6bbc9896aead9f67ea1b245b89ed510a61c88` (3.2.1.6). Upstream lock files are retained, using ECommons.IPC 1.0.0.19, SocketIOClient 3.1.1, and NAudio 2.2.1.

Clone the feature branch and its submodule:

```powershell
git clone --recurse-submodules --branch jp_relay https://github.com/MintakaSeiran/HuntAlerts-JP.git
cd HuntAlerts-JP
```

Run the following commands from the repository root. The SDK path is an example from the Windows development machine; on another machine, set `$xlDotnet` to an existing .NET 10 SDK's `dotnet.exe`. Verify the SDK and DLL reference directory before building. All host DLLs must come from the same Dalamud API 15 distribution.

```powershell
$xlDotnet = Join-Path $env:LOCALAPPDATA 'Microsoft/dotnet10-sdk/dotnet.exe'
$env:DALAMUD_HOME = Join-Path $env:APPDATA 'XIVLauncher/addon/Hooks/dev'
& $xlDotnet --info
Get-Item (Join-Path $env:DALAMUD_HOME 'Dalamud.dll')
& $xlDotnet restore HuntAlerts/HuntAlerts.csproj --locked-mode
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
& $xlDotnet build HuntAlerts/HuntAlerts.csproj -c Release --no-restore -p:LangVersion=14.0
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
& $xlDotnet run --project tests/HuntAlerts.Relay.Tests/HuntAlerts.Relay.Tests.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
```

Output: `HuntAlerts/bin/Release/HuntAlerts/latest.zip`.

The Release build succeeded with zero warnings and zero errors using SDK 10.0.401, MSBuild 18.9.11, and DLLs from Dalamud 15.0.3.5. All 32 game-independent checks passed. In-game Japanese sheet lookups, configuration reloads, FC/CWLS delivery, and clickable map links remain unverified.
