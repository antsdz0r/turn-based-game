using System;
using System.Collections.Generic;

namespace TBRPG.Core;

/// <summary>
/// Endless mode: after the Hollow King falls, the party can descend again. Each loop draws a new map and region
/// order and makes every enemy much tougher; the score is how deep the party gets (loops x floors + floor).
/// </summary>
public static class Endless
{
    public const double HpPerLoop = 0.6;
    public const double DamagePerLoop = 0.35;

    public static void Scale(IEnumerable<Combatant> enemies, int loop)
    {
        if (loop <= 0) return;
        foreach (var e in enemies)
        {
            e.MaxHp = e.Hp = (int)Math.Round(e.MaxHp * (1 + HpPerLoop * loop));
            e.DamageMult *= 1 + DamagePerLoop * loop;
            e.BreakMax = e.Break = e.BreakMax + (loop >= 2 ? 1 : 0);
        }
    }

    /// <summary>Depth score: total floors descended across loops.</summary>
    public static int Depth(RunState run) => run.Loop * DungeonMap.Floors + run.Floor + 1;

    public static bool CanDescend(RunState run) => run.Daily is null && run.Weekly is null;

    /// <summary>Starts the next loop: new map, new region order, a rest, and the counters banked so the profile
    /// only adds what this loop earns.</summary>
    public static void Descend(RunState run)
    {
        run.Loop++;
        run.Banked = new BankedStats(run.Stats.EnemiesSlain, run.Stats.Breaks, run.Stats.Combos, run.Stats.Bounties);
        run.Map = DungeonMap.Generate(new Random(unchecked(run.Seed + run.Loop * 7919)));
        run.RegionOrder = RunState.RollRegionOrder(new Random(unchecked(run.Seed * 17 + run.Loop * 131)));
        run.CurrentNode = -1;
        run.NodeCompleted = true;
        run.ActiveBattle = null;
        run.Shop.Clear();
        run.ShopNode = -1;
        run.HealParty(60);
        foreach (var hero in run.Party) if (hero.Hp <= 0) hero.Hp = 1;
    }
}

/// <summary>Run counters already added to the profile by an earlier loop's victory.</summary>
public sealed record BankedStats(int EnemiesSlain, int Breaks, int Combos, int Bounties);
