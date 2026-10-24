using System.Collections.Generic;

namespace TBRPG.Core;

/// <summary>Per-hero numbers for the run summary.</summary>
public sealed partial class Battle
{
    public Dictionary<string, int> DamageByHero { get; } = new();
    public int BestHit { get; private set; }
    public string? BestHitBy { get; private set; }
}
