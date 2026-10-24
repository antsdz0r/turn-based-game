using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TBRPG.Core;

/// <summary>A weekly rule twist. Three are drawn each week; together they change how the run plays.</summary>
public sealed record MutatorDef(string Id, string Name, string Description, string ColorHex);

/// <summary>
/// Weekly challenge: one seed, one party (drawn from every hero, locked ones included) and three mutators
/// for the whole week. Mutators are read by the run (enemy setup, gold, shops) and by the battle rules.
/// </summary>
public static class Weekly
{
    public static readonly IReadOnlyList<MutatorDef> Mutators = new List<MutatorDef>
    {
        new("splitters", "Splitting Plague", "Every ordinary enemy is Splitting: it breaks into smaller copies when killed.", "#b3d05a"),
        new("volatile", "Powder Keg", "Every ordinary enemy is Volatile: it explodes on the front row when killed.", "#ff8a2c"),
        new("omens", "Ill Omens", "Every fight has an omen.", "#d9c8ff"),
        new("bounties", "Bounty Season", "Every fight has a bounty, and bounties pay double.", "#ffd76b"),
        new("glass", "Glass Party", "Heroes have 30% less max HP but deal 30% more damage.", "#9fd8ff"),
        new("frenzy", "Frenzy", "Enemies deal 30% more damage but have 30% less HP.", "#ff6a4a"),
        new("brittle", "Brittle Bones", "Enemy break meters are 1 pip shorter, but enemies have 15% more HP.", "#e8dcc0"),
        new("ironclad", "Ironclad", "Every enemy starts each fight behind a 6 Shield.", "#b0aec8"),
        new("war_drums", "War Drums", "Every enemy is Empowered for the first two rounds.", "#a63d25"),
        new("pauper", "Pauper's Road", "Battles pay half gold, but shops are 40% cheaper.", "#c9b26b"),
    };

    public static MutatorDef? Get(string id) => Mutators.FirstOrDefault(m => m.Id == id);

    /// <summary>ISO week id like "2026-W43".</summary>
    public static string IdFor(DateTime date) => $"{ISOWeek.GetYear(date)}-W{ISOWeek.GetWeekOfYear(date):00}";
    public static string ThisWeek => IdFor(DateTime.Now);
    public static int SeedFor(string week) => unchecked(week.GetHashCodeStable() * 31 + 101) & int.MaxValue;

    private static int GetHashCodeStable(this string s)
    {
        unchecked
        {
            int h = 23;
            foreach (char c in s) h = h * 31 + c;
            return h;
        }
    }

    /// <summary>Three mutators that never cancel each other out (Glass and Frenzy never share a week, nor the two death effects).</summary>
    public static List<string> MutatorsFor(string week)
    {
        var rng = new Random(SeedFor(week));
        var picked = new List<string>();
        foreach (var m in Mutators.OrderBy(_ => rng.Next()))
        {
            if (picked.Count == 3) break;
            if (m.Id == "glass" && picked.Contains("frenzy") || m.Id == "frenzy" && picked.Contains("glass")) continue;
            if (m.Id == "splitters" && picked.Contains("volatile") || m.Id == "volatile" && picked.Contains("splitters")) continue;
            picked.Add(m.Id);
        }
        return picked;
    }

    /// <summary>Three heroes from the whole roster (locked heroes too: the weekly is a chance to try them), in a sensible formation.</summary>
    public static List<(string id, Row row)> PartyFor(string week)
    {
        var rng = new Random(SeedFor(week) ^ 0x77);
        var ids = Database.Heroes.Select(h => h.Id).OrderBy(_ => rng.Next()).Take(3).ToList();
        var party = ids.Select(id => (id, row: Database.Hero(id).PreferredRow)).ToList();
        if (party.All(p => p.row == Row.Back)) party[0] = (party[0].id, Row.Front);
        while (party.Count(p => p.row == Row.Front) > Battle.MaxFrontRow)
        {
            int i = party.FindLastIndex(p => p.row == Row.Front);
            party[i] = (party[i].id, Row.Back);
        }
        return party.OrderBy(p => p.row).ToList();
    }

    public static RunState Create(string week)
    {
        var run = RunState.NewRun(PartyFor(week), SeedFor(week), heat: 1);
        run.Weekly = week;
        run.Mutators = MutatorsFor(week);
        if (run.Has("glass"))
            foreach (var h in run.Party) { h.BonusMaxHpPct -= 30; h.BonusPowerPct += 30; h.Hp = h.MaxHp; }
        return run;
    }

    /// <summary>Mutators applied to a fight's enemies when the run builds the battle.</summary>
    public static void ApplyToEnemies(RunState run, List<Combatant> enemies)
    {
        foreach (var e in enemies)
        {
            if (!e.IsBoss && !e.IsElite && e.Modifier is null)
            {
                if (run.Has("splitters")) EnemyMods.Apply(e, "splitting");
                if (run.Has("volatile")) EnemyMods.Apply(e, "volatile");
            }
            if (run.Has("brittle"))
            {
                e.BreakMax = e.Break = Math.Max(1, e.BreakMax - 1);
                e.MaxHp = e.Hp = (int)Math.Round(e.MaxHp * 1.15);
            }
        }
    }
}
