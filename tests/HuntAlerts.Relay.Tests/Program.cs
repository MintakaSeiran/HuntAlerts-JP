using System.Globalization;
using System.Text;
using HuntAlerts.Helpers;
using HuntAlerts.Messaging;

var checks = 0;
void Check(bool success, string label)
{
    if (!success) throw new Exception(label);
    checks++;
}

HuntTrainMessage Entry() => new("", "srank", "ARR", "Yojimbo", "OtherWorld", "JP", "JP", "", 0,
    "Horizon", 0, "Western Thanalan", 1, "26.8, 16.9", false, false, "Zona Seeker",
    mapLocationX: 26.8f, mapLocationY: 16.9f);
string Build(HuntTrainMessage entry, bool flag = false) =>
    JapaneseRelayText.Build(entry, flag, "ゾーナ・シーカー", "西ザナラーン", "ホライズン");

var entry = Entry();
var expected = "[S] ゾーナ・シーカー / 西ザナラーン / 最寄り: ホライズン / サーバー: Yojimbo / POS: (26.8, 16.9)";
Check(Build(entry) == expected, "Requested format, world and POS without DC");
Check(Build(entry, true) == expected + " / <flag>", "Flag must not replace POS");
Check(!Build(entry).Contains("OtherWorld"), "Current world must not leak into target world");
Check(!Build(entry).Contains("ET"), "No invented start ET");
Check(!Build(entry).Contains("インスタンス"), "Single-instance formatting unchanged");
entry.instance = 3;
Check(Build(entry).Contains(" / インスタンス3 / POS:"), "Multiple-instance information preserved");
entry = Entry();
Check(!Build(entry).Contains("DC:"), "DC omitted");
Check(Build(entry).Contains("サーバー: Yojimbo"), "Server retained");
entry.huntWorld = "unknown";
Check(!Build(entry).Contains("サーバー:"), "Unknown server omitted");
entry = Entry();
Check(JapaneseRelayText.Build(entry, false, "Unmapped creature", "Unmapped zone", "Unmapped aetheryte")
    .StartsWith("[S] Unmapped creature / Unmapped zone / 最寄り: Unmapped aetheryte"), "Original names retained if untranslated");
Check(!JapaneseRelayText.Build(entry, false, "invalid", "unknown", " ").Contains("最寄り:"), "Missing names omitted");
entry.huntType = "new_hunt";
entry.huntKind = "Dawntrail";
Check(Build(entry).StartsWith("[モブハントツアー] 黄金 /"), "Train is not mislabeled S rank");
Check(!Build(entry).Contains("ゾーナ"), "Train does not include creature");
entry = Entry();
var previousCulture = CultureInfo.CurrentCulture;
try
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    Check(Build(entry) == expected, "Coordinates use decimal point under other cultures");
    entry.mapLocationX = entry.mapLocationY = 0;
    foreach (var text in new[] { "26.8, 16.9", "(26.8, 16.9)", "26.8 16.9", "X: 26.8 Y: 16.9" })
    {
        entry.locationCoords = text;
        Check(JapaneseRelayText.Coordinates(entry) == "(26.8, 16.9)", $"Fallback coordinates: {text}");
    }
    foreach (var text in new[] { "", "invalid", "unknown", "0, 0", "NaN, 16.9", "-1, 16.9", "26.8", "Infinity, 16.9" })
    {
        entry.locationCoords = text;
        Check(!Build(entry).Contains("POS:"), $"Unknown coordinates omitted: {text}");
    }
    entry.mapLocationX = float.NaN;
    entry.mapLocationY = 16.9f;
    Check(!JapaneseRelayText.HasMapPosition(entry), "Invalid numeric coordinates cannot set flag");
}
finally { CultureInfo.CurrentCulture = previousCulture; }

