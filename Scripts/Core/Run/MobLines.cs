using System;
using System.Collections.Generic;

namespace TBRPG.Core;

public enum MobCue { Taunt, Attack, Hurt, Death }

/// <summary>
/// What monsters say. Every enemy belongs to a family (bones, ghoul, cultist...), which decides both its lines
/// and the synthesized voice the UI plays (Audio/Voice/{voice}_{cue}_{n}.ogg).
/// </summary>
public static class MobLines
{
    /// <summary>Family of an enemy id (lines and voice).</summary>
    public static string Family(string enemyId) => enemyId switch
    {
        "bones" or "archer" => "bones",
        "ghoul" or "gravemaw" or "thrall" or "leech" or "blight" or "champion" => "ghoul",
        "acolyte" or "caller" or "stormcaller" or "bellringer" or "ash_priest" => "cultist",
        "wraith" or "wisp" or "moth" or "phylactery" or "rime_sentinel" or "ink_wraith" => "wraith",
        "siren" or "siren_queen" => "siren",
        "rats" or "hound" or "alpha_hound" or "shellback" or "mimic" or "stalker" => "beast",
        "harpy" or "thunderbird" or "quill_swarm" => "bird",
        "thornling" or "spore_puffer" or "strangler" or "seed_pod" => "plant",
        "briar_witch" => "witch",
        "tome" => "book",
        "imp" => "imp",
        "construct" or "arc_golem" or "ballista" or "index_golem" => "machine",
        "golem" or "behemoth" or "gargoyle" => "giant",
        "templar" or "bulwark" or "wight" or "revenant" => "knight",
        "colossus" => "boss_colossus",
        "matriarch" => "boss_matriarch",
        "lich" => "boss_lich",
        "tyrant" => "boss_tyrant",
        "hollow_king" => "boss_king",
        "mother_bloom" => "boss_bloom",
        "archivist" => "boss_archivist",
        "ember_wyrm" => "boss_wyrm",
        _ => "cultist",
    };

    /// <summary>The synthesized voice for a family (same names as the voice files).</summary>
    public static string Voice(string enemyId) => Family(enemyId);

