namespace TBRPG.Core;

/// <summary>Damage types. Every enemy is weak, normal, resistant or immune to each.</summary>
public enum Element { Physical, Fire, Ice, Lightning }

/// <summary>How much damage of an element a unit takes: Weak x1.5 (and chips its break meter), Resist x0.5, Immune x0.</summary>
public enum Affinity { Normal, Weak, Resist, Immune }

/// <summary>Formation row. Melee attacks can only reach the front row while it has a living unit.</summary>
public enum Row { Front, Back }

/// <summary>Where a hero must stand to use an ability.</summary>
public enum RowRequirement { Any, Front, Back }

public enum TargetKind { SingleEnemy, AllEnemies, SingleAlly, AllAllies, Self }

public enum StatusKind
{
    Poison,     // Amount damage at turn start, then Amount -1
    Burn,       // Amount fire damage at turn start for Turns turns
    Bleed,      // Amount damage at turn start for Turns turns, ignores armor and shields
    Regen,      // heal Amount at turn start for Turns turns
    Stun,       // skips the next turn
    Shield,     // absorbs Amount damage; expires after Turns turns
    Marked,     // takes +25% damage, attackers get +20% crit
    Weakened,   // deals -25% damage
    Empowered,  // deals +25% damage
    Taunt,      // enemies must target this hero with single-target attacks
    Slowed,     // -3 speed (acts later next round)
    Defending,  // takes -50% damage until its next turn
    Riposte,    // counters melee attackers for Amount physical damage until its next turn
    Guarded,    // attacks on this hero hit its protector instead (SourceId) until the protector's next turn
}

/// <summary>Which animation the UI plays.</summary>
public enum Vfx { Melee, Projectile, Arrow, Beam, Heal, Buff, Debuff, Cloud }

/// <summary>Result of the optional parry timing input.</summary>
public enum DefenseQuality { None, Good, Perfect }

public enum BattleState { NotStarted, HeroTurn, EnemyTurn, Victory, Defeat }

public enum ItemKind { Weapon, Armor, Trinket, Consumable }

public enum EquipSlot { Weapon, Armor, Trinket }

public enum NodeType { Battle, Elite, Treasure, Shop, Rest, Event, Boss }
