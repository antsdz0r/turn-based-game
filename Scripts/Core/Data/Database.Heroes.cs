using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>All game content. Split into partial files: heroes, enemies, items.</summary>
public static partial class Database
{
    private static readonly Dictionary<string, AbilityDef> AbilityTable = BuildAbilities().Concat(BuildAbilitiesPack2()).Concat(BuildMasteryAbilities()).Concat(BuildDuoAbilities()).ToDictionary(a => a.Id);

    public static AbilityDef Ability(string id) => AbilityTable[id];

    private static IEnumerable<AbilityDef> BuildAbilities()
    {
        // ───── Kael Ironvow — Vanguard (Resolve) ─────
        yield return new AbilityDef
        {
            Id = "strike", Name = "Strike", IsBasic = true, Melee = true, Power = 6, Accuracy = 95,
            Description = "A heavy blow with sword and shield. +1 Resolve.",
        };
        yield return new AbilityDef
        {
            Id = "shield_bash", Name = "Shield Bash",
            UpText = "Also interrupts a charging enemy.", UpInterrupt = true, Melee = true, Power = 5, Cost = 1, Accuracy = 95,
            Requires = RowRequirement.Front,
            Applies = { new StatusApply(StatusKind.Weakened, 0, 2) },
            Description = "5 physical damage and Weakens the target (-25% damage) for 2 turns.",
        };
        yield return new AbilityDef
        {
            Id = "challenge", Name = "Challenge",
            UpText = "Also readies a 5-damage Riposte.", UpApplies = { new StatusApply(StatusKind.Riposte, 5, 1, OnSelf: true) }, Target = TargetKind.Self, Cost = 1, Shield = 8, Cooldown = 2, Vfx = Vfx.Buff,
            Applies = { new StatusApply(StatusKind.Taunt, 0, 2, OnSelf: true) },
            Description = "Taunt all enemies for 2 turns and gain an 8-point Shield. Enemy intents retarget to Kael.",
        };
        yield return new AbilityDef
        {
            Id = "intercept", Name = "Intercept",
            UpText = "Also cleanses the protected ally.", UpCleanse = true, Target = TargetKind.SingleAlly, Cost = 2, Protect = true, Vfx = Vfx.Buff,
            Applies = { new StatusApply(StatusKind.Defending, 1, 1, OnSelf: true) },
            Description = "Until Kael's next turn, attacks on the chosen ally hit Kael instead, and Kael defends (-50% damage).",
        };
        yield return new AbilityDef
        {
            Id = "riposte", Name = "Riposte Stance", Target = TargetKind.Self, Cost = 2, Cooldown = 3, Shield = 6, Vfx = Vfx.Buff,
            Applies = { new StatusApply(StatusKind.Riposte, 7, 1, OnSelf: true) },
            Description = "Gain a 6 Shield. Until Kael's next turn, every melee attacker is countered for 7 damage.",
        };

        // ───── Orrin Ashveil — Arcanist (Mana) ─────
        yield return new AbilityDef
        {
            Id = "spark", Name = "Arc Spark", IsBasic = true, Element = Element.Lightning, Power = 4, Vfx = Vfx.Projectile,
            Description = "A crackling bolt of lightning.",
        };
        yield return new AbilityDef
        {
            Id = "fireball", Name = "Fireball",
            UpText = "Also Marks the target for 2 turns.", UpApplies = { new StatusApply(StatusKind.Marked, 0, 2) }, Element = Element.Fire, Power = 8, Cost = 3, Vfx = Vfx.Projectile,
            Applies = { new StatusApply(StatusKind.Burn, 3, 2) },
            Description = "8 fire damage and Burn (3 per turn, 2 turns).",
        };
        yield return new AbilityDef
        {
            Id = "frost_lance", Name = "Frost Lance",
            UpText = "+1 break pip on a weakness.", UpBreakBonus = 1, Element = Element.Ice, Power = 6, Cost = 2, Vfx = Vfx.Projectile,
            Applies = { new StatusApply(StatusKind.Slowed, 0, 2) },
            Description = "6 ice damage and Slows the target (-3 speed) for 2 turns.",
        };
        yield return new AbilityDef
        {
            Id = "chain_lightning", Name = "Chain Lightning",
            UpText = "Can be cast from either row.", UpAnyRow = true, Target = TargetKind.AllEnemies, Element = Element.Lightning,
            Power = 5, Cost = 3, Requires = RowRequirement.Back, Vfx = Vfx.Beam,
            Description = "5 lightning damage to every enemy. Must channel from the back row.",
        };
        yield return new AbilityDef
        {
            Id = "prismatic", Name = "Prismatic Surge", Element = Element.Fire, Power = 4, Cost = 5, Cooldown = 3, Vfx = Vfx.Beam,
            ElementSequence = [Element.Fire, Element.Ice, Element.Lightning],
            Description = "Hits one enemy with fire, ice AND lightning (4 each). Perfect for shattering break meters.",
        };

        // ───── Maela of the Hearth — Warden (Faith) ─────
        yield return new AbilityDef
        {
            Id = "ember_mace", Name = "Ember Mace", IsBasic = true, Melee = true, Element = Element.Fire, Power = 4, Accuracy = 95,
            Description = "A blessed mace wreathed in embers. +1 Faith.",
        };
        yield return new AbilityDef
        {
            Id = "mend", Name = "Mend",
            UpText = "Also grants Regen 3 for 2 turns.", UpApplies = { new StatusApply(StatusKind.Regen, 3, 2) }, Target = TargetKind.SingleAlly, Heal = 12, Cleanse = true, Cost = 2, Vfx = Vfx.Heal,
            Description = "Heal an ally for 12 HP and remove their negative effects.",
        };
        yield return new AbilityDef
        {
            Id = "renewal", Name = "Renewal",
            UpText = "Also cleanses every ally.", UpCleanse = true, Target = TargetKind.AllAllies, Cost = 3, Cooldown = 2, Vfx = Vfx.Heal,
            Applies = { new StatusApply(StatusKind.Regen, 3, 3) },
            Description = "The whole party regenerates 3 HP per turn for 3 turns.",
        };
        yield return new AbilityDef
        {
            Id = "aegis", Name = "Aegis", Target = TargetKind.AllAllies, Shield = 7, Cost = 3, Cooldown = 2, Vfx = Vfx.Buff,
            Description = "Every ally gains a 7-point Shield for 2 turns.",
        };
        yield return new AbilityDef
        {
            Id = "shepherd", Name = "Shepherd",
            UpText = "The moved ally is Empowered (+25% damage) for 2 turns.", UpApplies = { new StatusApply(StatusKind.Empowered, 0, 2) }, Target = TargetKind.SingleAlly, Shield = 5, Cost = 1, SwitchTargetRow = true, Vfx = Vfx.Buff,
            Description = "Move an ally to the other row (swapping if the front is full) and give them a 5 Shield.",
        };

        // ───── Nyx Thorne — Shadowblade (Combo) ─────
        yield return new AbilityDef
        {
            Id = "quick_cut", Name = "Quick Cut", IsBasic = true, Melee = true, Power = 5, Accuracy = 100,
            Description = "A fast slash. +1 Combo (+1 more on a critical hit).",
        };
        yield return new AbilityDef
        {
            Id = "envenom", Name = "Envenomed Knife",
            UpText = "Also Weakens the target for 1 turn.", UpApplies = { new StatusApply(StatusKind.Weakened, 0, 1) }, Power = 3, Cost = 1, Accuracy = 95, Vfx = Vfx.Projectile,
            Applies = { new StatusApply(StatusKind.Poison, 4, 1) },
            Description = "Throw a knife: 3 damage and 4 Poison (stacks; deals its value each turn, then -1).",
        };
        yield return new AbilityDef
        {
            Id = "hemorrhage", Name = "Hemorrhage",
            UpText = "Also Marks the target for 2 turns.", UpApplies = { new StatusApply(StatusKind.Marked, 0, 2) }, Melee = true, Power = 5, Cost = 2, Accuracy = 95,
            Applies = { new StatusApply(StatusKind.Bleed, 3, 3) },
            Description = "5 damage and Bleed (3 per turn for 3 turns, ignores armor and shields).",
        };
        yield return new AbilityDef
        {
            Id = "sever", Name = "Sever", Power = 4, Cost = 1, Cooldown = 2, Interrupt = true, BreakBonus = 1, IgnoresArmor = true,
            Vfx = Vfx.Melee,
            Description = "Dart in from any row: 4 damage that ignores armor. Interrupts a charging attack.",
        };
        yield return new AbilityDef
        {
            Id = "eviscerate", Name = "Eviscerate", Melee = true, Power = 4, PowerPerResource = 4, MinResource = 2, CritBonus = 25,
            Accuracy = 100,
            Description = "Finisher: spends ALL Combo. 4 damage + 4 per Combo point, +25% crit chance.",
        };

        // ───── Wren Halloway — Ranger (Focus) ─────
        yield return new AbilityDef
        {
            Id = "shoot", Name = "Shoot", IsBasic = true, Power = 5, Accuracy = 95, Vfx = Vfx.Arrow,
            Description = "A precise arrow that can hit any row.",
        };
        yield return new AbilityDef
        {
            Id = "hunters_mark", Name = "Hunter's Mark",
            UpText = "Also Slows the target for 1 turn.", UpApplies = { new StatusApply(StatusKind.Slowed, 0, 1) }, Cost = 1, Power = 0, Vfx = Vfx.Debuff,
            Applies = { new StatusApply(StatusKind.Marked, 0, 3) },
            Description = "Mark an enemy for 3 turns: it takes +25% damage, and attacks against it get +20% crit and +10% accuracy.",
        };
        yield return new AbilityDef
        {
            Id = "piercing_shot", Name = "Piercing Shot",
            UpText = "Also interrupts a charging enemy.", UpInterrupt = true, Power = 9, Cost = 2, IgnoresArmor = true, Accuracy = 100, Vfx = Vfx.Arrow,
            Description = "9 damage that ignores armor.",
        };
        yield return new AbilityDef
        {
            Id = "volley", Name = "Volley",
            UpText = "Can be fired from either row.", UpAnyRow = true, Target = TargetKind.AllEnemies, Power = 4, Cost = 3, Requires = RowRequirement.Back,
            Accuracy = 90, Vfx = Vfx.Arrow,
            Description = "4 damage to every enemy. Needs room to aim: back row only.",
        };
        yield return new AbilityDef
        {
            Id = "storm_arrow", Name = "Storm Arrow",
            UpText = "+1 break pip on a weakness.", UpBreakBonus = 1, Element = Element.Lightning, Power = 6, Cost = 2, Cooldown = 2, Accuracy = 100,
            Vfx = Vfx.Arrow,
            Description = "6 lightning damage. Tumble to the other row afterwards.",
            SwitchSelfRow = true,
        };

        // ───── Brakka Bloodmane — Berserker (Rage) ─────
        yield return new AbilityDef
        {
            Id = "cleave", Name = "Cleave", IsBasic = true, Melee = true, Power = 6, Accuracy = 95,
            Description = "A heavy axe swing. Builds Rage.",
        };
        yield return new AbilityDef
        {
            Id = "reckless_swing", Name = "Reckless Swing",
            UpText = "+1 break pip on a weakness.", UpBreakBonus = 1, Melee = true, Power = 13, Cost = 1, SelfDamage = 4, Accuracy = 90,
            Description = "Throw caution away: a huge blow that also hurts Brakka.",
        };
        yield return new AbilityDef
        {
            Id = "bloodthirst", Name = "Bloodthirst",
            UpText = "Also Weakens the target for 1 turn.", UpApplies = { new StatusApply(StatusKind.Weakened, 0, 1) }, Melee = true, Power = 10, Cost = 1, LifestealPct = 50, Accuracy = 95,
            Description = "A savage bite of the axe. Heals Brakka for 50% of the damage. The safe alternative to Reckless Swing.",
        };
        yield return new AbilityDef
        {
            Id = "whirlwind", Name = "Whirlwind",
            UpText = "Ignores armor.", UpIgnoresArmor = true, Target = TargetKind.AllEnemies, Power = 9, Cost = 2, Cooldown = 2,
            Accuracy = 90, Vfx = Vfx.Melee,
            Description = "Spin through the enemy line, hitting every enemy.",
        };
        yield return new AbilityDef
        {
            Id = "undying_rage", Name = "Undying Rage", Melee = true, Power = 4, PowerPerResource = 3, MinResource = 3,
            BerserkScaling = true, Accuracy = 100, CritBonus = 10,
            Description = "Finisher: spends ALL Rage (3 power per point). Deals more the more wounded Brakka is.",
        };

        // ───── Sable Nightveil — Hexer (Hex) ─────
        yield return new AbilityDef
        {
            Id = "hexbolt", Name = "Hexbolt", IsBasic = true, Element = Element.Ice, Power = 4, Accuracy = 95, Vfx = Vfx.Projectile,
            Applies = { new StatusApply(StatusKind.Weakened, 0, 1, Chance: 35) },
            Description = "A bolt of cold malice. 35% chance to Weaken.",
        };
        yield return new AbilityDef
        {
            Id = "curse_frailty", Name = "Curse of Frailty",
            UpText = "Also Slows the target for 2 turns.", UpApplies = { new StatusApply(StatusKind.Slowed, 0, 2) }, Cost = 1, Accuracy = 100, Vfx = Vfx.Debuff,
            Applies = { new StatusApply(StatusKind.Marked, 0, 2), new StatusApply(StatusKind.Weakened, 0, 2) },
            Description = "Marks and Weakens an enemy for 2 turns.",
        };
        yield return new AbilityDef
        {
            Id = "creeping_doom", Name = "Creeping Doom",
            UpText = "Also Burns the target (2 for 2 turns).", UpApplies = { new StatusApply(StatusKind.Burn, 2, 2) }, Cost = 2, Accuracy = 100, Vfx = Vfx.Cloud,
            Applies = { new StatusApply(StatusKind.Poison, 5, 3), new StatusApply(StatusKind.Slowed, 0, 2) },
            Description = "Poison 5 and Slow an enemy.",
        };
        yield return new AbilityDef
        {
            Id = "torment", Name = "Torment",
            UpText = "Also interrupts a charging enemy.", UpInterrupt = true, Power = 3, PowerPerDebuff = 4, Cost = 2, IgnoresArmor = true, Accuracy = 100,
            Vfx = Vfx.Beam,
            Description = "3 damage, +4 for every affliction on the target. Ignores armor.",
        };
        yield return new AbilityDef
        {
            Id = "siphon_soul", Name = "Siphon Soul", Element = Element.Ice, Power = 7, Cost = 2, LifestealPct = 100,
            Accuracy = 100, Vfx = Vfx.Beam,
            Description = "Drain an enemy's soul: Sable heals for all the damage dealt.",
        };
    }

