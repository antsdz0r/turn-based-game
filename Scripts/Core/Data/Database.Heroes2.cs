using System.Collections.Generic;

namespace TBRPG.Core;

/// <summary>The two unlockable heroes added with the Garden and the Archive: Corvin the Alchemist and Isolde the Spellblade.</summary>
public static partial class Database
{
    private static IEnumerable<AbilityDef> BuildAbilitiesPack2()
    {
        // ───── Corvin Vey — Alchemist (Reagents) ─────
        yield return new AbilityDef
        {
            Id = "flask_toss", Name = "Flask Toss", IsBasic = true, Power = 4, Accuracy = 95, Vfx = Vfx.Projectile,
            Applies = { new StatusApply(StatusKind.Poison, 2, 2, Chance: 50) },
            Description = "Lob a corked vial. 50% chance to Poison 2.",
        };
        yield return new AbilityDef
        {
            Id = "acid_flask", Name = "Acid Flask",
            UpText = "Also Marks the target for 2 turns.", UpApplies = { new StatusApply(StatusKind.Marked, 0, 2) },
            Power = 5, Cost = 2, IgnoresArmor = true, BreakBonus = 1, Vfx = Vfx.Projectile,
            Applies = { new StatusApply(StatusKind.Poison, 3, 3) },
            Description = "Acid eats through armor: 5 damage (ignores armor) and Poison 3. +1 break pip on a weakness.",
        };
        yield return new AbilityDef
        {
            Id = "firebomb", Name = "Firebomb",
            UpText = "+1 break pip on a weakness.", UpBreakBonus = 1,
            Target = TargetKind.AllEnemies, Element = Element.Fire, Power = 6, Cost = 2, Cooldown = 2, Vfx = Vfx.Cloud,
            Applies = { new StatusApply(StatusKind.Burn, 2, 2) },
            Description = "A bottle of liquid fire: 6 fire damage and Burn 2 to every enemy.",
        };
        yield return new AbilityDef
        {
            Id = "elixir", Name = "Elixir",
            UpText = "Also Empowers the ally for 1 turn.", UpApplies = { new StatusApply(StatusKind.Empowered, 0, 1) },
            Target = TargetKind.SingleAlly, Heal = 10, Cleanse = true, Cost = 2, Vfx = Vfx.Heal,
            Applies = { new StatusApply(StatusKind.Regen, 2, 2) },
            Description = "A bitter cure: heal an ally 10, cleanse them, and give Regen 2 for 2 turns.",
        };
        yield return new AbilityDef
        {
            Id = "volatile_mix", Name = "Volatile Mixture", Element = Element.Lightning, Power = 3, PowerPerResource = 3, MinResource = 2,
            Accuracy = 100, Vfx = Vfx.Beam,
            Description = "Finisher: shakes every Reagent into one crackling flask (3 lightning power per point).",
        };

        // ───── Isolde Vane — Spellblade (Charge) ─────
        yield return new AbilityDef
        {
            Id = "runeblade", Name = "Runeblade", IsBasic = true, Melee = true, Power = 5, Accuracy = 95,
            Description = "A clean cut with a rune-etched sword. Builds Charge.",
        };
        yield return new AbilityDef
        {
            Id = "flame_edge", Name = "Flame Edge",
            UpText = "Also Marks the target for 1 turn.", UpApplies = { new StatusApply(StatusKind.Marked, 0, 1) },
            Melee = true, Element = Element.Fire, Power = 8, Cost = 1, Accuracy = 95,
            Applies = { new StatusApply(StatusKind.Burn, 2, 2) },
            Description = "Ignite the blade: 8 fire damage and Burn 2.",
        };
        yield return new AbilityDef
        {
            Id = "frost_edge", Name = "Frost Edge",
            UpText = "+1 break pip on a weakness.", UpBreakBonus = 1,
            Melee = true, Element = Element.Ice, Power = 7, Cost = 1, Accuracy = 95,
            Applies = { new StatusApply(StatusKind.Slowed, 0, 2), new StatusApply(StatusKind.Weakened, 0, 1) },
            Description = "Frost the blade: 7 ice damage, Slow for 2 turns and Weaken for 1 (numbs their next big hit).",
        };
        yield return new AbilityDef
        {
            Id = "storm_edge", Name = "Storm Edge",
            UpText = "Ignores armor.", UpIgnoresArmor = true,
            Element = Element.Lightning, Power = 8, Cost = 1, Cooldown = 2, Interrupt = true, Accuracy = 100, Vfx = Vfx.Beam,
            Description = "Hurl the blade's charge as a bolt: 8 lightning damage to any enemy (either row) that INTERRUPTS a charging enemy.",
        };
        yield return new AbilityDef
        {
            Id = "trinity_cut", Name = "Trinity Cut", Melee = true, Power = 5, Cost = 4, Cooldown = 3, Accuracy = 100,
            ElementSequence = [Element.Fire, Element.Ice, Element.Lightning],
            Description = "Three cuts: fire, ice and lightning (5 each). Shreds break meters.",
        };
    }

