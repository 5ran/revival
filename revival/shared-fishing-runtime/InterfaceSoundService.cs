using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Client.Services;

public static class InterfaceSoundService
{
    private const int DefaultVolume = 24;
    private static readonly object Sync = new();
    private static readonly List<byte[]> Sources = new();
    private static volatile SoundPlayer[] _players = Array.Empty<SoundPlayer>();
    private static int _nextPlayer = -1;
    private static bool _initialized;
    private static bool _enabled = true;
    private static int _volume = DefaultVolume;

    public static event Action? SettingsChanged;

    public static bool Enabled => _enabled;

    public static int Volume => _volume;

    public static void Initialize(bool enabled = true, int volume = DefaultVolume)
    {
        lock (Sync)
        {
            _enabled = enabled;
            _volume = Math.Clamp(volume, 0, 100);
            if (!_initialized)
            {
                LoadSource("ui-click-1.wav");
                LoadSource("ui-click-2.wav");
                LoadSource("ui-click-3.wav");
                _initialized = true;
            }

            RebuildPlayers();
        }
    }

    public static void SetEnabled(bool enabled, bool persist = true)
    {
        if (_enabled == enabled)
        {
            return;
        }

        _enabled = enabled;
        if (persist)
        {
            Persist();
        }

        SettingsChanged?.Invoke();
    }

    public static void SetVolume(int volume, bool persist = true)
    {
        volume = Math.Clamp(volume, 0, 100);
        if (_volume == volume)
        {
            return;
        }

        _volume = volume;
        _ = Task.Run(() =>
        {
            lock (Sync)
            {
                RebuildPlayers();
            }
        });

        if (persist)
        {
            Persist();
        }

        SettingsChanged?.Invoke();
    }

    public static void PlayClick()
    {
        if (!_enabled || _volume == 0)
        {
            return;
        }

        var players = _players;
        if (players.Length == 0)
        {
            return;
        }

        var index = (Interlocked.Increment(ref _nextPlayer) & int.MaxValue) % players.Length;
        players[index].Play();
    }

    private static void RebuildPlayers()
    {
        var next = new SoundPlayer[Sources.Count];
        for (var index = 0; index < Sources.Count; index++)
        {
            next[index] = new SoundPlayer(new MemoryStream(ScalePcm16Wave(Sources[index], _volume / 100d), writable: false));
            next[index].Load();
        }

        var previous = _players;
        _players = next;
        foreach (var player in previous)
        {
            player.Dispose();
        }
    }

    private static void LoadSource(string fileName)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://Client/Assets/Sounds/{fileName}"));
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        Sources.Add(memory.ToArray());
    }

    private static byte[] ScalePcm16Wave(byte[] source, double volume)
    {
        var result = (byte[])source.Clone();
        if (result.Length < 44)
        {
            return result;
        }

        for (var offset = 44; offset + 1 < result.Length; offset += 2)
        {
            var sample = BitConverter.ToInt16(result, offset);
            var scaled = (short)Math.Clamp((int)Math.Round(sample * volume), short.MinValue, short.MaxValue);
            result[offset] = (byte)(scaled & 0xFF);
            result[offset + 1] = (byte)((scaled >> 8) & 0xFF);
        }

        return result;
    }

    private static void Persist()
    {
        var store = new UserSettingsStore();
        var snapshot = store.Load();
        snapshot.InterfaceSounds ??= new InterfaceSoundsSettingsSnapshot();
        snapshot.InterfaceSounds.Enabled = _enabled;
        snapshot.InterfaceSounds.Volume = _volume;
        store.Save(snapshot);
    }
}