var boundary = new string('あ', 165) + "aa"; // /p + space = 3; 497-byte body.
Check(Encoding.UTF8.GetByteCount("/p " + boundary) == 500, "UTF-8 fixture boundary");
Check(JapaneseRelayText.FitsChat("/p", boundary), "500 bytes accepted");
Check(!JapaneseRelayText.FitsChat("/p", boundary + "a"), "501 bytes rejected without truncating information");
Check(!JapaneseRelayText.FitsChat("/cwl8", boundary), "Channel contributes to byte budget");
Check(JapaneseRelayText.FitsChat("/fc", Build(Entry(), true)), "Normal Japanese relay with flag fits chat");
Check(ChatAlertText.Spawn(false, "SHB", "Aglaope", "Zeromus", 1) == "SHB S Rank Aglaope spawned on Zeromus! (Click for info)", "Original English spawn preserved");
Check(ChatAlertText.Spawn(true, "SHB", "アグラオペ", "Zeromus", 1) == "Zeromusに漆黒 Sランク アグラオペが出現しました！（クリックで詳細）", "Japanese spawn and link label");
Check(ChatAlertText.Spawn(true, "SHB", "アグラオペ", "Zeromus", 2).Contains("（インスタンス2）"), "Japanese spawn instance");
Check(ChatAlertText.Spawn(false, "SHB", "Aglaope", "Zeromus", 2).Contains("(i2)"), "English spawn instance");
Check(ChatAlertText.Spawn(true, "SHB", "Unmapped", "Zeromus", 1).Contains("Unmapped"), "Unmapped creature retained in alerts");
Check(ChatAlertText.Spawn(true, "", "unknown", "invalid", 1) == "不明なワールドにSランク 名称不明が出現しました！（クリックで詳細）", "Missing alert data");
Check(ChatAlertText.Kill(true, "SHB", "アグラオペ", "Zeromus", "21:34") == "Zeromusの漆黒 Sランク アグラオペは21:34に討伐されました。", "Japanese kill notification");
Check(ChatAlertText.Kill(false, "SHB", "Aglaope", "Zeromus", "09:34 PM") == "SHB S Rank Aglaope on Zeromus was killed at 09:34 PM.", "English kill preserved");
Check(ChatAlertText.Train(true, "Shadowbringers, Endwalker", "Zeromus") == "Zeromusで漆黒・暁月 モブハントツアーが開始予定です！（クリックで詳細）", "Multiple expansion train");
Check(ChatAlertText.Train(false, "Dawntrail", "Zeromus") == "Dawntrail train starting on Zeromus! (Click for info)", "English train preserved");
foreach (var japanese in new[] { true, false })
{
    Check(!ChatAlertText.Spawn(japanese, "SHB", "Aglaope", "Zeromus", 2).Contains("DC:"), "No spawn DC label");
    Check(!ChatAlertText.Kill(japanese, "SHB", "Aglaope", "Zeromus", "21:34").Contains("DC:"), "No kill DC label");
    Check(!ChatAlertText.Train(japanese, "SHB", "Zeromus").Contains("DC:"), "No train DC label");
}
var rankNames = new HuntRankNames();
rankNames.Add("Forgiven Rebellion");
rankNames.Add("フォーギヴン・リベリオン");
Check(HuntRankNames.SsNameIds.SequenceEqual(new uint[] { 0x22D3, 0x2977, 0x345E }), "Only verified SS boss IDs, not minions");
foreach (var name in new[] { "Forgiven Rebellion", " forgiven rebellion ", "Forgiven Rebellion [JP]", "Forgiven Rebellion［JP］", "フォーギヴン・リベリオン" })
    Check(rankNames.Rank("srank", name) == "SS", "Exact SS name including upstream localized suffix: " + name);
foreach (var name in new string?[] { null, "", "Aglaope", "Forgiven Gossip", "Forgiven Rebellion minion", "SS", "SS spawned" })
    Check(rankNames.Rank("srank", name) == "S", "Unknown, ordinary and minion names are not promoted: " + name);
Check(rankNames.Rank("new_hunt", "Forgiven Rebellion") == "S", "Train must not be classified SS");
var inbox = new DeferredHuntInbox<int>();
var received = new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);
Check(!inbox.Enqueue(1, true, received), "Offline receipt enqueued");
Check(!inbox.TryTake(false, out _) && inbox.Count == 1, "No player: preserve message, do not filter using empty context");
Check(inbox.TryTake(true, out var pending) && pending.Message == 1 && pending.HistoryOnly, "Return: offline receipt remains history-only");
Check(pending.ReceivedAtUtc == received, "Dedupe uses original receive time, not replay time");
Check(!inbox.TryTake(true, out _), "No replay of same queued message");
inbox.Enqueue(2, false, received);
inbox.MarkHistoryOnly();
Check(inbox.TryTake(true, out pending) && pending.HistoryOnly, "Logout silences previously queued live notification");
inbox.Enqueue(3, false, received);
Check(inbox.TryTake(true, out pending) && !pending.HistoryOnly, "New live notification still permits original effects");
for (var i = 0; i < DeferredHuntInbox<int>.Capacity; i++)
    Check(!inbox.Enqueue(i, true, received.AddMinutes(i)), "Within queue capacity");
Check(inbox.Enqueue(50, true, received.AddMinutes(50)) && inbox.Count == 50, "Bounded to latest 50, reports overflow");
for (var i = 1; i <= 50; i++)
    Check(inbox.TryTake(true, out pending) && pending.Message == i && pending.ReceivedAtUtc == received.AddMinutes(i), "FIFO and original timestamps survive travel");
inbox.Enqueue(51, true, received);
inbox.Clear();
Check(inbox.Count == 0 && !inbox.TryTake(true, out _), "Unload clears pending entries");
OutcomeTests.Run(Check);
Console.WriteLine($"Passed {checks} relay, chat, rank, outcomes and deferred-history checks.");