    /// <summary>Mastery alternative abilities (see Mastery.Alternatives): each replaces one of the hero's usual four.</summary>
    private static IEnumerable<AbilityDef> BuildMasteryAbilities()
    {
        yield return new AbilityDef
        {
            Id = "shield_wall", Name = "Shield Wall", UpText = "The wall also Taunts for 2 turns.", UpApplies = { new StatusApply(StatusKind.Taunt, 0, 2, OnSelf: true) },
            Target = TargetKind.AllAllies, Shield = 5, Cost = 3, Cooldown = 2, Vfx = Vfx.Buff,
            Applies = { new StatusApply(StatusKind.Taunt, 0, 1, OnSelf: true) },
            Description = "Mastery. Every ally gains a 5 Shield and Kael Taunts the enemies for a turn. (Replaces Intercept.)",
        };
        yield return new AbilityDef
        {
            Id = "meteor", Name = "Meteor", Target = TargetKind.AllEnemies, Element = Element.Fire, Power = 8, Cost = 5, Cooldown = 3, Vfx = Vfx.Cloud,
            Applies = { new StatusApply(StatusKind.Burn, 3, 2) },
            Description = "Mastery. A falling star: 8 fire damage and Burn 3 to every enemy. (Replaces Prismatic Surge.)",
        };
        yield return new AbilityDef
        {
            Id = "sanctuary", Name = "Sanctuary", UpText = "Also cleanses every ally.", UpCleanse = true,
            Target = TargetKind.AllAllies, Heal = 7, Cost = 3, Cooldown = 2, Vfx = Vfx.Heal,
            Description = "Mastery. The hearth-flame flares: heal every ally 7 HP right now. (Replaces Renewal.)",
        };
        yield return new AbilityDef
        {
            Id = "fan_of_knives", Name = "Fan of Knives", UpText = "+1 break pip on a weakness.", UpBreakBonus = 1,
            Target = TargetKind.AllEnemies, Power = 4, Cost = 2, Cooldown = 1, Accuracy = 95, Vfx = Vfx.Projectile,
            Applies = { new StatusApply(StatusKind.Bleed, 2, 2) },
            Description = "Mastery. Knives at everyone: 4 damage and Bleed 2 to every enemy. (Replaces Hemorrhage.)",
        };
        yield return new AbilityDef
        {
            Id = "snare_shot", Name = "Snare Shot", UpText = "Also Marks the target for 2 turns.", UpApplies = { new StatusApply(StatusKind.Marked, 0, 2) },
            Power = 6, Cost = 2, Cooldown = 2, Interrupt = true, Accuracy = 100, Vfx = Vfx.Arrow,
            Applies = { new StatusApply(StatusKind.Slowed, 0, 2) },
            Description = "Mastery. A weighted net-arrow: 6 damage, INTERRUPTS a charging enemy and Slows it. (Replaces Volley.)",
        };
        yield return new AbilityDef
        {
            Id = "blood_frenzy", Name = "Blood Frenzy", Target = TargetKind.Self, SelfDamage = 5, Cooldown = 3, Vfx = Vfx.Buff,
            Applies = { new StatusApply(StatusKind.Empowered, 0, 3), new StatusApply(StatusKind.Regen, 3, 3) },
            Description = "Mastery. Brakka bites her own lip: pay 5 HP to be Empowered for 3 turns and regenerate 3 HP a turn. (Replaces Whirlwind.)",
        };
        yield return new AbilityDef
        {
            Id = "plague_swarm", Name = "Plague Swarm", UpText = "Also Weakens every enemy for 1 turn.", UpApplies = { new StatusApply(StatusKind.Weakened, 0, 1) },
            Target = TargetKind.AllEnemies, Power = 2, Cost = 3, Cooldown = 2, Accuracy = 100, Vfx = Vfx.Cloud,
            Applies = { new StatusApply(StatusKind.Poison, 3, 3) },
            Description = "Mastery. A cloud of plague flies: Poison 3 on every enemy. (Replaces Creeping Doom.)",
        };
        yield return new AbilityDef
        {
            Id = "smoke_flask", Name = "Smoke Flask", UpText = "Weakens for 2 turns instead of 1.", UpApplies = { new StatusApply(StatusKind.Weakened, 0, 2) },
            Target = TargetKind.AllEnemies, Power = 2, Cost = 2, Cooldown = 2, Accuracy = 100, Vfx = Vfx.Cloud,
            Applies = { new StatusApply(StatusKind.Weakened, 0, 1) },
            Description = "Mastery. Choking smoke: Weakens every enemy (-25% damage) for a turn. Throw it before a big hit. (Replaces Acid Flask.)",
        };
        yield return new AbilityDef
        {
            Id = "aegis_rune", Name = "Aegis Rune", UpText = "The riposte is stronger.", UpApplies = { new StatusApply(StatusKind.Riposte, 8, 1) },
            Target = TargetKind.Self, Shield = 8, Cost = 2, Cooldown = 2, Vfx = Vfx.Buff,
            Applies = { new StatusApply(StatusKind.Riposte, 5, 1) },
            Description = "Mastery. Isolde carves a ward in the air: an 8 Shield and a 5-damage Riposte until her next turn. (Replaces Frost Edge.)",
        };
    }

