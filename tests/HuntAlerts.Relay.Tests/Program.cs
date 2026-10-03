using System.Globalization;
using System.Text;
using HuntAlerts.Helpers;

var checks = 0;
void Check(bool success, string label)
{
    if (!success) throw new Exception(label);
    checks++;
}

HuntTrainMessage Entry() => new("", "srank", "ARR", "Yojimbo", "OtherWorld", "JP", "JP", "", 0,
    "Horizon", 0, "Western Thanalan", 1, "26.8, 16.9", false, false, "Zona Seeker",
    mapLocationX: 26.8f, mapLocationY: 16.9f);
string Build(HuntTrainMessage entry, bool flag = false, string dc = "Meteor") =>
    JapaneseRelayText.Build(entry, flag, "ゾーナ・シーカー", "西ザナラーン", "ホライズン", dc);

var entry = Entry();
var expected = "[S] ゾーナ・シーカー / 西ザナラーン / 最寄り: ホライズン / DC: Meteor / サーバー: Yojimbo / POS: (26.8, 16.9)";
Check(Build(entry) == expected, "Requested AS Mob Plate format, target DC/world and POS");
Check(Build(entry, true) == expected + " / <flag>", "Flag must not replace POS");
Check(!Build(entry).Contains("OtherWorld"), "Current world must not leak into target world");
Check(!Build(entry).Contains("ET"), "No invented start ET");
Check(!Build(entry).Contains("インスタンス"), "Single-instance formatting unchanged");
entry.instance = 3;
Check(Build(entry).Contains(" / インスタンス3 / POS:"), "Multiple-instance information preserved");
entry = Entry();
Check(!Build(entry, dc: "").Contains("DC:"), "Unknown DC omitted");
Check(Build(entry, dc: "").Contains("サーバー: Yojimbo"), "Server survives unknown DC");
entry.huntWorld = "unknown";
Check(!Build(entry, dc: "").Contains("サーバー:"), "Unknown server omitted");
entry = Entry();
Check(JapaneseRelayText.Build(entry, false, "Unmapped creature", "Unmapped zone", "Unmapped aetheryte", "Meteor")
    .StartsWith("[S] Unmapped creature / Unmapped zone / 最寄り: Unmapped aetheryte"), "Original names retained if untranslated");
Check(!JapaneseRelayText.Build(entry, false, "invalid", "unknown", " ", "").Contains("最寄り:"), "Missing names omitted");
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
    Check(ChatAlertText.Spawn(japanese, "SHB", "Aglaope", "Zeromus", 2, " Meteor ") == "[DC: Meteor] " + ChatAlertText.Spawn(japanese, "SHB", "Aglaope", "Zeromus", 2), "Spawn DC prefix preserves message and trims whitespace");
    Check(ChatAlertText.Kill(japanese, "SHB", "Aglaope", "Zeromus", "21:34", "Meteor") == "[DC: Meteor] " + ChatAlertText.Kill(japanese, "SHB", "Aglaope", "Zeromus", "21:34"), "Kill DC prefix");
    Check(ChatAlertText.Train(japanese, "SHB", "Zeromus", "Meteor") == "[DC: Meteor] " + ChatAlertText.Train(japanese, "SHB", "Zeromus"), "Train DC prefix");
    foreach (var dc in new string?[] { null, "", " ", "unknown", "invalid" })
        Check(ChatAlertText.Spawn(japanese, "SHB", "Aglaope", "Zeromus", 1, dc) == ChatAlertText.Spawn(japanese, "SHB", "Aglaope", "Zeromus", 1), "Missing DC omitted");
}
Console.WriteLine($"Passed {checks} relay and chat alert checks.");
