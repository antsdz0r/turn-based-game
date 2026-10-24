using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public static partial class Database
{
    private static readonly Dictionary<string, ItemDef> ItemTable = BuildItems().ToDictionary(i => i.Id);
    private static readonly Dictionary<string, RelicDef> RelicTable = BuildRelics().ToDictionary(r => r.Id);

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
    }

    public static ItemDef RandomEquipment(Random rng, int maxTier)
    {
        var pool = AllItems.Where(i => i.IsEquipment && i.Tier <= maxTier).ToList();
        return pool[rng.Next(pool.Count)];
    }

    public static ItemDef RandomConsumable(Random rng)
    {
        var pool = AllItems.Where(i => !i.IsEquipment).ToList();
        return pool[rng.Next(pool.Count)];
    }

    /// <summary>A relic the party doesn't own yet, or null if they own them all.</summary>
    public static RelicDef? RandomRelic(Random rng, ICollection<string> owned)
    {
        var pool = AllRelics.Where(r => !r.Special && !owned.Contains(r.Id)).ToList();
        return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
    }
}
