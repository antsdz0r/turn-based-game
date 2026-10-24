using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>Mid-battle saves, hero mastery (costumes and alternative abilities) and run history.</summary>
public class ProgressionTests
{
    private static RunState RunInFirstBattle(int seed)
    {
        var run = TestKit.NewRun(seed);
        var node = run.Map.OnFloor(0).First(n => n.Type == NodeType.Battle || n.Type == NodeType.Elite);
        run.CurrentNode = node.Id;
        run.NodeCompleted = false;
        return run;
    }

    private static string Snapshot(Battle b) =>
        string.Join("|", b.Heroes.Concat(b.Enemies).Select(u => $"{u.DefId}:{u.Hp}:{u.Row}:{string.Join(",", u.Statuses.Select(s => s.Kind))}"))
        + $"|{b.State}|{b.Round}|{b.Current?.DefId}";

    [Theory]
    [InlineData(3)] [InlineData(17)] [InlineData(404)]
    public void A_battle_saved_mid_fight_resumes_exactly_where_it_was(int seed)
    {
        var run = RunInFirstBattle(seed);
        var battle = new Battle(run.CreateBattle());
        battle.Start();
        for (int step = 0; step < 14 && !battle.IsOver; step++)
        {
            if (battle.State == BattleState.EnemyTurn) battle.ExecuteEnemyTurn(DefenseQuality.Good);
            else TestKit.GreedyTurn(battle);
        }
        run.ActiveBattle!.Commands = battle.Commands.ToList();
        var json = run.ToJson();

        var loaded = RunState.FromJson(json)!;
        var resumed = new Battle(loaded.CreateBattle());
        int replayed = resumed.StartAndReplay(loaded.ActiveBattle!.Commands);
        Assert.Equal(battle.Commands.Count, replayed);
        Assert.Equal(Snapshot(battle), Snapshot(resumed));
    }

