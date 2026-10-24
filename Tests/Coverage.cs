using System.Collections.Generic;
using System.Linq;
using TBRPG.Core;

namespace TBRPG.Tests;

/// <summary>What tactical answers a hero's kit brings (used by the coverage test and the balance report).</summary>
public static class Coverage
{
    public const string Armor = "armor answer";
    public const string BigHit = "big-hit answer";
    public const string BackRow = "back-row answer";

    private static IEnumerable<AbilityDef> Kit(string heroId)
    {
        var def = Database.Hero(heroId);
        return def.Abilities.Prepend(def.BasicAbility).Select(Database.Ability);
    }

    public static bool AnswersArmor(string heroId) =>
        Kit(heroId).Any(a => a.Power > 0 && a.TargetsEnemies && (a.IgnoresArmor || a.Element != Element.Physical || a.PowerPerDebuff > 0));

    public static bool AnswersBigHit(string heroId) =>
        Kit(heroId).Any(a => a.Shield > 0 || a.Heal > 0 || a.Protect || a.LifestealPct > 0
                             || a.Applies.Any(s => s.Kind is StatusKind.Taunt or StatusKind.Regen or StatusKind.Weakened));

    public static bool AnswersBackRow(string heroId) =>
        Kit(heroId).Any(a => a.Power > 0 && a.TargetsEnemies && (!a.Melee || a.Target == TargetKind.AllEnemies));

    public static List<string> Missing(IReadOnlyList<string> party)
    {
        var missing = new List<string>();
        if (!party.Any(AnswersArmor)) missing.Add(Armor);
        if (!party.Any(AnswersBigHit)) missing.Add(BigHit);
        if (!party.Any(AnswersBackRow)) missing.Add(BackRow);
        return missing;
    }
}
