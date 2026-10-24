using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>
/// An omen: a battlefield condition that changes how one fight plays (blood moon, fog, crumbling floor...).
/// Rolled for some ordinary and elite fights; shown in the battle's objective panel.
/// </summary>
public sealed record OmenDef(string Id, string Name, string Description, string ColorHex);

/// <summary>A bounty: an optional objective for one fight. Meet it and win to earn extra gold.</summary>
public sealed record BountyDef(string Id, string Name, string Description, int Gold);

public static class Omens
{
    public static readonly IReadOnlyList<OmenDef> All = new List<OmenDef>
    {
        new("blood_moon", "Blood Moon", "Everyone deals +20% damage. Fights are fast and deadly.", "#ff6b6b"),
        new("thick_fog", "Thick Fog", "Every attack has -15% accuracy (never-miss attacks ignore it).", "#a9b8c8"),
        new("consecrated", "Consecrated Ground", "At the start of each round, every hero heals 2.", "#ffe09a"),
        new("ember_storm", "Ember Storm", "At the start of each round, a falling ember hits a random unit for 5 fire damage.", "#ff8a2c"),
        new("crumbling_floor", "Crumbling Floor", "At the start of each round, everyone standing in a front row takes 2 damage.", "#b89262"),
        new("ley_surge", "Ley Surge", "At the start of each round, every hero gains 1 resource.", "#6fd0c0"),
    };

    public static OmenDef? Get(string? id) => id is null ? null : All.FirstOrDefault(o => o.Id == id);

    /// <summary>About one fight in three (never the first fight, never a boss) gets an omen.</summary>
    public static string? Roll(NodeType type, int floor, Random rng) =>
        type is NodeType.Battle or NodeType.Elite && floor > 0 && rng.Next(100) < 35 ? All[rng.Next(All.Count)].Id : null;
}

public static class Bounties
{
    public static readonly IReadOnlyList<BountyDef> All = new List<BountyDef>
    {
        new("swift", "Swift Victory", "Win by the end of round 4.", 20),
        new("combo", "Teamwork", "Trigger a team combo.", 20),
        new("breaker", "Shatter Their Guard", "Break 2 enemies.", 18),
        new("untouched", "Nobody Falls", "Win without any hero falling.", 15),
        new("exploit", "Exploit Weakness", "Land 4 hits on weaknesses.", 15),
    };

    public static BountyDef? Get(string? id) => id is null ? null : All.FirstOrDefault(b => b.Id == id);

    /// <summary>Most ordinary and elite fights offer a bounty; bosses don't.</summary>
    public static string? Roll(NodeType type, Random rng) =>
        type is NodeType.Battle or NodeType.Elite && rng.Next(100) < 70 ? All[rng.Next(All.Count)].Id : null;
}

public sealed partial class Battle
{
    public string? Omen { get; }
    public string? Bounty { get; }
    public int Breaks { get; private set; }
    public int WeaknessHits { get; private set; }
    public int HeroFalls { get; private set; }

    /// <summary>True while the bounty's condition holds (Swift: still on time; Nobody Falls: nobody fell yet).</summary>
    public bool BountyMet => Bounty switch
    {
        "swift" => Round <= 4,
        "combo" => CombosTriggered > 0,
        "breaker" => Breaks >= Math.Min(2, _enemies.Count(e => !e.Summoned)),
        "untouched" => HeroFalls == 0,
        "exploit" => WeaknessHits >= 4,
        _ => false,
    };

    /// <summary>Progress text for the objective panel, e.g. "1/2".</summary>
    public string BountyProgress => Bounty switch
    {
        "swift" => $"round {Math.Min(Round, 99)}/4",
        "combo" => $"{Math.Min(CombosTriggered, 1)}/1",
        "breaker" => $"{Math.Min(Breaks, 2)}/{Math.Min(2, _enemies.Count(e => !e.Summoned))}",
        "untouched" => HeroFalls == 0 ? "so far, so good" : "failed",
        "exploit" => $"{Math.Min(WeaknessHits, 4)}/4",
        _ => "",
    };

    private bool HasOmen(string id) => Omen == id;

    /// <summary>Start-of-round omen effects (after intents are chosen).</summary>
    private void OmenRoundStart()
    {
        switch (Omen)
        {
            case "consecrated":
                foreach (var hero in AliveHeroes.ToList()) HealUnit(hero, 2, fromHero: false);
                break;
            case "ley_surge":
                foreach (var hero in AliveHeroes) GainResource(hero, 1);
                break;
            case "ember_storm":
                var all = _heroes.Concat(_enemies).Where(u => u.IsAlive).ToList();
                if (all.Count > 0)
                {
                    var victim = all[_rng.Next(all.Count)];
                    Emit(new TextEvent(victim, "Falling ember!", "#ff8a2c"));
                    Log($"  [color=#ff8a2c]An ember falls on {victim.Name}.[/color]");
                    DotDamage(victim, StatusKind.Burn, 5, useShield: true);
                }
                break;
            case "crumbling_floor":
                foreach (var unit in _heroes.Concat(_enemies).Where(u => u.IsAlive && u.Row == Row.Front).ToList())
                    DotDamage(unit, StatusKind.Bleed, 2, useShield: true);
                break;
        }
    }
}
