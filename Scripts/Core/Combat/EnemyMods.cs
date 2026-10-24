using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>Random modifiers that make elites (and some later enemies) play differently.</summary>
public sealed record EnemyModDef(string Id, string Prefix, string Description, string ColorHex);

public static class EnemyMods
{
    public static readonly IReadOnlyList<EnemyModDef> All = new List<EnemyModDef>
    {
        new("frenzied", "Frenzied", "Frenzied: deals +30% damage and acts sooner (+2 speed).", "#ff6a4a"),
        new("armored", "Armored", "Armored: +4 armor and starts the battle behind a 10 Shield.", "#b0aec8"),
        new("splitting", "Splitting", "Splitting: when killed, it splits into two smaller copies.", "#b3d05a"),
        new("vampiric", "Vampiric", "Vampiric: heals for 40% of the damage it deals.", "#a8406a"),
        new("thorned", "Thorned", "Thorned: melee attackers take 3 damage.", "#6f9a40"),
    };

    public static EnemyModDef? Get(string? id) => id is null ? null : All.FirstOrDefault(m => m.Id == id);

    public static void Apply(Combatant unit, string id)
    {
        var mod = Get(id);
        if (mod is null || unit.Modifier is not null) return;
        unit.Modifier = id;
        unit.Name = $"{mod.Prefix} {unit.Name}";
        switch (id)
        {
            case "frenzied": unit.DamageMult = 1.3; unit.BaseSpeed += 2; break;
            case "armored": unit.Armor += 4; break;
            case "splitting": break;
            case "vampiric": unit.MaxHp = unit.Hp = (int)Math.Round(unit.MaxHp * 1.1); break;
            case "thorned": break;
        }
    }

    /// <summary>Elites always get one; from the second region on, ordinary enemies sometimes do too.</summary>
    public static void Roll(List<Combatant> enemies, int region, Random rng)
    {
        foreach (var e in enemies)
        {
            if (e.IsBoss) continue;
            bool roll = e.IsElite || (region >= 1 && rng.Next(100) < 8 + 6 * region);
            if (!roll) continue;
            var pool = All.Where(m => !(m.Id == "splitting" && e.IsElite && e.MaxHp > 70)).ToList();
            Apply(e, pool[rng.Next(pool.Count)].Id);
        }
    }
}
