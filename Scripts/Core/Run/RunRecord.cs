using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>One finished run, kept in the profile's history (and shown as the end-of-run summary).</summary>
public sealed class RunRecord
{
    public string Date { get; set; } = "";
    public bool Won { get; set; }
    public int Heat { get; set; }
    public string? Daily { get; set; }
    public int Floor { get; set; }
    public double Seconds { get; set; }
    public List<string> Party { get; set; } = new();
    /// <summary>Region keys in the order the run visited them (only the ones reached).</summary>
    public List<string> Route { get; set; } = new();
    public int EnemiesSlain { get; set; }
    public int Damage { get; set; }
    public int Gold { get; set; }
    public int Bosses { get; set; }
    public int Combos { get; set; }
    public int Breaks { get; set; }
    public int BestHit { get; set; }
    public string? BestHitBy { get; set; }
    public string? Mvp { get; set; }
    public int MvpDamage { get; set; }
    public string? DefeatedBy { get; set; }
    public int Shards { get; set; }
    public List<string> Feats { get; set; } = new();

    public static RunRecord From(RunState run, bool won, int shards, List<string>? feats = null)
    {
        int act = DungeonMap.RegionOf(run.Floor);
        var mvp = run.Stats.HeroDamage.OrderByDescending(kv => kv.Value).FirstOrDefault();
        return new RunRecord
        {
            Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            Won = won,
            Heat = run.Heat,
            Daily = run.Daily,
            Floor = run.Floor + 1,
            Seconds = run.Stats.Seconds,
            Party = run.Party.Select(h => h.Id).ToList(),
            Route = Enumerable.Range(0, act + 1).Select(a => run.RegionAt(a).Key).ToList(),
            EnemiesSlain = run.Stats.EnemiesSlain,
            Damage = run.Stats.DamageDealt,
            Gold = run.Stats.GoldEarned,
            Bosses = run.BossesKilled,
            Combos = run.Stats.Combos,
            Breaks = run.Stats.Breaks,
            BestHit = run.Stats.BestHit,
            BestHitBy = run.Stats.BestHitBy,
            Mvp = mvp.Key,
            MvpDamage = mvp.Value,
            DefeatedBy = won ? null : run.Stats.DefeatedBy,
            Shards = shards,
            Feats = feats ?? new(),
        };
    }

    public string RouteText => string.Join(" → ", Route.Select(k => Database.RegionByKey(k).Name.Replace("The ", "")));
    public string TimeText => Seconds <= 0 ? "-" : $"{(int)(Seconds / 60)}:{(int)(Seconds % 60):00}";
}
