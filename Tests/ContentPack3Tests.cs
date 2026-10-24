using System;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>The region pool, the Garden and the Archive, the new weapons and heroes, and feats.</summary>
public class ContentPack3Tests
{
    [Theory]
    [InlineData(1)] [InlineData(42)] [InlineData(999)] [InlineData(31337)]
    public void Every_run_starts_in_the_crypt_ends_in_the_throne_and_draws_three_middle_regions(int seed)
    {
        var run = TestKit.NewRun(seed);
        Assert.Equal(DungeonMap.RegionCount, run.RegionOrder.Count);
        Assert.Equal("crypt", run.RegionOrder[0]);
        Assert.Equal("cinder", run.RegionOrder[^1]);
        var middle = run.RegionOrder.Skip(1).Take(3).ToList();
        Assert.Equal(3, middle.Distinct().Count());
        Assert.All(middle, key => Assert.Contains(key, Database.MiddleRegions));
        // Shallower regions come first.
        var acts = middle.Select(k => Database.RegionByKey(k).Act).ToList();
        Assert.Equal(acts.OrderBy(a => a), acts);
    }

    [Fact]
    public void Region_order_survives_a_save_and_old_saves_keep_the_classic_order()
    {
        var run = TestKit.NewRun(77);
        var loaded = RunState.FromJson(run.ToJson())!;
        Assert.Equal(run.RegionOrder, loaded.RegionOrder);
        loaded.RegionOrder.Clear();
        for (int act = 0; act < DungeonMap.RegionCount; act++) Assert.Equal(Database.Region(act).Key, loaded.RegionAt(act).Key);
    }

    [Fact]
    public void Both_new_regions_show_up_across_seeds()
    {
        var seen = Enumerable.Range(0, 60).SelectMany(s => TestKit.NewRun(s).RegionOrder).ToHashSet();
        Assert.Contains("garden", seen);
        Assert.Contains("archive", seen);
    }

    [Fact]
    public void A_region_played_deeper_than_its_native_act_is_tougher()
    {
        var garden = Database.RegionByKey("garden");
        var native = Database.CreateEncounter(["thornling"], 5);
        var deep = Database.CreateEncounter(["thornling"], 5);
        Database.ScaleForAct(deep, garden, garden.Act + 2);
        Assert.True(deep[0].MaxHp > native[0].MaxHp);
        Assert.True(deep[0].DamageMult > native[0].DamageMult);
        Assert.Equal(deep[0].MaxHp, deep[0].Hp);
    }

    [Theory]
    [InlineData("garden")] [InlineData("archive")]
    public void New_region_encounters_come_from_their_own_pools(string key)
    {
        var region = Database.RegionByKey(key);
        var rng = new Random(3);
        for (int i = 0; i < 20; i++)
        {
            var ids = Database.PickEncounter(NodeType.Battle, region, 5, rng);
            Assert.Contains(region.Easy.Concat(region.Hard), g => g.SequenceEqual(ids));
        }
        Assert.Contains(region.Boss, id => Database.EnemyDefinition(id).IsBoss);
    }

    [Fact]
    public void Seed_pods_feed_the_mother_bloom_each_round()
    {
        var battle = TestKit.NewBattle(["seed_pod", "mother_bloom", "seed_pod"], 5, seed: 4);
        battle.Start();
        var bloom = battle.Enemies.First(e => e.DefId == "mother_bloom");
        bloom.Hp = bloom.MaxHp / 2;
        Assert.Equal(2, battle.FeedersOf(bloom));
        int round = battle.Round;
        int before = bloom.Hp;
        Assert.True(TestKit.RunUntil(battle, b => b.Round > round));
        Assert.True(bloom.Hp > before, "the pods should have healed her at the round start");
    }

