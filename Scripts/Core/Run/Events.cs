using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public sealed class EventChoice
{
    public required string Label { get; init; }
    public required string Hint { get; init; }
    public Func<RunState, bool> Available { get; init; } = _ => true;
    /// <summary>Applies the outcome and returns the text shown to the player.</summary>
    public required Func<RunState, Random, string> Apply { get; init; }
}

public sealed class EventDef
{
    /// <summary>Region the event can appear in (-1 = any).</summary>
    public int Region { get; init; } = -1;
    /// <summary>Relative chance among the events that can appear (chain events use a big weight).</summary>
    public int Weight { get; init; } = 10;
    /// <summary>Unique events appear at most once per run.</summary>
    public bool Unique { get; init; }
    public Func<RunState, bool>? Requires { get; init; }
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public required List<EventChoice> Choices { get; init; }
}

/// <summary>Map events: short choices with real consequences.</summary>
public static class Events
{
    private static string DamageParty(RunState run, int amount)
    {
        foreach (var hero in run.Party) hero.Hp = Math.Max(1, hero.Hp - amount);
        return $"Every hero loses {amount} HP.";
    }

    public static readonly IReadOnlyList<EventDef> All = new List<EventDef>
    {
        new()
        {
            Id = "shrine", Title = "The Ember Shrine",
            Text = "A shrine to the Last Ember still smolders in the dark. Old prayers are carved into the stone, and a bowl waits for an offering of blood.",
            Choices =
            [
                new()
                {
                    Label = "Offer blood", Hint = "Every hero loses 7 HP. Gain a random relic.",
                    Apply = (run, rng) =>
                    {
                        var text = DamageParty(run, 7);
                        if (Database.RandomRelic(rng, run.Relics) is { } relic)
                        {
                            run.Relics.Add(relic.Id);
                            return $"{text} The embers flare and leave behind [color={relic.ColorHex}]{relic.Name}[/color]: {relic.Description}";
                        }
                        run.Gold += 60;
                        return $"{text} The shrine has nothing left to give but 60 gold.";
                    },
                },
                new()
                {
                    Label = "Pray", Hint = "Heal every hero for 20% of max HP.",
                    Apply = (run, _) => { run.HealParty(20); return "Warmth spreads through the party. Wounds close."; },
                },
                new() { Label = "Leave", Hint = "Nothing happens.", Apply = (_, _) => "You leave the shrine to its silence." },
            ],
        },
        new()
        {
            Id = "cart", Title = "The Overturned Cart",
            Text = "A merchant's cart lies on its side, its owner long gone. Something skitters among the crates.",
            Choices =
            [
                new()
                {
                    Label = "Search the crates", Hint = "Gain 45 gold. 50% chance a hero is bitten (-10 HP).",
                    Apply = (run, rng) =>
                    {
                        run.Gold += 45;
                        run.Stats.GoldEarned += 45;
                        if (rng.Next(2) == 0)
                        {
                            var hero = run.Party[rng.Next(run.Party.Count)];
                            hero.Hp = Math.Max(1, hero.Hp - 10);
                            return $"+45 gold. A rat the size of a dog bites {hero.Def.Name} (-10 HP)!";
                        }
                        return "+45 gold, and nothing bites.";
                    },
                },
                new()
                {
                    Label = "Take only the satchel", Hint = "Gain a random consumable.",
                    Available = run => run.Consumables.Count < RunState.MaxConsumables,
                    Apply = (run, rng) =>
                    {
                        var item = Database.RandomConsumable(rng);
                        run.Consumables.Add(item.Id);
                        return $"Inside: {item.Name}. {item.Description}";
                    },
                },
                new() { Label = "Leave", Hint = "Nothing happens.", Apply = (_, _) => "You leave the cart behind." },
            ],
        },
        new()
        {
            Id = "statue", Title = "The Whispering Statue",
            Text = "A statue of a forgotten hero whispers techniques long lost. It asks for gold to remember them.",
            Choices =
            [
                new()
                {
                    Label = "Pay 40 gold", Hint = "Upgrade a random ability.",
                    Available = run => run.Gold >= 40,
                    Apply = (run, rng) =>
                    {
                        run.Gold -= 40;
                        if (run.UpgradeOption(rng) is { } option)
                        {
                            run.ApplyReward(option);
                            return $"The statue whispers. {option.Description}";
                        }
                        return "The statue has nothing left to teach you.";
                    },
                },
                new()
                {
                    Label = "Smash it", Hint = "Gain 30 gold, but every hero loses 4 HP.",
                    Apply = (run, _) =>
                    {
                        run.Gold += 30;
                        run.Stats.GoldEarned += 30;
                        return "+30 gold from the gilded base. " + DamageParty(run, 4);
                    },
                },
                new() { Label = "Leave", Hint = "Nothing happens.", Apply = (_, _) => "The whispers fade behind you." },
            ],
        },
        new()
        {
            Id = "pilgrim", Title = "The Wounded Pilgrim",
            Text = "A pilgrim of the Ember lies against the wall, bleeding. She clutches a sealed reliquary.",
            Choices =
            [
                new()
                {
                    Label = "Give a consumable", Hint = "Lose your first consumable. Gain a relic.",
                    Available = run => run.Consumables.Count > 0,
                    Apply = (run, rng) =>
                    {
                        var given = Database.Item(run.Consumables[0]).Name;
                        run.Consumables.RemoveAt(0);
                        if (Database.RandomRelic(rng, run.Relics) is { } relic)
                        {
                            run.Relics.Add(relic.Id);
                            return $"You give her your {given}. She presses the reliquary into your hands: [color={relic.ColorHex}]{relic.Name}[/color].";
                        }
                        run.Gold += 50;
                        return $"You give her your {given}. She pays you 50 gold.";
                    },
                },
                new()
                {
                    Label = "Give 25 gold", Hint = "She blesses you: heal 30%.",
                    Available = run => run.Gold >= 25,
                    Apply = (run, _) => { run.Gold -= 25; run.HealParty(30); return "She smiles and murmurs a blessing. The party feels stronger."; },
                },
                new() { Label = "Walk past", Hint = "Nothing happens.", Apply = (_, _) => "You walk on. Her prayers follow you." },
            ],
        },
        new()
        {
            Id = "armory", Title = "The Forgotten Armory",
            Text = "Rusted racks line the walls. Most of the weapons are ruined, but not all.",
            Choices =
            [
                new()
                {
                    Label = "Take the best piece", Hint = "Gain a random piece of equipment.",
                    Apply = (run, rng) =>
                    {
                        var item = Database.RandomEquipment(rng, run.MaxTier);
                        run.Equipment.Add(item.Id);
                        return $"You find [color=#ffd76b]{item.Name}[/color] ({item.Description}). It's in your bag.";
                    },
                },
                new()
                {
                    Label = "Sell scrap to a passing peddler", Hint = "Gain 35 gold.",
                    Apply = (run, _) => { run.Gold += 35; run.Stats.GoldEarned += 35; return "+35 gold."; },
                },
            ],
        },
        new()
        {
            Id = "mirror", Title = "The Mirror Pool",
            Text = "Still black water reflects your party, but the reflections move a heartbeat too late. One of them beckons.",
            Choices =
            [
                new()
                {
                    Label = "Step into the reflection", Hint = "A random hero gains 25 XP but loses 6 HP.",
                    Apply = (run, rng) =>
                    {
                        var hero = run.Party[rng.Next(run.Party.Count)];
                        int levels = hero.AddXp(25);
                        hero.Hp = Math.Max(1, hero.Hp - 6);
                        return $"{hero.Def.Name} trades places with their reflection for a moment (+25 XP, -6 HP)."
                               + (levels > 0 ? $" [color=#ffd76b]Level {hero.Level}![/color]" : "");
                    },
                },
                new()
                {
                    Label = "Drink from the pool", Hint = "Every hero heals 10 HP. 30% chance one hero loses 5 HP.",
                    Apply = (run, rng) =>
                    {
                        foreach (var hero in run.Party) hero.Hp = Math.Min(hero.MaxHp, hero.Hp + 10);
                        if (rng.Next(100) < 30)
                        {
                            var hero = run.Party[rng.Next(run.Party.Count)];
                            hero.Hp = Math.Max(1, hero.Hp - 5);
                            return $"The water is cold and sweet (+10 HP each), but {hero.Def.Name} sees something in it they won't talk about (-5 HP).";
                        }
                        return "The water is cold and sweet. Every hero recovers 10 HP.";
                    },
                },
                new() { Label = "Leave", Hint = "Nothing happens.", Apply = (_, _) => "You do not look back at the water." },
            ],
        },
        new()
        {
            Id = "gambler", Title = "The Bone Gambler",
            Text = "A skeleton in a merchant's coat rattles a cup of knucklebones. \"Double or nothing, living ones?\"",
            Choices =
            [
                new()
                {
                    Label = "Bet 30 gold", Hint = "50%: win 75 gold. 50%: lose the bet.",
                    Available = run => run.Gold >= 30,
                    Apply = (run, rng) =>
                    {
                        run.Gold -= 30;
                        if (rng.Next(2) == 0)
                        {
                            run.Gold += 75;
                            run.Stats.GoldEarned += 45;
                            return "The bones fall your way. +75 gold!";
                        }
                        return "The skeleton clacks its jaw in what might be laughter. The 30 gold is gone.";
                    },
                },
                new()
                {
                    Label = "Bet a consumable", Hint = "Lose your first consumable; 60%: gain a random relic.",
                    Available = run => run.Consumables.Count > 0,
                    Apply = (run, rng) =>
                    {
                        var lost = Database.Item(run.Consumables[0]).Name;
                        run.Consumables.RemoveAt(0);
                        if (rng.Next(100) < 60 && Database.RandomRelic(rng, run.Relics) is { } relic)
                        {
                            run.Relics.Add(relic.Id);
                            return $"You wager your {lost} and win [color={relic.ColorHex}]{relic.Name}[/color]: {relic.Description}";
                        }
                        return $"You lose your {lost} to a very lucky throw.";
                    },
                },
                new() { Label = "Decline", Hint = "Nothing happens.", Apply = (_, _) => "\"Your loss,\" it rattles." },
            ],
        },
    };

