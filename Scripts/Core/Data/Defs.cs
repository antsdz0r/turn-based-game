using System.Collections.Generic;

namespace TBRPG.Core;

/// <summary>A hero's unique resource (Resolve, Mana, Faith, Combo, Focus).</summary>
public sealed record ResourceDef(string Name, string ColorHex, int Max, int Start, int RegenPerTurn,
                                 int OnBasic = 0, int OnHit = 0, int OnCrit = 0);

public sealed class HeroDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public required string Blurb { get; init; }
    public required string ColorHex { get; init; }
    public int MaxHp { get; init; }
    public int Armor { get; init; }
    public int Speed { get; init; }
    public int Crit { get; init; } = 5;
    public int Dodge { get; init; }
    public Row PreferredRow { get; init; } = Row.Front;
    public required ResourceDef Resource { get; init; }
    public required string BasicAbility { get; init; }
    public required string[] Abilities { get; init; }
}

public enum MoveKind { Attack, Heal, Shield, Empower, Summon, Defend, Consume, Usurp }

/// <summary>Who an enemy picks for a single-hero attack.</summary>
public enum HeroPick { Random, LowestHp, Backline, Frontline, HighestHp, Marked }

/// <summary>FrontOnly: hits only heroes standing in the front row (nobody, if the row is empty).</summary>
/// <summary>Branded: every hero carrying a Mark (the Storm Tyrant's Thunderstrike in phase 2).
/// BackOnly: only heroes in the back row (nobody, if it's empty).</summary>
public enum MoveTarget { Hero, FrontRow, BackRow, AllHeroes, Self, Ally, AllAllies, FrontOnly, Branded, BackOnly }

/// <summary>An enemy action. "Charged" moves spend one round charging (shown as an intent) before they fire.</summary>
public sealed class EnemyMoveDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public MoveKind Kind { get; init; } = MoveKind.Attack;
    public MoveTarget Target { get; init; } = MoveTarget.Hero;
    public HeroPick Pick { get; init; } = HeroPick.Random;
    public Element Element { get; init; } = Element.Physical;
    public int Power { get; init; }
    public int Hits { get; init; } = 1;
    public bool Melee { get; init; }
    public int Accuracy { get; init; } = 95;
    public List<StatusApply> Applies { get; init; } = new();
    public string? SummonId { get; init; }
    public bool Charged { get; init; }
    public Vfx Vfx { get; init; } = Vfx.Melee;

    public bool HitsHeroes => Target is MoveTarget.Hero or MoveTarget.FrontRow or MoveTarget.BackRow or MoveTarget.AllHeroes or MoveTarget.FrontOnly or MoveTarget.Branded or MoveTarget.BackOnly;
}

public sealed class EnemyDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Sprite { get; init; }
    public string Tint { get; init; } = "#ffffff";
    public float Scale { get; init; } = 1f;
    /// <summary>True when the enemy reuses right-facing (hero) art.</summary>
    public bool FlipSprite { get; init; }
    public required string Blurb { get; init; }
    public int MaxHp { get; init; }
    public int Armor { get; init; }
    public int Speed { get; init; }
    public int Dodge { get; init; }
    public int Crit { get; init; } = 5;
    public Row Row { get; init; } = Row.Front;
    public int BreakMax { get; init; } = 2;
    public Dictionary<Element, Affinity> Affinities { get; init; } = new();
    public required List<EnemyMoveDef> Moves { get; init; }
    public bool IsBoss { get; init; }
    public bool IsElite { get; init; }
    /// <summary>Phase 2 (below 50% HP) replaces affinities, break meter and moves.</summary>
    public Dictionary<Element, Affinity>? Phase2Affinities { get; init; }
    public int Phase2BreakMax { get; init; }
    /// <summary>Armor in phase 2 (null = unchanged).</summary>
    public int? Phase2Armor { get; init; }
    /// <summary>Boss mechanic: every round a random element is exposed (double damage, +1 break chip).</summary>
    public bool ShiftingWeakPoint { get; init; }
    /// <summary>Boss mechanic: takes 60% less damage while an ally with this id is alive (the Lich's phylactery).</summary>
    public string? ProtectedBy { get; init; }
    /// <summary>Boss mechanic: in phase 2 a random element is exposed every round (like the Colossus).</summary>
    public bool Phase2ShiftingWeakPoint { get; init; }
    public List<EnemyMoveDef>? Phase2Moves { get; init; }
    public string? Phase2Name { get; init; }
    public int Gold { get; init; } = 10;
    /// <summary>Boss title card: a subtitle under the name and a one-line hint at its signature mechanic.</summary>
    public string? Epithet { get; init; }
    public string? MechanicHint { get; init; }
    /// <summary>Boss mechanic: heals this % of max HP at the start of every round for each living ally with this id (the Mother Bloom's seed pods).</summary>
    public string? RegrowFrom { get; init; }
    public int RegrowPct { get; init; } = 4;
    /// <summary>Boss mechanic: the element that hit it last deals half damage (the Archivist reads your spells).</summary>
    public bool AdaptiveResist { get; init; }
}

