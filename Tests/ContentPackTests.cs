using System;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>Gravemason, Cinder Tome, the Volatile modifier and the duel / dice events.</summary>
public class ContentPackTests
{
    [Fact]
    public void Gravemason_smashes_shields_and_sunders_armor()
    {
        var battle = TestKit.NewBattle(["bones", "archer"], 0);
        battle.Start();
        var hero = battle.Heroes[0];
        var enemy = battle.Enemies[0];
        hero.WeaponEffect = "crush";
        enemy.Armor = 3;
        battle.AddShield(enemy, 20, 2);
        battle.Strike(hero, enemy, 1, Element.Physical, 0, 0, false, false, 0, DefenseQuality.None);
        if (!enemy.IsAlive) return;
        Assert.False(enemy.Has(StatusKind.Shield));
        Assert.Equal(2, enemy.Armor);
    }

    [Fact]
    public void Cinder_tome_burns_then_chips_break_on_burning_enemies()
    {
        var battle = TestKit.NewBattle(["bones", "archer"], 0);
        battle.Start();
        var hero = battle.Heroes[0];
        var enemy = battle.Enemies[0];
        enemy.Affinities.Clear();
        enemy.MaxHp = enemy.Hp = 500;
        hero.WeaponEffect = "cinder";
        battle.Strike(hero, enemy, 1, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.True(enemy.Has(StatusKind.Burn));
        int pips = enemy.Break;
        battle.Strike(hero, enemy, 1, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.True(enemy.Break == pips - 1 || enemy.Broken);
    }

    [Fact]
    public void Volatile_enemies_explode_on_the_front_row()
    {
        var battle = TestKit.NewBattle(["bones", "archer"], 0);
        battle.Start();
        var enemy = battle.Enemies[0];
        EnemyMods.Apply(enemy, "volatile");
        var front = battle.Heroes.Where(h => h.Row == Row.Front).ToList();
        var back = battle.Heroes.Where(h => h.Row == Row.Back).ToList();
        var hpBefore = battle.Heroes.ToDictionary(h => h, h => h.Hp + h.Amount(StatusKind.Shield));
        enemy.Hp = 1;
        battle.Strike(battle.Heroes[0], enemy, 50, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(enemy.IsAlive);
        Assert.All(front, h => Assert.True(h.Hp + h.Amount(StatusKind.Shield) < hpBefore[h]));
        Assert.All(back, h => Assert.Equal(hpBefore[h], h.Hp + h.Amount(StatusKind.Shield)));
    }

    [Fact]
    public void Duel_event_forces_an_elite_fight()
    {
        var run = TestKit.NewRun(8);
        var duel = Events.All.First(e => e.Id == "duelist");
        duel.Choices[0].Apply(run, new Random(2));
        Assert.NotNull(run.ForcedEncounter);
        Assert.True(run.ForcedElite);
        Assert.All(run.ForcedEncounter!, id => Assert.True(Database.HasEnemy(id)));
    }

    [Fact]
    public void Dice_wager_moves_exactly_forty_gold()
    {
        var dice = Events.All.First(e => e.Id == "gambler");
        for (int seed = 0; seed < 10; seed++)
        {
            var run = TestKit.NewRun(seed);
            run.Gold = 100;
            dice.Choices[0].Apply(run, new Random(seed));
            Assert.Contains(run.Gold, new[] { 60, 140 });
        }
    }
}
