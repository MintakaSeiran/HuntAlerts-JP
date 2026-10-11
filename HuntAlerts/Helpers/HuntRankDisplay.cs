using System;
using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.Logging;
using Lumina.Excel.Sheets;

namespace HuntAlerts.Helpers;

internal static class HuntRankDisplay
{
    private static HuntRankNames? names;

    internal static string Rank(HuntTrainMessage entry)
    {
        if (names == null)
        {
            var loaded = new HuntRankNames();
            try
            {
                foreach (var language in new[] { ClientLanguage.English, ClientLanguage.Japanese, ClientLanguage.German, ClientLanguage.French })
                {
                    var sheet = Svc.Data.GetExcelSheet<BNpcName>(language);
                    foreach (var id in HuntRankNames.SsNameIds)
                        if (sheet.TryGetRow(id, out var row)) loaded.Add(row.Singular.ExtractText());
                }
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"SS display name lookup failed: {ex.Message}");
            }
            names = loaded;
        }
        return names.Rank(entry.huntType, entry.creatureName);
    }
}
