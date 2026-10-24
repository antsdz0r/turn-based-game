using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public sealed class MapNode
{
    public int Id { get; set; }
    /// <summary>Global floor index (0 .. Floors-1).</summary>
    public int Floor { get; set; }
    /// <summary>Index inside its floor (0 = top).</summary>
    public int Index { get; set; }
    public NodeType Type { get; set; }
    public List<int> Next { get; set; } = new();
    public bool Visited { get; set; }
    /// <summary>Golden rooms lie behind toll gates and give better rewards.</summary>
    public bool Golden { get; set; }
    /// <summary>Ids in Next that are behind a sealed gate: entering them costs a toll in gold.</summary>
    public List<int> Tolls { get; set; } = new();

    public int Region => DungeonMap.RegionOf(Floor);
    public int LocalFloor => Floor % DungeonMap.FloorsPerRegion;
}

/// <summary>
/// Three regions of five floors each; the fifth floor of every region is its boss. Inside a region,
/// every node links to 1-2 nodes on the next floor and every node has at least one incoming link,
/// so nothing is unreachable. Each boss links to every first-floor node of the next region.
/// </summary>
public sealed class DungeonMap
{
    public const int RegionCount = 5;
    public const int FloorsPerRegion = 4;
    public const int Floors = RegionCount * FloorsPerRegion;

    public List<MapNode> Nodes { get; set; } = new();

    public static int RegionOf(int floor) => Math.Clamp(floor / FloorsPerRegion, 0, RegionCount - 1);
    public static bool IsBossFloor(int floor) => floor % FloorsPerRegion == FloorsPerRegion - 1;
    public static bool IsFinalRegion(int region) => region == RegionCount - 1;
    public static int TollPrice(int region) => 25 + 15 * region;

    public IEnumerable<MapNode> OnFloor(int floor) => Nodes.Where(n => n.Floor == floor).OrderBy(n => n.Index);
    public int CountOnFloor(int floor) => Nodes.Count(n => n.Floor == floor);
    public MapNode Node(int id) => Nodes[id];
    public IEnumerable<MapNode> InRegion(int region) => Nodes.Where(n => n.Region == region);

    public static DungeonMap Generate(Random rng)
    {
        var map = new DungeonMap();
        var floors = new List<List<MapNode>>();
        for (int f = 0; f < Floors; f++)
        {
            int local = f % FloorsPerRegion;
            int count = IsBossFloor(f) ? 1 : local == 0 ? 3 : local == 1 ? rng.Next(3, 5) : rng.Next(2, 5);
            var floor = new List<MapNode>();
            for (int i = 0; i < count; i++)
            {
                var node = new MapNode { Id = map.Nodes.Count, Floor = f, Index = i };
                map.Nodes.Add(node);
                floor.Add(node);
            }
            floors.Add(floor);
        }

        for (int f = 0; f < Floors - 1; f++)
        {
            var a = floors[f];
            var b = floors[f + 1];
            foreach (var node in a)
            {
                if (IsBossFloor(f))
                {
                    foreach (var next in b) Link(node, next); // boss → every start of the next region
                    continue;
                }
                double p = (node.Index + 0.5) / a.Count;
                int nearest = Math.Clamp((int)(p * b.Count), 0, b.Count - 1);
                Link(node, b[nearest]);
                if (b.Count > 1 && rng.Next(100) < 45)
                {
                    int neighbor = nearest + (rng.Next(2) == 0 ? -1 : 1);
                    if (neighbor >= 0 && neighbor < b.Count) Link(node, b[neighbor]);
                }
            }
            foreach (var target in b)
            {
                if (a.Any(n => n.Next.Contains(target.Id))) continue;
                double q = (target.Index + 0.5) / b.Count;
                var source = a.OrderBy(n => Math.Abs((n.Index + 0.5) / a.Count - q)).First();
                Link(source, target);
            }
        }

        AssignTypes(floors, rng);
        AssignTolls(floors, rng);
        return map;
    }

    /// <summary>
    /// On forks, one branch may be sealed by a toll gate. The room behind it becomes golden. A gate is only
    /// placed where the node still has a free path, so a broke party can always continue.
    /// </summary>
    private static void AssignTolls(List<List<MapNode>> floors, Random rng)
    {
        for (int region = 0; region < RegionCount; region++)
        {
            int placed = 0;
            for (int local = 0; local < FloorsPerRegion - 2 && placed < 2; local++)
            {
                int f = region * FloorsPerRegion + local;
                foreach (var node in floors[f].OrderBy(_ => rng.Next()))
                {
                    if (placed >= 2 || node.Next.Count < 2 || rng.Next(100) >= 45) continue;
                    var targets = node.Next.Select(id => floors[f + 1].First(n => n.Id == id))
                        .Where(n => n.Type is not (NodeType.Boss or NodeType.Shop) && !n.Golden).ToList();
                    if (targets.Count == 0) continue;
                    var gated = targets[rng.Next(targets.Count)];
                    node.Tolls.Add(gated.Id);
                    gated.Golden = true;
                    placed++;
                }
            }
        }
    }

    private static void Link(MapNode from, MapNode to)
    {
        if (!from.Next.Contains(to.Id)) from.Next.Add(to.Id);
    }

    private static void AssignTypes(List<List<MapNode>> floors, Random rng)
    {
        NodeType Weighted(params (NodeType type, int weight)[] options)
        {
            int total = options.Sum(o => o.weight);
            int roll = rng.Next(total);
            foreach (var (type, weight) in options)
            {
                if (roll < weight) return type;
                roll -= weight;
            }
            return options[0].type;
        }

        void Guarantee(List<NodeType> allowedToReplace, List<MapNode> floor, NodeType type)
        {
            if (floor.Any(n => n.Type == type)) return;
            var candidates = floor.Where(n => allowedToReplace.Contains(n.Type)).ToList();
            if (candidates.Count == 0) candidates = floor;
            candidates[rng.Next(candidates.Count)].Type = type;
        }

        for (int f = 0; f < floors.Count; f++)
        {
            int local = f % FloorsPerRegion;
            foreach (var node in floors[f])
            {
                // Four floors per region: an opening fight, a crossroads, a camp, and the boss.
                node.Type = local switch
                {
                    0 => NodeType.Battle,
                    1 => Weighted((NodeType.Battle, 40), (NodeType.Event, 30), (NodeType.Elite, 15), (NodeType.Treasure, 15)),
                    2 => Weighted((NodeType.Rest, 40), (NodeType.Elite, 30), (NodeType.Event, 15), (NodeType.Battle, 15)),
                    _ => NodeType.Boss,
                };
            }
            if (local == 1) Guarantee(new List<NodeType> { NodeType.Event, NodeType.Elite, NodeType.Battle }, floors[f], NodeType.Treasure);
            if (local == 1) Guarantee(new List<NodeType> { NodeType.Event, NodeType.Elite, NodeType.Battle }, floors[f], NodeType.Shop);
            if (local == 2) Guarantee(new List<NodeType> { NodeType.Event, NodeType.Battle, NodeType.Elite }, floors[f], NodeType.Rest);
            // The crossroads keeps at least one plain fight to gain XP from.
            if (local == 1 && floors[f].Count > 2 && !floors[f].Any(n => n.Type == NodeType.Battle))
            {
                var spare = floors[f].Where(n => n.Type is not (NodeType.Shop or NodeType.Treasure)).ToList();
                if (spare.Count > 0) spare[rng.Next(spare.Count)].Type = NodeType.Battle;
            }
        }
    }
}
