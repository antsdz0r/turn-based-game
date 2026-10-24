using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>Status stacking, stun protection, shields, boss escalations and the new weapons/relics/items.</summary>
public class RulesTests
{
    private static Battle Crypt(string[]? enemies = null, string[]? relics = null, string[]? items = null) =>
        TestKit.NewBattle(enemies ?? ["bones", "archer"], 0, relics: relics, consumables: items);

    [Fact]
    public void Poison_stacks_up_to_the_cap()
    {
        var battle = Crypt();
        battle.Start();
        var enemy = battle.Enemies[0];
        battle.ApplyStatus(enemy, new StatusApply(StatusKind.Poison, 15, 3), -1);
        battle.ApplyStatus(enemy, new StatusApply(StatusKind.Poison, 15, 3), -1);
        Assert.Equal(Battle.MaxPoison, enemy.Amount(StatusKind.Poison));
    }

    [Fact]
    public void Shields_never_exceed_max_hp()
    {
        var battle = Crypt();
        battle.Start();
        var hero = battle.Heroes[0];
        battle.AddShield(hero, 999, 2);
        battle.AddShield(hero, 999, 2);
        Assert.Equal(hero.MaxHp, hero.Amount(StatusKind.Shield));
    }

    [Fact]
    public void A_unit_that_lost_a_turn_to_stun_shrugs_off_the_next_one()
    {
        var battle = Crypt();
        battle.Start();
        foreach (var e in battle.Enemies) battle.ApplyStatus(e, new StatusApply(StatusKind.Stun, 1, 1), -1);
        Assert.True(TestKit.RunUntil(battle, b => b.Round >= 2));
        var steadfast = battle.AliveEnemies.Where(e => !e.Has(StatusKind.Stun)).ToList();
        Assert.NotEmpty(steadfast);
        foreach (var e in steadfast)
        {
            battle.ApplyStatus(e, new StatusApply(StatusKind.Stun, 1, 1), -1);
            Assert.False(e.Has(StatusKind.Stun), $"{e.Name} was stun-locked");
        }
    }

    [Fact]
    public void Bosses_get_harder_to_break_after_each_break()
    {
        var battle = TestKit.NewBattle(Database.Region(0).Boss, 3);
        battle.Start();
        var boss = battle.Enemies.First(e => e.IsBoss);
        int before = boss.BreakMax;
        battle.ChipBreak(boss, boss.Break);
        Assert.True(boss.Broken);
        Assert.True(TestKit.RunUntil(battle, _ => !boss.Broken || !boss.IsAlive));
        Assert.Equal(before + 1, boss.BreakMax);
        Assert.Equal(boss.BreakMax, boss.Break);
    }