    // Properties (not fields): partial-class field initializers across files run in an unspecified order.
    private static HeroDef[] HeroesPack2 =>
    [
        new()
        {
            Id = "alchemist", Name = "Corvin Vey", Role = "Alchemist", ColorHex = "#8fbf5a",
            Blurb = "A plague doctor who brews his own war. Poisons, firebombs and cures; spends leftover Reagents on a volatile finisher.",
            MaxHp = 30, Armor = 1, Speed = 5, Crit = 5, PreferredRow = Row.Back,
            Resource = new ResourceDef("Reagents", "#9be06a", 6, 3, 1),
            BasicAbility = "flask_toss", Abilities = ["acid_flask", "firebomb", "elixir", "volatile_mix"],
        },
        new()
        {
            Id = "spellblade", Name = "Isolde Vane", Role = "Spellblade", ColorHex = "#5fb8e0",
            Blurb = "A knight of the Order of the Rune. Builds Charge with her sword, then cuts with fire, ice and lightning to break anything.",
            MaxHp = 38, Armor = 2, Speed = 5, Crit = 8, PreferredRow = Row.Front,
            Resource = new ResourceDef("Charge", "#7fd8ff", 5, 1, 0, OnBasic: 1, OnCrit: 1),
            BasicAbility = "runeblade", Abilities = ["flame_edge", "frost_edge", "storm_edge", "trinity_cut"],
        },
    ];

    private static TraitDef[] TraitsPack2 =>
    [
        // ── Corvin, the Alchemist ──
        new("corvin_potency", "alchemist", "Potent Toxins", "Poison Corvin applies is 2 stronger."),
        new("corvin_medic", "alchemist", "Field Medic", "Corvin's heals are 30% stronger."),
        new("corvin_brewmaster", "alchemist", "Brewmaster", "+1 Reagent every turn."),
        new("corvin_catalyst", "alchemist", "Catalyst", "Start every battle with +2 Reagents.", StartResource: 2),
        new("corvin_apron", "alchemist", "Leather Apron", "+10 max HP and +1 armor.", MaxHp: 10, Armor: 1),
        new("corvin_quick", "alchemist", "Quick Hands", "+2 speed: act earlier in the round.", Speed: 2),

        // ── Isolde, the Spellblade ──
        new("isolde_edge", "spellblade", "Elemental Edge", "+20% damage with fire, ice and lightning."),
        new("isolde_attune", "spellblade", "Attunement", "+1 Charge every turn."),
        new("isolde_bladeward", "spellblade", "Bladeward", "Defending also grants Isolde a 6 Shield."),
        new("isolde_surge", "spellblade", "Overcharge", "Start every battle with +2 Charge.", StartResource: 2),
        new("isolde_mail", "spellblade", "Runed Mail", "+2 armor and +8 max HP.", MaxHp: 8, Armor: 2),
        new("isolde_fleet", "spellblade", "Fleet Footwork", "+10% dodge and +1 speed.", Dodge: 10, Speed: 1),
    ];
}
