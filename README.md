# HuntAlerts-JP — 日本語Relayフォーク

[HuntAlerts](https://github.com/huntsffxiv/huntalerts) の日本語Relay改修版です。元の作者はAsuna / HuntsFFXIV、日本語Relay改修はMintakaSeiran。

GitHubの元リポジトリをForkし、[現在の上流（GitLab）](https://projects.gamba.pro/Asuna/huntalerts) の履歴を取り込んでいます。`main` は上流1.4.1.7を取り込んだ基準ブランチ、`codex/japanese-relay` はこのREADMEに記載する改修ブランチです。[日本語Relayだけの差分](https://github.com/MintakaSeiran/HuntAlerts-JP/compare/main...codex/japanese-relay) を確認できます。

このフォークの問い合わせ先は [HuntAlerts-JPのIssues](https://github.com/MintakaSeiran/HuntAlerts-JP/issues) です。通常版の配布元は `https://puni.sh/api/repository/asuna`、通常版のサポート先は [Puni.sh Discord](https://discord.gg/punishxiv) の `asuna-plugins` チャンネルです。

上流のプロジェクト指定に従い、ライセンスは [AGPL-3.0-or-later](LICENSE)。ECommonsはサブモジュール側のライセンスを参照してください。

## 日本語Relay改修版

2026-10-01。上流 `69ec1140add0e5ce97553785c8ad04d04ac68224`（1.4.1.7）を基準にしたローカル改修。
公式配布とは別の成果物で、表示名は `HuntAlerts (日本語Relay)`、InformationalVersionは `1.4.1.7-jp-relay.1`。
AssemblyVersionとDalamudのmanifestは上流と同じ `1.4.1.7`。API 15 / .NET 10 / C# 14 / x64。

### 共有文

AS Mob Plateの募集文に合わせて、日本語Relayは次の形式にする。

```text
[S] ゾーナ・シーカー / 西ザナラーン / 最寄り: ホライズン / DC: Meteor / サーバー: Yojimbo / POS: (26.8, 16.9)
```

- DCは通知の対象ワールドから取得する。現在いるDC・ワールドへ置き換えない。
- モブ名は英語のBNpcNameと日本語の同一行IDを照合する。エリア・エーテライトは通知のIDと日本語PlaceNameを使用し、IDがなければ英語名を照合する。取得できない名前は通知の元の表記を使う。
- 「最寄り」は上流が通知に設定した移動先エーテライトを使用する。今回、距離判定の方式は変更していない。
- 数値の座標が有効ならPOSを小数1桁で表示する。過去の通知などは座標文字列からも取得し、不明・0・非数・負数なら省略する。
- `Add clickable map flag when relaying` がONで地図用ID・座標がそろう場合、末尾に `<flag>` を追加する。POSは併記する。旗の設定で例外が発生した場合はリンクを付けずに送信する。
- 2以上のインスタンス番号を表示する。確定できない開始ETは付けない。不明なDC・サーバー・名前の項目は省略する。
- ツアーは `[モブハントツアー] 黄金 / ...` などの形式にし、Sランクと区別する。
- コマンド込みでUTF-8の500バイトを超える文章は、情報を切り捨てず送信を中止し、自分のチャットに理由を表示する。

### 設定と導入

1. 通常版HuntAlertsを無効化する。同じ内部名・コマンドを使うので同時ロードしない。
2. `HuntAlerts/bin/Release/HuntAlerts/latest.zip` を開発用プラグインの任意フォルダーへ全ファイル展開し、Dalamudの開発用プラグイン登録で `HuntAlerts.dll` を指定する。あるいは `HuntAlerts/bin/Release/HuntAlerts.dll` を直接登録する。
3. ロード後、プラグイン名が `HuntAlerts (日本語Relay)` と表示されることを確認する。
4. `/huntalerts settings` → Notificationsの `Relayを日本語で送信（AS Mob Plate形式）` を確認する。既定ON。OFFなら従来の英語文へ戻る。
5. 通知のRelayボタンで設定したチャンネルへ共有する。横の矢印から `Echo (test)` を選ぶと、自分向けに実際の文章を確認できる。

設定追加は `JapaneseRelay=true` の1項目。既存設定ファイルに項目がなくてもONで読み込む。設定Version 4、履歴Version 2、既定共有先、IPCの引数・型は維持する。通常版の自動更新でこの改修は取り込まれない。戻す場合は改修版を無効化して通常版を有効化する。

今回、導入済みプラグイン・設定ファイルへの上書きやゲーム内チャット送信は実施していない。

### ビルドと検証

ECommonsはサブモジュール `96b6bbc9896aead9f67ea1b245b89ed510a61c88`（3.2.1.6）。上流のlock fileを維持し、ECommons.IPC 1.0.0.19、SocketIOClient 3.1.1、NAudio 2.2.1を使用する。

このリポジトリのルートで実行。初回取得時はサブモジュールも取得する：

```powershell
git clone --recurse-submodules --branch codex/japanese-relay https://github.com/MintakaSeiran/HuntAlerts-JP.git
cd HuntAlerts-JP
```

以下は開発に使用したWindows端末のSDK配置例。別の環境では、`$xlDotnet` を実在する.NET 10 SDKの `dotnet.exe` へ変更する。ビルド前にSDKとDLL参照先を確認する。Dalamud API 15の同一配布DLL一式が必要。

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

SDK 10.0.401 / MSBuild 18.9.11、同一配布のDalamud 15.0.3.5 DLLを確認してReleaseビルド成功（警告0・エラー0）。32項目のゲーム非依存検証に成功。実機の日本語シート照合、設定再読込、FC/CWLS送信、クリック可能な地図リンクは未確認。まずEchoで文章を確認し、その後に使用する共有先で確認する。