    [Fact]
    public void Defending_shakes_off_an_affliction()
    {
        var battle = Crypt();
        battle.Start();
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn));
        var hero = battle.Current!;
        battle.ApplyStatus(hero, new StatusApply(StatusKind.Bleed, 2, 3), -1);
        Assert.True(battle.Defend(hero));
        Assert.False(hero.Has(StatusKind.Bleed));
    }

    [Fact]
    public void A_multi_hit_attack_cannot_skip_a_boss_phase()
    {
        var battle = TestKit.NewBattle(Database.Region(2).Boss, 11);
        battle.Start();
        var boss = battle.Enemies.First(e => e.IsBoss);
        boss.Hp = boss.MaxHp / 2 + 3;
        var hero = battle.Heroes[0];
        for (int i = 0; i < 3 && boss.IsAlive; i++)
            battle.Strike(hero, boss, 4, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.Equal(2, boss.Phase);
    }

    [Fact]
    public void Colossus_exposes_two_weak_points_in_phase_two()
    {
        var battle = TestKit.NewBattle(Database.Region(0).Boss, 3);
        battle.Start();
        var boss = battle.Enemies.First(e => e.IsBoss);
        boss.Phase = 2;
        Assert.True(TestKit.RunUntil(battle, b => b.Round >= 2));
        Assert.NotNull(boss.ExposedElement2);
        Assert.NotEqual(boss.ExposedElement, boss.ExposedElement2);
    }

    [Fact]
    public void Thunderstrike_in_phase_two_hits_every_branded_hero()
    {
        var battle = TestKit.NewBattle(Database.Region(3).Boss, 15);
        battle.Start();
        var tyrant = battle.Enemies.First(e => e.IsBoss);
        var strike = tyrant.Enemy!.Phase2Moves!.First(m => m.Id == "thunderstrike2");
        battle.ApplyStatus(battle.Heroes[0], new StatusApply(StatusKind.Marked, 0, 3), tyrant.Uid);
        battle.ApplyStatus(battle.Heroes[2], new StatusApply(StatusKind.Marked, 0, 3), tyrant.Uid);
        var targets = battle.IntentTargets(tyrant, new Intent { Move = strike });
        Assert.Equal(2, targets.Count);
        Assert.All(targets, h => Assert.True(h.Has(StatusKind.Marked)));
    }

    [Fact]
    public void Warden_pike_reaches_the_back_row()
    {
        var battle = Crypt(["bones", "archer"]);
        battle.Start();
        var hero = battle.Heroes.First(h => h.Basic is { Def.Melee: true });
        var backRow = battle.Enemies.First(e => e.Row == Row.Back);
        Assert.DoesNotContain(backRow, battle.ValidTargets(hero, hero.Basic!));
        hero.WeaponEffect = "reach";
        Assert.Contains(backRow, battle.ValidTargets(hero, hero.Basic!));
    }

    [Fact]
    public void Stormglass_weakens_physical_hits()
    {
        var battle = Crypt();
        var hero = battle.Heroes[0];
        var enemy = battle.Enemies[0];
        enemy.Affinities.Clear();
        enemy.Armor = 0;
        int plain = battle.ComputeDamage(hero, enemy, 20, Element.Physical, true, false, DefenseQuality.None);
        hero.WeaponEffect = "stormglass";
        int focus = battle.ComputeDamage(hero, enemy, 20, Element.Physical, true, false, DefenseQuality.None);
        Assert.True(focus < plain);
    }

    [Fact]
    public void Static_sigil_chips_break_once_per_enemy()
    {
        var battle = Crypt(relics: ["static_sigil"]);
        battle.Start();
        var hero = battle.Heroes[0];
        var enemy = battle.Enemies[0];
        enemy.Affinities.Clear();
        int start = enemy.Break;
        battle.Strike(hero, enemy, 1, Element.Lightning, 0, 0, true, false, 0, DefenseQuality.None);
        if (!enemy.IsAlive) return;
        Assert.Equal(start - 1, enemy.Break);
        battle.Strike(hero, enemy, 1, Element.Lightning, 0, 0, true, false, 0, DefenseQuality.None);
        if (!enemy.IsAlive) return;
        Assert.Equal(start - 1, enemy.Break);
    }

    [Fact]
    public void Thunder_flask_chips_every_enemy_and_ward_flask_shields_every_hero()
    {
        var battle = Crypt(items: ["thunder_flask", "ward_flask"]);
        battle.Start();
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn));
        var before = battle.AliveEnemies.ToDictionary(e => e, e => e.Break);
        Assert.True(battle.UseItem(battle.Current!, 0, null));
        foreach (var (enemy, pips) in before)
            if (enemy.IsAlive && !enemy.Broken) Assert.True(enemy.Break < pips, $"{enemy.Name} kept its break pips");

        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn));
        Assert.True(battle.UseItem(battle.Current!, 0, null));
        Assert.All(battle.AliveHeroes, h => Assert.True(h.Amount(StatusKind.Shield) >= 10));
    }

    [Fact]
    public void Boss_rewards_offer_a_signature_weapon_and_never_duplicates()
    {
        var run = TestKit.NewRun(11);
        run.Gold = 10_000;
        var result = new BattleResult { Boss = true, Elite = true };
        for (int i = 0; i < 10; i++)
        {
            Assert.True(run.RerollRewards(result));
            Assert.Contains(result.Choices, c => c.Kind == RewardKind.Equipment && Database.Item(c.Id).WeaponEffect is not null);
            Assert.Equal(result.Choices.Count, result.Choices.Select(c => (c.Kind, c.Id, c.HeroId)).Distinct().Count());
        }
    }

    [Fact]
    public void Every_weapon_effect_is_described_in_battle()
    {
        Assert.All(Database.WeaponEffects, e => Assert.False(string.IsNullOrEmpty(Database.WeaponEffectText(e))));
    }
}
