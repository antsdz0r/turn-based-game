using System.Collections.Generic;

namespace TBRPG.Core;

/// <summary>
/// Everything that happens in battle, in order. Rules resolve instantly; the UI replays these
/// events one at a time with animations and sound.
/// </summary>
public abstract record BattleEvent;

public sealed record RoundStartEvent(int Round) : BattleEvent;

/// <summary>A unit's turn begins (after start-of-turn ticks).</summary>
public sealed record TurnStartEvent(Combatant Actor) : BattleEvent;

/// <summary>A unit starts an action (lunge, cast, shoot...).</summary>
/// <summary>Big = a powerful attack (charged moves, boss sweeps, hero ultimates/finishers): the UI plays a power-up.</summary>
public sealed record ActionEvent(Combatant Actor, string Name, Vfx Vfx, Element Element, IReadOnlyList<Combatant> Targets, bool Big = false) : BattleEvent;

public sealed record HitEvent(Combatant Target, int Damage, int Absorbed, int HpAfter, Element? Element,
                              bool Crit, bool Weak, bool Resisted, bool Immune, bool Broken, DefenseQuality Defense) : BattleEvent;

public sealed record MissEvent(Combatant Target) : BattleEvent;

/// <summary>Damage over time (poison, burn, bleed).</summary>
public sealed record TickEvent(Combatant Target, StatusKind Kind, int Damage, int HpAfter) : BattleEvent;

public sealed record HealEvent(Combatant Target, int Amount, int HpAfter) : BattleEvent;

public sealed record ShieldEvent(Combatant Target, int Amount) : BattleEvent;

public sealed record StatusEvent(Combatant Target, StatusKind Kind, bool Applied) : BattleEvent;

/// <summary>Floating text over a unit.</summary>
public sealed record TextEvent(Combatant Target, string Text, string ColorHex) : BattleEvent;

public sealed record BreakDamageEvent(Combatant Target, int BreakLeft) : BattleEvent;

public sealed record BreakEvent(Combatant Target, bool Interrupted) : BattleEvent;

public sealed record DeathEvent(Combatant Unit) : BattleEvent;

public sealed record SummonEvent(Combatant Unit) : BattleEvent;

public sealed record ChargeEvent(Combatant Unit, string MoveName) : BattleEvent;

public sealed record PhaseChangeEvent(Combatant Unit, string Title) : BattleEvent;

public sealed record MoveRowEvent(Combatant Unit) : BattleEvent;

/// <summary>Intents changed outside round start (taunt, target died, row changed).</summary>
public sealed record IntentsChangedEvent : BattleEvent;

public sealed record BattleEndEvent(bool Victory) : BattleEvent;

/// <summary>Targeting preview (not an event): expected result of an ability on one target.</summary>
public sealed record AbilityPreview(int Damage, int HitChance, bool Weak, bool Resisted, bool Immune, bool Breaks,
                                    int Heal, int Shield, bool Lethal);