    private static readonly Dictionary<(string, MobCue), string[]> Lines = new()
    {
        [("bones", MobCue.Taunt)] = ["Fresh bones for the pile.", "*rattling laughter*", "Lie down. Stay down. Like us."],
        [("bones", MobCue.Attack)] = ["Clatter and fall!", "Join us in the dust!", "*clack clack*"],
        [("bones", MobCue.Hurt)] = ["My ribs!", "*crack*"],
        [("bones", MobCue.Death)] = ["*collapses into a heap*", "Back... to sleep..."],

        [("ghoul", MobCue.Taunt)] = ["Meat... warm meat...", "*hungry snarl*", "Smells... alive."],
        [("ghoul", MobCue.Attack)] = ["FEED!", "*snarls and lunges*", "Bite... tear..."],
        [("ghoul", MobCue.Hurt)] = ["*yelp*", "Hrrk!"],
        [("ghoul", MobCue.Death)] = ["*gurgling death rattle*"],

        [("cultist", MobCue.Taunt)] = ["The Hollow King sees you.", "Ash to ash.", "Your ember will feed the throne."],
        [("cultist", MobCue.Attack)] = ["Burn for him!", "Kneel to the ash!", "By the hollow crown!"],
        [("cultist", MobCue.Hurt)] = ["Heretic!", "You dare?"],
        [("cultist", MobCue.Death)] = ["My king... forgive...", "The ash... takes me..."],

        [("wraith", MobCue.Taunt)] = ["Cold... so cold...", "Stay with us... forever...", "*a whisper behind your ear*"],
        [("wraith", MobCue.Attack)] = ["Freeze...", "Your breath is mine...", "*shrieking wail*"],
        [("wraith", MobCue.Hurt)] = ["*hiss*", "No...!"],
        [("wraith", MobCue.Death)] = ["Free... at last...", "*fades with a sigh*"],

        [("siren", MobCue.Taunt)] = ["Come closer... listen...", "Sing with us, little sparks.", "The water remembers you."],
        [("siren", MobCue.Attack)] = ["Down... into the deep!", "Drown in my song!", "La-la-laaa..."],
        [("siren", MobCue.Hurt)] = ["You'll ruin my voice!", "*shriek*"],
        [("siren", MobCue.Death)] = ["The song... ends...", "*a last, broken note*"],

        [("beast", MobCue.Taunt)] = ["*low growl*", "*skittering*"],
        [("beast", MobCue.Attack)] = ["*SNARL*", "*howl*"],
        [("beast", MobCue.Hurt)] = ["*yelp*"],
        [("beast", MobCue.Death)] = ["*whimper*"],

        [("bird", MobCue.Taunt)] = ["*shrill cry*"],
        [("bird", MobCue.Attack)] = ["*SCREECH*", "Fall from the sky!"],
        [("bird", MobCue.Hurt)] = ["*squawk*"],
        [("bird", MobCue.Death)] = ["*plummets*"],

        [("imp", MobCue.Taunt)] = ["Hehehe! Fresh kindling!", "Ooh, you'll crackle nicely!"],
        [("imp", MobCue.Attack)] = ["Burn burn burn!", "Catch, hehe!", "Toasty!"],
        [("imp", MobCue.Hurt)] = ["Ow! Rude!", "Hey!"],
        [("imp", MobCue.Death)] = ["Pfft... fizzle...", "No fair!"],

        [("machine", MobCue.Taunt)] = ["INTRUDERS DETECTED.", "THREAT ASSESSMENT: LOW.", "*grinding gears*"],
        [("machine", MobCue.Attack)] = ["PURGE.", "DISCHARGING.", "TARGET LOCKED."],
        [("machine", MobCue.Hurt)] = ["DAMAGE SUSTAINED.", "*sparks*"],
        [("machine", MobCue.Death)] = ["SYSTEMS... FAILING...", "*powers down*"],

        [("giant", MobCue.Taunt)] = ["*the ground rumbles*", "SMALL... THINGS..."],
        [("giant", MobCue.Attack)] = ["CRUSH!", "*molten roar*"],
        [("giant", MobCue.Hurt)] = ["*grinding groan*"],
        [("giant", MobCue.Death)] = ["*cracks apart and cools*"],

        [("knight", MobCue.Taunt)] = ["Hold the line.", "The oath binds even the dead.", "You shall not pass."],
        [("knight", MobCue.Attack)] = ["Judgment!", "For the old crown!", "Fall!"],
        [("knight", MobCue.Hurt)] = ["A scratch.", "Hngh!"],
        [("knight", MobCue.Death)] = ["My watch... ends...", "At last... rest."],

        [("plant", MobCue.Taunt)] = ["*creaking branches*", "*a hungry rustle*", "*thorns scrape on stone*"],
        [("plant", MobCue.Attack)] = ["*lashes out*", "*SNAP*"],
        [("plant", MobCue.Hurt)] = ["*sap spurts*"],
        [("plant", MobCue.Death)] = ["*wilts and crumbles*"],

        [("witch", MobCue.Taunt)] = ["Mind the thorns, dearies.", "Such pretty compost you'll make.", "My garden is hungry tonight."],
        [("witch", MobCue.Attack)] = ["Bleed for my roses!", "Wither!", "Grow, my sweets!"],
        [("witch", MobCue.Hurt)] = ["My bark!", "Nasty little weeds!"],
        [("witch", MobCue.Death)] = ["Back... to the soil...", "Who will water them now?"],

        [("book", MobCue.Taunt)] = ["*pages riffle hungrily*", "Shhhh.", "Chapter one: you die."],
        [("book", MobCue.Attack)] = ["*SNAP*", "Paper cut!", "Read THIS!"],
        [("book", MobCue.Hurt)] = ["*torn pages*", "My spine!"],
        [("book", MobCue.Death)] = ["The end.", "*flutters shut*"],

        [("boss_bloom", MobCue.Taunt)] = ["Feed, my children.", "Everything rots. Everything feeds me."],
        [("boss_bloom", MobCue.Attack)] = ["Take root!", "Bloom!", "Into the soil!"],
        [("boss_bloom", MobCue.Hurt)] = ["My petals!", "You prune me?!"],
        [("boss_bloom", MobCue.Death)] = ["The garden... withers...", "Winter... at last..."],
        [("boss_archivist", MobCue.Taunt)] = ["Shh.", "I have read your ending already."],
        [("boss_archivist", MobCue.Attack)] = ["Redacted.", "Footnote.", "Erase."],
        [("boss_archivist", MobCue.Hurt)] = ["A new word...", "Unwritten!"],
        [("boss_archivist", MobCue.Death)] = ["The last page... turns...", "Silence... at last..."],

        [("boss_wyrm", MobCue.Taunt)] = ["*a sound like a furnace door opening*", "*the air itself starts to burn*"],
        [("boss_wyrm", MobCue.Attack)] = ["*ROAR*", "*a blast of white heat*"],
        [("boss_wyrm", MobCue.Hurt)] = ["*molten blood hisses on the stone*"],
        [("boss_wyrm", MobCue.Death)] = ["*the Wyrm's fire gutters... and goes out*"],

        [("boss_colossus", MobCue.Attack)] = ["CRUSH... THEM!", "BONES... TO DUST!"],
        [("boss_colossus", MobCue.Hurt)] = ["*bones grind and crack*"],
        [("boss_colossus", MobCue.Death)] = ["*the Colossus collapses in a roar of bone*"],
        [("boss_matriarch", MobCue.Attack)] = ["The tide rises!", "Sink!", "Breathe the deep!"],
        [("boss_matriarch", MobCue.Hurt)] = ["You wound a queen?!"],
        [("boss_matriarch", MobCue.Death)] = ["The water... recedes...", "My halls... dry..."],
        [("boss_lich", MobCue.Taunt)] = ["Your warmth offends me.", "Winter takes all things."],
        [("boss_lich", MobCue.Attack)] = ["Be still. Forever.", "Freeze.", "Winter's judgment."],
        [("boss_lich", MobCue.Hurt)] = ["Insolent warmth!"],
        [("boss_lich", MobCue.Death)] = ["My soul... was in the jar...", "Spring... comes..."],
        [("boss_tyrant", MobCue.Taunt)] = ["Kneel before the storm!", "I am the thunder."],
        [("boss_tyrant", MobCue.Attack)] = ["Thunder answers me!", "Be struck down!", "STORM!"],
        [("boss_tyrant", MobCue.Hurt)] = ["You spark... in my storm?"],
        [("boss_tyrant", MobCue.Death)] = ["The sky... falls silent...", "Impossible..."],
        [("boss_king", MobCue.Attack)] = ["Kneel.", "Your light is mine.", "Bow to the hollow crown!"],
        [("boss_king", MobCue.Hurt)] = ["You... burn me?", "The ember... resists?"],
        [("boss_king", MobCue.Death)] = ["The flame... returns to you...", "Hollow... at last... empty..."],
    };

    /// <summary>A line for this enemy and moment, or null if it has nothing to say.</summary>
    public static string? Line(string enemyId, MobCue cue, Random rng)
    {
        var family = Family(enemyId);
        return Lines.TryGetValue((family, cue), out var lines) ? lines[rng.Next(lines.Length)] : null;
    }

    /// <summary>Every family that has lines (validated against the voice files by the tests).</summary>
    public static IEnumerable<string> Families
    {
        get
        {
            var seen = new HashSet<string>();
            foreach (var (family, _) in Lines.Keys) if (seen.Add(family)) yield return family;
        }
    }
}
