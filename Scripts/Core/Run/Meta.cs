using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>A permanent upgrade bought with Ember Shards at the Ember Shrine. Costs[i] buys level i+1.</summary>
public sealed record UnlockDef(string Id, string Name, string Description, int[] Costs)
{
    public int MaxLevel => Costs.Length;
}

/// <summary>A hero that has to be unlocked: by defeating a boss, or by buying them at the shrine.</summary>
public sealed record HeroUnlock(string HeroId, string BossId, string Condition, int Shards);

/// <summary>Everything that carries over between runs: Ember Shards, shrine upgrades and hero unlocks.</summary>
public static class Meta
{
    public static readonly IReadOnlyList<UnlockDef> Upgrades = new List<UnlockDef>
    {
        new("purse", "Deeper Purse", "Start every run with +25 gold per level.", [20, 40, 60]),
        new("satchel", "Packed Satchel", "Start with an extra Healing Potion per level.", [25, 50]),
        new("gift", "Keeper's Gift", "Start every run with a random relic.", [80]),
        new("veterans", "Seasoned Heroes", "Your heroes start at level 2 (and pick a trait right away).", [100]),
        new("haggler", "Silver Tongue", "Shop prices and reward rerolls are 15% cheaper.", [60]),
        new("ember_heart", "Ember Heart", "Heroes start every run with +5 max HP per level.", [40, 80]),
    };

    public static readonly IReadOnlyList<HeroUnlock> HeroUnlocks = new List<HeroUnlock>
    {
        new("berserker", "colossus", "Defeat the Bone Colossus", 60),
        new("hexer", "matriarch", "Defeat the Drowned Matriarch", 90),
    };

    public static int Level(Profile p, string id) => p.Upgrades.GetValueOrDefault(id);

    public static bool IsHeroUnlocked(Profile p, string heroId)
    {
        var unlock = HeroUnlocks.FirstOrDefault(u => u.HeroId == heroId);
        return unlock is null || p.BossesDefeated.Contains(unlock.BossId) || p.Upgrades.ContainsKey("hero_" + heroId);
    }

    public static int NextCost(Profile p, UnlockDef u) => Level(p, u.Id) >= u.MaxLevel ? -1 : u.Costs[Level(p, u.Id)];

    public static bool Buy(Profile p, UnlockDef u)
    {
        int cost = NextCost(p, u);
        if (cost < 0 || p.Shards < cost) return false;
        p.Shards -= cost;
        p.Upgrades[u.Id] = Level(p, u.Id) + 1;
        return true;
    }

    public static bool BuyHero(Profile p, HeroUnlock u)
    {
        if (IsHeroUnlocked(p, u.HeroId) || p.Shards < u.Shards) return false;
        p.Shards -= u.Shards;
        p.Upgrades["hero_" + u.HeroId] = 1;
        return true;
    }

    /// <summary>Shards earned by a finished run.</summary>
    public static int ShardsFor(RunState run, bool won)
    {
        int shards = (run.Floor + 1) * 2 + run.Stats.ElitesSlain * 3 + run.BossesKilled * 10 + (won ? 30 + run.Heat * 5 : 0);
        if (run.Daily is not null) shards = (int)Math.Round(shards * 1.5);
        return shards;
    }

    /// <summary>Applies shrine upgrades to a brand-new (non-daily) run.</summary>
    public static void ApplyStartBonuses(RunState run, Profile p)
    {
        var rng = new Random(run.Seed ^ 0x5eed);
        run.Gold += 25 * Level(p, "purse");
        for (int i = 0; i < Level(p, "satchel"); i++) run.Consumables.Add("potion");
        if (Level(p, "gift") > 0 && Database.RandomRelic(rng, run.Relics) is { } relic) run.Relics.Add(relic.Id);
        run.Haggler = Level(p, "haggler") > 0;
        foreach (var hero in run.Party)
        {
            hero.BonusMaxHp += 5 * Level(p, "ember_heart");
            if (Level(p, "veterans") > 0 && hero.Level < 2) hero.AddXp(HeroState.XpForLevel(2) - hero.Xp);
            hero.Hp = hero.MaxHp;
        }
    }
}

/// <summary>Daily challenge: the same seed, party and twist for everyone on a given day.</summary>
public sealed record DailyDef(string Id, string Name, string Description);

public static class Daily
{
    public static readonly IReadOnlyList<DailyDef> Modifiers = new List<DailyDef>
    {
        new("glass", "Glass Party", "Heroes have 30% less max HP but deal 30% more damage."),
        new("frenzy", "Frenzy", "Enemies deal 30% more damage but have 30% less HP."),
        new("rich", "Gold Rush", "Start with +150 gold, but shop prices are 50% higher."),
        new("relics", "Relic Hoard", "Start with 3 random relics, but elites drop no relics."),
    };

    public static string Today => DateTime.Now.ToString("yyyy-MM-dd");
    public static int SeedFor(string date) => int.Parse(date.Replace("-", "")) * 31 + 7;
    public static DailyDef ModifierFor(string date) => Modifiers[SeedFor(date) % Modifiers.Count];

    /// <summary>Three of the five starting heroes, chosen by the seed, in a sensible formation.</summary>
    public static List<(string id, Row row)> PartyFor(string date)
    {
        var rng = new Random(SeedFor(date));
        var ids = new[] { "vanguard", "arcanist", "warden", "shadowblade", "ranger" }.OrderBy(_ => rng.Next()).Take(3).ToList();
        var party = ids.Select(id => (id, row: Database.Hero(id).PreferredRow)).ToList();
        if (party.All(p => p.row == Row.Back)) party[0] = (party[0].id, Row.Front);
        while (party.Count(p => p.row == Row.Front) > Battle.MaxFrontRow)
        {
            int i = party.FindLastIndex(p => p.row == Row.Front);
            party[i] = (party[i].id, Row.Back);
        }
        return party.OrderBy(p => p.row).ToList();
    }

    public static RunState Create(string date)
    {
        var mod = ModifierFor(date);
        var run = RunState.NewRun(PartyFor(date), SeedFor(date), heat: 1);
        run.Daily = mod.Id;
        run.DailyDate = date;
        var rng = new Random(SeedFor(date) ^ 77);
        switch (mod.Id)
        {
            case "glass":
                foreach (var h in run.Party) { h.BonusMaxHpPct -= 30; h.BonusPowerPct += 30; h.Hp = h.MaxHp; }
                break;
            case "rich": run.Gold += 150; break;
            case "relics":
                for (int i = 0; i < 3; i++)
                    if (Database.RandomRelic(rng, run.Relics) is { } r) run.Relics.Add(r.Id);
                break;
        }
        return run;
    }
}
