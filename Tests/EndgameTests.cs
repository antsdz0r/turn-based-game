using System;
using System.Collections.Generic;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>Weekly challenge, endless mode, enemy tactics, duo techniques, special rooms, pact relics, finishers and lore.</summary>
public class EndgameTests
{
    [Fact]
    public void Weekly_is_the_same_for_everyone_and_its_mutators_never_cancel_out()
    {
        foreach (var week in new[] { "2026-W01", "2026-W17", "2026-W43", "2027-W09" })
        {
            var a = Weekly.MutatorsFor(week);
            Assert.Equal(a, Weekly.MutatorsFor(week));
            Assert.Equal(3, a.Distinct().Count());
            Assert.False(a.Contains("glass") && a.Contains("frenzy"));
            Assert.False(a.Contains("splitters") && a.Contains("volatile"));
            Assert.Equal(Weekly.PartyFor(week), Weekly.PartyFor(week));
            var run = Weekly.Create(week);
            Assert.Equal(week, run.Weekly);
            Assert.Equal(a, run.Mutators);
        }
    }

    [Fact]
    public void Weekly_mutators_change_the_fight()
    {
        var run = TestKit.NewRun(3);
        run.Mutators = ["splitters", "brittle", "omens"];
        var node = run.Map.Nodes.First(n => n.Floor is > 0 and < 4 && n.Type == NodeType.Battle);
        run.CurrentNode = node.Id;
        run.NodeCompleted = false;
        var setup = run.CreateBattle();
        Assert.NotNull(setup.Omen);
        Assert.All(setup.Enemies.Where(e => !e.IsElite && !e.IsBoss), e => Assert.Equal("splitting", e.Modifier));
    }

    [Fact]
    public void Descending_into_endless_mode_starts_a_harder_loop_without_double_counting()
    {
        var run = TestKit.NewRun(8);
        run.Stats.EnemiesSlain = 50;
        var p = new Profile();
        p.RecordRunEnd(run, won: true);
        Assert.Equal(50, p.TotalEnemiesSlain);
        var oldMap = run.Map;
        Endless.Descend(run);
        Assert.Equal(1, run.Loop);
        Assert.NotSame(oldMap, run.Map);
        Assert.Equal(-1, run.CurrentNode);
        run.Stats.EnemiesSlain = 70;
        p.RecordRunEnd(run, won: false);
        Assert.Equal(70, p.TotalEnemiesSlain);       // only the 20 slain in the loop were added
        Assert.Equal(1, p.RunsWon);                  // a loop isn't a second win
        Assert.True(p.EndlessBest > DungeonMap.Floors);

        var plain = Database.CreateEncounter(["bones"], 0);
        var looped = Database.CreateEncounter(["bones"], 0);
        Endless.Scale(looped, 1);
        Assert.True(looped[0].MaxHp > plain[0].MaxHp);
        Assert.True(looped[0].DamageMult > plain[0].DamageMult);
    }

    [Fact]
    public void Daily_and_weekly_runs_cannot_go_endless()
    {
        Assert.False(Endless.CanDescend(Daily.Create("2026-10-25")));
        Assert.False(Endless.CanDescend(Weekly.Create("2026-W43")));
        Assert.True(Endless.CanDescend(TestKit.NewRun(1)));
    }

    [Fact]
    public void Enemies_about_to_break_often_brace()
    {
        int braced = 0;
        for (int seed = 0; seed < 30; seed++)
        {
            var battle = TestKit.NewBattle(["bones"], 0, seed);
            battle.Start();
            var bones = battle.Enemies[0];
            bones.Break = 1;
            int round = battle.Round;
            TestKit.RunUntil(battle, b => b.Round > round);
            if (bones.IsAlive && bones.Intent?.Move.Id == "rattle") braced++;
        }
        Assert.True(braced >= 5, $"only {braced}/30 braced");
    }

    [Fact]
    public void Elites_finish_off_a_hero_they_can_kill()
    {
        int focused = 0, total = 0;
        for (int seed = 0; seed < 20; seed++)
        {
            var battle = TestKit.NewBattle(["templar"], 6, seed);
            battle.Start();
            var weak = battle.Heroes.First(h => h.Row == Row.Front);
            weak.Hp = 1;
            int round = battle.Round;
            TestKit.RunUntil(battle, b => b.Round > round);
            var e = battle.Enemies[0];
            if (e.Intent is { Move.Target: MoveTarget.Hero, Move.Pick: HeroPick.Random } intent && weak.IsAlive)
            {
                total++;
                if (intent.Target == weak) focused++;
            }
        }
        Assert.True(total == 0 || focused * 2 >= total, $"focused {focused}/{total}");
    }

    [Fact]
    public void Duo_techniques_appear_only_when_both_heroes_are_in_the_party()
    {
        var run = TestKit.NewRun(5, "vanguard", "warden", "ranger");
        var node = run.Map.OnFloor(0).First();
        run.CurrentNode = node.Id;
        run.NodeCompleted = false;
        var heroes = run.CreateBattle().Heroes;
        Assert.Contains(heroes.First(h => h.DefId == "vanguard").Abilities, a => a.Def.Id == "duo_hearth_guard");
        Assert.DoesNotContain(heroes.SelectMany(h => h.Abilities), a => a.Def.Id == "duo_crossfire");
        Assert.NotEmpty(Database.ActiveSynergies(["vanguard", "warden"]));
        Assert.Empty(Database.ActiveSynergies(["vanguard", "ranger"]));
    }