    // ═════════════════════════ Heroes ═════════════════════════

    public static readonly IReadOnlyList<HeroDef> Heroes = new List<HeroDef>
    {
        new()
        {
            Id = "vanguard", Name = "Kael Ironvow", Role = "Vanguard", ColorHex = "#7a8ca8",
            Blurb = "A sworn shield of the Last Ember. Taunts, protects allies and punishes melee attackers.",
            MaxHp = 46, Armor = 3, Speed = 4, Crit = 5, PreferredRow = Row.Front,
            Resource = new ResourceDef("Resolve", "#e0b84a", 5, 1, 0, OnBasic: 1, OnHit: 1),
            BasicAbility = "strike", Abilities = ["shield_bash", "challenge", "intercept", "riposte"],
        },
        new()
        {
            Id = "arcanist", Name = "Orrin Ashveil", Role = "Arcanist", ColorHex = "#7a56b8",
            Blurb = "An old scholar of fire, frost and storm. Exploits weaknesses and shatters break meters.",
            MaxHp = 26, Armor = 0, Speed = 5, Crit = 5, PreferredRow = Row.Back,
            Resource = new ResourceDef("Mana", "#6f9be0", 8, 4, 2),
            BasicAbility = "spark", Abilities = ["fireball", "frost_lance", "chain_lightning", "prismatic"],
        },
        new()
        {
            Id = "warden", Name = "Maela of the Hearth", Role = "Warden", ColorHex = "#c9b26b",
            Blurb = "Keeper of the hearth-flame. Heals, cleanses, shields and repositions the party.",
            MaxHp = 32, Armor = 1, Speed = 3, Crit = 5, PreferredRow = Row.Back,
            Resource = new ResourceDef("Faith", "#ffe27a", 6, 3, 1, OnBasic: 1),
            BasicAbility = "ember_mace", Abilities = ["mend", "renewal", "aegis", "shepherd"],
        },
        new()
        {
            Id = "shadowblade", Name = "Nyx Thorne", Role = "Shadowblade", ColorHex = "#3fa39a",
            Blurb = "A fast killer. Builds Combo with quick cuts, poisons and bleeds, then finishes with Eviscerate.",
            MaxHp = 28, Armor = 1, Speed = 8, Crit = 20, Dodge = 15, PreferredRow = Row.Front,
            Resource = new ResourceDef("Combo", "#ff6b9a", 5, 0, 0, OnBasic: 1, OnCrit: 1),
            BasicAbility = "quick_cut", Abilities = ["envenom", "hemorrhage", "sever", "eviscerate"],
        },
        new()
        {
            Id = "ranger", Name = "Wren Halloway", Role = "Ranger", ColorHex = "#6f9a4a",
            Blurb = "A patient archer. Marks prey for the whole party and rains arrows on every row.",
            MaxHp = 27, Armor = 1, Speed = 6, Crit = 10, Dodge = 5, PreferredRow = Row.Back,
            Resource = new ResourceDef("Focus", "#7fd4a0", 5, 2, 1),
            BasicAbility = "shoot", Abilities = ["hunters_mark", "piercing_shot", "volley", "storm_arrow"],
        },
        new()
        {
            Id = "berserker", Name = "Brakka Bloodmane", Role = "Berserker", ColorHex = "#c2562b",
            Blurb = "A northern axe-wielder who fights harder the closer she is to death. Trades HP for power.",
            MaxHp = 44, Armor = 1, Speed = 5, Crit = 8, PreferredRow = Row.Front,
            Resource = new ResourceDef("Rage", "#ff6a4a", 6, 0, 0, OnBasic: 1, OnHit: 1, OnCrit: 1),
            BasicAbility = "cleave", Abilities = ["reckless_swing", "bloodthirst", "whirlwind", "undying_rage"],
        },
        new()
        {
            Id = "hexer", Name = "Sable Nightveil", Role = "Hexer", ColorHex = "#a8406a",
            Blurb = "A veiled witch. Stacks curses on her prey, then tears them apart with Torment.",
            MaxHp = 29, Armor = 0, Speed = 6, Crit = 5, Dodge = 5, PreferredRow = Row.Back,
            Resource = new ResourceDef("Hex", "#c070ff", 5, 2, 1),
            BasicAbility = "hexbolt", Abilities = ["curse_frailty", "creeping_doom", "torment", "siphon_soul"],
        },
    }.Concat(HeroesPack2).ToList();

