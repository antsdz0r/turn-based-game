using System;
using System.Collections.Generic;
using System.Linq;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

public class RunTests
{
    [Theory]
    [InlineData(1)] [InlineData(42)] [InlineData(9001)] [InlineData(123456)]
    public void Every_map_node_is_reachable_and_leads_to_the_final_boss(int seed)
    {
        var map = DungeonMap.Generate(new Random(seed));
        var reached = new HashSet<int>();
        var queue = new Queue<int>(map.OnFloor(0).Select(n => n.Id));
        while (queue.Count > 0)
        {
            int id = queue.Dequeue();
            if (!reached.Add(id)) continue;
            foreach (var next in map.Node(id).Next) queue.Enqueue(next);
        }
        Assert.Equal(map.Nodes.Count, reached.Count);
        foreach (var node in map.Nodes.Where(n => n.Floor < DungeonMap.Floors - 1))
            Assert.NotEmpty(node.Next);
        var last = map.OnFloor(DungeonMap.Floors - 1).ToList();
        Assert.NotEmpty(last);
        Assert.All(last, n => Assert.Equal(NodeType.Boss, n.Type));
    }

    [Fact]
    public void Same_seed_gives_the_same_map()
    {
        var a = TestKit.NewRun(77).ToJson();
        var b = TestKit.NewRun(77).ToJson();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Save_and_load_round_trips()
    {
        var run = TestKit.NewRun(5);
        run.Gold = 321;
        run.Relics.Add(Database.RandomRelic(new Random(1), run.Relics)!.Id);
        string json = run.ToJson();
        var loaded = RunState.FromJson(json);
        Assert.NotNull(loaded);
        Assert.Equal(json, loaded!.ToJson());
    }

    [Fact]
    public void Same_battle_seed_plays_out_identically()
    {
        var region = Database.Region(1);
        var a = TestKit.AutoPlay(TestKit.NewBattle(region.Hard[0], 5, seed: 99));
        var b = TestKit.AutoPlay(TestKit.NewBattle(region.Hard[0], 5, seed: 99));
        Assert.Equal(a, b);
    }
}

public class TollTests
{
    [Theory]
    [InlineData(2)] [InlineData(31)] [InlineData(777)]
    public void A_toll_gate_never_blocks_the_only_path(int seed)
    {
        var map = DungeonMap.Generate(new Random(seed));
        foreach (var node in map.Nodes.Where(n => n.Tolls.Count > 0))
            Assert.Contains(node.Next, id => !node.Tolls.Contains(id));
    }
}
