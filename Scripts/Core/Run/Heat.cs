using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>Difficulty levels unlocked by winning. Each level includes all lower ones.</summary>
public static class Heat
{
    public const int Max = 5;

    public static readonly IReadOnlyList<string> Modifiers = new List<string>
    {
        "Enemies carry modifiers (Frenzied, Thorned, Splitting...) twice as often, from the first region on.",
        "Enemies deal +10% damage. Elites and the boss gain +1 break pip.",
        "Start with 30 less gold. Shop prices +20%.",
        "Rest sites heal 25% instead of 35%.",
        "Enemies start every battle with a 5 Shield.",
    };

    public static string Describe(int heat) => heat <= 0
        ? "Heat 0: the standard descent."
        : string.Join("\n", Modifiers.Take(heat).Select((m, i) => $"Heat {i + 1}: {m}"));
}
