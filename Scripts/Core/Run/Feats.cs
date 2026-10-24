using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>
/// A long-term goal that spans runs. Completing it pays Ember Shards once, and some feats unlock a weapon
/// (locked weapons never show up in rewards or shops until then).
/// </summary>
public sealed record FeatDef(string Id, string Name, string Description, int Shards, Func<Profile, int> Count, int Goal,
                             string? UnlocksItem = null);

public static class Feats
{
    private static int Boss(Profile p, string id) => p.BossesDefeated.Contains(id) ? 1 : 0;

    public static readonly IReadOnlyList<FeatDef> All = new List<FeatDef>
    {
        // ── Progress ──
        new("descent", "Into the Dark", "Reach the second region.", 10, p => p.BestFloor >= DungeonMap.FloorsPerRegion + 1 ? 1 : 0, 1),
        new("halfway", "Halfway Down", "Reach the fourth region.", 20, p => p.BestFloor >= DungeonMap.FloorsPerRegion * 3 + 1 ? 1 : 0, 1),
        new("rekindled", "Ember Rekindled", "Defeat the Hollow King and win a run.", 40, p => p.RunsWon, 1),
        new("cartographer", "Cartographer", "Clear every one of the seven regions (across any number of runs).", 50,
            p => p.RegionsCleared.Count, 7),

        // ── Bosses (unlock their region's signature weapon) ──
        new("bonebreaker", "Bonebreaker", "Defeat the Bone Colossus.", 10, p => Boss(p, "colossus"), 1),
        new("weedkiller", "Weedkiller", "Defeat the Mother Bloom. Unlocks the Gravewheat Scythe.", 25, p => Boss(p, "mother_bloom"), 1,
            UnlocksItem: "gravewheat_scythe"),
        new("silence", "Silence in the Stacks", "Defeat the Archivist. Unlocks the Vigil Lantern.", 25, p => Boss(p, "archivist"), 1,
            UnlocksItem: "vigil_lantern"),
        new("wyrmslayer", "Wyrmslayer", "Find the Sealed Door and slay the Ember Wyrm.", 50, p => Boss(p, "ember_wyrm"), 1),
        new("regicide", "All the King's Servants", "Defeat all six region bosses.", 60,
            p => new[] { "colossus", "matriarch", "lich", "tyrant", "mother_bloom", "archivist" }.Count(b => p.BossesDefeated.Contains(b)), 6),

        // ── Mastery (unlock weapons that reward the skill) ──
        new("breaker", "Breaker of Things", "Break enemies 40 times. Unlocks the Penitent's Flail.", 20, p => p.TotalBreaks, 40,
            UnlocksItem: "penitents_flail"),
        new("combo_artist", "Combo Artist", "Trigger 25 elemental combos. Unlocks the Rimefang Rapier.", 20, p => p.TotalCombos, 25,
            UnlocksItem: "rimefang_rapier"),
        new("bounty_hunter", "Bounty Hunter", "Complete 20 battle bounties.", 25, p => p.TotalBounties, 20),
        new("slayer", "Slayer", "Slay 300 enemies.", 20, p => p.TotalEnemiesSlain, 300),
        new("exterminator", "Exterminator", "Slay 1000 enemies.", 50, p => p.TotalEnemiesSlain, 1000),

        // ── Challenge ──
        new("heat2", "Turning Up the Heat", "Win a run at Heat 2 or higher.", 30, p => p.HighestHeatWon >= 2 ? 1 : 0, 1),
        new("heat5", "Inferno", "Win a run at Heat 5.", 100, p => p.HighestHeatWon >= 5 ? 1 : 0, 1),
        new("roster", "Full Roster", "Win runs with 6 different heroes.", 40, p => p.HeroWins.Count(kv => kv.Value > 0), 6),
        new("swift", "Swift Flame", "Win a run in under 30 minutes.", 30, p => p.FastestWinSeconds is > 0.0 and < 1800.0 ? 1 : 0, 1),
        new("weekly", "Week of Trials", "Win a weekly challenge.", 30, p => p.WeeklyBest.Values.Any(v => v > DungeonMap.Floors) ? 1 : 0, 1),
        new("abyss", "Into the Abyss", "Clear a whole endless loop (descend 40 floors in one run).", 60, p => p.EndlessBest >= DungeonMap.Floors * 2 ? 1 : 0, 1),
        new("daily", "Daily Devotion", "Win a daily challenge.", 20, p => p.DailyBest.Values.Any(v => v > DungeonMap.Floors) ? 1 : 0, 1),
    };

    public static int Progress(Profile p, FeatDef f) => Math.Min(f.Goal, f.Count(p));
    public static bool IsDone(Profile p, FeatDef f) => f.Count(p) >= f.Goal;

    /// <summary>Weapons still locked behind unfinished feats.</summary>
    public static HashSet<string> LockedItems(Profile p) =>
        All.Where(f => f.UnlocksItem is not null && !p.FeatsClaimed.Contains(f.Id) && !IsDone(p, f)).Select(f => f.UnlocksItem!).ToHashSet();

    /// <summary>Pays out every feat completed since the last check. Returns the newly completed feats.</summary>
    public static List<FeatDef> Claim(Profile p)
    {
        var fresh = All.Where(f => !p.FeatsClaimed.Contains(f.Id) && IsDone(p, f)).ToList();
        foreach (var f in fresh)
        {
            p.FeatsClaimed.Add(f.Id);
            p.Shards += f.Shards;
        }
        return fresh;
    }
}