/// <summary>Equipment and consumables. Every stat here is read by the battle rules.</summary>
public sealed class ItemDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public ItemKind Kind { get; init; }
    public required string Icon { get; init; }
    public int Price { get; init; } = 40;
    public int Tier { get; init; } = 1;

    // Equipment stats
    public int MaxHp { get; init; }
    public int Armor { get; init; }
    public int Speed { get; init; }
    public int Crit { get; init; }
    public int Dodge { get; init; }
    public int PowerPct { get; init; }
    public Element? BoostElement { get; init; }
    public int BoostPct { get; init; }
    public int StartResource { get; init; }
    /// <summary>Signature mechanic of a weapon family (see Database.WeaponEffects), or null for plain stat weapons.</summary>
    public string? WeaponEffect { get; init; }
    /// <summary>Weapon family shown in the UI (Greatblade, Bow, Daggers...).</summary>
    public string? Family { get; init; }

    // Consumable effects
    public int HealAmount { get; init; }
    public int ResourceAmount { get; init; }
    public bool CleanseAll { get; init; }
    public int RegenAmount { get; init; }
    public int DamageAll { get; init; }
    public Element DamageElement { get; init; } = Element.Fire;
    public int ReviveHpPct { get; init; }
    /// <summary>Thrown at an enemy: interrupts a charged attack (bosses lose break pips instead).</summary>
    public bool Interrupts { get; init; }
    /// <summary>Shield given to every hero (Warding Flask).</summary>
    public int ShieldAll { get; init; }
    /// <summary>Break pips chipped from every enemy (Thunder Flask).</summary>
    public int BreakAll { get; init; }

    /// <summary>Common / Rare / Legendary, from the item tier.</summary>
    public string Rarity => Tier switch { <= 1 => "Common", 2 => "Rare", _ => "Legendary" };

    public bool IsEquipment => Kind != ItemKind.Consumable;
    public EquipSlot Slot => Kind switch
    {
        ItemKind.Armor => EquipSlot.Armor,
        ItemKind.Trinket => EquipSlot.Trinket,
        _ => EquipSlot.Weapon,
    };

    /// <summary>Consumables that are used on an ally (heal, tonic, cleanse, revive).</summary>
    public bool TargetsAlly => Kind == ItemKind.Consumable && DamageAll == 0 && ShieldAll == 0 && !Interrupts;
    public bool TargetsEnemy => Kind == ItemKind.Consumable && Interrupts;
    public bool NeedsTarget => TargetsAlly || TargetsEnemy;
}

public sealed class RelicDef
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string ColorHex { get; init; }
    public int Price { get; init; } = 120;
    /// <summary>Story relics: never offered randomly (shops, chests, rewards).</summary>
    public bool Special { get; init; }
    /// <summary>Pact relics: a big blessing paired with a real curse. Only offered by the Shrine of Pacts.</summary>
    public bool Pact { get; init; }
    /// <summary>The curse half of a pact relic (shown in red).</summary>
    public string? Curse { get; init; }
}

/// <summary>A region of the vault: look, music, enemy pools and boss. Index is its slot in Database.Regions;
/// NativeAct is the depth it was tuned for (a run places regions in acts 0-4, see RunState.RegionOrder).</summary>
public sealed record RegionDef(int Index, string Name, string Subtitle, string Background, string Tint, string Music,
                               string MapIcon, string[][] Easy, string[][] Hard, string[][] Elites, string[] Boss, int NativeAct = -1)
{
    /// <summary>The act this region's enemies were balanced for.</summary>
    public int Act => NativeAct < 0 ? Index : NativeAct;

    /// <summary>Short id from the background file name: crypt, drowned, frost, storm, cinder.</summary>
    public string Key => Background[(Background.LastIndexOf('/') + 1)..].Replace(".png", "");
}
