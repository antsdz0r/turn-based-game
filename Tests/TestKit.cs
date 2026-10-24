using System;
using System.Collections.Generic;
using System.Linq;
using TBRPG.Core;

namespace TBRPG.Tests;

/// <summary>Helpers that build parties and battles and play them out without the UI.</summary>
public static class TestKit
{
    public static RunState NewRun(int seed = 1234, params string[] heroIds)
    {
        if (heroIds.Length == 0) heroIds = Database.Heroes.Take(3).Select(h => h.Id).ToArray();
        var picks = heroIds.Select((id, i) => (id, i == 0 ? Row.Front : Row.Back)).ToList();
        return RunState.NewRun(picks, seed);
    }

    public static Battle NewBattle(string[] enemyIds, int floor, int seed = 7, RunState? run = null,
                                   IEnumerable<string>? relics = null, IEnumerable<string>? consumables = null)
    {
        run ??= NewRun();
        var battle = new Battle(new BattleSetup
        {
            Heroes = run.Party.Select(Database.CreateHero).ToList(),
            Enemies = Database.CreateEncounter(enemyIds, floor),
            Floor = floor,
            Seed = seed,
            Relics = new HashSet<string>(relics ?? Enumerable.Empty<string>()),
            Consumables = new List<string>(consumables ?? Enumerable.Empty<string>()),
        });
        return battle;
    }

    /// <summary>
    /// Advances a started battle with passive play (heroes Defend, enemies act) until the condition holds.
    /// Returns false if the battle ended or 400 steps passed first.
    /// </summary>
    public static bool RunUntil(Battle battle, Func<Battle, bool> condition)
    {
        for (int step = 0; step < 400; step++)
        {
            if (condition(battle)) return true;
            if (battle.IsOver) return false;
            if (battle.State == BattleState.EnemyTurn) battle.ExecuteEnemyTurn(DefenseQuality.None);
            else if (battle.State == BattleState.HeroTurn) battle.Defend(battle.Current!);
            else return false;
        }
        return condition(battle);
    }