    [Fact]
    public void Killing_the_pods_stops_the_regrowth()
    {
        var battle = TestKit.NewBattle(["seed_pod", "mother_bloom", "seed_pod"], 5, seed: 4);
        battle.Start();
        foreach (var pod in battle.Enemies.Where(e => e.DefId == "seed_pod").ToList())
            battle.Strike(battle.Heroes[0], pod, 999, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        var bloom = battle.Enemies.First(e => e.DefId == "mother_bloom");
        Assert.Equal(0, battle.FeedersOf(bloom));
    }

    [Fact]
    public void The_archivist_halves_the_element_that_hit_it_last()
    {
        var battle = TestKit.NewBattle(["archivist"], 13, seed: 2);
        battle.Start();
        var hero = battle.Heroes[0];
        var boss = battle.Enemies[0];
        int fresh = battle.ComputeDamage(hero, boss, 30, Element.Ice, true, false, DefenseQuality.None);
        battle.Strike(hero, boss, 5, Element.Ice, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.Equal(Element.Ice, boss.AdaptedTo);
        int adapted = battle.ComputeDamage(hero, boss, 30, Element.Ice, true, false, DefenseQuality.None);
        Assert.True(adapted < fresh);
        // Switching elements resets it.
        battle.Strike(hero, boss, 5, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.Equal(Element.Fire, boss.AdaptedTo);
        Assert.Equal(fresh, battle.ComputeDamage(hero, boss, 30, Element.Ice, true, false, DefenseQuality.None));
    }

    [Fact]
    public void Spore_puffer_poisons_the_front_row_when_it_bursts()
    {
        var battle = TestKit.NewBattle(["spore_puffer"], 5, seed: 6);
        battle.Start();
        battle.Strike(battle.Heroes[0], battle.Enemies[0], 999, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.All(battle.Heroes.Where(h => h.Row == Row.Front), h => Assert.True(h.Has(StatusKind.Poison)));
        Assert.All(battle.Heroes.Where(h => h.Row == Row.Back), h => Assert.False(h.Has(StatusKind.Poison)));
    }

    [Fact]
    public void The_scythe_sweeps_its_basic_attack_into_a_second_enemy()
    {
        var battle = TestKit.NewBattle(["bones", "bones"], 0, seed: 9);
        battle.Start();
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn && b.Current!.Basic!.Def.Melee));
        var hero = battle.Current!;
        hero.WeaponEffect = "reap";
        var first = battle.ValidTargets(hero, hero.Basic!).First();
        var other = battle.Enemies.First(e => e != first);
        int otherHp = other.Hp;
        foreach (var e in battle.Enemies) { e.Armor = 0; e.Dodge = -100; }
        battle.UseAbility(hero, hero.Basic!, first);
        Assert.True(other.Hp < otherHp || !other.IsAlive);
    }

    [Fact]
    public void Flail_misses_more_often()
    {
        var battle = TestKit.NewBattle(["bones"], 0);
        Assert.True(Database.Item("penitents_flail").Crit > 0);
        Assert.Contains("flail", Database.WeaponEffects);
        Assert.False(string.IsNullOrEmpty(Database.WeaponEffectText("flail")));
        Assert.NotNull(battle);
    }

    [Theory]
    [InlineData("alchemist")] [InlineData("spellblade")]
    public void New_heroes_are_complete_and_locked_until_earned(string id)
    {
        var def = Database.Hero(id);
        Assert.Equal(id, def.Id);
        Assert.Equal(4, def.Abilities.Length);
        Assert.Equal(6, Database.TraitsFor(id).Count());
        Assert.False(Meta.IsHeroUnlocked(new Profile(), id));
        var unlock = Meta.HeroUnlocks.First(u => u.HeroId == id);
        var p = new Profile();
        p.RecordBoss(unlock.BossId);
        Assert.True(Meta.IsHeroUnlocked(p, id));
    }

    [Theory]
    [InlineData("alchemist")] [InlineData("spellblade")]
    public void New_heroes_can_win_an_easy_fight(string id)
    {
        int wins = 0;
        for (int seed = 0; seed < 6; seed++)
        {
            var run = TestKit.LevelledRun(2, seed + 1, id, "warden", "ranger");
            if (TestKit.PlayGreedy(TestKit.NewBattle(Database.Region(0).Easy[0], 0, seed, run))) wins++;
        }
        Assert.True(wins >= 4, $"{id} won only {wins}/6 easy fights");
    }

    [Fact]
    public void Feats_pay_out_once_and_unlock_their_weapon()
    {
        var p = new Profile();
        Assert.Contains("gravewheat_scythe", Feats.LockedItems(p));
        p.RecordBoss("mother_bloom");
        Assert.DoesNotContain("gravewheat_scythe", Feats.LockedItems(p));
        int shards = p.Shards;
        var fresh = Feats.Claim(p);
        Assert.Contains(fresh, f => f.Id == "weedkiller");
        Assert.True(p.Shards > shards);
        Assert.Empty(Feats.Claim(p));
    }

    [Fact]
    public void Locked_weapons_are_never_offered()
    {
        try
        {
            Database.LockedItems = Feats.LockedItems(new Profile());
            var rng = new Random(5);
            for (int i = 0; i < 300; i++)
            {
                Assert.DoesNotContain(Database.RandomEquipment(rng, 3).Id, Database.LockedItems);
                Assert.DoesNotContain(Database.RandomFamilyWeapon(rng, 3)!.Id, Database.LockedItems);
            }
        }
        finally { Database.LockedItems = new(); }
    }

    [Fact]
    public void Run_end_adds_lifetime_counters_and_regions()
    {
        var run = TestKit.NewRun(5);
        run.Stats.Breaks = 7;
        run.Stats.Combos = 3;
        run.Stats.RegionsCleared.Add("garden");
        var p = new Profile();
        p.RecordRunEnd(run, won: false);
        Assert.Equal(7, p.TotalBreaks);
        Assert.Equal(3, p.TotalCombos);
        Assert.Contains("garden", p.RegionsCleared);
    }

    [Theory]
    [InlineData("region_garden")] [InlineData("boss_garden")] [InlineData("region_archive")] [InlineData("boss_archive")]
    public void New_cutscenes_exist(string id)
    {
        var run = TestKit.NewRun(8, "alchemist", "spellblade", "warden");
        var scene = Story.Get(id, run);
        Assert.NotNull(scene);
        Assert.DoesNotContain(scene!.Lines, l => l.Text is "Let's go." or "Enough talk.");
    }

    [Fact]
    public void Every_new_enemy_has_lines()
    {
        foreach (var id in new[] { "thornling", "spore_puffer", "strangler", "stalker", "briar_witch", "seed_pod", "mother_bloom",
                                   "tome", "ink_wraith", "gargoyle", "quill_swarm", "index_golem", "archivist" })
            Assert.NotNull(MobLines.Line(id, MobCue.Attack, new Random(1)));
    }
}
