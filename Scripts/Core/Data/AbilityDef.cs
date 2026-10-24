using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TBRPG.Core;

/// <summary>A status an ability or enemy move applies.</summary>
public sealed record StatusApply(StatusKind Kind, int Amount, int Turns, bool OnSelf = false, int Chance = 100);

/// <summary>
/// A hero ability, defined purely as data. Effects are applied in this order:
/// damage (Power x Hits) → statuses → heal → shield → cleanse → movement.
/// </summary>
public sealed class AbilityDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

    public TargetKind Target { get; init; } = TargetKind.SingleEnemy;
    public Element Element { get; init; } = Element.Physical;
    public int Power { get; init; }
    public int Hits { get; init; } = 1;
    /// <summary>If set, one hit per element in this order (overrides Element and Hits).</summary>
    public Element[]? ElementSequence { get; init; }
    /// <summary>0 = never misses.</summary>
    public int Accuracy { get; init; } = 100;
    public int CritBonus { get; init; }
    public bool Melee { get; init; }
    public bool IgnoresArmor { get; init; }
    /// <summary>Extra break-meter damage when it hits a weakness.</summary>
    public int BreakBonus { get; init; }

    public int Cost { get; init; }
    public int Cooldown { get; init; }
    public RowRequirement Requires { get; init; } = RowRequirement.Any;
    public bool IsBasic { get; init; }

    public List<StatusApply> Applies { get; init; } = new();
    public int Heal { get; init; }
    public int Shield { get; init; }
    public bool Cleanse { get; init; }
    /// <summary>The user switches row (Front ↔ Back) after acting.</summary>
    public bool SwitchSelfRow { get; init; }
    /// <summary>The ally target switches row.</summary>
    public bool SwitchTargetRow { get; init; }
    /// <summary>The user protects the ally target until its next turn.</summary>
    public bool Protect { get; init; }
    /// <summary>Cancels the target's charging attack.</summary>
    public bool Interrupt { get; init; }
    /// <summary>Spends all of the resource; each point adds this much power.</summary>
    public int PowerPerResource { get; init; }
    public int MinResource { get; init; }
    /// <summary>HP the user pays to use it (never kills).</summary>
    public int SelfDamage { get; init; }
    /// <summary>Heals the user for this % of the damage dealt.</summary>
    public int LifestealPct { get; init; }
    /// <summary>Extra power per negative status on the target.</summary>
    public int PowerPerDebuff { get; init; }
    /// <summary>Damage grows with the user's missing HP (+1% per 1% missing).</summary>
    public bool BerserkScaling { get; init; }

    public Vfx Vfx { get; init; } = Vfx.Melee;

    public int HitCount => ElementSequence?.Length ?? Hits;
    public Element HitElement(int hit) => ElementSequence is { Length: > 0 } seq ? seq[hit % seq.Length] : Element;

    public bool TargetsEnemies => Target is TargetKind.SingleEnemy or TargetKind.AllEnemies;
    public bool NeedsTarget => Target is TargetKind.SingleEnemy or TargetKind.SingleAlly;

    /// <summary>Upgraded abilities are ~35% stronger, apply +1 status potency and have -1 cooldown.</summary>
    public static int Up(int value, bool upgraded) => upgraded && value > 0 ? (int)Math.Round(value * 1.35) : value;

    public int EffectivePower(bool upgraded) => Up(Power, upgraded);
    public int EffectiveHeal(bool upgraded) => Up(Heal, upgraded);
    public int EffectiveShield(bool upgraded) => Up(Shield, upgraded);
    public int EffectiveCooldown(bool upgraded) => upgraded ? Math.Max(0, Cooldown - 1) : Cooldown;

    public IEnumerable<StatusApply> EffectiveApplies(bool upgraded) =>
        upgraded ? Applies.Select(a => a with { Amount = a.Amount > 0 ? a.Amount + 1 : 0, Turns = a.Turns > 0 ? a.Turns + 1 : 0 }) : Applies;

    /// <summary>A full, generated rules description for tooltips.</summary>
    public string RulesText(bool upgraded)
    {
        var sb = new StringBuilder();
        sb.Append(Description);
        sb.Append('\n');
        var tags = new List<string>();
        if (IsBasic) tags.Add("Basic attack");
        if (Cost > 0) tags.Add($"Cost {Cost}");
        if (MinResource > 0) tags.Add($"Needs {MinResource}+");
        int cd = EffectiveCooldown(upgraded);
        if (cd > 0) tags.Add($"Cooldown {cd}");
        if (Requires != RowRequirement.Any) tags.Add($"{Requires} row only");
        if (SelfDamage > 0) tags.Add($"Costs {SelfDamage} HP");
        if (LifestealPct > 0) tags.Add($"Lifesteal {LifestealPct}%");
        if (Melee) tags.Add("Melee");
        else if (TargetsEnemies) tags.Add("Ranged");
        if (Accuracy is > 0 and < 100) tags.Add($"{Accuracy}% accuracy");
        if (upgraded) tags.Add("UPGRADED");
        sb.Append(string.Join("  ·  ", tags));
        return sb.ToString();
    }
}

/// <summary>An ability as owned by a hero in battle.</summary>
public sealed class AbilityInstance
{
    public AbilityDef Def { get; }
    public bool Upgraded { get; }
    public int CooldownLeft { get; set; }

    public AbilityInstance(AbilityDef def, bool upgraded)
    {
        Def = def;
        Upgraded = upgraded;
    }

    public string DisplayName => Upgraded ? Def.Name + "+" : Def.Name;
}
