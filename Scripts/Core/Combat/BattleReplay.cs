using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public enum BattleCommandKind { Ability, Defend, Move, Item, Enemy }

/// <summary>
/// One command given to a battle, by position instead of object identity (hero 0, enemy 2, ability -1 = basic),
/// so the same commands replayed on a battle built from the same setup reproduce it exactly.
/// </summary>
public sealed record BattleCommand(BattleCommandKind Kind, int Actor = -1, int Ability = -1, bool TargetIsHero = false,
                                   int Target = -1, int Item = -1, DefenseQuality Defense = DefenseQuality.None);

/// <summary>Mid-battle save: a battle is its setup (seed and starting consumables) plus the commands given so far.</summary>
public sealed class BattleSave
{
    public int Node { get; set; } = -1;
    public int Seed { get; set; }
    public List<string> Consumables { get; set; } = new();
    public List<BattleCommand> Commands { get; set; } = new();
}

/// <summary>
/// Every public command goes through here: it runs the rule code, and when the command was legal it is recorded,
/// so a battle can be saved at any moment and rebuilt by replaying its commands (the rules are deterministic).
/// </summary>
public sealed partial class Battle
{
    public List<BattleCommand> Commands { get; } = new();
    /// <summary>Raised after every recorded command (the UI saves the run then).</summary>
    public event System.Action? CommandRecorded;
    private bool _replaying;

    private (bool hero, int index) Ref(Combatant? unit) =>
        unit is null ? (false, -1) : unit.IsHero ? (true, _heroes.IndexOf(unit)) : (false, _enemies.IndexOf(unit));

    private Combatant? Unit(bool hero, int index)
    {
        var list = hero ? _heroes : _enemies;
        return index >= 0 && index < list.Count ? list[index] : null;
    }

    private void Record(BattleCommand command)
    {
        Commands.Add(command);
        if (!_replaying) CommandRecorded?.Invoke();
    }

    public bool UseAbility(Combatant hero, AbilityInstance ability, Combatant? target)
    {
        int index = ability == hero.Basic ? -1 : hero.Abilities.IndexOf(ability);
        var (th, ti) = Ref(target);
        var command = new BattleCommand(BattleCommandKind.Ability, _heroes.IndexOf(hero), index, th, ti);
        if (!UseAbilityCore(hero, ability, target)) return false;
        Record(command);
        return true;
    }

    public bool Defend(Combatant hero)
    {
        var command = new BattleCommand(BattleCommandKind.Defend, _heroes.IndexOf(hero));
        if (!DefendCore(hero)) return false;
        Record(command);
        return true;
    }

    public bool Move(Combatant hero)
    {
        var command = new BattleCommand(BattleCommandKind.Move, _heroes.IndexOf(hero));
        if (!MoveCore(hero)) return false;
        Record(command);
        return true;
    }

    public bool UseItem(Combatant hero, int index, Combatant? target)
    {
        var (th, ti) = Ref(target);
        var command = new BattleCommand(BattleCommandKind.Item, _heroes.IndexOf(hero), TargetIsHero: th, Target: ti, Item: index);
        if (!UseItemCore(hero, index, target)) return false;
        Record(command);
        return true;
    }

    public void ExecuteEnemyTurn(DefenseQuality defense)
    {
        if (State != BattleState.EnemyTurn) return;
        ExecuteEnemyTurnCore(defense);
        Record(new BattleCommand(BattleCommandKind.Enemy, Defense: defense));
    }

    /// <summary>Applies one recorded command. Returns false if it no longer fits the battle (a desync).</summary>
    private bool Apply(BattleCommand c)
    {
        var actor = Unit(true, c.Actor);
        switch (c.Kind)
        {
            case BattleCommandKind.Enemy:
                if (State != BattleState.EnemyTurn) return false;
                ExecuteEnemyTurn(c.Defense);
                return true;
            case BattleCommandKind.Defend:
                return actor is not null && Defend(actor);
            case BattleCommandKind.Move:
                return actor is not null && Move(actor);
            case BattleCommandKind.Item:
                return actor is not null && UseItem(actor, c.Item, Unit(c.TargetIsHero, c.Target));
            case BattleCommandKind.Ability:
                if (actor is null) return false;
                var ability = c.Ability < 0 ? actor.Basic : c.Ability < actor.Abilities.Count ? actor.Abilities[c.Ability] : null;
                return ability is not null && UseAbility(actor, ability, Unit(c.TargetIsHero, c.Target));
        }
        return false;
    }

    /// <summary>
    /// Starts the battle and replays saved commands without raising events for the UI.
    /// Stops at the first command that no longer applies, so a damaged save still gives a playable battle.
    /// Returns how many commands were replayed.
    /// </summary>
    public int StartAndReplay(IEnumerable<BattleCommand> commands)
    {
        _replaying = true;
        var happened = Happened;
        var logged = Logged;
        Happened = null;
        Logged = null;
        int applied = 0;
        try
        {
            Start();
            foreach (var c in commands.ToList())
            {
                if (IsOver || !Apply(c)) break;
                applied++;
            }
        }
        finally
        {
            Happened = happened;
            Logged = logged;
            _replaying = false;
        }
        return applied;
    }
}
