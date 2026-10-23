# HuntAlerts-JP - Japanese Relay Fork

A Japanese relay fork of [HuntAlerts](https://github.com/huntsffxiv/huntalerts). Original authors: Asuna / HuntsFFXIV. Japanese relay changes: MintakaSeiran.

This README describes the implemented features in English. Translations of Japanese interface labels and relay examples are provided for documentation; the Japanese relay output in the plugin remains Japanese.

## Implemented Features

### History Travel (1.4.1.16)

Right-click a history card to request travel directly through Lifestream without opening the detail window. Left-click still opens details. Current Lifestream integration must be enabled and available, and the player and target must be in the same known region. Applies to S/SS and train cards; defeated entries remain actionable.

Version 1.4.1.16 adds queued travel retargeting: while the game's World Visit reservation window is open with a positive queue position and an enabled Cancel button, right-click another card. Only a matching HuntAlerts-started request within the same datacenter can be switched. The plugin stops its old routine and Lifestream, cancels the reservation, and waits for the Japanese system cancellation message, cleared reservation state, unchanged source world, and both routines becoming idle. These conditions must remain stable for 500 ms. During confirmation the last selected card wins. Transfer-ready/loading, cross-DC travel, unsupported client languages, or a 15-second confirmation timeout do not start another trip. HUD-only queue information is insufficient; open the reservation window first. Published as a maintainer-approved prerelease; game/IPC behavior remains unverified. See [implementation and manual checks](docs/TRAVEL_RETARGET_RESEARCH.md).

### SS Notifications (1.4.1.14)

The S-rank sound settings now include an SS spawn sound selector (inherit normal S, or game Sound 1-16) and an optional seven-color SS spawn chat log. Defaults preserve existing behavior. Confirmed SS names use the existing rank lookup. Inherit also retains the normal S custom MP3 setting. The seven-color text preserves the details link and resets its color afterward; relay and kill messages are unchanged. Snooze, cutscene muting, duty suppression and silent deferred-history processing still apply. No SS-specific MP3 import is added. In-game audio, chat colors and link interaction remain unverified.

### SS Colors (Included in 1.4.1.14)

General > History provides independent SS badge and background palettes: Standard, Yellow, and Rainbow. Defaults preserve the existing appearance. Only confirmed SS history cards use these settings; S ranks, minions, train cards and detail-window badges are unchanged. Rainbow is a static horizontal gradient. Defeated SS cards retain a darker tint and the defeated stamp. Both two-row and three-row layouts are supported. Logic checks: 208 passed; native geometry cases: 1,296 passed. In-game verification is pending. Published as a maintainer-approved prerelease.

### Layout Update (1.4.1.12)

General > History offers two-row and three-row S/SS history cards (default: three rows). Both restore the original rank and world badges, expansion label, area and right-aligned time. Defeated cards show kill time instead of spawn time; the tooltip retains both. Three rows show the prior average directly; two rows keep it in the tooltip/detail window. Defeated dimming, stamp, archive and calculations are unchanged. Published as a prerelease at the maintainer's request. In-game verification remains pending.

### Defeat History Update (1.4.1.7_jp_min.5 / 1.4.1.11)

Published as a prerelease at the maintainer's request. In-game verification is pending.

- Kill events dim matching history cards, add a red diagonal defeated stamp, and show the local death time. Matching uses world, creature, instance and timestamps, with a 24-hour limit.
- Subsequent appearances show the average **announcement-to-death** interval and sample count from earlier completed appearances of the same target on the same world/instance. This is not combat duration. Unmatched kills do not contribute.
- An append-only `HuntAlerts.Outcomes.jsonl` archive in the plugin configuration directory preserves observations beyond the 50-card cache and Clear. The history folder button opens that directory. Previously discarded kill events cannot be recovered.
- Known targets receive kill updates even when snoozed or after changing world filters. Deferred kills update silently after travel. Unknown targets still require current filters to pass.
- Chat and relay no longer display DC names. DC filtering and travel-time reception retention remain enabled.

Detailed specification and limitations: [Defeat history](docs/DEFEAT_HISTORY.md).

### History Update (1.4.1.7_jp_min.4 / 1.4.1.10)

Released as a prerelease at the maintainer's request. In-game behavior remains unverified. The following describes 1.4.1.10; the update above supersedes its kill-event and snooze behavior.

- History cards and their detail header show **SS RANK** only for confirmed SS boss names. Names are loaded in EN/JP/DE/FR from BNpcName IDs `0x22D3`, `0x2977`, and `0x345E`. Normal S ranks, minions, missing names, and unrecognized names are not promoted. Existing cached entries use the same display-time lookup without a history migration. Chat, relay, toast text and IPC schemas remain unchanged.
- Received hunt events are no longer discarded just because the player is logged out during datacenter travel. The plugin holds the latest 50 pending events in memory and processes up to five per framework update after a valid player/world/DC context returns. Overflow drops the oldest pending event and writes a warning.
- Deferred events are filtered using the **post-login** world/DC and the current settings. Current-DC/current-world modes do not assume the previous DC or guess the destination. All-configured and home-world modes retain their usual meaning. Snoozed events are skipped; snooze is checked again during processing.
- Accepted deferred train/spawn events enter persistent history silently, without chat, sound, popup or IPC replay. Live events retain their normal effects. Original posted times are preserved; train deduplication uses original receive times, not the replay time. Kill messages remain chat-only and are not replayed or added to history after travel.
- This requires the game, plugin and Socket.IO connection to remain running. It cannot recover events never received during a network disconnect, and pending events do not survive plugin unload or game restart. It is not a server history/backfill API.

Japanese specification and manual test steps: [History update](docs/HISTORY_UPDATE.md).

### Hunt Alerts and History

- Receive hunt train and S-rank notifications from the upstream service, with optional S-rank kill notifications. Available worlds are grouped into JP, NA, EU, and OCE regions.
- Configure train and S-rank expansion groups independently. Train groups include Centurio, Shadowbringers, Endwalker, and Dawntrail.
- Select all configured worlds, the current datacenter, the current world, or the home world independently for trains and S ranks. World selectors include a search field and datacenter grouping.
- Display alerts in chat, in the notification window, and optionally in a toast banner. Chat settings include the output channel and separate colors for trains, S-rank spawns, and kills.
- Keep up to 50 cached alerts in persistent history. Clearing the visible history preserves existing chat links until their cache slots are overwritten.
- Optionally suppress duplicate train messages.

### Notification Controls

- Snooze incoming alerts for 5, 15, 30, 60, or 120 minutes, and wake them early from settings or the history window. Snoozed spawn/train messages are skipped; this release still update kills for known targets.
- Optionally hide alert popups, chat messages, and sounds while in instanced content; accepted train and S-rank spawn alerts still enter history.
- Optionally mute alert sounds during cutscenes while retaining the popup and chat message.
- Choose game sound effects or import custom MP3 sounds separately for trains and S ranks, with adjustable custom sound volume.
- Enable a toast banner with a duration of 3, 5, 8, or 12 seconds. Choose no animation, fade, slide, or slide plus fade; preview, reposition, or reset the banner in settings.

### Alert Actions and Navigation

- Open an alert's location with **Flag on Map** and open the game's **Party Finder**. The Party Finder action opens the interface; it does not generate an AS Mob Plate recruitment listing.
- Use **Nav** to target the on-screen navigation arrow when the alert has a territory and coordinates. The draggable arrow points toward the selected location while the player is in its zone.
- Optionally retarget the arrow from map links in Shout, Yell, and Party/Cross-world Party chat. Each channel can be enabled separately, and the active waypoint can be cleared.
- Enable optional Lifestream integration for teleport and world-change requests, with optional map flagging on arrival. The notification window offers the travel action when the alert's target region matches the player's region.
- Enable Ctrl-click teleport requests from alert chat links when Lifestream is available. Integration setting changes apply to subsequent hunt messages.
- Share an alert through Relay using Japanese or the original English formatting. The default channel and the one-time channel picker use the same selected format.

### Commands and Diagnostics

| Command | Implemented behavior |
| --- | --- |
| `/huntalerts` | Open recent hunt history. |
| `/huntalerts settings` or `/huntalerts s` | Open settings. |
| `/huntalerts debug` or `/huntalerts d` | Open the debug view for simulated hunt messages. |

Connection diagnostics show the current state, reconnect attempts, the last error, and recent activity, with a manual reconnect action. The debug view can simulate train or S-rank messages with a world, expansion, location, coordinates, and optional creature information; normal filtering still applies.

The plugin also exposes upstream IPC events for received hunt messages and typed hunt alerts, plus queries for Lifestream integration and map-on-arrival settings. This fork retains those event payloads and query signatures.

This repository was forked from the original GitHub repository and incorporates the history of the [current upstream on GitLab](https://projects.gamba.pro/Asuna/huntalerts). The `main` branch contains the upstream 1.4.1.7 baseline. The `jp_relay` branch contains this fork's changes. See the [changes compared with the upstream baseline](https://github.com/MintakaSeiran/HuntAlerts-JP/compare/main...jp_relay).

For this fork, use [HuntAlerts-JP Issues](https://github.com/MintakaSeiran/HuntAlerts-JP/issues). The original plugin repository is `https://puni.sh/api/repository/asuna`; its support channel is `asuna-plugins` in the [Puni.sh Discord](https://discord.gg/punishxiv).

This fork retains the upstream project's [AGPL-3.0-or-later](LICENSE) license declaration. Refer to the ECommons submodule for its license.

## Download and Installation

[Download the compiled plugin ZIP](https://github.com/MintakaSeiran/HuntAlerts-JP/releases/download/1.4.1.7_jp_min.10/HuntAlerts-JP-1.4.1.7_jp_min.10.zip) | [Release notes and checksum](https://github.com/MintakaSeiran/HuntAlerts-JP/releases/tag/1.4.1.7_jp_min.10)

This is a prerelease: the build and automated checks pass, but in-game behavior has not been verified.

### JSON Files

| File | Purpose |
| --- | --- |
| [pluginmaster.json](pluginmaster.json) | Custom repository manifest containing the plugin entry and release download URLs. Use its raw URL in Dalamud's Custom Plugin Repositories. |
| [HuntAlerts.json](HuntAlerts.json) | Individual plugin manifest copied from the published build, including the internal name, numeric assembly version, and Dalamud API level. Keep this file beside the DLL for manual installation. |

Both JSON files are also available as release assets. The source template at `HuntAlerts/HuntAlerts.json` is used during building; the root-level `HuntAlerts.json` is the generated distribution manifest.

To install through Dalamud, add this URL to **Custom Plugin Repositories**:

```text
https://raw.githubusercontent.com/MintakaSeiran/HuntAlerts-JP/jp_relay/pluginmaster.json
```

This fork retains the internal name `HuntAlerts`. Disable the original plugin and avoid installing or updating both variants simultaneously. Verify that the installed plugin's repository URL points to `MintakaSeiran/HuntAlerts-JP`. Select this fork explicitly when switching repositories; installing an update does not itself verify in-game compatibility.

For manual development installation, extract the entire ZIP into a development plugin folder and register `HuntAlerts.dll` in Dalamud's development plugin locations. Keep the manifest and all bundled dependencies beside the DLL. After loading, verify that the display name includes the Japanese relay suffix. To revert, disable this fork and enable the original plugin.

## Japanese Relay Changes

Updated October 15, 2026. Based on upstream commit `69ec1140add0e5ce97553785c8ad04d04ac68224` (1.4.1.7).

InformationalVersion: `1.4.1.7_jp_min.10`; assembly version: `1.4.1.16`. The published feed provides `1.4.1.16`. The build targets Dalamud API 15, .NET 10, C# 14, and x64.

### Relay Message Format

Messages follow the layout used by AS Mob Plate recruitment comments. This example is translated into English for documentation; actual relay messages use Japanese names and labels where available:

```text
[S] Zona Seeker / Western Thanalan / Nearest: Horizon / Server: Yojimbo / POS: (26.8, 16.9)
```

- This release omit the DC name from relay output; the target server remains visible.
- Creature names are matched between English and Japanese BNpcName rows by row ID. Area and aetheryte names use the alert's IDs and Japanese PlaceName data, with English name matching when IDs are unavailable. Untranslated names retain their original spelling.
- The nearest aetheryte field uses the destination selected by upstream for the alert. This fork does not change the distance calculation.
- Valid coordinates appear in POS with one decimal place. Older alerts can also use coordinate strings. Missing, zero, non-finite, or negative coordinates are omitted.
- When `Add clickable map flag when relaying` is enabled and the required map ID and coordinates are available, `<flag>` is appended. POS remains visible. If setting the flag throws an exception, the message is sent without the link.
- Instance numbers of 2 or higher are included. No unconfirmed start ET is added. Unknown server and name fields are omitted.
- Train alerts use a Japanese hunt train label and expansion name, distinguishing them from S-rank alerts.
- Messages exceeding 500 UTF-8 bytes, including the channel command, are not sent. An explanation appears in the player's chat instead of silently truncating information.

### Settings

1. Open `/huntalerts settings` and find the Japanese relay checkbox under Notifications. Its Japanese label means "Send Relay in Japanese (AS Mob Plate format)." It is enabled by default. Disable it to restore the original English format.
2. Use the alert's Relay button to share through the configured channel.
3. Select `Echo (test)` from the adjacent arrow menu to check the actual message in your own chat before using a shared channel.

Supported relay destinations are Say, Yell, Shout, Party, Alliance, Free Company, Linkshells 1-8, Cross-world Linkshells 1-8, and Echo. The default is Party. Choosing a destination from the arrow menu sends only that relay to it and does not replace the saved default.

| Relay setting | Default | Behavior |
| --- | --- | --- |
| Japanese relay / `JapaneseRelay` | Enabled | Use Japanese game names and the AS Mob Plate-style format described above. Disable to use the original English formatter. |
| Default Relay Channel / `DefaultRelayChannel` | `/p` | Destination used by the main Relay button. |
| Add clickable map flag / `RelayFlagLink` | Enabled | Append a clickable location link when map information is available. Japanese messages also retain their POS text. |

The added configuration fields are `JapaneseRelay=true` and `JapaneseChatAlerts=true`. Both default to enabled when absent from an existing configuration file. Configuration version 4, history version 2, the default relay channel, and IPC argument types remain unchanged. Updates to the original plugin do not include this fork's changes.

### Japanese Chat Notifications

S-rank spawn notices, S-rank kill notices, and hunt train notices can now appear in Japanese in the player's chat log. Expansion labels, creature names, instance labels, and the clickable details prompt are localized. Creature names use the same Japanese game-data lookup as Relay, falling back to the original name if no translation is available. Server names retain their original spelling. Kill notices use the local time in 24-hour `HH:mm` format.

The following is an English translation of the Japanese spawn message for documentation:

```text
An S-rank Aglaope from Shadowbringers has appeared on Zeromus! (Click for details)
```

Under **Chat**, the Japanese-labeled checkbox means "Display notification logs in Japanese." It controls `JapaneseChatAlerts`, is enabled by default, and works independently of `JapaneseRelay`. Disabling it restores English chat messages. This release omit the DC prefix in both languages. Chat channel selection, colors, clickable links, and Ctrl-click travel behavior remain unchanged. Stored notification data and IPC payloads retain their original names. The setting also selects Japanese names and defeated labels in the new S-rank history cards; it does not translate the full interface or arbitrary service messages.

Development and automated verification did not overwrite installed plugins or configuration files, or send in-game chat messages.

## Building and Verification

Release 1.4.1.11 uses the installed Dalamud 15.0.3.7 host DLL set. `Directory.Build.targets` adds an ECommons-only alias for Lumina's `ExcelPage` to resolve a name collision with the host's updated FFXIVClientStructs. The ECommons pin and package versions are unchanged. Release packaging succeeded with one NU1900 audit warning, 208 logic checks and 36 native ImGui card geometry cases passed. In-game tests and deployment remain pending.

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

Historical verification for 1.4.1.10: the Release build and packaging succeeded with zero errors using SDK 10.0.401, MSBuild 18.9.11, and DLLs from Dalamud 15.0.3.6. One NU1900 warning remains because NuGet vulnerability information could not be retrieved; a completed vulnerability audit is not claimed. All 181 game-independent checks passed. In-game SS labels, datacenter-travel reception, Japanese sheet lookups, configuration reloads, FC/CWLS delivery, and clickable map links remain unverified. That earlier ZIP has SHA-256 `ad006c1c121b2c9bb98d5b86b53036bbfb04832ffe1658ce2558f8916b8588c2`.
