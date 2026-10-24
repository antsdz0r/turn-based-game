using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>
/// A team combo: one hero leaves a status on an enemy (the setup), another hero hits it with the right
/// element (the payoff). The setup status is consumed and the payoff hit gets a bonus.
/// </summary>
public sealed record ComboDef(string Id, string Name, StatusKind Setup, Element Payoff, double DamageMult, int BreakBonus,
                              string Description, string ColorHex);

public static class Combos
{
    public static readonly IReadOnlyList<ComboDef> All = new List<ComboDef>
    {
        new("shatter", "Shatter", StatusKind.Slowed, Element.Physical, 1.5, 1,
            "A PHYSICAL hit on an enemy another hero Slowed (chilled): +50% damage and +1 break pip.", "#bfeaff"),
        new("ignite", "Ignite", StatusKind.Poison, Element.Fire, 1.0, 0,
            "A FIRE hit on an enemy another hero Poisoned: the venom ignites for twice its Poison as extra damage.", "#ff8a2c"),
        new("overload", "Overload", StatusKind.Burn, Element.Lightning, 1.3, 0,
            "A LIGHTNING hit on an enemy another hero set Burning: +30% damage and the blast arcs to every other enemy for half.", "#d9b8ff"),
        new("frostbite", "Frostbite", StatusKind.Bleed, Element.Ice, 1.4, 1,
            "An ICE hit on an enemy another hero made Bleed: +40% damage, +1 break pip, and it is Slowed.", "#7fd4ff"),
    };

    public static ComboDef? Get(string id) => All.FirstOrDefault(c => c.Id == id);
}

public sealed partial class Battle
{
    private bool _comboChain;                       // a combo's own follow-up hits can't trigger combos
    public int CombosTriggered { get; private set; }

    /// <summary>The combo this hit would trigger, or null. Only setups left by a DIFFERENT hero count.</summary>
    public ComboDef? ComboFor(Combatant attacker, Combatant target, Element element)
    {
        if (_comboChain || !attacker.IsHero || target.IsHero || !target.IsAlive) return null;
        foreach (var combo in Combos.All)
        {
            if (combo.Payoff != element) continue;
            if (target.Get(combo.Setup) is { } setup && setup.SourceUid != attacker.Uid && _heroes.Any(h => h.Uid == setup.SourceUid))
                return combo;
        }
        return null;
    }

    /// <summary>Combos the enemy is primed for right now (shown in its tooltip).</summary>
    public IEnumerable<ComboDef> PrimedCombos(Combatant enemy) =>
        Combos.All.Where(c => !enemy.IsHero && enemy.Get(c.Setup) is { } s && _heroes.Any(h => h.Uid == s.SourceUid));

    /// <summary>Consumes the setup and applies the payoff's extra effects (after the hit itself).</summary>
    private void ResolveCombo(ComboDef combo, Combatant attacker, Combatant target, int power, int hitDamage)
    {
        var setup = target.Get(combo.Setup);
        string setter = setup is null ? "an ally" : _heroes.FirstOrDefault(h => h.Uid == setup.SourceUid)?.Name ?? "an ally";
        CombosTriggered++;
        Emit(new ComboEvent(target, combo.Name, combo.ColorHex));
        Log($"  [color={combo.ColorHex}][b]COMBO: {combo.Name}![/b][/color] [color=#a99f8f]({setter} set it up, {attacker.Name} finished it)[/color]");
        if (setup is not null) RemoveStatus(target, setup);

        _comboChain = true;
        try
        {
            switch (combo.Id)
            {
                case "ignite":
                    if (setup is not null && target.IsAlive) DotDamage(target, StatusKind.Burn, setup.Amount * 2, useShield: true);
                    break;
                case "overload":
                    foreach (var other in AliveEnemies.Where(e => e != target).ToList())
                        Strike(attacker, other, Math.Max(1, power / 2), Element.Lightning, 0, 0, false, false, 0, DefenseQuality.None);
                    break;
                case "frostbite":
                    if (target.IsAlive) ApplyStatus(target, new StatusApply(StatusKind.Slowed, 0, 2), attacker.Uid);
                    break;
            }
        }
        finally { _comboChain = false; }
    }
}
