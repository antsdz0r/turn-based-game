using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public static partial class Database
{
    /// <summary>
    /// Cross-checks every id that content refers to (encounters, summons, abilities, traits, unlocks,
    /// event fights). Returns a list of problems; empty means the content is consistent.
    /// Run at startup (logged) and by the unit tests.
    /// </summary>
    public static List<string> Validate()
    {
        var problems = new List<string>();
        void Enemy(string id, string where)
        {
            if (!HasEnemy(id)) problems.Add($"{where}: unknown enemy id '{id}'");
        }

        foreach (var region in Regions)
        {
            foreach (var group in region.Easy.Concat(region.Hard).Concat(region.Elites))
                foreach (var id in group) Enemy(id, $"Region '{region.Name}' encounter");
            foreach (var id in region.Boss) Enemy(id, $"Region '{region.Name}' boss");
            if (region.Boss.All(HasEnemy) && !region.Boss.Any(id => EnemyDefinition(id).IsBoss))
                problems.Add($"Region '{region.Name}': boss encounter has no boss");
        }

        foreach (var def in AllEnemies)
        {
            foreach (var move in def.Moves.Concat(def.Phase2Moves ?? Enumerable.Empty<EnemyMoveDef>()))
                if (move.SummonId is { } summon) Enemy(summon, $"Enemy '{def.Id}' move '{move.Id}'");
            if (def.ProtectedBy is { } guardian) Enemy(guardian, $"Enemy '{def.Id}' ward");
            if (def.Moves.Count == 0) problems.Add($"Enemy '{def.Id}' has no moves");
        }

        foreach (var hero in Heroes)
            foreach (var id in hero.Abilities.Prepend(hero.BasicAbility))
                if (!AbilityTable.ContainsKey(id)) problems.Add($"Hero '{hero.Id}': unknown ability '{id}'");

        foreach (var alt in Mastery.Alternatives)
        {
            if (!AbilityTable.ContainsKey(alt.AbilityId)) problems.Add($"Mastery: unknown ability '{alt.AbilityId}'");
            if (!Hero(alt.HeroId).Abilities.Contains(alt.Replaces)) problems.Add($"Mastery: '{alt.HeroId}' has no ability '{alt.Replaces}' to replace");
        }
        foreach (var hero in Heroes)
            if (Mastery.Costume(hero.Id) is null) problems.Add($"Mastery: hero '{hero.Id}' has no costume");

        foreach (var syn in Synergies)
        {
            if (!AbilityTable.ContainsKey(syn.AbilityId)) problems.Add($"Synergy: unknown ability '{syn.AbilityId}'");
            if (Heroes.All(h => h.Id != syn.HeroA) || Heroes.All(h => h.Id != syn.HeroB)) problems.Add($"Synergy '{syn.AbilityId}': unknown hero");
        }

        foreach (var trait in AllTraits)
            if (Heroes.All(h => h.Id != trait.HeroId)) problems.Add($"Trait '{trait.Id}': unknown hero '{trait.HeroId}'");

        foreach (var unlock in Meta.HeroUnlocks)
        {
            if (Heroes.All(h => h.Id != unlock.HeroId)) problems.Add($"Unlock: unknown hero '{unlock.HeroId}'");
            Enemy(unlock.BossId, $"Unlock for '{unlock.HeroId}'");
        }

        foreach (var def in AllEnemies)
            if (def.RegrowFrom is { } feeder) Enemy(feeder, $"Enemy '{def.Id}' regrowth");

        foreach (var key in MiddleRegions.Append("crypt").Append("cinder"))
            if (Regions.All(r => r.Key != key)) problems.Add($"Region pool: unknown region '{key}'");

        foreach (var feat in Feats.All)
            if (feat.UnlocksItem is { } item && !HasItem(item)) problems.Add($"Feat '{feat.Id}': unknown item '{item}'");
        foreach (var id in Feats.All.GroupBy(f => f.Id).Where(g => g.Count() > 1).Select(g => g.Key))
            problems.Add($"Feat '{id}' is defined twice");

        foreach (var fight in Events.EventFights)
            foreach (var id in fight) Enemy(id, "Event fight");

        foreach (var item in AllItems)
            if (item.WeaponEffect is { } effect && !WeaponEffects.Contains(effect))
                problems.Add($"Item '{item.Id}': unknown weapon effect '{effect}'");

        foreach (var effect in WeaponEffects)
            if (string.IsNullOrEmpty(WeaponEffectText(effect))) problems.Add($"Weapon effect '{effect}' has no description");
        foreach (var effect in WeaponEffects)
            if (AllItems.All(i => i.WeaponEffect != effect)) problems.Add($"Weapon effect '{effect}' is used by no item");

        return problems;
    }
}
