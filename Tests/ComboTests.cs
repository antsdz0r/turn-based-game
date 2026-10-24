using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>Team combos: setup by one hero, payoff by another, setup consumed, no self-combos, no chains.</summary>
public class ComboTests
{
    private static (Battle battle, Combatant setter, Combatant finisher, Combatant enemy) Setup(params string[] enemies)
    {
        var battle = TestKit.NewBattle(enemies.Length > 0 ? enemies : ["bones", "bones", "archer"], 0);
        battle.Start();
        foreach (var e in battle.Enemies) { e.MaxHp = e.Hp = 500; e.Affinities.Clear(); e.Armor = 0; e.BreakMax = e.Break = 9; }
        return (battle, battle.Heroes[1], battle.Heroes[0], battle.Enemies[0]);
    }

    [Fact]
    public void Shatter_needs_a_slow_from_another_hero_and_consumes_it()
    {
        var (battle, setter, finisher, enemy) = Setup();
        battle.ApplyStatus(enemy, new StatusApply(StatusKind.Slowed, 0, 2), setter.Uid);
        int plain = battle.ComputeDamage(finisher, battle.Enemies[1], 20, Element.Physical, true, false, DefenseQuality.None);
        int combo = battle.ComputeDamage(finisher, enemy, 20, Element.Physical, true, false, DefenseQuality.None);
        Assert.Equal((int)System.Math.Round(plain * 1.5), combo);

        int pips = enemy.Break;
        battle.Strike(finisher, enemy, 20, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(enemy.Has(StatusKind.Slowed));
        Assert.True(enemy.Break < pips);
        Assert.Equal(1, battle.CombosTriggered);
    }

    [Fact]
    public void A_hero_cannot_combo_with_their_own_setup()
    {
        var (battle, _, finisher, enemy) = Setup();
        battle.ApplyStatus(enemy, new StatusApply(StatusKind.Slowed, 0, 2), finisher.Uid);
        Assert.Null(battle.ComboFor(finisher, enemy, Element.Physical));
    }

    [Fact]
    public void Ignite_detonates_the_poison()
    {
        var (battle, setter, finisher, enemy) = Setup();
        battle.ApplyStatus(enemy, new StatusApply(StatusKind.Poison, 6, 3), setter.Uid);
        int hp = enemy.Hp;
        battle.Strike(finisher, enemy, 1, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(enemy.Has(StatusKind.Poison));
        Assert.True(hp - enemy.Hp >= 12, $"only {hp - enemy.Hp} damage");
    }

    [Fact]
    public void Overload_arcs_to_every_other_enemy_but_never_chains()
    {
        var (battle, setter, finisher, enemy) = Setup();
        foreach (var e in battle.Enemies) battle.ApplyStatus(e, new StatusApply(StatusKind.Burn, 2, 3), setter.Uid);
        var others = battle.Enemies.Where(e => e != enemy).ToList();
        var before = others.ToDictionary(e => e, e => e.Hp);
        battle.Strike(finisher, enemy, 10, Element.Lightning, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.All(others, e => Assert.True(e.Hp < before[e]));
        Assert.All(others, e => Assert.True(e.Has(StatusKind.Burn)));   // the arcs don't consume their burns
        Assert.Equal(1, battle.CombosTriggered);
    }

    [Fact]
    public void Frostbite_slows_after_consuming_the_bleed()
    {
        var (battle, setter, finisher, enemy) = Setup();
        battle.ApplyStatus(enemy, new StatusApply(StatusKind.Bleed, 2, 3), setter.Uid);
        battle.Strike(finisher, enemy, 5, Element.Ice, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(enemy.Has(StatusKind.Bleed));
        Assert.True(enemy.Has(StatusKind.Slowed));
    }

    [Fact]
    public void A_killing_combo_counts_the_death_once()
    {
        var (battle, setter, finisher, enemy) = Setup();
        battle.ApplyStatus(enemy, new StatusApply(StatusKind.Poison, 10, 3), setter.Uid);
        enemy.Hp = 15;                                     // survives the 1-damage hit, dies to the ignition
        int deaths = 0;
        battle.Happened += e => { if (e is DeathEvent d && d.Unit == enemy) deaths++; };
        battle.Strike(finisher, enemy, 1, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(enemy.IsAlive);
        Assert.Equal(1, deaths);
    }

    [Fact]
    public void Every_combo_can_be_set_up_and_paid_off_by_some_hero()
    {
        var kits = Database.Heroes.Select(h => h.Abilities.Prepend(h.BasicAbility).Select(Database.Ability).ToList()).ToList();
        foreach (var combo in Combos.All)
        {
            var setters = kits.Where(k => k.Any(a => a.Applies.Concat(a.UpApplies).Any(s => s.Kind == combo.Setup && !s.OnSelf))).ToList();
            var payers = kits.Where(k => k.Any(a => a.Power > 0 && a.TargetsEnemies
                                                   && Enumerable.Range(0, a.HitCount).Any(i => a.HitElement(i) == combo.Payoff))).ToList();
            Assert.True(setters.Count > 0, $"{combo.Name}: no hero applies {combo.Setup}");
            Assert.True(payers.Any(p => setters.Any(s => s != p)), $"{combo.Name}: no second hero can pay it off");
        }
    }

    [Fact]
    public void Every_boss_has_a_title_card()
    {
        foreach (var boss in Database.AllEnemies.Where(e => e.IsBoss))
        {
            Assert.False(string.IsNullOrWhiteSpace(boss.Epithet), boss.Id);
            Assert.False(string.IsNullOrWhiteSpace(boss.MechanicHint), boss.Id);
        }
    }
}
