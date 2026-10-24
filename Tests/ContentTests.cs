using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

public class ContentTests
{
    [Fact]
    public void Content_is_valid()
    {
        var problems = Database.Validate();
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void Every_encounter_and_event_fight_creates_real_enemies()
    {
        foreach (var region in Database.Regions)
        {
            var all = region.Easy.Concat(region.Hard).Concat(region.Elites).Append(region.Boss);
            foreach (var ids in all)
            {
                var enemies = Database.CreateEncounter(ids, region.Act * DungeonMap.FloorsPerRegion);
                Assert.Equal(ids.Length, enemies.Count);
            }
        }
        foreach (var ids in Events.EventFights)
            Assert.Equal(ids.Length, Database.CreateEncounter(ids, 2).Count);
    }

    [Fact]
    public void Altar_guardians_are_enemy_ids_not_sprite_names()   // regression: KeyNotFoundException 'cultist'
    {
        Assert.All(Events.AltarGuardians, id => Assert.True(Database.HasEnemy(id), id));
        Assert.All(Events.ShadeAmbush, id => Assert.True(Database.HasEnemy(id), id));
    }

    [Fact]
    public void Unknown_enemy_ids_fall_back_instead_of_crashing()
    {
        var enemies = Database.CreateEncounter(["definitely_not_an_enemy"], 0);
        Assert.NotEmpty(enemies);
    }
}
