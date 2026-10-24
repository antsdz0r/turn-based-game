using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>An alternative ability a hero can bring instead of one of their usual four (mastery level 2).</summary>
public sealed record AltAbility(string HeroId, string Replaces, string AbilityId);

/// <summary>A hero's look: the default, or a mastery costume (a full redesign, Units/{hero}_c.png).</summary>
public sealed record CostumeDef(string HeroId, string Sprite, string Name, string Description);

/// <summary>
/// Hero mastery: every hero earns mastery XP in the runs they take part in.
///   Level 1 unlocks a costume, level 2 an alternative ability, level 3 ("Mastered") starts every run with the
///   hero's basic attack already upgraded and pays a one-time shard bonus.
/// </summary>
public static class Mastery
{
    public static readonly int[] Thresholds = [100, 250, 500];
    public const int MaxLevel = 3;
    public const int MasteredShards = 25;

    public static readonly IReadOnlyList<CostumeDef> Costumes = new List<CostumeDef>
    {
        new("vanguard", "vanguard_c", "Cinderforged Juggernaut", "Black-iron plate split by ember cracks, a horned great helm, tower shield and warhammer."),
        new("arcanist", "arcanist_c", "Storm Sage", "Navy robes stitched with lightning runes, a crown of floating rune stones and a crystal staff."),
        new("warden", "warden_c", "Sunpriestess", "White-and-gold vestments, a radiant halo and a sun-disc staff."),
        new("shadowblade", "shadowblade_c", "Kitsune", "A white fox mask, a fox-tail scarf and twin curved blades."),
        new("ranger", "ranger_c", "Wildwarden", "A stag-skull headdress, a cloak of leaves and a living longbow."),
        new("berserker", "berserker_c", "Bear Jarl", "A whole bear pelt worn as a hood, iron bracers and two hand axes."),
        new("hexer", "hexer_c", "Grave Witch", "A half-skull mask, a cloak of black feathers and a raven familiar."),
        new("alchemist", "alchemist_c", "Clockwork Tinkerer", "Brass goggles, a mechanical arm and a backpack of bubbling tanks."),
        new("spellblade", "spellblade_c", "Dragon Knight", "Red dragon-scale armor, a horned dragon helm and a flaming greatsword."),
    };

    public static readonly IReadOnlyList<AltAbility> Alternatives = new List<AltAbility>
    {
        new("vanguard", "intercept", "shield_wall"),
        new("arcanist", "prismatic", "meteor"),
        new("warden", "renewal", "sanctuary"),
        new("shadowblade", "hemorrhage", "fan_of_knives"),
        new("ranger", "volley", "snare_shot"),
        new("berserker", "whirlwind", "blood_frenzy"),
        new("hexer", "creeping_doom", "plague_swarm"),
        new("alchemist", "acid_flask", "smoke_flask"),
        new("spellblade", "frost_edge", "aegis_rune"),
    };

    public static int Xp(Profile p, string heroId) => p.HeroXp.GetValueOrDefault(heroId);
    public static int Level(Profile p, string heroId) => Thresholds.Count(t => Xp(p, heroId) >= t);
    /// <summary>XP needed for the next level, or -1 when mastered.</summary>
    public static int NextThreshold(Profile p, string heroId) => Level(p, heroId) >= MaxLevel ? -1 : Thresholds[Level(p, heroId)];

    public static bool HasCostume(Profile p, string heroId) => Level(p, heroId) >= 1;
    public static bool HasAlt(Profile p, string heroId) => Level(p, heroId) >= 2;
    public static bool IsMastered(Profile p, string heroId) => Level(p, heroId) >= 3;

    public static CostumeDef? Costume(string heroId) => Costumes.FirstOrDefault(c => c.HeroId == heroId);
    public static AltAbility? Alt(string heroId) => Alternatives.FirstOrDefault(a => a.HeroId == heroId);

    /// <summary>Mastery XP a run gives each hero who took part: depth, bosses and a win.</summary>
    public static int XpFor(RunState run, bool won) => (run.Floor + 1) * 4 + run.BossesKilled * 15 + (won ? 40 + 10 * run.Heat : 0);

    public static string LevelName(int level) => level switch
    {
        0 => "Novice",
        1 => "Adept",
        2 => "Veteran",
        _ => "Mastered",
    };

    public static string RewardText(int level) => level switch
    {
        1 => "a new costume",
        2 => "an alternative ability",
        3 => "Mastered: starts every run with an upgraded basic attack",
        _ => "",
    };

    /// <summary>Gives every hero in the run its mastery XP. Returns the level-ups (hero, new level).</summary>
    public static List<(string HeroId, int Level)> Award(Profile p, RunState run, bool won)
    {
        var ups = new List<(string, int)>();
        if (run.Daily is not null) return ups;      // dailies are for score, not progress
        int xp = XpFor(run, won);
        foreach (var hero in run.Party.Select(h => h.Id).Distinct())
        {
            int before = Level(p, hero);
            p.HeroXp[hero] = Xp(p, hero) + xp;
            int after = Level(p, hero);
            for (int lv = before + 1; lv <= after; lv++)
            {
                ups.Add((hero, lv));
                if (lv == MaxLevel) p.Shards += MasteredShards;
            }
        }
        return ups;
    }

    /// <summary>A hero's loadout for a new run: costume and alternative ability as chosen, if unlocked.</summary>
    public static void ApplyLoadout(HeroState hero, Profile p, bool costume, bool alt)
    {
        hero.Costume = costume && HasCostume(p, hero.Id) ? Costume(hero.Id)?.Sprite : null;
        hero.AltAbility = alt && HasAlt(p, hero.Id) ? Alt(hero.Id)?.AbilityId : null;
        if (IsMastered(p, hero.Id) && !hero.Upgrades.Contains(hero.Def.BasicAbility)) hero.Upgrades.Add(hero.Def.BasicAbility);
    }
}
