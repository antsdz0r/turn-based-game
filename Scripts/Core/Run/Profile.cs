using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TBRPG.Core;

/// <summary>Persistent statistics across runs (separate from the current run's save).</summary>
public sealed class Profile
{
    public int RunsStarted { get; set; }
    public int RunsWon { get; set; }
    public int BestFloor { get; set; }
    public int TotalEnemiesSlain { get; set; }
    public double FastestWinSeconds { get; set; }
    public Dictionary<string, int> HeroWins { get; set; } = new();
    /// <summary>Highest heat level won (-1 = never won).</summary>
    public int HighestHeatWon { get; set; } = -1;
    public bool TutorialSeen { get; set; }
    /// <summary>Enemy ids met at least once (unlocks Compendium entries).</summary>
    public List<string> SeenEnemies { get; set; } = new();
    /// <summary>Ember Shards: earned at the end of every run, spent at the Ember Shrine.</summary>
    public int Shards { get; set; }
    public int LastShardsEarned { get; set; }
    /// <summary>Shrine upgrade levels (and "hero_xxx" = 1 for heroes bought with shards).</summary>
    public Dictionary<string, int> Upgrades { get; set; } = new();
    /// <summary>Boss ids defeated at least once (unlock heroes).</summary>
    public List<string> BossesDefeated { get; set; } = new();
    /// <summary>Daily challenge results: date → best floor reached (16 = won).</summary>
    public Dictionary<string, int> DailyBest { get; set; } = new();

    [JsonIgnore] public int MaxHeatUnlocked => Math.Min(Heat.Max, HighestHeatWon + 1);

    public bool MarkSeen(IEnumerable<string> enemyIds)
    {
        bool changed = false;
        foreach (var id in enemyIds)
        {
            if (SeenEnemies.Contains(id)) continue;
            SeenEnemies.Add(id);
            changed = true;
        }
        return changed;
    }

    public void RecordBoss(string bossId)
    {
        if (!BossesDefeated.Contains(bossId)) BossesDefeated.Add(bossId);
    }

    public void RecordRunEnd(RunState run, bool won)
    {
        LastShardsEarned = Meta.ShardsFor(run, won);
        Shards += LastShardsEarned;
        if (run.DailyDate is { } date)
            DailyBest[date] = Math.Max(DailyBest.GetValueOrDefault(date), won ? DungeonMap.Floors + 1 : run.Floor + 1);
        BestFloor = Math.Max(BestFloor, run.Floor + 1);
        TotalEnemiesSlain += run.Stats.EnemiesSlain;
        if (!won) return;
        RunsWon++;
        HighestHeatWon = Math.Max(HighestHeatWon, run.Heat);
        if (FastestWinSeconds <= 0 || run.Stats.Seconds < FastestWinSeconds) FastestWinSeconds = run.Stats.Seconds;
        foreach (var hero in run.Party) HeroWins[hero.Id] = HeroWins.GetValueOrDefault(hero.Id) + 1;
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static Profile FromJson(string json)
    {
        try { return JsonSerializer.Deserialize<Profile>(json) ?? new Profile(); }
        catch (Exception) { return new Profile(); }
    }
}

public sealed class Settings
{
    public float Music { get; set; } = 0.7f;
    public float Sfx { get; set; } = 0.8f;
    public bool Fullscreen { get; set; } = true;
    /// <summary>Settings format version (old files get their display defaults migrated).</summary>
    public int Version { get; set; }
    public bool TimingDefense { get; set; } = true;
    public int BattleSpeed { get; set; } = 2;

    public string ToJson()
    {
        Version = 2;
        return JsonSerializer.Serialize(this);
    }

    public static Settings FromJson(string json)
    {
        try
        {
            var s = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            s.Music = Math.Clamp(s.Music, 0, 1);
            s.Sfx = Math.Clamp(s.Sfx, 0, 1);
            s.BattleSpeed = Math.Clamp(s.BattleSpeed, 1, 3);
            if (s.Version < 2)
            {
                // v2: the game now starts fullscreen and at 2x battle speed by default.
                s.Fullscreen = true;
                s.BattleSpeed = Math.Max(2, s.BattleSpeed);
                s.Version = 2;
            }
            return s;
        }
        catch (Exception) { return new Settings(); }
    }
}