    /// <summary>
    /// A greedy bot used for balance numbers. Scores every legal action by expected value: damage x hit chance,
    /// break and kill bonuses, a rough value for the statuses it applies, healing/shields only when useful,
    /// minus a little per resource spent (so free attacks win ties). Defends when a charged attack is about
    /// to land on it. It never parries, so real players should do somewhat better.
    /// </summary>
    public static void GreedyTurn(Battle battle, Dictionary<string, (int uses, int damage)>? abilityStats = null)
    {
        var hero = battle.Current!;
        var options = hero.Abilities.ToList();
        if (hero.Basic is not null) options.Add(hero.Basic);
        bool threatened = battle.AliveEnemies.Any(e => e.Intent is { Charging: false, Move.Charged: true } intent
                                                       && battle.IntentTargets(e, intent).Contains(hero));
        double bestScore = threatened ? 14 : 0.5;
        (AbilityInstance ability, Combatant? target)? best = null;
        int wounded = battle.AliveHeroes.Count(h => h.HealthPct < 50);

        double StatusValue(StatusApply a, Combatant t) => t.Has(a.Kind) && a.Kind != StatusKind.Poison ? 0 : a.Kind switch
        {
            StatusKind.Poison => a.Amount * 1.5,
            StatusKind.Bleed or StatusKind.Burn => a.Amount * a.Turns,
            StatusKind.Marked => 4 * a.Turns,
            StatusKind.Weakened => 3 * a.Turns,
            StatusKind.Slowed => 2,
            StatusKind.Stun => t.IsBoss ? 0 : 12,
            StatusKind.Regen => wounded > 0 ? a.Amount * a.Turns : 1,
            StatusKind.Empowered => 6,
            StatusKind.Taunt or StatusKind.Riposte => 4,
            _ => 1,
        };

        foreach (var ability in options)
        {
            if (!battle.CanUse(hero, ability, out _)) continue;
            var def = ability.Def;
            var targets = battle.ValidTargets(hero, ability);
            if (!def.NeedsTarget) targets = targets.Take(1).ToList();
            int count = def.Target is TargetKind.AllEnemies ? battle.AliveEnemies.Count()
                      : def.Target is TargetKind.AllAllies ? battle.AliveHeroes.Count() : 1;
            foreach (var target in targets.DefaultIfEmpty(hero))
            {
                var p = battle.Preview(hero, ability, target);
                double score = def.TargetsEnemies
                    ? p.Damage * count * p.HitChance / 100.0 + (p.Breaks ? 25 : 0) + (p.Lethal ? 15 : 0)
                    : (p.Heal > 0 && wounded > 0 ? Math.Min(p.Heal, target.MaxHp - target.Hp) * 1.5 * count : 0) + p.Shield * 0.7 * count;
                // Protecting an ally that an enemy is about to hit hard is worth a lot.
                if (def.Protect && target != hero && battle.AliveEnemies.Any(e => e.Intent is { Charging: false } i
                        && i.Move.HitsHeroes && battle.IntentTargets(e, i).Contains(target) && target.HealthPct < 60))
                    score += 12;
                score += def.EffectiveApplies(ability.Upgraded).Where(a => !a.OnSelf).Sum(a => StatusValue(a, target)) * count;
                score += def.EffectiveApplies(ability.Upgraded).Where(a => a.OnSelf).Sum(a => StatusValue(a, hero));
                if (def.EffectiveCleanse(ability.Upgraded) && target.Statuses.Any(s => Combatant.IsNegative(s.Kind))) score += 8;
                // Lifesteal is worth more the more wounded the hero is.
                if (def.LifestealPct > 0 && hero.HealthPct < 70) score += p.Damage * def.LifestealPct / 100.0 * 1.5;
                score -= battle.EffectiveCost(hero, def) * 1.5;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = (ability, def.NeedsTarget ? target : null);
                }
            }
        }
        int before = battle.DamageDealt;
        if (best is { } pick && battle.UseAbility(hero, pick.ability, pick.target))
        {
            if (abilityStats is not null)
            {
                var key = $"{hero.DefId}.{pick.ability.Def.Id}";
                var (uses, dmg) = abilityStats.GetValueOrDefault(key);
                abilityStats[key] = (uses + 1, dmg + battle.DamageDealt - before);
            }
            return;
        }
        if (!battle.Defend(hero)) throw new InvalidOperationException($"{hero.Name} could neither act nor defend.");
    }

    /// <summary>Plays a whole battle with the greedy bot. Returns true on victory.</summary>
    public static bool PlayGreedy(Battle battle, Dictionary<string, (int uses, int damage)>? abilityStats = null, int maxRounds = 40)
    {
        battle.Start();
        int steps = 0;
        while (!battle.IsOver && battle.Round <= maxRounds && steps++ < 20_000)
        {
            if (battle.State == BattleState.EnemyTurn) battle.ExecuteEnemyTurn(DefenseQuality.None);
            else GreedyTurn(battle, abilityStats);
        }
        return battle.State == BattleState.Victory;
    }

    /// <summary>A party levelled to the given level, each hero taking its first offered traits.</summary>
    public static RunState LevelledRun(int level, int seed, params string[] heroIds)
    {
        var run = NewRun(seed, heroIds);
        foreach (var hero in run.Party)
        {
            if (level > 1) hero.AddXp(HeroState.XpForLevel(Math.Min(level, HeroState.MaxLevel)));
            while (hero.PendingTraits > 0 && run.TraitChoices(hero) is { Count: > 0 } choices)
                run.PickTrait(hero, choices[0].Id);
            hero.PendingTraits = 0;
            hero.Hp = hero.MaxHp;
        }
        return run;
    }

    /// <summary>
    /// Plays a battle with a simple bot that cycles through every ability. Throws if the battle gets stuck
    /// (a turn where nothing can act). Returns the combat log.
    /// </summary>
    public static List<string> AutoPlay(Battle battle, int maxRounds = 60)
    {
        var log = new List<string>();
        battle.Logged += log.Add;
        battle.Start();
        int step = 0;
        while (!battle.IsOver && battle.Round <= maxRounds)
        {
            if (++step > 20_000) throw new InvalidOperationException("Battle never advanced (soft-lock).");
            var actor = battle.Current ?? throw new InvalidOperationException($"No current actor in state {battle.State}.");
            if (battle.State == BattleState.EnemyTurn)
            {
                battle.ExecuteEnemyTurn(DefenseQuality.None);
                continue;
            }
            if (battle.State != BattleState.HeroTurn)
                throw new InvalidOperationException($"Unexpected state {battle.State}.");

            bool acted = false;
            var options = actor.Abilities.ToList();
            if (actor.Basic is not null) options.Add(actor.Basic);
            int n = options.Count;
            for (int k = 0; k < n && !acted; k++)
            {
                var ability = options[(step + k) % n];
                if (!battle.CanUse(actor, ability, out _)) continue;
                var targets = battle.ValidTargets(actor, ability);
                var target = targets.Count > 0 ? targets[step % targets.Count] : null;
                acted = battle.UseAbility(actor, ability, target);
            }
            if (!acted && !battle.Defend(actor))
                throw new InvalidOperationException($"{actor.Name} could neither act nor defend.");
        }
        return log;
    }
}