    [Fact]
    public void Consumables_used_before_quitting_come_back_for_the_replay()
    {
        var run = RunInFirstBattle(9);
        int before = run.Consumables.Count;
        var battle = new Battle(run.CreateBattle());
        battle.Start();
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn));
        int potion = battle.Consumables.IndexOf("potion");
        Assert.True(battle.UseItem(battle.Current!, potion, battle.Current));
        Assert.Equal(before - 1, run.Consumables.Count);
        run.ActiveBattle!.Commands = battle.Commands.ToList();

        var loaded = RunState.FromJson(run.ToJson())!;
        var resumed = new Battle(loaded.CreateBattle());
        resumed.StartAndReplay(loaded.ActiveBattle!.Commands);
        Assert.Equal(before - 1, loaded.Consumables.Count);
    }

    [Fact]
    public void Winning_clears_the_battle_save()
    {
        var run = RunInFirstBattle(5);
        var battle = new Battle(run.CreateBattle());
        battle.Start();
        foreach (var e in battle.Enemies.ToList()) battle.Strike(battle.Heroes[0], e, 999, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        run.ApplyVictory(battle);
        Assert.Null(run.ActiveBattle);
    }

    [Fact]
    public void Mastery_levels_unlock_costume_then_alt_then_mastered()
    {
        var p = new Profile();
        Assert.Equal(0, Mastery.Level(p, "vanguard"));
        p.HeroXp["vanguard"] = Mastery.Thresholds[0];
        Assert.True(Mastery.HasCostume(p, "vanguard"));
        Assert.False(Mastery.HasAlt(p, "vanguard"));
        p.HeroXp["vanguard"] = Mastery.Thresholds[2];
        Assert.True(Mastery.IsMastered(p, "vanguard"));
    }

    [Fact]
    public void Every_hero_has_a_costume_and_an_alternative_ability()
    {
        foreach (var hero in Database.Heroes)
        {
            Assert.NotNull(Mastery.Costume(hero.Id));
            var alt = Mastery.Alt(hero.Id);
            Assert.NotNull(alt);
            Assert.Contains(alt!.Replaces, hero.Abilities);
            Assert.NotNull(Database.Ability(alt.AbilityId));
        }
    }

    [Fact]
    public void Loadout_swaps_the_ability_and_the_sprite()
    {
        var p = new Profile();
        p.HeroXp["arcanist"] = Mastery.Thresholds[2];
        var run = TestKit.NewRun(1, "vanguard", "arcanist", "warden");
        var hero = run.Hero("arcanist");
        Mastery.ApplyLoadout(hero, p, costume: true, alt: true);
        Assert.Equal("arcanist_c", hero.Sprite);
        Assert.Contains("meteor", hero.AbilityIds);
        Assert.DoesNotContain("prismatic", hero.AbilityIds);
        Assert.Contains(hero.Def.BasicAbility, hero.Upgrades);    // mastered: upgraded basic attack
        var unit = Database.CreateHero(hero);
        Assert.Equal("arcanist_c", unit.Sprite);
        Assert.Contains(unit.Abilities, a => a.Def.Id == "meteor");
        // Not unlocked: nothing changes.
        var fresh = run.Hero("warden");
        Mastery.ApplyLoadout(fresh, new Profile(), costume: true, alt: true);
        Assert.Null(fresh.Costume);
        Assert.Null(fresh.AltAbility);
    }

    [Theory]
    [InlineData("shield_wall")] [InlineData("meteor")] [InlineData("sanctuary")] [InlineData("fan_of_knives")] [InlineData("snare_shot")]
    [InlineData("blood_frenzy")] [InlineData("plague_swarm")] [InlineData("smoke_flask")] [InlineData("aegis_rune")]
    public void Every_alternative_ability_can_be_used_in_battle(string abilityId)
    {
        var alt = Mastery.Alternatives.First(a => a.AbilityId == abilityId);
        var p = new Profile();
        p.HeroXp[alt.HeroId] = Mastery.Thresholds[1];
        var others = Database.Heroes.Select(h => h.Id).Where(id => id != alt.HeroId).Take(2);
        var run = TestKit.NewRun(2, others.Prepend(alt.HeroId).ToArray());
        Mastery.ApplyLoadout(run.Hero(alt.HeroId), p, false, true);
        var battle = TestKit.NewBattle(["bones", "bones"], 2, 3, run);
        battle.Start();
        var hero = battle.Heroes.First(h => h.DefId == alt.HeroId);
        hero.Resource = hero.MaxResource;
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn && b.Current == hero));
        hero.Resource = hero.MaxResource;
        var ability = hero.Abilities.First(a => a.Def.Id == abilityId);
        var target = battle.ValidTargets(hero, ability).FirstOrDefault();
        Assert.True(battle.CanUse(hero, ability, out var why), why);
        Assert.True(battle.UseAbility(hero, ability, ability.Def.NeedsTarget ? target : null));
    }

    [Fact]
    public void Run_end_awards_mastery_and_records_history()
    {
        var run = TestKit.NewRun(4, "ranger", "hexer", "warden");
        run.Stats.HeroDamage["hexer"] = 300;
        run.Stats.HeroDamage["ranger"] = 120;
        run.Stats.BestHit = 41;
        run.Stats.BestHitBy = "ranger";
        var p = new Profile();
        p.HeroXp["hexer"] = Mastery.Thresholds[0] - 1;
        p.RecordRunEnd(run, won: true);
        Assert.True(Mastery.Xp(p, "ranger") > 0);
        Assert.Equal(Mastery.Xp(p, "hexer"), Mastery.Xp(p, "ranger") + Mastery.Thresholds[0] - 1);
        var record = Assert.Single(p.History);
        Assert.True(record.Won);
        Assert.Equal("hexer", record.Mvp);
        Assert.Equal(41, record.BestHit);
        Assert.Equal("crypt", record.Route[0]);
        Assert.Contains(p.LastMastery, m => m.HeroId == "hexer" && m.Level == 1);
    }

    [Fact]
    public void History_keeps_only_the_last_twenty_runs()
    {
        var p = new Profile();
        for (int i = 0; i < 25; i++) p.RecordRunEnd(TestKit.NewRun(i), won: false);
        Assert.Equal(Profile.MaxHistory, p.History.Count);
        var loaded = Profile.FromJson(p.ToJson());
        Assert.Equal(Profile.MaxHistory, loaded.History.Count);
    }

    [Fact]
    public void Battles_track_damage_per_hero_and_the_best_hit()
    {
        var battle = TestKit.NewBattle(["bones"], 0);
        battle.Start();
        battle.Strike(battle.Heroes[1], battle.Enemies[0], 9, Element.Fire, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.True(battle.DamageByHero[battle.Heroes[1].DefId] > 0);
        Assert.Equal(battle.Heroes[1].DefId, battle.BestHitBy);
    }
}
