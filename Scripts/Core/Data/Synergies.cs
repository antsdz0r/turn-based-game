using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>A duo technique: when both heroes are in the party, the first one gains a joint ability in battle.</summary>
public sealed record SynergyDef(string HeroA, string HeroB, string AbilityId, string Flavor);

public static partial class Database
{
    public static readonly IReadOnlyList<SynergyDef> Synergies = new List<SynergyDef>
    {
        new("vanguard", "warden", "duo_hearth_guard", "Oath and hearth: Kael raises his shield while Maela's flame warms the line."),
        new("arcanist", "spellblade", "duo_spellbind", "Orrin chains the target in runes, Isolde cuts through every one."),
        new("ranger", "shadowblade", "duo_crossfire", "Wren pins the target, Nyx is already behind it."),
        new("hexer", "berserker", "duo_blood_curse", "Sable curses the blood, Brakka spills it."),
        new("alchemist", "warden", "duo_panacea", "Corvin's tinctures in Maela's holy flame."),
        new("arcanist", "alchemist", "duo_volatile_reaction", "One spark, one flask, one very large fire."),
        new("berserker", "vanguard", "duo_shieldbreaker", "Kael braces his shield, Brakka leaps off it."),
        new("shadowblade", "hexer", "duo_death_mark", "A whisper of doom, then a single knife."),
        new("spellblade", "ranger", "duo_storm_volley", "Isolde charges the arrows, Wren lets them fly."),
    };

    public static IEnumerable<SynergyDef> ActiveSynergies(IEnumerable<string> party)
    {
        var ids = party.ToHashSet();
        return Synergies.Where(s => ids.Contains(s.HeroA) && ids.Contains(s.HeroB));
    }

    /// <summary>Gives each active duo ability to its first hero (once per battle: it starts on a short cooldown).</summary>
    public static void ApplySynergies(List<Combatant> heroes)
    {
        foreach (var syn in ActiveSynergies(heroes.Select(h => h.DefId)))
        {
            var owner = heroes.First(h => h.DefId == syn.HeroA);
            if (owner.Abilities.Any(a => a.Def.Id == syn.AbilityId)) continue;
            owner.Abilities.Add(new AbilityInstance(Ability(syn.AbilityId), false) { CooldownLeft = 2 });
        }
    }

    private static IEnumerable<AbilityDef> BuildDuoAbilities()
    {
        yield return new AbilityDef
        {
            Id = "duo_hearth_guard", Name = "Hearth Guard", Target = TargetKind.AllAllies, Shield = 7, Cooldown = 5, Vfx = Vfx.Buff,
            Applies = { new StatusApply(StatusKind.Regen, 3, 2) },
            Description = "Duo (Kael + Maela). Every ally gains a 7 Shield and Regen 3 for 2 turns. Free; usable every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_spellbind", Name = "Spellbind", Power = 5, Cooldown = 5, Accuracy = 100, Vfx = Vfx.Beam, Interrupt = true,
            ElementSequence = [Element.Fire, Element.Ice, Element.Lightning, Element.Physical],
            Description = "Duo (Orrin + Isolde). Four hits: fire, ice, lightning and steel (5 each). Interrupts. Free; every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_crossfire", Name = "Crossfire", Power = 9, Hits = 2, Cooldown = 5, CritBonus = 30, Accuracy = 100, Vfx = Vfx.Arrow,
            Applies = { new StatusApply(StatusKind.Marked, 0, 2) },
            Description = "Duo (Wren + Nyx). An arrow and a knife at once: two hits of 9 with +30% crit, then Marks. Free; every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_blood_curse", Name = "Blood Curse", Target = TargetKind.AllEnemies, Power = 4, Cooldown = 5, Accuracy = 100, Vfx = Vfx.Cloud,
            Applies = { new StatusApply(StatusKind.Bleed, 3, 3), new StatusApply(StatusKind.Weakened, 0, 1) },
            Description = "Duo (Sable + Brakka). Every enemy bleeds 3 for 3 turns and is Weakened. Free; every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_panacea", Name = "Panacea", Target = TargetKind.AllAllies, Heal = 10, Cleanse = true, Cooldown = 5, Vfx = Vfx.Heal,
            Description = "Duo (Corvin + Maela). Heal every ally 10 and cleanse them. Free; every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_volatile_reaction", Name = "Volatile Reaction", Target = TargetKind.AllEnemies, Element = Element.Fire, Power = 9, BreakBonus = 1,
            Cooldown = 5, Accuracy = 100, Vfx = Vfx.Cloud, Applies = { new StatusApply(StatusKind.Burn, 3, 2) },
            Description = "Duo (Orrin + Corvin). An explosion: 9 fire damage and Burn 3 to every enemy. Free; every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_shieldbreaker", Name = "Shieldbreaker Leap", Melee = true, Power = 16, BreakBonus = 2, IgnoresArmor = true, Cooldown = 5, Accuracy = 100,
            Description = "Duo (Brakka + Kael). Brakka leaps off Kael's shield: 16 damage, ignores armor, +2 break pips on a weakness. Free; every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_death_mark", Name = "Death Mark", Power = 8, PowerPerDebuff = 4, IgnoresArmor = true, Cooldown = 5, Accuracy = 100, Vfx = Vfx.Debuff,
            Applies = { new StatusApply(StatusKind.Poison, 5, 3) },
            Description = "Duo (Nyx + Sable). 8 damage +4 per affliction on the target, ignores armor, then Poison 5. Free; every 5 turns.",
        };
        yield return new AbilityDef
        {
            Id = "duo_storm_volley", Name = "Storm Volley", Target = TargetKind.AllEnemies, Element = Element.Lightning, Power = 7, Cooldown = 5,
            Accuracy = 100, Vfx = Vfx.Arrow, Applies = { new StatusApply(StatusKind.Slowed, 0, 1) },
            Description = "Duo (Isolde + Wren). Charged arrows rain on every enemy: 7 lightning damage and Slow. Free; every 5 turns.",
        };
    }
}
