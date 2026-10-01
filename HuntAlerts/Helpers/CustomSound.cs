using System;
using System.IO;
using ECommons.DalamudServices;
using ECommons.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace HuntAlerts.Helpers;

public sealed class CustomSound
{
    private readonly string _fileName;
    private readonly object _lock = new();
    private WaveOutEvent? _output;
    private MediaFoundationReader? _reader;

    public CustomSound(string fileName) => _fileName = fileName;

    public string FilePath =>
        Path.Combine(Svc.PluginInterface.ConfigDirectory.FullName, _fileName);

    public bool Exists => File.Exists(FilePath);
    
    public string? Import(string sourcePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                return "File not found.";
            if (!sourcePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                return "Please choose an .mp3 file.";

            Stop();
            Svc.PluginInterface.ConfigDirectory.Create();
            File.Copy(sourcePath, FilePath, overwrite: true);
            return null;
        }
        catch (Exception e)
        {
            PluginLog.Warning($"HuntAlerts: sound import failed: {e.Message}");
            return e.Message;
        }
    }

    public void Remove()
    {
        try
        {
            Stop();
            if (Exists) File.Delete(FilePath);
        }
        catch (Exception e)
        {
            PluginLog.Warning($"HuntAlerts: sound remove failed: {e.Message}");
        }
    }

    public void Play(float volume)
    {
        if (!Exists) return;
        lock (_lock)
        {
            StopLocked();
            try
            {
                _reader = new MediaFoundationReader(FilePath);
                var sample = new VolumeSampleProvider(_reader.ToSampleProvider())
                {
                    Volume = Math.Clamp(volume, 0f, 1f),
                };
                _output = new WaveOutEvent();
                _output.Init(sample);
                _output.Play();
            }
            catch (Exception e)
            {
                PluginLog.Warning($"HuntAlerts: could not play sound: {e.Message}");
                StopLocked();
            }
        }
    }

    public void Stop()
    {
        lock (_lock) StopLocked();
    }
    
    private void StopLocked()
    {
        try { _output?.Dispose(); } catch { /* ignore */ }
        try { _reader?.Dispose(); } catch { /* ignore */ }
        _output = null;
        _reader = null;
    }

    public void Dispose() => Stop();
}

public static class Sounds
{
    public static readonly CustomSound Train = new("trainsound.mp3");
    public static readonly CustomSound SRank = new("sranksound.mp3");

    public static void DisposeAll()
    {
        Train.Dispose();
        SRank.Dispose();
    }
}
