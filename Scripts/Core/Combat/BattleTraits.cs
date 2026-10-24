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
        return (int)Math.Round(power);
    }
}