    public static HeroDef Hero(string id) => Heroes.FirstOrDefault(h => h.Id == id) ?? Heroes[0];

    /// <summary>Builds a battle combatant from a hero's saved run state.</summary>
    public static Combatant CreateHero(HeroState state)
    {
        var def = Hero(state.Id);
        var unit = new Combatant
        {
            DefId = def.Id,
            Name = def.Name,
            Sprite = state.Sprite,
            IsHero = true,
            Hero = def,
            ResourceDef = def.Resource,
        };
        var stats = state.ComputeStats();
        unit.MaxHp = stats.MaxHp;
        unit.Hp = state.Hp;
        unit.Armor = stats.Armor;
        unit.BaseSpeed = stats.Speed;
        unit.Crit = stats.Crit;
        unit.Dodge = stats.Dodge;
        unit.PowerPct = stats.PowerPct;
        foreach (var (element, pct) in stats.ElementPct) unit.ElementPct[element] = pct;
        unit.Row = state.Row;
        unit.Slot = state.Slot;
        unit.MaxResource = def.Resource.Max;
        unit.Resource = System.Math.Min(def.Resource.Max, def.Resource.Start + stats.StartResource);
        unit.Basic = new AbilityInstance(Ability(def.BasicAbility), state.Upgrades.Contains(def.BasicAbility));
        foreach (var id in state.AbilityIds)
            unit.Abilities.Add(new AbilityInstance(Ability(id), state.Upgrades.Contains(id)));
        foreach (var id in state.Traits) unit.Traits.Add(id);
        if (state.Weapon is { } weapon && HasItem(weapon)) unit.WeaponEffect = Item(weapon).WeaponEffect;
        return unit;
    }
}
