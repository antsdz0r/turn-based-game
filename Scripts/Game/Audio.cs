using System.Collections.Generic;
using Godot;

namespace TBRPG.Game;

/// <summary>
/// Music and sound effects. Lives on a node attached to the scene tree root, so music keeps
/// playing across scene changes (no autoload needed) and keeps playing while the game is paused.
/// </summary>
public static class Audio
{
    private const int SfxVoices = 14;

    private static Node? _host;
    private static AudioStreamPlayer? _music;
    private static readonly List<AudioStreamPlayer> SfxPlayers = new();
    private static readonly Dictionary<string, AudioStream?> Cache = new();
    private static string? _currentMusic;
    private static int _nextVoice;

    private static void EnsureHost(Node anyNode)
    {
        if (_host is not null && GodotObject.IsInstanceValid(_host)) return;
        if (!anyNode.IsInsideTree()) return;

        _host = new Node { Name = "AudioHost", ProcessMode = Node.ProcessModeEnum.Always };
        _music = new AudioStreamPlayer();
        _music.Finished += () => { if (_music is not null && GodotObject.IsInstanceValid(_music)) _music.Play(); };
        _host.AddChild(_music);

        SfxPlayers.Clear();
        for (int i = 0; i < SfxVoices; i++)
        {
            var player = new AudioStreamPlayer();
            _host.AddChild(player);
            SfxPlayers.Add(player);
        }
        _currentMusic = null;

        // The root may be busy building the current scene, so attach on the next idle frame.
        anyNode.GetTree().Root.CallDeferred(Node.MethodName.AddChild, _host);
    }

    private static AudioStream? Load(string path)
    {
        if (!Cache.TryGetValue(path, out var stream))
        {
            stream = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
            Cache[path] = stream;
        }
        return stream;
    }

    private static float ToDb(float linear) => linear <= 0.001f ? -80f : Mathf.LinearToDb(linear);

    /// <summary>Plays res://Audio/Music/{name}.ogg on loop. Does nothing if it's already playing.</summary>
    public static void Music(Node from, string name)
    {
        EnsureHost(from);
        if (_currentMusic == name || _music is null) return;
        var stream = Load($"res://Audio/Music/{name}.ogg");
        if (stream is null) return;
        _currentMusic = name;
        if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true; // newly imported tracks default to one-shot
        _music.Stream = stream;
        _music.VolumeDb = ToDb(GameSession.Settings.Music * 0.7f);
        if (_music.IsInsideTree()) _music.Play();
        else _music.Autoplay = true;
    }

    /// <summary>Plays res://Audio/Sfx/{name}.wav with a little random pitch.</summary>
    public static void Sfx(Node from, string name, float pitchVariation = 0.08f, float volume = 1f)
    {
        EnsureHost(from);
        if (SfxPlayers.Count == 0) return;
        var player = SfxPlayers[_nextVoice++ % SfxPlayers.Count];
        if (!GodotObject.IsInstanceValid(player) || !player.IsInsideTree()) return;
        var stream = Load($"res://Audio/Sfx/{name}.wav");
        if (stream is null) return;
        player.Stream = stream;
        player.PitchScale = 1f + (float)GD.RandRange(-pitchVariation, pitchVariation);
        player.VolumeDb = ToDb(GameSession.Settings.Sfx * volume);
        player.Play();
    }

    private static readonly List<AudioStreamPlayer> VoicePlayers = new();
    private static int _nextSpeaker;

    /// <summary>Plays a monster voice clip: res://Audio/Voice/{clip}.ogg (two dedicated channels, so sounds never cut it off).</summary>
    public static void Voice(Node from, string clip, float volume = 1f, float pitch = 1f)
    {
        EnsureHost(from);
        if (_host is null || !GodotObject.IsInstanceValid(_host)) return;
        if (VoicePlayers.Count == 0 || !GodotObject.IsInstanceValid(VoicePlayers[0]))
        {
            VoicePlayers.Clear();
            for (int i = 0; i < 2; i++)
            {
                var p = new AudioStreamPlayer();
                _host.AddChild(p);
                VoicePlayers.Add(p);
            }
        }
        var stream = Load($"res://Audio/Voice/{clip}.ogg");
        if (stream is null) return;
        if (stream is AudioStreamOggVorbis ogg) ogg.Loop = false;
        var player = VoicePlayers[_nextSpeaker++ % VoicePlayers.Count];
        if (!player.IsInsideTree()) return;
        player.Stream = stream;
        player.PitchScale = pitch;
        player.VolumeDb = ToDb(GameSession.Settings.Sfx * volume);
        player.Play();
    }

    public static void ApplyVolumes()
    {
        if (_music is not null && GodotObject.IsInstanceValid(_music))
            _music.VolumeDb = ToDb(GameSession.Settings.Music * 0.7f);
    }
}
