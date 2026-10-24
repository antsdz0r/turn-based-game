using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>Contextual battle hints already shown (charge, break, phase...).</summary>
    public List<string> SeenHints { get; set; } = new();
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
    /// <summary>Weekly challenge results: week id → best floor reached (Floors + 1 = won).</summary>
    public Dictionary<string, int> WeeklyBest { get; set; } = new();
    /// <summary>Deepest endless descent (loops x floors + floor).</summary>
    public int EndlessBest { get; set; }
    // ── Lifetime counters for feats ──
    public int TotalBreaks { get; set; }
    public int TotalCombos { get; set; }
    public int TotalBounties { get; set; }
    /// <summary>Region keys whose boss has fallen at least once.</summary>
    public List<string> RegionsCleared { get; set; } = new();
    /// <summary>Feats already completed and paid out.</summary>
    public List<string> FeatsClaimed { get; set; } = new();
    /// <summary>Hero mastery XP (see Mastery) and the loadout choices made at hero select.</summary>
    public Dictionary<string, int> HeroXp { get; set; } = new();
    public List<string> CostumeOn { get; set; } = new();
    public List<string> AltOn { get; set; } = new();
    /// <summary>The last 20 finished runs, newest first (Compendium → Run History).</summary>
    public List<RunRecord> History { get; set; } = new();
    /// <summary>Lore codex pages found (see Lore), and the ones found this session (shown as NEW).</summary>
    public List<string> LoreFound { get; set; } = new();
    [JsonIgnore] public List<string> LoreNew { get; set; } = new();
    /// <summary>Heroes unlocked during the current run (end screen).</summary>
    [JsonIgnore] public List<string> NewHeroes { get; set; } = new();
    /// <summary>Mastery level-ups earned by the run that just ended (end screen).</summary>
    [JsonIgnore] public List<(string HeroId, int Level)> LastMastery { get; set; } = new();
    /// <summary>Feats completed by the run that just ended (end screen).</summary>
    [JsonIgnore] public List<FeatDef> LastFeats { get; set; } = new();

    public const int MaxHistory = 20;

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
        var lockedBefore = Meta.HeroUnlocks.Where(u => !Meta.IsHeroUnlocked(this, u.HeroId)).Select(u => u.HeroId).ToList();
        if (!BossesDefeated.Contains(bossId)) BossesDefeated.Add(bossId);
        foreach (var hero in lockedBefore.Where(h => Meta.IsHeroUnlocked(this, h))) if (!NewHeroes.Contains(hero)) NewHeroes.Add(hero);
        Lore.Unlock(this, "boss_" + bossId);
    }

    public void RecordRunEnd(RunState run, bool won)
    {
        LastShardsEarned = Meta.ShardsFor(run, won);
        Shards += LastShardsEarned;
        if (run.DailyDate is { } date)
            DailyBest[date] = Math.Max(DailyBest.GetValueOrDefault(date), won ? DungeonMap.Floors + 1 : run.Floor + 1);
        BestFloor = Math.Max(BestFloor, run.Floor + 1);
        var banked = run.Banked ?? new BankedStats(0, 0, 0, 0);
        TotalEnemiesSlain += run.Stats.EnemiesSlain - banked.EnemiesSlain;
        TotalBreaks += run.Stats.Breaks - banked.Breaks;
        TotalCombos += run.Stats.Combos - banked.Combos;
        TotalBounties += run.Stats.Bounties - banked.Bounties;
        if (run.Weekly is { } week)
            WeeklyBest[week] = Math.Max(WeeklyBest.GetValueOrDefault(week), won ? DungeonMap.Floors + 1 : run.Floor + 1);
        if (run.Loop > 0 || won) EndlessBest = Math.Max(EndlessBest, Endless.Depth(run));
        foreach (var key in run.Stats.RegionsCleared) if (!RegionsCleared.Contains(key)) RegionsCleared.Add(key);
        if (won && run.Loop == 0)
        {
            RunsWon++;
            HighestHeatWon = Math.Max(HighestHeatWon, run.Heat);
            if (FastestWinSeconds <= 0 || run.Stats.Seconds < FastestWinSeconds) FastestWinSeconds = run.Stats.Seconds;
            foreach (var hero in run.Party) HeroWins[hero.Id] = HeroWins.GetValueOrDefault(hero.Id) + 1;
        }
        LastMastery = Mastery.Award(this, run, won);
        // Feats pay out at the end of the run, so the end screen can show them.
        LastFeats = Feats.Claim(this);
        Lore.Refresh(this);
        LastShardsEarned += LastFeats.Sum(f => f.Shards) + LastMastery.Count(m => m.Level == Mastery.MaxLevel) * Mastery.MasteredShards;
        History.Insert(0, RunRecord.From(run, won, LastShardsEarned, LastFeats.Select(f => f.Name).ToList()));
        if (History.Count > MaxHistory) History.RemoveRange(MaxHistory, History.Count - MaxHistory);
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
    // ── Accessibility ──
    /// <summary>Text size multiplier for UI text (1.0, 1.15 or 1.3).</summary>
    public float TextScale { get; set; } = 1f;
    /// <summary>No screen shake, camera punches, hit-stops or full-screen flashes.</summary>
    public bool ReduceMotion { get; set; }
    /// <summary>Colour-blind friendly element and health-bar colours (Okabe-Ito palette).</summary>
    public bool ColorblindMode { get; set; }
    /// <summary>Rebound keys: action id → Godot keycode (missing = default).</summary>
    public Dictionary<string, long> Keys { get; set; } = new();

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
            s.TextScale = Math.Clamp(s.TextScale <= 0 ? 1f : s.TextScale, 1f, 1.3f);
            s.Keys ??= new();
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