    public static EventDef Pick(Random rng, RunState run)
    {
        int region = run.Node?.Region ?? 0;
        var pool = All.Concat(Special).Where(e =>
            (e.Region < 0 || e.Region == region) &&
            !(e.Unique && run.SeenEvents.Contains(e.Id)) &&
            (e.Requires?.Invoke(run) ?? true)).ToList();
        int total = pool.Sum(e => e.Weight);
        int roll = rng.Next(Math.Max(1, total));
        foreach (var e in pool)
        {
            if (roll < e.Weight) return e;
            roll -= e.Weight;
        }
        return All[0];
    }

    private static string Fight(RunState run, string[] enemies, string text)
    {
        run.ForcedEncounter = enemies.ToList();
        run.ForcedElite = true;
        return text;
    }

    /// <summary>Rare encounters and the ghost-child chain that spans the whole descent.</summary>
    public static readonly IReadOnlyList<EventDef> Special = new List<EventDef>
    {
        new()
        {
            Id = "smith", Title = "The Wandering Smith", Weight = 7,
            Text = "A one-eyed dwarf has set up an anvil in a side chapel, sparks lighting up the bones around him. \"Coin first. Then I make your steel sing.\"",
            Choices =
            [
                new()
                {
                    Label = "Hone your blades", Hint = "Pay 60 gold: every hero deals +6% damage for the rest of the run.",
                    Available = r => r.Gold >= 60,
                    Apply = (run, _) => { run.Gold -= 60; foreach (var h in run.Party) h.BonusPowerPct += 6; return "The edges gleam. Every hero now hits a little harder."; },
                },
                new()
                {
                    Label = "Reinforce armor", Hint = "Pay 60 gold: every hero gains +8 max HP for the rest of the run.",
                    Available = r => r.Gold >= 60,
                    Apply = (run, _) => { run.Gold -= 60; foreach (var h in run.Party) { h.BonusMaxHp += 8; h.Hp += 8; } return "Rivets, plates, padding. The party feels sturdier."; },
                },
                new() { Label = "Move on", Hint = "Nothing happens.", Apply = (_, _) => "\"Your loss,\" he grunts, and keeps hammering." },
            ],
        },
        new()
        {
            Id = "altar", Title = "The Cursed Altar", Weight = 7, Requires = r => (r.Node?.Region ?? 0) >= 1,
            Text = "A black altar pulses like a heart. On it rests a relic wrapped in chains. Hooded guardians kneel around it, unmoving.",
            Choices =
            [
                new()
                {
                    Label = "Take the relic", Hint = "Gain a random relic. Curse: every hero has 15% less max HP for the rest of the run.",
                    Apply = (run, rng) =>
                    {
                        foreach (var h in run.Party) { h.BonusMaxHpPct -= 15; h.ClampHp(); }
                        if (Database.RandomRelic(rng, run.Relics) is { } relic)
                        {
                            run.Relics.Add(relic.Id);
                            return $"The chains fall away. [color={relic.ColorHex}]{relic.Name}[/color] is yours: {relic.Description}\nA cold weight settles on your hearts. [color=#ff6b6b](-15% max HP)[/color]";
                        }
                        return "The altar is empty after all. Only the curse remains.";
                    },
                },
                new()
                {
                    Label = "Smash the altar", Hint = "Fight its guardians (elite battle, relic among the rewards).",
                    Apply = (run, _) => Fight(run, ["cultist", "necromancer", "cultist"], "The guardians rise as the stone cracks!"),
                },
                new() { Label = "Leave it", Hint = "Nothing happens.", Apply = (_, _) => "Some things are better left alone." },
            ],
        },
        new()
        {
            Id = "lost_child", Title = "The Lost Child", Region = 0, Weight = 14, Unique = true,
            Text = "A small ghost sits among the graves, crying without a sound. She looks up at your torch as if she has not seen light in a hundred years.",
            Choices =
            [
                new()
                {
                    Label = "Light her way", Hint = "Pay 30 gold for oil and a candle. Something may come of it.",
                    Available = r => r.Gold >= 30,
                    Apply = (run, _) => { run.Gold -= 30; run.Flags.Add("ghost_helped"); return "You set a candle beside her. She smiles, and fades into the light. \"I'll find you again,\" a small voice whispers."; },
                },
                new()
                {
                    Label = "Sit with her", Hint = "Every hero loses 4 HP to the grave-cold. Something may come of it.",
                    Apply = (run, _) => { DamageParty(run, 4); run.Flags.Add("ghost_helped"); return "You stay until her crying stops. The cold sinks into your bones, but she is smiling when she fades."; },
                },
                new()
                {
                    Label = "Walk past", Hint = "Nothing happens... for now.",
                    Apply = (run, _) => { run.Flags.Add("ghost_ignored"); return "Her silent sobbing follows you down the stairs."; },
                },
            ],
        },
        new()
        {
            Id = "child_returns", Title = "A Small Light", Region = DungeonMap.RegionCount - 1, Weight = 120, Unique = true,
            Requires = r => r.Flags.Contains("ghost_helped"),
            Text = "Among the cinders, a small blue flame floats toward you. The ghost from the crypt holds it out with both hands. \"You lit my way. Now I light yours.\"",
            Choices =
            [
                new()
                {
                    Label = "Accept the lantern", Hint = "Gain The Child's Lantern: heroes heal 10% at the start of every battle.",
                    Apply = (run, _) => { run.Relics.Add("childs_lantern"); run.HealParty(25); return "Her lantern is warm. Your wounds ease (+25% HP), and she is gone, smiling."; },
                },
            ],
        },
        new()
        {
            Id = "weeping_shade", Title = "The Weeping Shade", Region = DungeonMap.RegionCount - 1, Weight = 120, Unique = true,
            Requires = r => r.Flags.Contains("ghost_ignored"),
            Text = "The silent sobbing again. The child from the crypt is here, but she has grown: a howling shade, and others like her circle in the smoke.",
            Choices =
            [
                new()
                {
                    Label = "Face her", Hint = "Fight three wraiths (elite battle, relic among the rewards).",
                    Apply = (run, _) => Fight(run, ["wraith", "wraith", "wraith"], "\"You left me in the dark!\" The shades descend!"),
                },
                new()
                {
                    Label = "Run", Hint = "Every hero loses 10 HP.",
                    Apply = (run, _) => DamageParty(run, 10) + " Her wail chases you through the smoke.",
                },
            ],
        },
    };
}
