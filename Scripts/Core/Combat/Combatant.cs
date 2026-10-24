using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public sealed class StatusEffect
{
    public StatusKind Kind { get; init; }
    public int Amount { get; set; }
    public int Turns { get; set; }
    /// <summary>For Guarded: the protector's Uid.</summary>
    public int SourceUid { get; init; } = -1;
}

/// <summary>What an enemy plans to do this round. Shown to the player and executed exactly as shown.</summary>
public sealed class Intent
{
    public required EnemyMoveDef Move { get; init; }
    /// <summary>Concrete hero for single-hero moves, ally for single-ally moves.</summary>
    public Combatant? Target { get; set; }
    /// <summary>True during the wind-up round of a charged move.</summary>
    public bool Charging { get; init; }
}

/// <summary>A hero or enemy during a battle.</summary>
public sealed class Combatant
{
    private static int _nextUid;

    public int Uid { get; } = ++_nextUid;
    public required string DefId { get; init; }
    public required string Name { get; set; }
    public required string Sprite { get; init; }
    public string Tint { get; init; } = "#ffffff";
    public float Scale { get; init; } = 1f;
    /// <summary>Sprite art faces right; enemies normally face left (their art is already mirrored).</summary>
    public bool FlipSprite { get; init; }
    public bool IsHero { get; init; }
    public bool IsBoss { get; init; }
    public bool IsElite { get; init; }

    public int MaxHp { get; set; }
    public int Hp { get; set; }
    public bool IsAlive => Hp > 0;
    public int Armor { get; set; }
    public int BaseSpeed { get; set; }
    public int Crit { get; set; }
    public int Dodge { get; set; }
    public int PowerPct { get; set; }
    public Dictionary<Element, int> ElementPct { get; } = new();
    public Dictionary<Element, Affinity> Affinities { get; set; } = new();
    public Row Row { get; set; }
    /// <summary>Order inside the row (lower = closer to the enemy).</summary>
    public int Slot { get; set; }

    public List<StatusEffect> Statuses { get; } = new();

    // ───── Heroes ─────
    public HeroDef? Hero { get; init; }
    public List<AbilityInstance> Abilities { get; } = new();
    public AbilityInstance? Basic { get; set; }
    public int Resource { get; set; }
    public int MaxResource { get; set; }
    public ResourceDef? ResourceDef { get; init; }

    // ───── Enemies ─────
    public EnemyDef? Enemy { get; init; }
    public Intent? Intent { get; set; }
    /// <summary>A charged move that fires on the next turn.</summary>
    public EnemyMoveDef? ChargedMove { get; set; }
    public int BreakMax { get; set; }
    public int Break { get; set; }
    /// <summary>Broken enemies take double damage and skip their next turn.</summary>
    public bool Broken { get; set; }
    public int Phase { get; set; } = 1;
    public List<EnemyMoveDef> Moves { get; set; } = new();
    /// <summary>For AI: moves used, to avoid spamming.</summary>
    public string? LastMoveId { get; set; }
    /// <summary>How many times this enemy used each move in this battle (limits summons/devours).</summary>
    public Dictionary<string, int> MoveUses { get; } = new();
    /// <summary>Level-up traits (heroes only).</summary>
    public HashSet<string> Traits { get; } = new();
    /// <summary>Elite modifier id (see EnemyMods), or null.</summary>
    public string? Modifier { get; set; }
    /// <summary>Multiplier on this enemy's attack damage (modifiers).</summary>
    public double DamageMult { get; set; } = 1.0;
    /// <summary>This round's exposed weak point (Bone Colossus), or null.</summary>
    public Element? ExposedElement { get; set; }
    public bool Summoned { get; init; }

    // ───── Status helpers ─────
    public StatusEffect? Get(StatusKind kind) => Statuses.FirstOrDefault(s => s.Kind == kind);
    public bool Has(StatusKind kind) => Statuses.Any(s => s.Kind == kind);
    public int Amount(StatusKind kind) => Get(kind)?.Amount ?? 0;
    public void Remove(StatusKind kind) => Statuses.RemoveAll(s => s.Kind == kind);

    public int Speed => Math.Max(1, BaseSpeed - (Has(StatusKind.Slowed) ? 3 : 0));

    public Affinity AffinityTo(Element element) =>
        Affinities.TryGetValue(element, out var affinity) ? affinity : Affinity.Normal;

    public IEnumerable<Element> Weaknesses => Affinities.Where(a => a.Value == Affinity.Weak).Select(a => a.Key);

    public static bool IsNegative(StatusKind kind) => kind is StatusKind.Poison or StatusKind.Burn or StatusKind.Bleed
        or StatusKind.Stun or StatusKind.Marked or StatusKind.Weakened or StatusKind.Slowed;

    public int HealthPct => MaxHp == 0 ? 0 : Hp * 100 / MaxHp;
}
