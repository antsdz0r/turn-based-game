using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace TBRPG.Core;

public sealed class HeroStats
{
    public int MaxHp { get; set; }
    public int Armor { get; set; }
    public int Speed { get; set; }
    public int Crit { get; set; }
    public int Dodge { get; set; }
    public int PowerPct { get; set; }
    public int StartResource { get; set; }
    public Dictionary<Element, int> ElementPct { get; } = new();
}

/// <summary>A hero's progress during a run (serialized in the run save).</summary>
public sealed class HeroState
{
    public const int MaxLevel = 8;
    public const int HpPerLevel = 5;
    public const int PowerPerLevel = 4;

    public string Id { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int Hp { get; set; }
    public Row Row { get; set; }
    public int Slot { get; set; }
    public List<string> Upgrades { get; set; } = new();
    public string? Weapon { get; set; }
    public string? Armor { get; set; }
    public string? Trinket { get; set; }
    /// <summary>Traits picked at level-ups, and level-ups still waiting for a pick.</summary>
    public List<string> Traits { get; set; } = new();
    public int PendingTraits { get; set; }
    /// <summary>Run-wide bonuses (shrine upgrades, events, daily modifiers).</summary>
    public int BonusMaxHp { get; set; }
    public int BonusMaxHpPct { get; set; }
    public int BonusPowerPct { get; set; }
    /// <summary>Mastery costume sprite (null = the default look).</summary>
    public string? Costume { get; set; }
    /// <summary>Mastery alternative ability brought instead of the usual one it replaces (null = none).</summary>
    public string? AltAbility { get; set; }

    /// <summary>The four abilities this hero brings (the alternative ability swapped in, if chosen).</summary>
    [JsonIgnore] public IReadOnlyList<string> AbilityIds =>
        AltAbility is { } alt && Mastery.Alternatives.FirstOrDefault(a => a.AbilityId == alt) is { } swap
            ? Def.Abilities.Select(id => id == swap.Replaces ? alt : id).ToList()
            : (IReadOnlyList<string>)Def.Abilities;
    [JsonIgnore] public string Sprite => Costume ?? Id;

    [JsonIgnore] public bool HasTraitsLeft => Database.TraitsFor(Id).Count() > Traits.Count + PendingTraits;

    [JsonIgnore] public HeroDef Def => Database.Hero(Id);

    /// <summary>XP needed to reach the next level (cumulative thresholds).</summary>
    public static int XpForLevel(int level) => level switch
    {
        2 => 20,
        3 => 50,
        4 => 90,
        5 => 140,
        6 => 200,
        7 => 275,
        8 => 360,
        _ => int.MaxValue,
    };

    public IEnumerable<string> EquippedIds()
    {
        if (Weapon is not null) yield return Weapon;
        if (Armor is not null) yield return Armor;
        if (Trinket is not null) yield return Trinket;
    }

    public string? GetSlot(EquipSlot slot) => slot switch
    {
        EquipSlot.Weapon => Weapon,
        EquipSlot.Armor => Armor,
        _ => Trinket,
    };

    public void SetSlot(EquipSlot slot, string? itemId)
    {
        switch (slot)
        {
            case EquipSlot.Weapon: Weapon = itemId; break;
            case EquipSlot.Armor: Armor = itemId; break;
            default: Trinket = itemId; break;
        }
    }

    /// <summary>Base stats + levels + equipment. Used both for battles and the character screen.</summary>
    public HeroStats ComputeStats()
    {
        var def = Def;
        var stats = new HeroStats
        {
            MaxHp = def.MaxHp + (Level - 1) * HpPerLevel,
            Armor = def.Armor,
            Speed = def.Speed,
            Crit = def.Crit,
            Dodge = def.Dodge,
            PowerPct = (Level - 1) * PowerPerLevel,
        };
        foreach (var id in EquippedIds().Where(Database.HasItem))
        {
            var item = Database.Item(id);
            stats.MaxHp += item.MaxHp;
            stats.Armor += item.Armor;
            stats.Speed += item.Speed;
            stats.Crit += item.Crit;
            stats.Dodge += item.Dodge;
            stats.PowerPct += item.PowerPct;
            stats.StartResource += item.StartResource;
            if (item.BoostElement is Element element)
                stats.ElementPct[element] = stats.ElementPct.GetValueOrDefault(element) + item.BoostPct;
        }
        stats.Armor = Math.Max(0, stats.Armor);
        stats.Speed = Math.Max(1, stats.Speed);
        foreach (var id in Traits.Where(Database.HasTraitDef))
        {
            var t = Database.Trait(id);
            stats.MaxHp += t.MaxHp;
            stats.Armor += t.Armor;
            stats.Speed += t.Speed;
            stats.Crit += t.Crit;
            stats.Dodge += t.Dodge;
            stats.PowerPct += t.PowerPct;
            stats.StartResource += t.StartResource;
        }
        stats.MaxHp += BonusMaxHp;
        stats.MaxHp = (int)Math.Round(stats.MaxHp * (1 + BonusMaxHpPct / 100.0));
        stats.PowerPct += BonusPowerPct;
        stats.Armor = Math.Max(0, stats.Armor);
        stats.Speed = Math.Max(1, stats.Speed);
        stats.MaxHp = Math.Max(1, stats.MaxHp);
        return stats;
    }

    public int MaxHp => ComputeStats().MaxHp;

    public void ClampHp() => Hp = Math.Clamp(Hp, 0, MaxHp);

    /// <summary>Adds XP and returns how many levels were gained.</summary>
    public int AddXp(int amount)
    {
        int gained = 0;
        Xp += amount;
        while (Level < MaxLevel && Xp >= XpForLevel(Level + 1))
        {
            Level++;
            gained++;
            if (HasTraitsLeft) PendingTraits++;
            Hp = Math.Min(MaxHp, Hp + HpPerLevel + 5);
        }
        return gained;
    }
}
