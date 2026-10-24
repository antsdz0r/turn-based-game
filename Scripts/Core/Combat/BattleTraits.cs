using System;
using System.Linq;
using System.Collections.Generic;

namespace TBRPG.Core;

/// <summary>Per-battle bookkeeping for hero traits and relics that trigger once per battle or round.</summary>
public sealed partial class Battle
{
    private readonly HashSet<int> _firstStrike = new();     // heroes that already made their first attack
    private readonly HashSet<int> _guardianSaved = new();   // allies Maela's Guardian Angel already saved
    private bool _phoenixUsed;
    private bool _freeItemUsed;
    private int _spreeRound = -1;
    private Combatant? _extraTurn;

    public static bool HasTrait(Combatant unit, string id) => unit.IsHero && unit.Traits.Contains(id);
    public static bool HasWeapon(Combatant unit, string effect) => unit.IsHero && unit.WeaponEffect == effect;
    private readonly HashSet<int> _huntMarked = new();
    private readonly HashSet<int> _staticHit = new();                  // Static Sigil: enemies already shocked
    private readonly Dictionary<int, int> _focusCharge = new();        // Stormglass charge per hero
    private bool _arcing;                                              // Stormglass arc in progress (no chains)
    private readonly Dictionary<int, int> _steadfastUntil = new();     // stun immunity: unit uid → last round

    /// <summary>Resource cost after weapon effects (Last Whisper: -1).</summary>
    public int EffectiveCost(Combatant hero, AbilityDef def) =>
        Math.Max(0, def.Cost - (def.Cost > 0 && HasWeapon(hero, "blood_price") ? 1 : 0) - (def.Cost > 0 && hero.IsHero && HasRelic("hollow_lantern") ? 1 : 0));

    /// <summary>Number of hits (Gutterfangs: the basic attack strikes twice).</summary>
    public int HitsFor(Combatant hero, AbilityDef def) => def.IsBasic && HasWeapon(hero, "twin") ? def.HitCount * 2 : def.HitCount;

    /// <summary>The ability the Hollow King's Usurp would seal on this hero right now (shown on his intent).</summary>
    public AbilityInstance? UsurpTarget(Combatant hero) =>
        hero.Abilities.Where(a => a.CooldownLeft == 0).OrderByDescending(a => a.Def.Cost).ThenByDescending(a => a.Def.Power).FirstOrDefault();

    /// <summary>Twin Flame relic: damage over time on enemies ticks double.</summary>
    private int DotMult(Combatant unit) => !unit.IsHero && HasRelic("twin_flame") ? 2 : 1;
}

public sealed partial class Battle
{
    public string? Daily { get; }

    /// <summary>Power of one ability hit on a target, including every scaling rule (shared with the preview).</summary>
    private int AbilityPower(Combatant hero, AbilityDef def, bool upgraded, Combatant target, int spentResource)
    {
        double power = def.EffectivePower(upgraded) + spentResource * def.PowerPerResource;
        if (def.PowerPerDebuff > 0) power += def.PowerPerDebuff * target.Statuses.Count(s => Combatant.IsNegative(s.Kind));
        if (def.BerserkScaling) power *= 1 + (100 - hero.HealthPct) / 100.0;   // +1% per 1% missing HP
        if (def.Target == TargetKind.AllEnemies && HasTrait(hero, "wren_barrage")) power *= 1.4;
        if (def.IsBasic && HasWeapon(hero, "twin")) power *= 0.6;
        return (int)Math.Round(power);
    }
}
