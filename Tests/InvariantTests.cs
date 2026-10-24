using System.Collections.Generic;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>
/// Rules that must hold after every single action in every encounter: HP, resources, shields, poison and break
/// stay within bounds; the dead hold no statuses or intents; turn order follows speed; loops terminate.
/// </summary>
public class InvariantTests
{
    private static void CheckInvariants(Battle battle)
    {
        foreach (var u in battle.Heroes.Concat(battle.Enemies))
        {
            Assert.InRange(u.Hp, 0, u.MaxHp);
            Assert.InRange(u.Amount(StatusKind.Shield), 0, u.MaxHp);
            Assert.InRange(u.Amount(StatusKind.Poison), 0, Battle.MaxPoison);
            if (u.IsHero) Assert.InRange(u.Resource, 0, u.MaxResource);
            else Assert.InRange(u.Break, 0, u.BreakMax);
            if (!u.IsAlive)
            {
                Assert.Empty(u.Statuses);
                Assert.Null(u.Intent);
            }
        }
        if (battle.State == BattleState.HeroTurn) Assert.True(battle.Current is { IsHero: true, IsAlive: true });
        if (battle.State == BattleState.EnemyTurn) Assert.True(battle.Current is { IsHero: false, IsAlive: true });
    }

    public static IEnumerable<object[]> Encounters() => SoftLockTests.Encounters();

    [Theory]
    [MemberData(nameof(Encounters))]
    public void Bounds_hold_after_every_action(string enemyIds, int floor)
    {
        var run = TestKit.LevelledRun(1 + floor / 3, floor + 1);
        var battle = TestKit.NewBattle(enemyIds.Split(','), floor, seed: floor * 7 + 1, run: run);
        battle.Start();
        CheckInvariants(battle);
        for (int step = 0; step < 3000 && !battle.IsOver && battle.Round <= 40; step++)
        {
            if (battle.State == BattleState.EnemyTurn) battle.ExecuteEnemyTurn(DefenseQuality.None);
            else TestKit.GreedyTurn(battle);
            CheckInvariants(battle);
        }
    }

    [Fact]
    public void Turn_order_follows_speed()
    {
        var battle = TestKit.NewBattle(Database.Region(1).Hard[0], 5);
        battle.Start();
        var order = battle.TurnOrder;
        for (int i = 1; i < order.Count; i++)
            Assert.True(order[i - 1].Speed >= order[i].Speed, $"{order[i - 1].Name} ({order[i - 1].Speed}) before {order[i].Name} ({order[i].Speed})");
    }

    [Fact]
    public void Invalid_targets_are_rejected_without_spending_anything()
    {
        var battle = TestKit.NewBattle(["bones", "archer"], 0);
        battle.Start();
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn && b.Current!.Basic is { Def.Melee: true }
                                                  && b.Current.WeaponEffect is null));
        var hero = battle.Current!;
        var backRow = battle.Enemies.First(e => e.Row == Row.Back);
        int resource = hero.Resource;
        Assert.False(battle.UseAbility(hero, hero.Basic!, backRow));     // melee can't reach past a living front row
        Assert.False(battle.UseAbility(hero, hero.Basic!, null));        // missing target
        Assert.Same(hero, battle.Current);
        Assert.Equal(resource, hero.Resource);
    }

    [Fact]
    public void Ripostes_never_chain()
    {
        var battle = TestKit.NewBattle(["bones", "archer"], 0);
        battle.Start();
        var hero = battle.Heroes[0];
        var enemy = battle.Enemies[0];
        enemy.MaxHp = enemy.Hp = 500;
        enemy.Crit = 0;
        hero.Statuses.Add(new StatusEffect { Kind = StatusKind.Riposte, Amount = 5, Turns = 1 });
        enemy.Statuses.Add(new StatusEffect { Kind = StatusKind.Riposte, Amount = 5, Turns = 1 });
        int heroHp = hero.Hp + hero.Amount(StatusKind.Shield);
        battle.Strike(enemy, hero, 3, Element.Physical, 0, 0, true, true, 0, DefenseQuality.None);
        // The hero is hit once (no counter-counter), the enemy is hit once by the riposte.
        Assert.True(enemy.Hp < 500);
        Assert.True(hero.Hp + hero.Amount(StatusKind.Shield) >= heroHp - 3);
    }

    [Fact]
    public void Stormglass_arcs_never_chain()
    {
        var battle = TestKit.NewBattle(["bones", "bones", "archer"], 0);
        battle.Start();
        var hero = battle.Heroes[0];
        hero.WeaponEffect = "stormglass";
        foreach (var e in battle.Enemies) { e.MaxHp = e.Hp = 500; e.Affinities.Clear(); }
        var log = new List<string>();
        battle.Logged += log.Add;
        for (int i = 0; i < 9; i++)
            battle.Strike(hero, battle.Enemies[0], 3, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.Equal(3, log.Count(l => l.Contains("Stormglass arcs")));
    }

    [Fact]
    public void Every_hero_brings_at_least_one_tactical_answer()
    {
        foreach (var hero in Database.Heroes)
            Assert.True(Coverage.AnswersArmor(hero.Id) || Coverage.AnswersBigHit(hero.Id) || Coverage.AnswersBackRow(hero.Id), hero.Id);
    }

    [Fact]
    public void Upgrade_twists_change_behavior()
    {
        var twisted = Database.Heroes.SelectMany(h => h.Abilities).Select(Database.Ability).Where(a => a.UpText is not null).ToList();
        Assert.True(twisted.Count >= 10, $"only {twisted.Count} abilities have upgrade twists");
        var bash = Database.Ability("shield_bash");
        Assert.False(bash.EffectiveInterrupt(false));
        Assert.True(bash.EffectiveInterrupt(true));
        var volley = Database.Ability("volley");
        Assert.Equal(RowRequirement.Any, volley.EffectiveRequires(true));
    }
}
