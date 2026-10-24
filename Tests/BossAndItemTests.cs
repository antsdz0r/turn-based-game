using System;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>Boss escalations (Matriarch, Lich, Hollow King), the Shock Dart, the smith's reforge and reward comparisons.</summary>
public class BossAndItemTests
{
    [Fact]
    public void Undertow_only_hits_the_back_row()
    {
        var battle = TestKit.NewBattle(Database.Region(1).Boss, 7);
        battle.Start();
        var matriarch = battle.Enemies.First(e => e.IsBoss);
        var undertow = matriarch.Enemy!.Phase2Moves!.First(m => m.Id == "undertow");
        var targets = battle.IntentTargets(matriarch, new Intent { Move = undertow });
        Assert.NotEmpty(targets);
        Assert.All(targets, h => Assert.Equal(Row.Back, h.Row));
    }

    [Fact]
    public void Shattering_the_phylactery_hurts_and_staggers_the_lich()
    {
        var battle = TestKit.NewBattle(Database.Region(2).Boss, 11);
        battle.Start();
        var lich = battle.Enemies.First(e => e.IsBoss);
        var jar = battle.Enemies.First(e => e.DefId == "phylactery");
        int hp = lich.Hp, pips = lich.Break;
        jar.Hp = 1;
        battle.Strike(battle.Heroes[0], jar, 50, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(jar.IsAlive);
        Assert.True(lich.Hp < hp);
        Assert.True(lich.Break < pips || lich.Broken);
        Assert.False(battle.IsWarded(lich));
    }

    [Fact]
    public void Hollow_king_exposes_a_weak_point_in_phase_two_only()
    {
        var battle = TestKit.NewBattle(Database.Region(4).Boss, 19);
        battle.Start();
        var king = battle.Enemies.First(e => e.IsBoss);
        Assert.Null(king.ExposedElement);
        king.Phase = 2;
        Assert.True(TestKit.RunUntil(battle, b => b.Round >= 2));
        Assert.NotNull(king.ExposedElement);
        Assert.Null(king.ExposedElement2);
    }

    [Fact]
    public void Shock_dart_interrupts_a_charging_enemy()
    {
        var battle = TestKit.NewBattle(["arc_golem"], 13, consumables: ["shock_dart"]);
        battle.Start();
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn));
        var golem = battle.Enemies[0];
        golem.ChargedMove = golem.Moves.First(m => m.Charged);
        Assert.True(battle.UseItem(battle.Current!, 0, golem));
        Assert.Null(golem.ChargedMove);
    }

    [Fact]
    public void Shock_dart_staggers_a_boss_instead()
    {
        var battle = TestKit.NewBattle(Database.Region(3).Boss, 15, consumables: ["shock_dart"]);
        battle.Start();
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn));
        var tyrant = battle.Enemies.First(e => e.IsBoss);
        tyrant.ChargedMove = tyrant.Moves.First(m => m.Charged);
        int pips = tyrant.Break;
        Assert.True(battle.UseItem(battle.Current!, 0, tyrant));
        Assert.NotNull(tyrant.ChargedMove);
        Assert.True(tyrant.Break <= pips - 2 || tyrant.Broken);
    }

    [Fact]
    public void Smith_reforges_a_plain_weapon_into_a_family_weapon()
    {
        var run = TestKit.NewRun(21);
        run.Party[0].Weapon = "iron_edge";
        var smith = Events.All.Concat(Events.Special).First(e => e.Id == "smith");
        var reforge = smith.Choices.First(c => c.Label.StartsWith("Reforge"));
        Assert.True(reforge.Available(run));
        reforge.Apply(run, new Random(3));
        Assert.NotNull(Database.Item(run.Party[0].Weapon!).WeaponEffect);
        Assert.False(reforge.Available(run));
    }

    [Fact]
    public void Equipment_rewards_show_what_each_hero_has_in_that_slot()
    {
        var run = TestKit.NewRun(4);
        run.Party[0].Weapon = "iron_edge";
        var text = run.EquippedSummary(Database.Item("embercleaver"));
        Assert.Contains("Iron Edge", text);
        Assert.Contains("(empty)", text);
    }
}