    [Fact]
    public void Every_duo_technique_can_be_used()
    {
        foreach (var syn in Database.Synergies)
        {
            var third = Database.Heroes.Select(h => h.Id).First(id => id != syn.HeroA && id != syn.HeroB);
            var run = TestKit.NewRun(2, syn.HeroA, syn.HeroB, third);
            var battle = TestKit.NewBattle(["bones", "bones"], 2, 3, run);
            Database.ApplySynergies(battle.Heroes.ToList());
            battle.Start();
            var owner = battle.Heroes.First(h => h.DefId == syn.HeroA);
            var duo = owner.Abilities.First(a => a.Def.Id == syn.AbilityId);
            Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn && b.Current == owner && duo.CooldownLeft == 0), syn.AbilityId);
            var target = battle.ValidTargets(owner, duo).FirstOrDefault();
            Assert.True(battle.CanUse(owner, duo, out var why), $"{syn.AbilityId}: {why}");
            Assert.True(battle.UseAbility(owner, duo, duo.Def.NeedsTarget ? target : null), syn.AbilityId);
        }
    }

    [Fact]
    public void Special_rooms_exist_and_the_sealed_door_hides_the_ember_wyrm()
    {
        foreach (var id in new[] { "forge", "pact_shrine", "caravan", "hunt", "sealed_door" })
            Assert.Contains(Events.Rooms, e => e.Id == id);
        var run = TestKit.NewRun(4);
        run.Gold = 200;
        var door = Events.Rooms.First(e => e.Id == "sealed_door");
        door.Choices[0].Apply(run, new Random(1));
        Assert.Equal(new List<string> { "ember_wyrm" }, run.ForcedEncounter);
        Assert.True(Database.EnemyDefinition("ember_wyrm").IsBoss);
    }

    [Fact]
    public void The_pact_shrine_gives_a_pact_relic()
    {
        var run = TestKit.NewRun(4);
        run.CurrentNode = run.Map.OnFloor(5).First().Id;
        var shrine = Events.Rooms.First(e => e.Id == "pact_shrine");
        shrine.Choices[0].Apply(run, new Random(2));
        Assert.Contains(run.Relics, id => Database.Relic(id).Pact);
    }

    [Fact]
    public void Pact_relics_have_their_blessing_and_their_curse()
    {
        var run = TestKit.NewRun(6);
        int rest = run.RestHealPct;
        run.Relics.Add("bloodstone_crown");
        Assert.True(rest > 0);
        Assert.Equal(0, run.RestHealPct);

        var plain = TestKit.NewBattle(["bones"], 0);
        var glass = TestKit.NewBattle(["bones"], 0, relics: ["glass_heart"]);
        int taken = plain.ComputeDamage(plain.Enemies[0], plain.Heroes[0], 20, Element.Fire, true, false, DefenseQuality.None);
        int takenGlass = glass.ComputeDamage(glass.Enemies[0], glass.Heroes[0], 20, Element.Fire, true, false, DefenseQuality.None);
        Assert.True(takenGlass > taken);

        var lantern = TestKit.NewBattle(["bones"], 0, relics: ["hollow_lantern"]);
        var hero = lantern.Heroes.First(h => h.Abilities.Any(a => a.Def.Cost > 0));
        var ability = hero.Abilities.First(a => a.Def.Cost > 0).Def;
        Assert.Equal(ability.Cost - 1, lantern.EffectiveCost(hero, ability));
        Assert.All(Database.PactRelics, r => Assert.False(string.IsNullOrEmpty(r.Curse)));
    }

    [Fact]
    public void Finishers_are_flagged_for_the_cinematic()
    {
        var run = TestKit.NewRun(7, "shadowblade", "warden", "ranger");
        var battle = TestKit.NewBattle(["bones"], 0, 7, run);
        var events = new List<BattleEvent>();
        battle.Happened += events.Add;
        battle.Start();
        var nyx = battle.Heroes.First(h => h.DefId == "shadowblade");
        Assert.True(TestKit.RunUntil(battle, b => b.State == BattleState.HeroTurn && b.Current == nyx));
        nyx.Resource = 5;
        var evis = nyx.Abilities.First(a => a.Def.Id == "eviscerate");
        Assert.True(battle.UseAbility(nyx, evis, battle.ValidTargets(nyx, evis).First()));
        Assert.Contains(events.OfType<ActionEvent>(), a => a.Actor == nyx && a.Finisher);
    }

    [Fact]
    public void Lore_pages_unlock_from_bosses_and_milestones()
    {
        var p = new Profile();
        Assert.Equal(Lore.Pages.Count, Lore.Pages.Select(pg => pg.Id).Distinct().Count());
        p.RecordBoss("lich");
        Assert.Contains("boss_lich", p.LoreFound);
        p.RunsStarted = 1;
        Assert.Contains(Lore.Refresh(p), pg => pg.Id == "keeper_first");
        Assert.Null(Lore.Unlock(p, "boss_lich"));       // only once
        // Every boss in the game has a page.
        foreach (var boss in Database.AllEnemies.Where(e => e.IsBoss)) Assert.NotNull(Lore.Get("boss_" + boss.Id));
        foreach (var region in Database.Regions) Assert.NotNull(Lore.Get("region_" + region.Key));
    }
}
