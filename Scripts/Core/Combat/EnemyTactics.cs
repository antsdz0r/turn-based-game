using System.Linq;

namespace TBRPG.Core;

/// <summary>
/// A tactical layer on top of each enemy's script (EnemyAI): enemies react to the state of the fight.
///   • Guarding the break: an enemy one pip from breaking braces (Defend/Shield) instead of attacking.
///   • Desperation: a wounded elite or boss uses its heal or guard.
///   • Protecting: healers and shielders cover the ally closest to breaking or dying.
///   • Focus fire: elites and bosses go for a hero they can finish, a hero who is Empowered, or a Marked one,
///     and melee bruisers avoid heroes standing in a Riposte.
/// Every choice is still shown in the intent bubble before it happens, so the player can read and counter it.
/// </summary>
public sealed partial class Battle
{
    private static bool Smart(Combatant e) => e.IsElite || e.IsBoss;

    private EnemyMoveDef ApplyTactics(Combatant enemy, EnemyMoveDef move)
    {
        // Never override a scripted signature move.
        if (move.Charged || move.Kind is MoveKind.Summon or MoveKind.Usurp or MoveKind.Consume or MoveKind.Heal or MoveKind.Shield or MoveKind.Defend)
            return move;
        var guard = enemy.Moves.FirstOrDefault(m => m.Kind == MoveKind.Defend && m.Target == MoveTarget.Self && m.Id != enemy.LastMoveId)
                    ?? enemy.Moves.FirstOrDefault(m => m.Kind == MoveKind.Shield && m.Id != enemy.LastMoveId);
        var mend = enemy.Moves.FirstOrDefault(m => m.Kind == MoveKind.Heal && m.Id != enemy.LastMoveId);

        if (guard is not null && !enemy.Broken && enemy.Break == 1 && _rng.Next(100) < (Smart(enemy) ? 70 : 45))
        {
            Log($"  [color=#a99f8f]{enemy.Name} senses it is about to break and braces.[/color]");
            return guard;
        }
        if (Smart(enemy) && enemy.HealthPct < 25 && (mend ?? guard) is { } save && _rng.Next(100) < 50)
        {
            Log($"  [color=#a99f8f]{enemy.Name} is desperate.[/color]");
            return save;
        }
        return move;
    }

    /// <summary>Smart single-target picks for moves without a fixed preference (HeroPick.Random).</summary>
    private Combatant SmartPick(Combatant enemy, EnemyMoveDef move, System.Collections.Generic.List<Combatant> candidates)
    {
        if (move.Melee)
        {
            var safe = candidates.Where(h => !h.Has(StatusKind.Riposte)).ToList();
            if (safe.Count > 0) candidates = safe;
        }
        int estimate = IntentPower(move, enemy) * move.Hits;
        var killable = candidates.Where(h => h.Hp + h.Amount(StatusKind.Shield) <= estimate).OrderBy(h => h.Hp).FirstOrDefault();
        if (killable is not null) return killable;
        var empowered = candidates.FirstOrDefault(h => h.Has(StatusKind.Empowered));
        if (empowered is not null && _rng.Next(100) < 60) return empowered;
        var marked = candidates.FirstOrDefault(h => h.Has(StatusKind.Marked));
        if (marked is not null) return marked;
        return candidates[_rng.Next(candidates.Count)];
    }
}
