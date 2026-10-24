using System;
using System.IO;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>Omens, bounties, the five new enemies and the monster voice/line tables.</summary>
public class ContentPack2Tests
{
    private static Battle WithSetup(string[] enemies, string? omen = null, string? bounty = null, int floor = 2)
    {
        var run = TestKit.NewRun();
        return new Battle(new BattleSetup
        {
            Heroes = run.Party.Select(Database.CreateHero).ToList(),
            Enemies = Database.CreateEncounter(enemies, floor),
            Floor = floor, Seed = 5, Omen = omen, Bounty = bounty,
        });
    }

    [Fact]
    public void Blood_moon_raises_damage_for_everyone()
    {
        var plain = WithSetup(["bones"]);
        var moon = WithSetup(["bones"], omen: "blood_moon");
        int a = plain.ComputeDamage(plain.Heroes[0], plain.Enemies[0], 20, Element.Fire, true, false, DefenseQuality.None);
        int b = moon.ComputeDamage(moon.Heroes[0], moon.Enemies[0], 20, Element.Fire, true, false, DefenseQuality.None);
        Assert.True(b > a);
    }

    [Fact]
    public void Thick_fog_lowers_hit_chance_but_not_never_miss_attacks()
    {
        var fog = WithSetup(["bones"], omen: "thick_fog");
        var plain = WithSetup(["bones"]);
        Assert.Equal(plain.HitChance(plain.Enemies[0], 95) - 15, fog.HitChance(fog.Enemies[0], 95));
        Assert.Equal(100, fog.HitChance(fog.Enemies[0], 0));
    }

    [Fact]
    public void Crumbling_floor_hurts_only_the_front_rows()
    {
        var battle = WithSetup(["bones", "archer"], omen: "crumbling_floor");
        var hp = battle.Heroes.Concat(battle.Enemies).ToDictionary(u => u, u => u.Hp + u.Amount(StatusKind.Shield));
        battle.Start();
        foreach (var u in battle.Heroes.Concat(battle.Enemies))
        {
            int now = u.Hp + u.Amount(StatusKind.Shield);
            if (u.Row == Row.Front) Assert.True(now < hp[u], $"{u.Name} in front took no damage");
            else Assert.Equal(hp[u], now);
        }
    }

    [Fact]
    public void Every_omen_plays_out_without_breaking_the_rules()
    {
        foreach (var omen in Omens.All)
        {
            var battle = WithSetup(Database.Region(1).Hard[0], omen: omen.Id, floor: 5);
            TestKit.PlayGreedy(battle);
            Assert.True(battle.IsOver || battle.Round > 40, omen.Id);
        }
    }

    [Fact]
    public void Bounties_track_their_conditions()
    {
        var swift = WithSetup(["bones"], bounty: "swift");
        swift.Start();
        Assert.True(swift.BountyMet);

        var untouched = WithSetup(["bones"], bounty: "untouched");
        untouched.Start();
        Assert.True(untouched.BountyMet);
        var hero = untouched.Heroes[0];
        untouched.Strike(untouched.Enemies[0], hero, 999, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(hero.IsAlive);
        Assert.False(untouched.BountyMet);

        var breaker = WithSetup(["bones", "archer"], bounty: "breaker");
        breaker.Start();
        Assert.False(breaker.BountyMet);
        foreach (var e in breaker.Enemies) breaker.ChipBreak(e, 99);
        Assert.True(breaker.BountyMet);
    }

    [Fact]
    public void A_met_bounty_pays_gold_on_victory()
    {
        var run = TestKit.NewRun(9);
        var battle = WithSetup(["bones"], bounty: "untouched");
        battle.Start();
        foreach (var e in battle.Enemies) battle.Strike(battle.Heroes[0], e, 999, Element.Physical, 0, 0, true, false, 0, DefenseQuality.None);
        Assert.False(battle.AliveEnemies.Any());
        int before = run.Gold;
        var result = run.ApplyVictory(battle);
        Assert.Contains(result.Lines, l => l.Contains("Bounty complete"));
        Assert.True(run.Gold - before >= Bounties.Get("untouched")!.Gold);
    }

    [Theory]
    [InlineData("bellringer")] [InlineData("shellback")] [InlineData("rime_sentinel")] [InlineData("ballista")] [InlineData("ash_priest")]
    public void New_enemies_appear_in_their_region_and_have_a_voice(string id)
    {
        Assert.True(Database.HasEnemy(id));
        Assert.Contains(Database.Regions, r => r.Hard.Any(group => group.Contains(id)));
        Assert.NotNull(MobLines.Line(id, MobCue.Attack, new Random(1)));
    }

    [Fact]
    public void Ballista_bolt_skewers_only_the_back_row()
    {
        var battle = WithSetup(["ballista"], floor: 13);
        battle.Start();
        var ballista = battle.Enemies[0];
        var bolt = ballista.Moves.First(m => m.Id == "ballista_bolt");
        var targets = battle.IntentTargets(ballista, new Intent { Move = bolt });
        Assert.All(targets, h => Assert.Equal(Row.Back, h.Row));
    }

    [Fact]
    public void Every_enemy_family_has_voice_files()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "Audio", "Voice"))) dir = dir.Parent;
        if (dir is null) return;   // running outside the repository
        foreach (var enemy in Database.AllEnemies)
        {
            string voice = MobLines.Voice(enemy.Id);
            foreach (var cue in new[] { "talk", "attack", "hurt", "death" })
                Assert.True(File.Exists(Path.Combine(dir.FullName, "Audio", "Voice", $"{voice}_{cue}_0.ogg")), $"{enemy.Id}: {voice}_{cue}_0.ogg");
        }
    }
}
