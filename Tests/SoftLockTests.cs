using System.Collections.Generic;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>A bot plays every encounter with every hero; no battle may get stuck or throw.</summary>
public class SoftLockTests
{
    public static IEnumerable<object[]> Encounters()
    {
        foreach (var region in Database.Regions)
        {
            int floor = region.Act * DungeonMap.FloorsPerRegion;
            foreach (var ids in region.Easy.Concat(region.Hard).Concat(region.Elites))
                yield return new object[] { string.Join(",", ids), floor + 1 };
            yield return new object[] { string.Join(",", region.Boss), floor + 3 };
        }
    }

    public static IEnumerable<object[]> Parties()
    {
        var ids = Database.Heroes.Select(h => h.Id).ToList();
        for (int i = 0; i < ids.Count; i++)
            yield return new object[] { ids[i], ids[(i + 1) % ids.Count], ids[(i + 2) % ids.Count] };
    }

    [Theory]
    [MemberData(nameof(Encounters))]
    public void Encounter_never_soft_locks(string enemyIds, int floor)
    {
        foreach (var party in Parties())
        {
            var run = TestKit.NewRun(3, party.Cast<string>().ToArray());
            var battle = TestKit.NewBattle(enemyIds.Split(','), floor, seed: floor * 31, run: run);
            TestKit.AutoPlay(battle);
            Assert.True(battle.IsOver || battle.Round > 60);
        }
    }
}
