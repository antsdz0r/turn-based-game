using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

public class CombatTests
{
    private static (Battle battle, Combatant hero, Combatant enemy) Duel()
    {
        var battle = TestKit.NewBattle(Database.Region(0).Easy[0], 0);
        var enemy = battle.Enemies.First(e => e.Weaknesses.Any());
        return (battle, battle.Heroes[0], enemy);
    }

    [Fact]
    public void Weakness_hits_harder_than_neutral()
    {
        var (battle, hero, enemy) = Duel();
        var weak = enemy.Weaknesses.First();
        var neutral = new[] { Element.Physical, Element.Fire, Element.Ice, Element.Lightning }
            .FirstOrDefault(e => enemy.AffinityTo(e) == Affinity.Normal, weak);
        if (neutral == weak) return;   // no neutral element on this enemy
        int w = battle.ComputeDamage(hero, enemy, 20, weak, true, false, DefenseQuality.None);
        int n = battle.ComputeDamage(hero, enemy, 20, neutral, true, false, DefenseQuality.None);
        Assert.True(w > n, $"weak {w} vs neutral {n}");
    }

    [Fact]
    public void Broken_enemies_take_double_damage()
    {
        var (battle, hero, enemy) = Duel();
        int normal = battle.ComputeDamage(hero, enemy, 20, Element.Fire, true, false, DefenseQuality.None);
        enemy.Broken = true;
        int broken = battle.ComputeDamage(hero, enemy, 20, Element.Fire, true, false, DefenseQuality.None);
        Assert.InRange(broken, normal * 2 - 1, normal * 2 + 1);
    }

    [Fact]
    public void Armor_reduces_physical_but_not_magic()
    {
        var (battle, hero, enemy) = Duel();
        enemy.Affinities.Clear();
        enemy.Armor = 0;
        int bare = battle.ComputeDamage(hero, enemy, 20, Element.Physical, false, false, DefenseQuality.None);
        enemy.Armor = 5;
        int armored = battle.ComputeDamage(hero, enemy, 20, Element.Physical, false, false, DefenseQuality.None);
        int fire = battle.ComputeDamage(hero, enemy, 20, Element.Fire, false, false, DefenseQuality.None);
        Assert.Equal(bare - 5, armored);
        Assert.Equal(bare, fire);
    }

    [Fact]
    public void Parry_reduces_damage_more_than_block()
    {
        var (battle, hero, enemy) = Duel();
        int none = battle.ComputeDamage(enemy, hero, 30, Element.Physical, true, false, DefenseQuality.None);
        int good = battle.ComputeDamage(enemy, hero, 30, Element.Physical, true, false, DefenseQuality.Good);
        int perfect = battle.ComputeDamage(enemy, hero, 30, Element.Physical, true, false, DefenseQuality.Perfect);
        Assert.True(none > good && good > perfect, $"{none} > {good} > {perfect}");
    }

    [Fact]
    public void Damage_breakdown_explains_the_number()
    {
        var (battle, hero, enemy) = Duel();
        var why = new System.Collections.Generic.List<string>();
        battle.ComputeDamage(hero, enemy, 20, enemy.Weaknesses.First(), true, false, DefenseQuality.None, why);
        Assert.Contains(why, w => w.StartsWith("20 base"));
        Assert.Contains(why, w => w.Contains("weak"));
    }
}

public class WeaponTests
{
    [Fact]
    public void Last_Whisper_makes_abilities_cheaper()
    {
        var battle = TestKit.NewBattle(Database.Region(0).Easy[0], 0);
        var hero = battle.Heroes[0];
        var costly = hero.Abilities.First(a => a.Def.Cost > 0).Def;
        hero.WeaponEffect = "blood_price";
        Assert.Equal(costly.Cost - 1, battle.EffectiveCost(hero, costly));
    }

    [Fact]
    public void Gutterfangs_basic_attack_hits_twice()
    {
        var battle = TestKit.NewBattle(Database.Region(0).Easy[0], 0);
        var hero = battle.Heroes[0];
        var basic = hero.Basic!.Def;   // the basic attack lives outside the ability list
        hero.WeaponEffect = "twin";
        Assert.Equal(basic.HitCount * 2, battle.HitsFor(hero, basic));
    }

    [Fact]
    public void Oathbreaker_executes_broken_enemies()
    {
        var battle = TestKit.NewBattle(Database.Region(0).Easy[0], 0);
        var hero = battle.Heroes[0];
        var enemy = battle.Enemies[0];
        enemy.Broken = true;
        int plain = battle.ComputeDamage(hero, enemy, 20, Element.Fire, true, false, DefenseQuality.None);
        hero.WeaponEffect = "execution";
        int executed = battle.ComputeDamage(hero, enemy, 20, Element.Fire, true, false, DefenseQuality.None);
        Assert.True(executed > plain);
    }
}
