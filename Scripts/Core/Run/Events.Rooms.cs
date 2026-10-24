using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>Special rooms found on the map: the Ember Forge, the Shrine of Pacts, the merchant caravan, the hunt,
/// and the Sealed Door with the secret boss behind it.</summary>
public static partial class Events
{
    public static readonly string[] SecretBoss = ["ember_wyrm"];

    /// <summary>A random hero ability that isn't upgraded yet (null if everything is).</summary>
    private static (HeroState hero, string ability)? Unupgraded(RunState run, Random rng)
    {
        var options = run.Party.SelectMany(h => h.AbilityIds.Append(h.Def.BasicAbility).Where(a => !h.Upgrades.Contains(a)).Select(a => (h, a))).ToList();
        return options.Count == 0 ? null : options[rng.Next(options.Count)];
    }

    public static readonly IReadOnlyList<EventDef> Rooms = new List<EventDef>
    {
        new()
        {
            Id = "forge", Title = "The Ember Forge", Weight = 9,
            Text = "An old Keeper forge still glows here, fed by a thread of the Last Ember. The anvil is warm. The hammer waits for a hand.",
            Choices =
            [
                new()
                {
                    Label = "Temper a technique", Hint = "Pay 50 gold: a random ability of a random hero is upgraded.",
                    Available = r => r.Gold >= 50 && r.Party.Any(h => h.AbilityIds.Append(h.Def.BasicAbility).Any(a => !h.Upgrades.Contains(a))),
                    Apply = (run, rng) =>
                    {
                        if (Unupgraded(run, rng) is not { } pick) return "There is nothing left to temper.";
                        run.Gold -= 50;
                        run.Upgrade(pick.hero.Id, pick.ability);
                        return $"Sparks fly. [color=#ffd76b]{pick.hero.Def.Name.Split(' ')[0]}'s {Database.Ability(pick.ability).Name}[/color] is upgraded: {Database.Ability(pick.ability).UpgradeSummary}";
                    },
                },
                new()
                {
                    Label = "Quench in ember-light", Hint = "Free: one random hero deals +10% damage for the rest of the run, but loses 5 HP.",
                    Apply = (run, rng) =>
                    {
                        var hero = run.Party[rng.Next(run.Party.Count)];
                        hero.BonusPowerPct += 10;
                        hero.Hp = Math.Max(1, hero.Hp - 5);
                        return $"{hero.Def.Name.Split(' ')[0]} plunges their weapon into the ember. It burns their hands, and hums with power.";
                    },
                },
                new() { Label = "Warm your hands and move on", Hint = "Every hero recovers 10% HP.", Apply = (run, _) => { run.HealParty(10); return "For a moment, the cold of the vault is gone."; } },
            ],
        },
        new()
        {
            Id = "pact_shrine", Title = "The Shrine of Pacts", Weight = 7, Requires = r => (r.Node?.Region ?? 0) >= 1,
            Text = "A shrine of black glass. Something behind it speaks in your own voice: \"Power, freely given. Only a small price. Always a small price.\"",
            Choices =
            [
                new()
                {
                    Label = "Accept a pact", Hint = "Gain a random pact relic: a strong blessing with a real curse (you can read it after).",
                    Available = r => Database.PactRelics.Any(p => !r.Relics.Contains(p.Id)),
                    Apply = (run, rng) =>
                    {
                        var pool = Database.PactRelics.Where(p => !run.Relics.Contains(p.Id)).ToList();
                        var relic = pool[rng.Next(pool.Count)];
                        run.Relics.Add(relic.Id);
                        return $"The glass cracks and something cold settles in your pack: [color={relic.ColorHex}]{relic.Name}[/color]. {relic.Description} [color=#ff6b6b]Curse: {relic.Curse}[/color]";
                    },
                },
                new() { Label = "Pray for mercy", Hint = "Every hero recovers 20% HP.", Apply = (run, _) => { run.HealParty(20); return "The voice laughs, but the shrine's cold light soothes your wounds."; } },
                new()
                {
                    Label = "Smash the shrine", Hint = "+45 gold from the shards, but every hero loses 5 HP.",
                    Apply = (run, _) => { run.Gold += 45; run.Stats.GoldEarned += 45; return "Black glass explodes everywhere. " + DamageParty(run, 5); },
                },
            ],
        },
        new()
        {
            Id = "caravan", Title = "The Lantern Caravan", Weight = 8,
            Text = "A train of mules in lantern-lit harness, led by a merchant who swears the vault is \"perfectly safe, for customers\".",
            Choices =
            [
                new()
                {
                    Label = "Buy a sealed crate", Hint = "Pay 55 gold: a random piece of equipment (as good as this depth allows).",
                    Available = r => r.Gold >= 55,
                    Apply = (run, rng) =>
                    {
                        run.Gold -= 55;
                        var item = Database.RandomEquipment(rng, run.MaxTier);
                        run.Equipment.Add(item.Id);
                        return $"Inside the straw: [color=#ffc94a]{item.Name}[/color] ({item.Rarity}). {item.Description}";
                    },
                },
                new()
                {
                    Label = "Buy a potion bundle", Hint = "Pay 35 gold: two random consumables.",
                    Available = r => r.Gold >= 35 && r.Consumables.Count < RunState.MaxConsumables,
                    Apply = (run, rng) =>
                    {
                        run.Gold -= 35;
                        var got = new List<string>();
                        for (int i = 0; i < 2 && run.Consumables.Count < RunState.MaxConsumables; i++)
                        {
                            var c = Database.RandomConsumable(rng);
                            run.Consumables.Add(c.Id);
                            got.Add(c.Name);
                        }
                        return $"Wrapped in cloth: {string.Join(" and ", got)}.";
                    },
                },
                new()
                {
                    Label = "Buy the curiosity", Hint = "Pay 110 gold: a random relic.",
                    Available = r => r.Gold >= 110,
                    Apply = (run, rng) =>
                    {
                        var relic = Database.RandomRelic(rng, run.Relics);
                        if (relic is null) return "\"Sold out, I'm afraid.\"";
                        run.Gold -= 110;
                        run.Relics.Add(relic.Id);
                        return $"A velvet box: [color={relic.ColorHex}]{relic.Name}[/color]. {relic.Description}";
                    },
                },
                new() { Label = "Just browse", Hint = "Nothing happens.", Apply = (_, _) => "\"Come back when you're richer,\" he calls after you." },
            ],
        },
        new()
        {
            Id = "hunt", Title = "The Hunt", Weight = 8, Requires = r => (r.Node?.Region ?? 0) >= 1,
            Text = "Fresh tracks, deep gouges in the stone, and a trail of bones. Something big makes its lair nearby. Something that hoards.",
            Choices =
            [
                new()
                {
                    Label = "Track the beast", Hint = "Fight one of this region's elites. Elite reward: a relic is always among the choices.",
                    Apply = (run, rng) =>
                    {
                        var elites = run.NodeRegion.Elites;
                        return Fight(run, elites[rng.Next(elites.Length)], "You follow the trail into its lair. It was waiting.");
                    },
                },
                new()
                {
                    Label = "Raid the lair while it sleeps", Hint = "+35 gold, but a 50% chance it wakes: every hero loses 8 HP.",
                    Apply = (run, rng) =>
                    {
                        run.Gold += 35;
                        run.Stats.GoldEarned += 35;
                        return rng.Next(2) == 0 ? "You slip away with a fistful of coins." : "It wakes! You escape with the gold, barely. " + DamageParty(run, 8);
                    },
                },
                new() { Label = "Leave it be", Hint = "Nothing happens.", Apply = (_, _) => "Some things are better left sleeping." },
            ],
        },
        new()
        {
            Id = "sealed_door", Title = "The Sealed Door", Weight = 5, Unique = true, Requires = r => (r.Node?.Region ?? 0) >= 2,
            Text = "A door of red iron, sealed with seven Keeper sigils. Heat pours through the cracks, and behind it something breathes, slow and enormous. An inscription: \"HERE SLEEPS WHAT THE EMBER MADE AND COULD NOT UNMAKE.\"",
            Choices =
            [
                new()
                {
                    Label = "Break the seals", Hint = "Pay 80 gold to face the secret boss: the Ember Wyrm. A brutal fight, with an elite reward and a place in your records.",
                    Available = r => r.Gold >= 80,
                    Apply = (run, _) =>
                    {
                        run.Gold -= 80;
                        return Fight(run, SecretBoss, "The sigils shatter one by one. The door swings open, and two burning eyes open in the dark.");
                    },
                },
                new() { Label = "Leave it sealed", Hint = "Nothing happens. You can't come back.", Apply = (_, _) => "You step away from the heat. Some doors stay shut." },
            ],
        },
    };
}
