using HuntAlerts.Helpers;

internal static class OutcomeTests
{
    internal static void Run(Action<bool, string> check)
    {
        const long start = 1792010820;
        HuntOutcomeEvent Spawn(long time = start, string world = "Valefor", string creature = "Thousand-cast Theda", int instance = 1) =>
            new(1, world, creature, instance, time, 0);
        HuntOutcomeEvent Kill(long time = start + 180, string world = "Valefor", string creature = "Thousand-cast Theda", int instance = 1) =>
            new(1, world, creature, instance, time + 3, time);
        HuntTrainMessage Message(long time = start, string world = "Valefor", string creature = "Thousand-cast Theda", int instance = 1) =>
            new("", "srank", "ARR", world, "", "", "", "", time, "", 0, "", instance, "", false, false, creature);

        var book = new HuntOutcomeBook();
        check(book.Add(Spawn()), "Accept spawn");
        check(book.Get(Message()).DeathEpoch == 0, "No death inferred from age");
        check(book.Add(Kill()), "Accept kill");
        check(book.Get(Message()).DurationSeconds == 180, "User example: Valefor 05:47 to 05:50 = 180s");
        check(book.Get(Message()).Samples == 0, "First appearance has no prior sample");
        check(!book.Add(Kill()) && !book.Add(Spawn()), "Repeated notices are idempotent");
        book.Add(Spawn(start + 120, "Durandal"));
        book.Add(Kill(start + 300, "Durandal"));
        check(book.Get(Message(start + 120, "Durandal")).DurationSeconds == 180, "Durandal pairs separately");
        check(book.Get(Message(start + 120, "Durandal")).Samples == 0, "No cross-world averages");
        book.Add(Spawn(start + 540, "Aegis", "Minhocao"));
        book.Add(Kill(start + 840, "Aegis", "Minhocao"));
        check(book.Get(Message(start + 540, "Aegis", "Minhocao")).DurationSeconds == 300, "Aegis 05:56 to 06:01 = 300s");
        book.Add(Kill(start + 1440, "Bahamut"));
        check(book.Get(Message(start + 2000, "Bahamut")).Samples == 0, "Kill-only event excluded from average");
        var next = start + 172800;
        book.Add(Spawn(next));
        check(book.Get(Message(next)).AverageSeconds == 180 && book.Get(Message(next)).Samples == 1, "Next appearance shows prior average");
        check(book.Get(Message(next)).DeathEpoch == 0, "Old kill never marks new spawn defeated");
        book.Add(Kill(next + 420));
        var third = Message(next + 172800);
        check(book.Get(third).AverageSeconds == 300 && book.Get(third).Samples == 2, "Average of 180 and 420 = 300 seconds");
        check(book.Get(Message()).Samples == 0, "Future kills never leak into historical average");
        check(book.Get(Message(next, instance: 2)).Samples == 0, "Instances isolated");
        check(book.Get(Message(next, creature: "Other mob")).Samples == 0, "Mobs isolated");
        check(book.Get(Message(next, " valefor ", "THOUSAND-CAST THEDA [JP]")).DeathEpoch == next + 420, "Normalization and localized suffix");
        var reversed = new HuntOutcomeBook();
        reversed.Add(Kill());
        reversed.Add(Spawn());
        check(reversed.Get(Message()).DeathEpoch == start + 180, "Out-of-order receipt repaired");
        reversed.Add(Kill(start + 181));
        check(reversed.Get(Message(next)).Samples == 1, "Second kill report cannot double count a single spawn");
        reversed.Add(Spawn(start + 100));
        check(reversed.Get(Message()).DeathEpoch == 0 && reversed.Get(Message(start + 100)).DeathEpoch == start + 180,
            "Latest preceding appearance wins, no double attribution");
        var stale = new HuntOutcomeBook();
        stale.Add(Spawn());
        stale.Add(Kill(start - 1));
        check(stale.Get(Message()).DeathEpoch == 0, "Pre-spawn kill cannot complete entry");
        stale.Add(Kill(start + HuntOutcomeBook.MaxMatchSeconds + 1));
        check(stale.Get(Message()).DeathEpoch == 0, "Over-24h stale pairing rejected");
        stale.Add(Kill(start));
        check(stale.Get(Message()).DeathEpoch == start && stale.Get(Message(next)).Samples == 0, "Zero duration marked but excluded from average");
        check(!stale.Add(Spawn(0)) && !stale.Add(Spawn(-1)) && !stale.Add(Spawn(long.MaxValue)), "Invalid epochs rejected");
        check(!stale.Add(Spawn(world: "unknown")) && !stale.Add(Spawn(creature: "")), "Unknown identities rejected");
        check(!stale.Add(Spawn() with { Version = 2 }), "Unknown schema rejected");
        var train = Message(); train.huntType = "new_hunt";
        check(book.Get(train) == default, "Train not a defeated S rank");
        check(HuntOutcomeText.Duration(180) == "03:00" && HuntOutcomeText.Duration(3599.6) == "60:00", "Duration formatting and rounding");
        check(HuntOutcomeText.Clock(long.MaxValue) == "--:--", "UI handles invalid timestamps");

        var directory = Path.Combine(Path.GetTempPath(), "HuntAlerts-outcome-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, "outcomes.jsonl");
        try
        {
            var warnings = new List<string>();
            var store = new HuntOutcomeStore(file, warnings.Add);
            store.Record(Spawn()); store.Record(Kill());
            var length = new FileInfo(file).Length;
            check(!store.Record(Kill()) && new FileInfo(file).Length == length, "Duplicate never appends disk log");
            var restored = new HuntOutcomeStore(file, warnings.Add);
            check(restored.Book.Get(Message()).DurationSeconds == 180, "Death survives restart");
            check(restored.Book.Get(Message(next)).AverageSeconds == 180, "Archive restores long-term average independently of 50-slot cache");
            File.AppendAllText(file, "{interrupted");
            var recovered = new HuntOutcomeStore(file, warnings.Add);
            check(recovered.SkippedLines == 1 && recovered.Book.EventCount == 2, "Truncated last record leaves valid records intact");
            recovered.Record(Spawn(next));
            check(new HuntOutcomeStore(file, warnings.Add).Book.EventCount == 3, "Append after corrupt tail remains readable");
            var blocked = Path.Combine(directory, "blocked"); Directory.CreateDirectory(blocked);
            var failing = new HuntOutcomeStore(blocked, warnings.Add);
            failing.Record(Spawn());
            check(failing.LastError != null && failing.Book.EventCount == 1, "IO error is visible; current-session state retained");
            Directory.Delete(blocked);
            failing.Flush();
            check(failing.LastError == null && File.Exists(blocked), "Pending writes retry without losing event");
            check(new HuntOutcomeStore(blocked, warnings.Add).Book.EventCount == 1, "Retry persists exactly one event");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
