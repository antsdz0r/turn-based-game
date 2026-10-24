using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>A codex page. Source tells the player where it is found while it is still locked.</summary>
public sealed record LoreDef(string Id, string Title, string Source, string Text, Func<Profile, bool>? Condition = null);

/// <summary>
/// The lore codex (Compendium → Lore): pages found by entering regions, defeating bosses and reaching milestones.
/// Region and boss pages unlock through Lore.Unlock; milestone pages through Lore.Refresh at the end of a run.
/// </summary>
public static class Lore
{
    public static readonly IReadOnlyList<LoreDef> Pages = new List<LoreDef>
    {
        // ── Regions ──
        new("region_crypt", "The Ashen Crypt", "Enter the Ashen Crypt",
            "The first Keepers buried their dead beneath the Ember so that the flame would remember them. For a thousand years it did. When it dimmed, the memory went cold, and the dead got up to look for warmth."),
        new("region_drowned", "The Drowned Halls", "Enter the Drowned Halls",
            "The cellars were flooded on purpose, during the Siege of Smoke, to keep the fire from spreading. The water was never drained. Some say the Keepers who stayed to work the sluices are still down there, singing to keep each other awake."),
        new("region_frost", "The Frozen Ossuary", "Enter the Frozen Ossuary",
            "Ice preserves. The Keepers learned this the hard way, and stored their most dangerous dead in the cold. Vaelith was the last of them to be laid in the ossuary. She was not dead at the time."),
        new("region_storm", "The Storm Spire", "Enter the Storm Spire",
            "A lightning rod the height of a mountain, built to draw the sky's fire down into the Ember and feed it. When the Hollow King took the throne, he turned it into a fortress, and gave it to the only general who could stand on top of it."),
        new("region_garden", "The Rotting Garden", "Enter the Rotting Garden",
            "The monks grew healing herbs under glass, warmed by the Ember's glow. When the warmth failed, the plants did not die. They learned to eat what fell among them, and then they learned to make things fall."),
        new("region_archive", "The Silent Archive", "Enter the Silent Archive",
            "Every spell the Keepers ever forbade was written down, so that it could be recognised and resisted. The books were sealed in silence. Silence, it turns out, is a good listener."),
        new("region_cinder", "The Cinder Throne", "Enter the Cinder Throne",
            "The Ember's heart chamber, where the first flame was lit. The Hollow King has built his throne around it like a ribcage around a heart, and every beat of it feeds him."),

        // ── Bosses ──
        new("boss_colossus", "The Bone Colossus", "Defeat the Bone Colossus",
            "When the graves overflowed, the Hollow King did not dig new ones. He had the bones stacked against the crypt stair, and told them to stand. They have been standing guard ever since, a thousand dead pretending to be one."),
        new("boss_matriarch", "The Drowned Matriarch", "Defeat the Drowned Matriarch",
            "Her name was Oriel, and she was Ilsa's teacher. She chose to stay at the sluices when the waters rose. The song she sings is the Keepers' evening hymn. She has forgotten the words, but not the tune."),
        new("boss_lich", "Vaelith, the Frost Lich", "Defeat Vaelith",
            "Vaelith was a Keeper who feared the flame would one day fail. She cut out her own soul and sealed it in ice, so that she at least would last. She was right about the flame. She was wrong about what would be left of her."),
        new("boss_tyrant", "Kharos, the Storm Tyrant", "Defeat Kharos",
            "Kharos commanded the Keepers' own guard, before. He went over to the Hollow King for a promise: that he would never again have to choose who to save. Now he chooses who to strike, and calls it the same thing."),
        new("boss_mother_bloom", "The Mother Bloom", "Defeat the Mother Bloom",
            "She began as a single seed of emberwort, planted by a novice who wanted something beautiful near the dying flame. It was beautiful. It still is, in the way a fire is beautiful from inside the house."),
        new("boss_archivist", "The Archivist", "Defeat the Archivist",
            "The archive needed a keeper who would never be tempted by what it guarded. So the Keepers made one out of an empty robe and a binding spell, and gave it no face to feel with. They did not think it would read. It read everything."),
        new("boss_hollow_king", "The Hollow King", "Defeat the Hollow King",
            "No one remembers who he was. Perhaps that is the point. A man who wanted the Ember for himself, who found that a flame cannot be owned, only fed, and decided to become the one thing it would always need: fuel."),
        new("boss_ember_wyrm", "The Ember Wyrm", "Defeat the secret boss behind the Sealed Door",
            "The first spark of the Ember did not land on wood. It landed on a lizard sleeping in the ash, and the lizard woke up burning. The Keepers could not kill it, so they built a door, and then they built the whole cathedral on top of the door, and then they tried very hard to forget."),

        // ── The Keepers ──
        new("keeper_first", "Ilsa's First Night", "Start your first run",
            "Ilsa was seven when she was brought to the Ember. The Keepers asked her what she saw in the flame. She said, \"Someone waiting.\" They made her a Keeper the same night.",
            p => p.RunsStarted >= 1),
        new("keeper_deep", "The Keepers' Oath", "Reach the fourth region",
            "\"While one of us remains, the flame is kept. While the flame is kept, the dead sleep. While the dead sleep, the living may forget us. Let them.\"",
            p => p.BestFloor >= DungeonMap.FloorsPerRegion * 3 + 1),
        new("keeper_mastery", "Lessons in Ash", "Reach mastery level 1 with any hero",
            "Ilsa never fought. She watched others fight, and remembered every one of them. That is what a Keeper is: a memory that refuses to go out.",
            p => p.HeroXp.Keys.Any(h => Mastery.Level(p, h) >= 1)),
        new("keeper_victory", "What the Ember Wants", "Win a run",
            "The Ember does not want to be worshipped, or owned, or even kept. It wants to be shared. Every Keeper learns this at the end, and every one of them is glad.",
            p => p.RunsWon >= 1),
        new("keeper_endless", "Below the Bottom", "Descend 40 floors in endless mode",
            "There is no bottom to the vault. There is only the place where the Keepers stopped digging. Below it, the stairs go on, and there are footprints in the ash going down. None coming back.",
            p => p.EndlessBest >= DungeonMap.Floors * 2),
    };

    public static LoreDef? Get(string id) => Pages.FirstOrDefault(p => p.Id == id);

    /// <summary>Unlocks a page by id. Returns it if it was new.</summary>
    public static LoreDef? Unlock(Profile p, string id)
    {
        if (Get(id) is not { } page || p.LoreFound.Contains(id)) return null;
        p.LoreFound.Add(id);
        p.LoreNew.Add(id);
        return page;
    }

    /// <summary>Unlocks every milestone page whose condition is now met.</summary>
    public static List<LoreDef> Refresh(Profile p)
    {
        var fresh = new List<LoreDef>();
        foreach (var page in Pages.Where(pg => pg.Condition is not null && !p.LoreFound.Contains(pg.Id) && pg.Condition(p)))
            if (Unlock(p, page.Id) is { } found) fresh.Add(found);
        return fresh;
    }
}
