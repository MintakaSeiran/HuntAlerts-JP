using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace HuntAlerts.Helpers;

internal sealed class HuntOutcomeStore
{
    private readonly string path;
    private readonly Action<string> warn;
    private readonly List<HuntOutcomeEvent> pending = new();
    private string? loadError;
    internal HuntOutcomeBook Book { get; } = new();
    internal string? LastError { get; private set; }
    internal int SkippedLines { get; private set; }

    internal HuntOutcomeStore(string path, Action<string> warn)
    {
        this.path = path;
        this.warn = warn;
        try
        {
            if (!File.Exists(path)) return;
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var entry = JsonSerializer.Deserialize<HuntOutcomeEvent>(line);
                    if (entry == null || entry.Version != 1) { SkippedLines++; continue; }
                    Book.Add(entry);
                }
                catch (JsonException) { SkippedLines++; }
            }
            if (SkippedLines > 0) warn($"Hunt outcome archive: skipped {SkippedLines} unreadable/unsupported lines; original file preserved.");
        }
        catch (Exception ex) { Report(ex); loadError = LastError; }
    }

    internal bool Record(HuntOutcomeEvent entry)
    {
        if (!Book.Add(entry)) return false;
        pending.Add(entry);
        Flush();
        return true;
    }

    internal void Flush()
    {
        while (pending.Count > 0)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
                // Separate a previous interrupted write before appending a complete JSON record.
                writer.WriteLine();
                writer.WriteLine(JsonSerializer.Serialize(pending[0]));
                writer.Flush();
                stream.Flush(true);
                pending.RemoveAt(0);
                LastError = loadError;
            }
            catch (Exception ex) { Report(ex); break; }
        }
    }

    private void Report(Exception ex)
    {
        var message = $"Hunt outcome archive IO failed: {ex.Message}";
        if (LastError != message) warn(message);
        LastError = message;
    }
}
