using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TBRPG.Core;

public sealed class RunStats
{
    public int EnemiesSlain { get; set; }
    public int DamageDealt { get; set; }
    public int GoldEarned { get; set; }
    public int BattlesWon { get; set; }
    public int ElitesSlain { get; set; }
    public double Seconds { get; set; }
}

public enum RewardKind { Equipment, Relic, Consumable, Upgrade, Gold }

public sealed class RewardOption
{
    public RewardKind Kind { get; set; }
    public string Id { get; set; } = "";
    public string? HeroId { get; set; }
    public int Amount { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class BattleResult
{
    public int Gold { get; set; }
    public int Xp { get; set; }
    public List<string> Lines { get; set; } = new();
    public List<RewardOption> Choices { get; set; } = new();
    public bool Elite { get; set; }
    public bool Golden { get; set; }
    public int Rerolls { get; set; }
}

public sealed class ShopEntry
{
    public RewardKind Kind { get; set; }
    public string Id { get; set; } = "";
    public int Price { get; set; }
    public bool Sold { get; set; }
}

/// <summary>Everything about one run. Serialized to JSON so a run can be continued.</summary>
public sealed class RunState
{
    public const int MaxConsumables = 5;
    public const int StartingGold = 60;

    public int Seed { get; set; }
    /// <summary>Difficulty 0-5. Each level adds a modifier (cumulative); see Heat.Describe.</summary>
    public int Heat { get; set; }
    public int Gold { get; set; } = StartingGold;
    public List<HeroState> Party { get; set; } = new();
    public List<string> Consumables { get; set; } = new();
    public List<string> Equipment { get; set; } = new();
    public List<string> Relics { get; set; } = new();
    public DungeonMap Map { get; set; } = new();
    public int CurrentNode { get; set; } = -1;
    public bool NodeCompleted { get; set; } = true;
    public RunStats Stats { get; set; } = new();
    public List<ShopEntry> Shop { get; set; } = new();
    public int ShopNode { get; set; } = -1;
    /// <summary>Cutscenes already shown this run (region intros, boss intros).</summary>
    public List<string> SeenScenes { get; set; } = new();
    /// <summary>Story flags set by events (event chains read them in later regions).</summary>
    public List<string> Flags { get; set; } = new();
    /// <summary>Unique events already seen this run.</summary>
    public List<string> SeenEvents { get; set; } = new();
    /// <summary>A fight started by an event or a mimic (replaces the node's normal encounter).</summary>
    public List<string>? ForcedEncounter { get; set; }
    public bool ForcedElite { get; set; }
    public int BossesKilled { get; set; }
    /// <summary>Daily challenge modifier id and date (null for normal runs).</summary>
    public string? Daily { get; set; }
    public string? DailyDate { get; set; }
    /// <summary>Silver Tongue shrine upgrade: cheaper shops and rerolls.</summary>
    public bool Haggler { get; set; }

    [JsonIgnore] public MapNode? Node => CurrentNode >= 0 ? Map.Node(CurrentNode) : null;
    [JsonIgnore] public int Floor => Node?.Floor ?? 0;
    /// <summary>The region the party is in (or about to enter, after clearing a boss).</summary>
    [JsonIgnore] public int CurrentRegion
    {
        get
        {
            if (Node is not { } node) return 0;
            bool advanced = NodeCompleted && node.Type == NodeType.Boss;
            return Math.Min(DungeonMap.RegionCount - 1, node.Region + (advanced ? 1 : 0));
        }
    }
    [JsonIgnore] public RegionDef CurrentRegionDef => Database.Region(CurrentRegion);
    /// <summary>Region of the room being played right now (used for backgrounds and music in battles, shops...).</summary>
    [JsonIgnore] public RegionDef NodeRegion => Database.Region(Node?.Region ?? 0);
    /// <summary>Equipment tier rewards can roll at the current depth.</summary>
    [JsonIgnore] public int MaxTier => Floor >= 10 ? 3 : Floor >= 4 ? 2 : 1;

    public static RunState NewRun(IReadOnlyList<(string id, Row row)> heroes, int seed, int heat = 0)
    {
        var run = new RunState { Seed = seed, Heat = heat, Gold = heat >= 3 ? StartingGold - 30 : StartingGold };
        int front = 0, back = 0;
        foreach (var (id, row) in heroes)
        {
            var hero = new HeroState { Id = id, Row = row, Slot = row == Row.Front ? front++ : back++ };
            hero.Hp = hero.MaxHp;
            run.Party.Add(hero);
        }
        run.Map = DungeonMap.Generate(new Random(seed));
        run.Consumables.Add("potion");
        run.Consumables.Add("tonic");
        return run;
    }

    /// <summary>Deterministic randomness per node, so reloading a run gives the same results.</summary>
    public Random NodeRng(int salt = 0) => new(unchecked(Seed * 7919 + (CurrentNode + 1) * 131 + salt));

    public HeroState Hero(string id) => Party.FirstOrDefault(h => h.Id == id) ?? Party[0];

    // ═════════════════════════ Traits ═════════════════════════

    public HeroState? HeroNeedingTrait => Party.FirstOrDefault(h => h.PendingTraits > 0);

    /// <summary>Up to 3 traits this hero can pick now (stable across reloads).</summary>
    public List<TraitDef> TraitChoices(HeroState hero)
    {
        var pool = Database.TraitsFor(hero.Id).Where(t => !hero.Traits.Contains(t.Id)).ToList();
        var rng = new Random(unchecked(Seed * 53 + Party.IndexOf(hero) * 7919 + hero.Traits.Count * 131));
        return pool.OrderBy(_ => rng.Next()).Take(3).ToList();
    }

    public bool PickTrait(HeroState hero, string traitId)
    {
        if (hero.PendingTraits <= 0 || TraitChoices(hero).All(t => t.Id != traitId)) return false;
        int maxBefore = hero.MaxHp;
        hero.Traits.Add(traitId);
        hero.PendingTraits--;
        if (!hero.HasTraitsLeft && hero.PendingTraits > 0) hero.PendingTraits = 0;
        hero.Hp = Math.Clamp(hero.Hp + Math.Max(0, hero.MaxHp - maxBefore), 1, hero.MaxHp);
        return true;
    }

    // ═════════════════════════ Map travel ═════════════════════════

    public IEnumerable<MapNode> AvailableNodes()
    {
        if (!NodeCompleted) return Enumerable.Empty<MapNode>();
        if (CurrentNode < 0) return Map.OnFloor(0);
        return Map.Node(CurrentNode).Next.Select(Map.Node);
    }

    public bool IsReachable(int nodeId) => AvailableNodes().Any(n => n.Id == nodeId);

    /// <summary>Gold needed to travel from the current room to this one (0 if the path is free).</summary>
    public int TollFor(int nodeId) =>
        CurrentNode >= 0 && Map.Node(CurrentNode).Tolls.Contains(nodeId) ? DungeonMap.TollPrice(Map.Node(nodeId).Region) : 0;

    public bool CanEnter(int nodeId) => IsReachable(nodeId) && Gold >= TollFor(nodeId);

    public bool Enter(int nodeId)
    {
        if (!CanEnter(nodeId)) return false;
        Gold -= TollFor(nodeId);
        CurrentNode = nodeId;
        NodeCompleted = false;
        Map.Node(nodeId).Visited = true;
        return true;
    }

    public void CompleteNode() => NodeCompleted = true;

    // ═════════════════════════ Battles ═════════════════════════

    public BattleSetup CreateBattle()
    {
        var node = Node ?? Map.OnFloor(0).First();
        var rng = NodeRng(1);
        var ids = ForcedEncounter?.ToArray() ?? Database.PickEncounter(node.Type, node.Floor, rng);
        foreach (var hero in Party) if (hero.Hp <= 0) hero.Hp = 1;
        var enemies = Database.CreateEncounter(ids, node.Floor, Heat);
        if (ForcedElite) foreach (var e in enemies.Where(e => !e.IsBoss)) e.MaxHp = e.Hp = (int)Math.Round(e.MaxHp * 1.2);
        EnemyMods.Roll(enemies, node.Region, NodeRng(9));
        return new BattleSetup
        {
            Heroes = Party.Select(Database.CreateHero).ToList(),
            Enemies = enemies,
            Daily = Daily,
            Heat = Heat,
            Relics = new HashSet<string>(Relics),
            Consumables = Consumables,
            Floor = node.Floor,
            Seed = Seed + CurrentNode * 977 + (int)(Stats.Seconds * 10) % 1000,
        };
    }

    /// <summary>Applies a won battle: HP, gold, XP, levels, stats, and builds 3 reward choices.</summary>
    public BattleResult ApplyVictory(Battle battle)
    {
        var node = Node;
        bool elite = node?.Type == NodeType.Elite || ForcedElite;
        bool boss = node?.Type == NodeType.Boss;
        if (boss) BossesKilled++;
        ForcedEncounter = null;
        ForcedElite = false;
        var result = new BattleResult();
        var rng = NodeRng(2);

        foreach (var unit in battle.Heroes)
        {
            var hero = Hero(unit.DefId);
            hero.Hp = unit.IsAlive ? unit.Hp : 1;
            hero.Row = unit.Row;
            hero.Slot = unit.Slot;
            if (!unit.IsAlive) result.Lines.Add($"{unit.Name} is dragged back to their feet with 1 HP.");
        }

        int gold = battle.Enemies.Where(e => !e.Summoned).Sum(e => e.Enemy?.Gold ?? 0) + rng.Next(0, 8);
        if (Relics.Contains("gilded_purse")) gold = (int)Math.Round(gold * 1.3);
        bool golden = node?.Golden == true;
        if (golden)
        {
            gold = (int)Math.Round(gold * 1.5);
            result.Lines.Add("[color=#ffd76b]Golden room:[/color] +50% gold and a fourth reward to choose from.");
        }
        Gold += gold;
        result.Gold = gold;

        int xp = boss ? 45 : elite ? 25 : 14;
        if (Relics.Contains("scholars_lamp")) xp = (int)Math.Round(xp * 1.4);
        result.Xp = xp;
        foreach (var hero in Party)
        {
            int levels = hero.AddXp(xp);
            if (levels > 0)
                result.Lines.Add($"[color=#ffd76b]{hero.Def.Name} reached level {hero.Level}![/color] +{HeroState.HpPerLevel} max HP, +{HeroState.PowerPerLevel}% damage");
        }

        Stats.EnemiesSlain += battle.EnemiesSlain;
        Stats.DamageDealt += battle.DamageDealt;
        Stats.GoldEarned += gold;
        Stats.BattlesWon++;
        if (elite || boss) Stats.ElitesSlain++;

        if (boss && node is not null && !DungeonMap.IsFinalRegion(node.Region))
        {
            HealParty(40);
            var next = Database.Region(node.Region + 1);
            result.Lines.Add($"[color=#ffd76b]{Database.Region(node.Region).Name} cleared![/color] The party rests: +40% HP.");
            result.Lines.Add($"Ahead lies [color=#ffa040]{next.Name}[/color].");
        }

        result.Elite = elite || boss;
        result.Golden = golden;
        result.Choices = RollRewards(rng, (elite || boss) && Daily != "relics", golden ? 4 : 3);
        return result;
    }

    /// <summary>Gold to reroll the reward choices (gets pricier with each reroll).</summary>
    public int RerollPrice(BattleResult result) =>
        (int)Math.Round((15 + 5 * CurrentRegionOfNode + 10 * result.Rerolls) * (Haggler ? 0.85 : 1.0));
    [JsonIgnore] private int CurrentRegionOfNode => Node?.Region ?? 0;

    public bool RerollRewards(BattleResult result)
    {
        int price = RerollPrice(result);
        if (Gold < price) return false;
        Gold -= price;
        result.Rerolls++;
        result.Choices = RollRewards(NodeRng(40 + result.Rerolls), result.Elite, result.Golden ? 4 : 3);
        return true;
    }

    private List<RewardOption> RollRewards(Random rng, bool elite, int count = 3)
    {
        var choices = new List<RewardOption>();
        int maxTier = MaxTier;
        if (elite && Database.RandomRelic(rng, Relics) is { } relic) choices.Add(RelicOption(relic));

        int guard = 0;
        while (choices.Count < count && guard++ < 50)
        {
            int roll = rng.Next(100);
            RewardOption option;
            if (roll < 35) option = EquipmentOption(Database.RandomEquipment(rng, maxTier));
            else if (roll < 65) option = UpgradeOption(rng) ?? EquipmentOption(Database.RandomEquipment(rng, maxTier));
            else if (roll < 85) option = ConsumableOption(Database.RandomConsumable(rng));
            else if (Database.RandomRelic(rng, Relics) is { } r && rng.Next(100) < 40) option = RelicOption(r);
            else option = new RewardOption { Kind = RewardKind.Gold, Amount = 30 + rng.Next(25), Title = "Pouch of Embers", Description = "" };
            if (option.Kind == RewardKind.Gold) option.Description = $"Gain {option.Amount} gold.";
            if (choices.Any(c => c.Kind == option.Kind && c.Id == option.Id && c.HeroId == option.HeroId)) continue;
            choices.Add(option);
        }
        return choices;
    }

    public static RewardOption EquipmentOption(ItemDef item) => new()
    {
        Kind = RewardKind.Equipment, Id = item.Id, Title = item.Name,
        Description = $"{item.Kind}: {item.Description}",
    };

    public static RewardOption ConsumableOption(ItemDef item) => new()
    {
        Kind = RewardKind.Consumable, Id = item.Id, Title = item.Name, Description = item.Description,
    };

    public static RewardOption RelicOption(RelicDef relic) => new()
    {
        Kind = RewardKind.Relic, Id = relic.Id, Title = relic.Name, Description = "Relic: " + relic.Description,
    };

    /// <summary>A random not-yet-upgraded ability of a random hero, or null.</summary>
    public RewardOption? UpgradeOption(Random rng)
    {
        var options = Party.SelectMany(h => h.Def.Abilities.Append(h.Def.BasicAbility)
                .Where(a => !h.Upgrades.Contains(a))
                .Select(a => (hero: h, ability: a)))
            .ToList();
        if (options.Count == 0) return null;
        var (hero, abilityId) = options[rng.Next(options.Count)];
        var ability = Database.Ability(abilityId);
        return new RewardOption
        {
            Kind = RewardKind.Upgrade, Id = abilityId, HeroId = hero.Id,
            Title = $"{ability.Name}+",
            Description = $"Upgrade {hero.Def.Name}'s {ability.Name}: +35% effect, +1 status potency/duration, -1 cooldown.",
        };
    }

    public bool CanTake(RewardOption option) => option.Kind != RewardKind.Consumable || Consumables.Count < MaxConsumables;

    public bool ApplyReward(RewardOption option)
    {
        if (!CanTake(option)) return false;
        switch (option.Kind)
        {
            case RewardKind.Equipment: Equipment.Add(option.Id); break;
            case RewardKind.Consumable: Consumables.Add(option.Id); break;
            case RewardKind.Relic:
                if (!Relics.Contains(option.Id)) Relics.Add(option.Id);
                break;
            case RewardKind.Upgrade:
                var hero = Hero(option.HeroId ?? "");
                if (!hero.Upgrades.Contains(option.Id)) hero.Upgrades.Add(option.Id);
                break;
            case RewardKind.Gold:
                Gold += option.Amount;
                Stats.GoldEarned += option.Amount;
                break;
        }
        return true;
    }

    // ═════════════════════════ Equipment ═════════════════════════

    /// <summary>Equips an item from the bag; whatever was in that slot goes back to the bag.</summary>
    public bool Equip(string heroId, string itemId)
    {
        if (!Equipment.Contains(itemId) || !Database.HasItem(itemId)) return false;
        var item = Database.Item(itemId);
        if (!item.IsEquipment) return false;
        var hero = Hero(heroId);
        int oldMax = hero.MaxHp;
        var previous = hero.GetSlot(item.Slot);
        Equipment.Remove(itemId);
        if (previous is not null) Equipment.Add(previous);
        hero.SetSlot(item.Slot, itemId);
        AdjustHpAfterGearChange(hero, oldMax);
        return true;
    }

    public bool Unequip(string heroId, EquipSlot slot)
    {
        var hero = Hero(heroId);
        var current = hero.GetSlot(slot);
        if (current is null) return false;
        int oldMax = hero.MaxHp;
        hero.SetSlot(slot, null);
        Equipment.Add(current);
        AdjustHpAfterGearChange(hero, oldMax);
        return true;
    }

    private static void AdjustHpAfterGearChange(HeroState hero, int oldMax)
    {
        int newMax = hero.MaxHp;
        if (newMax > oldMax) hero.Hp += newMax - oldMax;
        hero.ClampHp();
        if (hero.Hp < 1) hero.Hp = 1;
    }

    /// <summary>Changes a hero's row on the map screen. At most 2 heroes in front; swaps if full.</summary>
    public void ToggleRow(string heroId)
    {
        var hero = Hero(heroId);
        if (hero.Row == Row.Back)
        {
            var front = Party.Where(h => h.Row == Row.Front && h != hero).ToList();
            if (front.Count >= Battle.MaxFrontRow) front.Last().Row = Row.Back;
            hero.Row = Row.Front;
        }
        else if (Party.Count(h => h.Row == Row.Front) > 1 || Party.Count == 1)
        {
            hero.Row = Row.Back;
        }
        else
        {
            // Nobody else is in front: swap with the first back-row hero.
            var swap = Party.FirstOrDefault(h => h != hero);
            if (swap is not null) swap.Row = Row.Front;
            hero.Row = Row.Back;
        }
        int f = 0, b = 0;
        foreach (var h in Party) h.Slot = h.Row == Row.Front ? f++ : b++;
    }

    // ═════════════════════════ Rest, treasure, shop ═════════════════════════

    public void HealParty(int percent)
    {
        foreach (var hero in Party)
            hero.Hp = Math.Min(hero.MaxHp, Math.Max(hero.Hp, 1) + hero.MaxHp * percent / 100);
    }

    /// <summary>Opens a chest: gold plus a piece of equipment or a relic. Returns description lines.</summary>
    public List<string> OpenTreasure()
    {
        var rng = NodeRng(3);
        var lines = new List<string>();
        bool golden = Node?.Golden == true;
        int gold = (25 + rng.Next(30)) * (golden ? 2 : 1);
        Gold += gold;
        Stats.GoldEarned += gold;
        lines.Add($"+{gold} gold");
        if (golden) lines.Add("[color=#ffd76b]A golden vault![/color] It holds a relic and double gold.");
        if ((golden || rng.Next(100) < 40) && Database.RandomRelic(rng, Relics) is { } relic)
        {
            Relics.Add(relic.Id);
            lines.Add($"Relic: [color={relic.ColorHex}]{relic.Name}[/color] — {relic.Description}");
        }
        else
        {
            var item = Database.RandomEquipment(rng, MaxTier);
            Equipment.Add(item.Id);
            lines.Add($"{item.Kind}: [color=#ffd76b]{item.Name}[/color] — {item.Description} (in your bag)");
        }
        return lines;
    }

    public List<ShopEntry> GetShop()
    {
        if (ShopNode == CurrentNode && Shop.Count > 0) return Shop;
        var rng = NodeRng(4);
        Shop = new List<ShopEntry>();
        int maxTier = MaxTier;
        var gear = Database.AllItems.Where(i => i.IsEquipment && i.Tier <= maxTier).OrderBy(_ => rng.Next()).Take(3);
        foreach (var item in gear) Shop.Add(new ShopEntry { Kind = RewardKind.Equipment, Id = item.Id, Price = Price(item.Price) });
        var potions = Database.AllItems.Where(i => !i.IsEquipment).OrderBy(_ => rng.Next()).Take(3);
        foreach (var item in potions) Shop.Add(new ShopEntry { Kind = RewardKind.Consumable, Id = item.Id, Price = Price(item.Price) });
        if (Database.RandomRelic(rng, Relics) is { } relic)
            Shop.Add(new ShopEntry { Kind = RewardKind.Relic, Id = relic.Id, Price = Price(relic.Price) });
        ShopNode = CurrentNode;
        return Shop;
    }

    public bool CanBuy(ShopEntry entry, out string reason)
    {
        reason = "";
        if (entry.Sold) { reason = "Sold out."; return false; }
        if (Gold < entry.Price) { reason = "Not enough gold."; return false; }
        if (entry.Kind == RewardKind.Consumable && Consumables.Count >= MaxConsumables) { reason = "Your satchel is full (5)."; return false; }
        if (entry.Kind == RewardKind.Relic && Relics.Contains(entry.Id)) { reason = "Already owned."; return false; }
        return true;
    }

    public bool Buy(ShopEntry entry)
    {
        if (!CanBuy(entry, out _)) return false;
        Gold -= entry.Price;
        entry.Sold = true;
        switch (entry.Kind)
        {
            case RewardKind.Equipment: Equipment.Add(entry.Id); break;
            case RewardKind.Consumable: Consumables.Add(entry.Id); break;
            case RewardKind.Relic: Relics.Add(entry.Id); break;
        }
        return true;
    }

    public const int ShopHealPrice = 30;
    public const int ShopUpgradePrice = 70;

    private int Price(int basePrice) =>
        (int)Math.Round(basePrice * (Heat >= 3 ? 1.2 : 1.0) * (Node?.Golden == true ? 0.75 : 1.0)
                        * (Haggler ? 0.85 : 1.0) * (Daily == "rich" ? 1.5 : 1.0));
    [JsonIgnore] public int HealServicePrice => Price(ShopHealPrice);
    [JsonIgnore] public int UpgradeServicePrice => Price(ShopUpgradePrice);
    [JsonIgnore] public int RestHealPct => (Heat >= 4 ? 25 : 35) + (Node?.Golden == true ? 20 : 0);

    public bool Upgrade(string heroId, string abilityId)
    {
        var hero = Hero(heroId);
        if (hero.Upgrades.Contains(abilityId)) return false;
        if (abilityId != hero.Def.BasicAbility && !hero.Def.Abilities.Contains(abilityId)) return false;
        hero.Upgrades.Add(abilityId);
        return true;
    }

    // ═════════════════════════ Save ═════════════════════════

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>Returns null for missing or corrupted data instead of throwing.</summary>
    public static RunState? FromJson(string json)
    {
        try
        {
            var run = JsonSerializer.Deserialize<RunState>(json, JsonOptions);
            if (run is null || run.Party.Count == 0 || run.Map.Nodes.Count == 0) return null;
            if (run.Party.Any(h => Database.Heroes.All(d => d.Id != h.Id))) return null;
            // Saves from before regions existed have a shorter map: they can't be continued.
            if (run.Map.Nodes.Max(n => n.Floor) != DungeonMap.Floors - 1) return null;
            if (run.CurrentNode >= run.Map.Nodes.Count) return null;
            return run;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
