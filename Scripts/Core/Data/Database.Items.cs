using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public static partial class Database
{
    private static readonly Dictionary<string, ItemDef> ItemTable = BuildItems().ToDictionary(i => i.Id);
    private static readonly Dictionary<string, RelicDef> RelicTable = BuildRelics().Concat(BuildPactRelics()).ToDictionary(r => r.Id);

    private static IEnumerable<RelicDef> BuildPactRelics()
    {
        yield return new RelicDef { Id = "bloodstone_crown", Name = "Bloodstone Crown", ColorHex = "#ff4a4a", Special = true, Pact = true,
            Description = "Heroes deal +30% damage.", Curse = "Rest sites no longer heal." };
        yield return new RelicDef { Id = "glass_heart", Name = "Glass Heart", ColorHex = "#ff8ab0", Special = true, Pact = true,
            Description = "Heroes have +25% crit chance.", Curse = "Heroes take 20% more damage." };
        yield return new RelicDef { Id = "greed_idol", Name = "Idol of Greed", ColorHex = "#ffd700", Special = true, Pact = true,
            Description = "Battles pay +60% gold.", Curse = "Shop prices are 30% higher." };
        yield return new RelicDef { Id = "hollow_lantern", Name = "Hollow Lantern", ColorHex = "#c070ff", Special = true, Pact = true,
            Description = "Abilities cost 1 less.", Curse = "Heroes start every battle with at most 80% of their max HP." };
        yield return new RelicDef { Id = "doom_clock", Name = "Doom Clock", ColorHex = "#d8d6e6", Special = true, Pact = true,
            Description = "Heroes deal +40% damage.", Curse = "From round 7 on, every hero takes 4 damage at the start of each round." };
        yield return new RelicDef { Id = "thorn_pact", Name = "Pact of Thorns", ColorHex = "#6f9a40", Special = true, Pact = true,
            Description = "Heroes always counter melee attacks for 5 damage.", Curse = "Heroes have 2 less armor." };
    }

    public static IEnumerable<RelicDef> PactRelics => AllRelics.Where(r => r.Pact);
    /// <summary>Description with the curse, for tooltips and reward cards.</summary>
    public static string RelicText(RelicDef r) => r.Curse is null ? r.Description : $"{r.Description} [color=#ff6b6b]Curse: {r.Curse}[/color]";

    public static ItemDef Item(string id) => ItemTable[id];
    public static bool HasItem(string id) => ItemTable.ContainsKey(id);
    public static IEnumerable<ItemDef> AllItems => ItemTable.Values;
    public static RelicDef Relic(string id) => RelicTable[id];
    public static bool HasRelicDef(string id) => RelicTable.ContainsKey(id);
    public static IEnumerable<RelicDef> AllRelics => RelicTable.Values;

    private static IEnumerable<ItemDef> BuildItems()
    {
        // Weapons
        yield return new ItemDef { Id = "iron_edge", Name = "Iron Edge", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 1, Price = 45,
            PowerPct = 10, Description = "+10% damage." };
        yield return new ItemDef { Id = "ember_brand", Name = "Ember Brand", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 1, Price = 50,
            BoostElement = Element.Fire, BoostPct = 25, Description = "+25% fire damage." };
        yield return new ItemDef { Id = "frostbite_wand", Name = "Frostbite Wand", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 1, Price = 50,
            BoostElement = Element.Ice, BoostPct = 25, Description = "+25% ice damage." };
        yield return new ItemDef { Id = "stormstring", Name = "Stormstring", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 1, Price = 50,
            BoostElement = Element.Lightning, BoostPct = 25, Description = "+25% lightning damage." };
        yield return new ItemDef { Id = "duelist_saber", Name = "Duelist's Saber", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 2, Price = 75,
            Crit = 10, PowerPct = 5, Description = "+10% crit chance, +5% damage." };
        yield return new ItemDef { Id = "kingsbane", Name = "Kingsbane", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 2, Price = 95,
            PowerPct = 20, Description = "+20% damage." };
        yield return new ItemDef { Id = "abyssal_trident", Name = "Abyssal Trident", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 3, Price = 120,
            PowerPct = 15, BoostElement = Element.Ice, BoostPct = 25, Description = "+15% damage, +25% ice damage." };
        yield return new ItemDef { Id = "emberheart_blade", Name = "Emberheart Blade", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 3, Price = 130,
            PowerPct = 28, Crit = 5, Description = "+28% damage, +5% crit chance." };
        yield return new ItemDef { Id = "tempest_bow", Name = "Tempest Bow", Kind = ItemKind.Weapon, Icon = "weapon", Tier = 3, Price = 120,
            BoostElement = Element.Lightning, BoostPct = 40, Speed = 1, Description = "+40% lightning damage, +1 speed." };

        // ── Weapon families: each has a mechanic AND a drawback ──
        yield return new ItemDef { Id = "embercleaver", Name = "Embercleaver", Family = "Greatblade", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 1, Price = 70, Speed = -1, WeaponEffect = "guardbreaker",
            Description = "Greatblade. Hits on a Shielded or Defending enemy chip 2 extra break pips. Drawback: -1 speed." };
        yield return new ItemDef { Id = "oathbreaker", Name = "Oathbreaker", Family = "Greatblade", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 95, Speed = -2, PowerPct = 10, WeaponEffect = "execution",
            Description = "Greatblade. +10% damage, and +50% more against BROKEN enemies. Drawback: -2 speed." };
        yield return new ItemDef { Id = "ashwood_longbow", Name = "Ashwood Longbow", Family = "Bow", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 1, Price = 70, WeaponEffect = "hunter",
            Description = "Bow. The first hit each battle Marks the target. +30% damage against Marked enemies. Drawback: -10% against unmarked ones." };
        yield return new ItemDef { Id = "windpiercer", Name = "Windpiercer", Family = "Bow", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 90, WeaponEffect = "piercing",
            Description = "Bow. Attacks never miss and ignore armor. Drawback: -15% damage." };
        yield return new ItemDef { Id = "gutterfangs", Name = "Gutterfangs", Family = "Daggers", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 1, Price = 65, WeaponEffect = "twin",
            Description = "Twin daggers. The basic attack strikes twice at 60% power (two chances to crit and chip break). Drawback: armor blocks each hit." };
        yield return new ItemDef { Id = "last_whisper", Name = "Last Whisper", Family = "Daggers", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 85, WeaponEffect = "blood_price",
            Description = "Ritual dagger. Abilities cost 1 less resource; every kill restores 1. Drawback: using an ability costs 2 HP." };
        yield return new ItemDef { Id = "wardens_pike", Name = "Warden's Pike", Family = "Spear", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 1, Price = 70, PowerPct = -10, WeaponEffect = "reach",
            Description = "Spear. Melee attacks reach the back row. +30% damage and +1 break pip against CHARGING enemies. Drawback: -10% damage." };
        yield return new ItemDef { Id = "oathkeeper", Name = "Oathkeeper", Family = "Shield", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 85, PowerPct = -15, Armor = 1, WeaponEffect = "oathkeeper",
            Description = "Sword and shield. Defending or protecting an ally readies a Riposte (6); ripostes chip 1 break pip. +1 armor. Drawback: -15% damage." };
        yield return new ItemDef { Id = "gravemason", Name = "Gravemason", Family = "Hammer", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 90, Speed = -2, PowerPct = 5, WeaponEffect = "crush",
            Description = "Hammer. Hits smash an enemy's Shield before dealing damage and sunder 1 armor for the rest of the battle. +5% damage. Drawback: -2 speed." };
        yield return new ItemDef { Id = "cinder_tome", Name = "Cinder Tome", Family = "Tome", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 1, Price = 65, WeaponEffect = "cinder",
            Description = "Tome. Fire hits set the target Burning (2 for 2 turns); fire hits on an already Burning enemy chip 1 break pip. Drawback: -15% non-fire damage." };
        yield return new ItemDef { Id = "stormglass_focus", Name = "Stormglass Focus", Family = "Focus", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 90, WeaponEffect = "stormglass",
            Description = "Focus. Every 3rd fire, ice or lightning hit arcs to another enemy for half damage. Drawback: -20% physical damage." };
        yield return new ItemDef { Id = "gravewheat_scythe", Name = "Gravewheat Scythe", Family = "Scythe", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 90, Armor = -1, WeaponEffect = "reap",
            Description = "Scythe. The basic attack sweeps on to a second enemy for half damage. Drawback: -1 armor." };
        yield return new ItemDef { Id = "penitents_flail", Name = "Penitent's Flail", Family = "Flail", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 1, Price = 70, Crit = 8, WeaponEffect = "flail",
            Description = "Flail. +8% crit, and critical hits chip 2 extra break pips. Drawback: -10% accuracy." };
        yield return new ItemDef { Id = "vigil_lantern", Name = "Vigil Lantern", Family = "Lantern", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 90, PowerPct = -10, WeaponEffect = "lantern",
            Description = "Lantern. Damage from abilities mends the most wounded ally for 20% of it. Drawback: -10% damage." };
        yield return new ItemDef { Id = "rimefang_rapier", Name = "Rimefang Rapier", Family = "Rapier", Kind = ItemKind.Weapon, Icon = "weapon",
            Tier = 2, Price = 95, Crit = 25, MaxHp = -6, WeaponEffect = "rime_crit",
            Description = "Rapier. +25% crit, and critical hits Slow the target. Drawback: -6 max HP." };

        // Armor
        yield return new ItemDef { Id = "padded_coat", Name = "Padded Coat", Kind = ItemKind.Armor, Icon = "armor", Tier = 1, Price = 35,
            MaxHp = 6, Description = "+6 max HP." };
        yield return new ItemDef { Id = "chain_hauberk", Name = "Chain Hauberk", Kind = ItemKind.Armor, Icon = "armor", Tier = 1, Price = 50,
            Armor = 2, Speed = -1, Description = "+2 armor, -1 speed." };
        yield return new ItemDef { Id = "warden_plate", Name = "Hearthguard Plate", Kind = ItemKind.Armor, Icon = "armor", Tier = 2, Price = 90,
            Armor = 3, MaxHp = 8, Description = "+3 armor, +8 max HP." };
        yield return new ItemDef { Id = "shadow_leathers", Name = "Shadow Leathers", Kind = ItemKind.Armor, Icon = "armor", Tier = 2, Price = 70,
            Dodge = 10, Speed = 1, Description = "+10% dodge, +1 speed." };
        yield return new ItemDef { Id = "coral_mail", Name = "Coral Mail", Kind = ItemKind.Armor, Icon = "armor", Tier = 3, Price = 120,
            Armor = 4, MaxHp = 14, Speed = -1, Description = "+4 armor, +14 max HP, -1 speed." };
        yield return new ItemDef { Id = "cinderweave", Name = "Cinderweave Robe", Kind = ItemKind.Armor, Icon = "armor", Tier = 3, Price = 110,
            MaxHp = 10, Dodge = 8, StartResource = 2, Description = "+10 max HP, +8% dodge, +2 starting resource." };

        // Trinkets
        yield return new ItemDef { Id = "swift_charm", Name = "Swift Charm", Kind = ItemKind.Trinket, Icon = "trinket", Tier = 1, Price = 50,
            Speed = 2, Description = "+2 speed: act earlier in the round." };
        yield return new ItemDef { Id = "focus_crystal", Name = "Focus Crystal", Kind = ItemKind.Trinket, Icon = "trinket", Tier = 1, Price = 45,
            StartResource = 2, Description = "Start every battle with +2 of your resource." };
        yield return new ItemDef { Id = "eagle_eye", Name = "Eagle Eye", Kind = ItemKind.Trinket, Icon = "trinket", Tier = 2, Price = 70,
            Crit = 12, Description = "+12% crit chance." };
        yield return new ItemDef { Id = "heart_of_oak", Name = "Heart of Oak", Kind = ItemKind.Trinket, Icon = "trinket", Tier = 2, Price = 70,
            MaxHp = 12, Description = "+12 max HP." };
        yield return new ItemDef { Id = "pearl_of_tides", Name = "Pearl of the Tides", Kind = ItemKind.Trinket, Icon = "trinket", Tier = 3, Price = 115,
            MaxHp = 8, Speed = 2, Crit = 6, Description = "+8 max HP, +2 speed, +6% crit chance." };

        // Consumables
        yield return new ItemDef { Id = "potion", Name = "Health Draught", Kind = ItemKind.Consumable, Icon = "potion", Price = 25,
            HealAmount = 18, Description = "Heal an ally for 18 HP." };
        yield return new ItemDef { Id = "tonic", Name = "Ember Tonic", Kind = ItemKind.Consumable, Icon = "tonic", Price = 25,
            ResourceAmount = 3, Description = "An ally gains 3 of their resource." };
        yield return new ItemDef { Id = "panacea", Name = "Panacea", Kind = ItemKind.Consumable, Icon = "panacea", Price = 30,
            CleanseAll = true, RegenAmount = 4, Description = "Remove all negative effects from an ally and Regen 4 for 3 turns." };
        yield return new ItemDef { Id = "firebomb", Name = "Firebomb", Kind = ItemKind.Consumable, Icon = "bomb", Price = 35,
            DamageAll = 8, DamageElement = Element.Fire, Description = "8 fire damage to every enemy." };
        yield return new ItemDef { Id = "frost_flask", Name = "Frost Flask", Kind = ItemKind.Consumable, Icon = "flask", Price = 35,
            DamageAll = 7, DamageElement = Element.Ice, Description = "7 ice damage to every enemy." };
        yield return new ItemDef { Id = "phoenix_feather", Name = "Phoenix Feather", Kind = ItemKind.Consumable, Icon = "feather", Price = 60,
            ReviveHpPct = 40, Description = "Revive a fallen ally with 40% HP." };
        yield return new ItemDef { Id = "shock_dart", Name = "Shock Dart", Kind = ItemKind.Consumable, Icon = "bomb", Price = 40,
            Interrupts = true, DamageElement = Element.Lightning,
            Description = "Throw at an enemy: 4 lightning damage and INTERRUPTS its charged attack. Bosses can't be interrupted; they lose 2 break pips instead." };
        yield return new ItemDef { Id = "ward_flask", Name = "Warding Flask", Kind = ItemKind.Consumable, Icon = "flask", Price = 35,
            ShieldAll = 10, Description = "Every hero gains a 10 Shield. Throw it before a big charged attack lands." };
        yield return new ItemDef { Id = "thunder_flask", Name = "Thunder Flask", Kind = ItemKind.Consumable, Icon = "bomb", Price = 40,
            DamageAll = 5, DamageElement = Element.Lightning, BreakAll = 1,
            Description = "5 lightning damage to every enemy and chips 1 break pip from each, weakness or not." };
    }

    private static IEnumerable<RelicDef> BuildRelics()
    {
        yield return new RelicDef { Id = "childs_lantern", Name = "The Child's Lantern", ColorHex = "#b8f5e4", Special = true,
            Description = "A gift from a grateful ghost. At the start of every battle, every hero heals 10% of max HP." };
        // ── Game-changers ──
        yield return new RelicDef { Id = "duelists_coin", Name = "Duelist's Coin", ColorHex = "#ffe09a", Price = 140,
            Description = "Each hero's first attack in every battle is a guaranteed critical hit." };
        yield return new RelicDef { Id = "alchemist_kit", Name = "Alchemist's Kit", ColorHex = "#9ef0d6", Price = 130,
            Description = "The first consumable you use in each battle is not used up." };
        yield return new RelicDef { Id = "twin_flame", Name = "Twin Flame", ColorHex = "#ff8a2c", Price = 150,
            Description = "Poison, Burn and Bleed on enemies deal double damage." };
        yield return new RelicDef { Id = "berserker_torc", Name = "Berserker's Torc", ColorHex = "#d85f2a", Price = 130,
            Description = "Heroes below 50% HP deal +40% damage." };
        yield return new RelicDef { Id = "phoenix_feather", Name = "Phoenix Feather", ColorHex = "#ffb85a", Price = 160,
            Description = "Once per battle, the first hero to fall rises again with 30% HP." };
        yield return new RelicDef { Id = "headsman_seal", Name = "Headsman's Seal", ColorHex = "#ff9b9b", Price = 140,
            Description = "Breaking an enemy also tears away 15% of its max HP." };
        yield return new RelicDef { Id = "storm_crown", Name = "Storm Crown", ColorHex = "#d9b8ff", Price = 150,
            Description = "Every third round, lightning strikes every enemy for 6 damage." };
        yield return new RelicDef { Id = "cinder_idol", Name = "Cinder Idol", ColorHex = "#ff7a3d",
            Description = "Heroes deal +25% fire damage." };
        yield return new RelicDef { Id = "menders_thread", Name = "Mender's Thread", ColorHex = "#6be07a",
            Description = "Healing on heroes is 30% stronger." };
        yield return new RelicDef { Id = "shattered_aegis", Name = "Shattered Aegis", ColorHex = "#8fb3ff",
            Description = "Whenever you BREAK an enemy, every hero gains a 5 Shield." };
        yield return new RelicDef { Id = "hungry_blade", Name = "Hungry Blade", ColorHex = "#ff6b9a",
            Description = "A hero who lands a killing blow gains 2 of their resource." };
        yield return new RelicDef { Id = "hunters_lens", Name = "Hunter's Lens", ColorHex = "#7fd4a0",
            Description = "+25% crit chance against Marked enemies." };
        yield return new RelicDef { Id = "ember_heart", Name = "Ember Heart", ColorHex = "#ffb347",
            Description = "Every hero starts each battle with Regen 2 for 3 turns." };
        yield return new RelicDef { Id = "storm_battery", Name = "Storm Battery", ColorHex = "#c9a0ff",
            Description = "Lightning hits on a weakness deal +1 break damage." };
        yield return new RelicDef { Id = "gilded_purse", Name = "Gilded Purse", ColorHex = "#ffd76b",
            Description = "+30% gold from battles." };
        yield return new RelicDef { Id = "warding_bell", Name = "Warding Bell", ColorHex = "#bfeaff",
            Description = "Every hero starts each battle with a 6 Shield." };
        yield return new RelicDef { Id = "bloodpact_ring", Name = "Bloodpact Ring", ColorHex = "#d0303a",
            Description = "A hero who lands a killing blow heals 4 HP." };
        yield return new RelicDef { Id = "frostbound_locket", Name = "Frostbound Locket", ColorHex = "#7fd4ff",
            Description = "Heroes' ice hits Slow the target for 1 turn." };
        yield return new RelicDef { Id = "venom_gland", Name = "Venom Gland", ColorHex = "#8fd14a",
            Description = "Poison applied by heroes is 2 stronger." };
        yield return new RelicDef { Id = "scholars_lamp", Name = "Scholar's Lamp", ColorHex = "#fff2a8",
            Description = "+40% experience from battles." };
        // ── Combo relics: they reward setting something up first ──
        yield return new RelicDef { Id = "static_sigil", Name = "Static Sigil", ColorHex = "#c9a0ff",
            Description = "The first lightning hit on each enemy every battle chips 1 break pip, even without a weakness." };
        yield return new RelicDef { Id = "shatterglass", Name = "Shatterglass Charm", ColorHex = "#bfeaff",
            Description = "Physical hits on a Slowed enemy chip 1 break pip. Chill it, then shatter it." };
        yield return new RelicDef { Id = "sentinels_oath", Name = "Sentinel's Oath", ColorHex = "#8fb3ff",
            Description = "Whenever a hero Defends, the most wounded other hero gains a 6 Shield." };
        yield return new RelicDef { Id = "hunters_tally", Name = "Hunter's Tally", ColorHex = "#7fd4a0",
            Description = "Killing a Marked enemy restores 2 resource and 4 HP to the killer." };
    }

    /// <summary>Items still locked behind feats (set from the profile at startup; empty in tests). Never offered in runs.</summary>
    public static HashSet<string> LockedItems { get; set; } = new();
    /// <summary>Items that can show up in rewards, shops and events.</summary>
    public static IEnumerable<ItemDef> OfferedItems => AllItems.Where(i => !LockedItems.Contains(i.Id));

    public static ItemDef RandomEquipment(Random rng, int maxTier)
    {
        var pool = OfferedItems.Where(i => i.IsEquipment && i.Tier <= maxTier).ToList();
        return pool[rng.Next(pool.Count)];
    }

    public static ItemDef RandomConsumable(Random rng)
    {
        var pool = OfferedItems.Where(i => !i.IsEquipment).ToList();
        return pool[rng.Next(pool.Count)];
    }

    /// <summary>A relic the party doesn't own yet, or null if they own them all.</summary>
    /// <summary>Every weapon mechanic the battle rules implement (checked by Database.Validate).</summary>
    public static readonly HashSet<string> WeaponEffects = new()
        { "guardbreaker", "execution", "hunter", "piercing", "twin", "blood_price", "reach", "oathkeeper", "stormglass", "crush", "cinder",
          "reap", "flail", "lantern", "rime_crit" };

    /// <summary>Short in-battle description of each weapon mechanic (hero tooltip).</summary>
    public static string WeaponEffectText(string? effect) => effect switch
    {
        "guardbreaker" => "Embercleaver: +2 break pips against Shielded or Defending enemies.",
        "execution" => "Oathbreaker: +50% damage against BROKEN enemies.",
        "hunter" => "Ashwood Longbow: first hit Marks; +30% vs Marked, -10% otherwise.",
        "piercing" => "Windpiercer: never misses, ignores armor, -15% damage.",
        "twin" => "Gutterfangs: basic attack strikes twice at 60%.",
        "blood_price" => "Last Whisper: abilities cost 1 less and 2 HP; kills restore 1.",
        "reach" => "Warden's Pike: melee reaches the back row; +30% and +1 break vs charging enemies.",
        "oathkeeper" => "Oathkeeper: Defend / Protect readies a Riposte that chips break.",
        "stormglass" => "Stormglass Focus: every 3rd elemental hit arcs to another enemy.",
        "crush" => "Gravemason: hits smash Shields and sunder 1 armor for the battle.",
        "cinder" => "Cinder Tome: fire hits Burn; fire on a Burning enemy chips 1 break pip.",
        "reap" => "Gravewheat Scythe: the basic attack also hits a second enemy for half.",
        "flail" => "Penitent's Flail: crits chip 2 break pips; -10% accuracy.",
        "lantern" => "Vigil Lantern: ability damage heals the most wounded ally 20%.",
        "rime_crit" => "Rimefang Rapier: +25% crit; crits Slow.",
        _ => "",
    };

    /// <summary>A signature weapon for boss rewards (a weapon family, Rare or better if possible).</summary>
    public static ItemDef? RandomFamilyWeapon(Random rng, int maxTier)
    {
        var pool = OfferedItems.Where(i => i.WeaponEffect is not null && i.Tier <= Math.Max(2, maxTier)).ToList();
        return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
    }

    public static RelicDef? RandomRelic(Random rng, ICollection<string> owned)
    {
        var pool = AllRelics.Where(r => !r.Special && !owned.Contains(r.Id)).ToList();
        return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
    }
}
