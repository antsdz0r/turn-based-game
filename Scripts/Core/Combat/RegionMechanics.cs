using System;
using System.Linq;

namespace TBRPG.Core;

/// <summary>Round-start mechanics of the newer regions' bosses.</summary>
public sealed partial class Battle
{
    /// <summary>Living allies that feed a regrowing boss (the Mother Bloom's seed pods).</summary>
    public int FeedersOf(Combatant boss) =>
        boss.Enemy?.RegrowFrom is { } id ? AliveEnemies.Count(e => e != boss && e.DefId == id) : 0;

    private void RegionRoundStart()
    {
        // Mother Bloom: every living seed pod heals her a slice of her max HP.
        foreach (var boss in AliveEnemies.Where(e => e.Enemy?.RegrowFrom is not null).ToList())
        {
            int pods = FeedersOf(boss);
            if (pods == 0 || boss.Hp >= boss.MaxHp) continue;
            int heal = Math.Max(1, boss.MaxHp * boss.Enemy!.RegrowPct * pods / 100);
            Emit(new TextEvent(boss, $"Fed by {pods} pod{(pods > 1 ? "s" : "")}", "#b3d05a"));
            Log($"  [color=#b3d05a]{pods} seed pod{(pods > 1 ? "s feed" : " feeds")} {boss.Name}.[/color]");
            HealUnit(boss, heal, fromHero: false);
        }
    }
}
