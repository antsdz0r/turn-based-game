using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TBRPG.Core;
using Xunit;

namespace TBRPG.Tests;

/// <summary>
/// Not a pass/fail test: plays many fights with a greedy bot and writes Tests/balance_report.md
/// (win rates per boss and elite, weapon comparison, ability usage). Use it to spot outliers:
/// a weapon far above the others, a boss nobody beats, an ability the bot never picks.
/// </summary>
public class BalanceReport
{
    private const int Seeds = 12;

    private static IEnumerable<string[]> Parties()
    {
        var ids = Database.Heroes.Select(h => h.Id).ToList();
        for (int i = 0; i < ids.Count; i++) yield return [ids[i], ids[(i + 2) % ids.Count], ids[(i + 4) % ids.Count]];
    }

    [Fact]
    [Trait("Category", "Balance")]
    public void Write_balance_report()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Ashenbound balance report");
        sb.AppendLine();
        sb.AppendLine($"Greedy bot (see TestKit.GreedyTurn: no parries, simple threat response), {Seeds} seeds per party, {Parties().Count()} parties. No relics or gear unless noted.");
        sb.AppendLine("Hero level = the level a party usually has when it reaches that region.");
        sb.AppendLine();

        var abilityStats = new Dictionary<string, (int uses, int damage)>();

        sb.AppendLine("## Bosses and elites");
        sb.AppendLine("Party HP left = average share of the party's max HP still standing after a win (attrition matters in a roguelite).");
        sb.AppendLine();
        sb.AppendLine("| Region | Fight | Hero level | Win rate | Avg rounds | Party HP left |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var region in Database.Regions)
        {
            int level = Math.Min(HeroState.MaxLevel, 2 + region.Act * 2);
            var fights = region.Elites.Select(e => ("Elite: " + string.Join("+", e), e, 1))
                .Append(("Boss: " + string.Join("+", region.Boss), region.Boss, 3));
            foreach (var (label, ids, localFloor) in fights)
            {
                int wins = 0, total = 0, rounds = 0;
                double hpLeft = 0;
                foreach (var party in Parties())
                    for (int seed = 0; seed < Seeds; seed++)
                    {
                        var run = TestKit.LevelledRun(level, seed + 1, party);
                        var battle = TestKit.NewBattle(ids, region.Act * DungeonMap.FloorsPerRegion + localFloor, seed * 97 + 5, run);
                        if (TestKit.PlayGreedy(battle, abilityStats))
                        {
                            wins++;
                            hpLeft += battle.Heroes.Sum(h => h.Hp) / (double)battle.Heroes.Sum(h => h.MaxHp);
                        }
                        rounds += battle.Round;
                        total++;
                    }
                string hpText = wins == 0 ? "-" : $"{100 * hpLeft / wins:0}%";
                sb.AppendLine($"| {region.Name} | {label} | {level} | {100 * wins / total}% | {rounds / (double)total:0.0} | {hpText} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Weapons (first hero holds the weapon, Storm Spire hard fights and elites, level 6)");
        sb.AppendLine("| Weapon | Win rate | Avg rounds | Avg damage dealt |");
        sb.AppendLine("|---|---|---|---|");
        var weapons = Database.AllItems.Where(i => i.Kind == ItemKind.Weapon).OrderBy(i => i.Tier).ToList();
        foreach (var weapon in weapons.Cast<ItemDef?>().Prepend(null))
        {
            int wins = 0, total = 0, rounds = 0, damage = 0;
            foreach (var party in Parties())
                for (int seed = 0; seed < Seeds / 2; seed++)
                    foreach (var ids in Database.Region(3).Hard.Concat(Database.Region(3).Elites))
                    {
                        var run = TestKit.LevelledRun(6, seed + 1, party);
                        run.Party[0].Weapon = weapon?.Id;
                        var battle = TestKit.NewBattle(ids, 13, seed * 31 + 3, run);
                        if (TestKit.PlayGreedy(battle)) wins++;
                        rounds += battle.Round;
                        damage += battle.DamageDealt;
                        total++;
                    }
            sb.AppendLine($"| {weapon?.Name ?? "(none)"}{(weapon?.WeaponEffect is not null ? " *" : "")} | {100 * wins / total}% | {rounds / (double)total:0.0} | {damage / total} |");
        }
        sb.AppendLine("\\* weapon family with a mechanic");

        sb.AppendLine();
        sb.AppendLine("## Abilities picked by the greedy bot (all boss/elite fights above)");
        sb.AppendLine("Abilities with very few uses may be too weak or too situational; very high damage per use may be too strong.");
        sb.AppendLine("Damage per use counts direct hits only: poison, bleed and burn ticks, marks and heals show as 0 here.");
        sb.AppendLine("| Hero.ability | Uses | Avg damage per use |");
        sb.AppendLine("|---|---|---|");
        var allAbilities = Database.Heroes.SelectMany(h => h.Abilities.Prepend(h.BasicAbility).Select(a => $"{h.Id}.{a}"));
        foreach (var key in allAbilities.OrderBy(k => abilityStats.GetValueOrDefault(k).uses))
        {
            var (uses, dmg) = abilityStats.GetValueOrDefault(key);
            sb.AppendLine($"| {key} | {uses} | {(uses == 0 ? "-" : (dmg / (double)uses).ToString("0.0"))} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Party coverage (every 3-hero party)");
        sb.AppendLine("Does the party have an answer to armor (armor-ignoring or elemental damage), to a big telegraphed hit");
        sb.AppendLine("(shield, heal, taunt or protect, beyond Defend), and to back-row threats (a non-melee attack)?");
        var heroIds = Database.Heroes.Select(h => h.Id).ToList();
        int parties = 0;
        var gaps = new List<string>();
        for (int a = 0; a < heroIds.Count; a++)
            for (int b = a + 1; b < heroIds.Count; b++)
                for (int c = b + 1; c < heroIds.Count; c++)
                {
                    parties++;
                    var party = new[] { heroIds[a], heroIds[b], heroIds[c] };
                    var missing = Coverage.Missing(party);
                    if (missing.Count > 0) gaps.Add($"| {string.Join(" + ", party)} | {string.Join(", ", missing)} |");
                }
        sb.AppendLine($"{parties - gaps.Count} of {parties} parties have all three answers.");
        if (gaps.Count > 0)
        {
            sb.AppendLine("| Party | Missing |");
            sb.AppendLine("|---|---|");
            foreach (var gap in gaps) sb.AppendLine(gap);
        }

        string dir = FindTestsDir();
        File.WriteAllText(Path.Combine(dir, "balance_report.md"), sb.ToString());
    }

    private static string FindTestsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TBRPG.Tests.csproj"))) dir = dir.Parent;
        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
