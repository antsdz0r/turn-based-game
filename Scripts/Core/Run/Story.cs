using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>
/// One line of a cutscene. Speaker is "narrator", "keeper", "boss", or "h0"/"h1"/"h2" (party members in order).
/// A line with choices ends the scene with a decision (see Story.ApplyChoice).
/// </summary>
public sealed record DialogueLine(string Speaker, string Text, IReadOnlyList<DialogueChoice>? Choices = null);

public sealed record DialogueChoice(string Text, string Action, int Cost = 0);

/// <summary>A short scene: who stands on the right (the keeper or a boss), and the lines.</summary>
public sealed record Cutscene(string Id, string Title, string Subtitle, int Region, string RightSprite, string RightName,
                              string RightTint, float RightScale, string Music, IReadOnlyList<DialogueLine> Lines);

/// <summary>All story text: cutscenes before each region and boss, the ending, and in-battle barks.</summary>
public static class Story
{
    public const string KeeperSprite = "cleric";
    public const string KeeperName = "Ilsa, the Last Keeper";
    private const string KeeperTint = "#9fd8ff";

    public static string RegionIntroId(int region) => $"region_{Database.Region(region).Key}";
    public static string BossIntroId(int region) => $"boss_{Database.Region(region).Key}";
    public const string EndingId = "ending";

    public static int OfferPrice(int region) => 30 + 15 * region;

    public static Cutscene? Get(string id, RunState run)
    {
        var h = run.Party.Select(p => p.Id).ToList();
        string Q(int slot, string region) => Quip(h.ElementAtOrDefault(slot) ?? "vanguard", region);
        string B(int slot, string region) => BossRetort(h.ElementAtOrDefault(slot) ?? "vanguard", region);
        int offer = OfferPrice(run.CurrentRegion);

        switch (id)
        {
            case "region_crypt":
                return new Cutscene(id, "The Ashen Crypt", "Region I", 0, KeeperSprite, KeeperName, KeeperTint, 1f, "menu", new[]
                {
                    new DialogueLine("narrator", "For a thousand years the Last Ember burned beneath the cathedral, and the dead slept."),
                    new DialogueLine("narrator", "Three nights ago it guttered. Tonight the graves of the Ashen Crypt stand open."),
                    new DialogueLine("keeper", "You came. I had begun to think no one living still remembered the way down."),
                    new DialogueLine("h0", "We heard the bells ring on their own. Who are you?"),
                    new DialogueLine("keeper", "Ilsa. The last of the Ember-Keepers... or what is left of her. The Hollow King tore out the flame's heart and carried it to his throne, three halls below."),
                    new DialogueLine("h1", Q(1, "crypt")),
                    new DialogueLine("keeper", "The crypt is first. Its dead are restless, and something enormous guards the stair. Keep your torches lit."),
                    new DialogueLine("h2", Q(2, "crypt")),
                });

            case "boss_crypt":
                return new Cutscene(id, "The Bone Colossus", "Guardian of the Crypt", 0, "colossus", "The Bone Colossus", "#ffffff", 1.25f, "boss", new[]
                {
                    new DialogueLine("narrator", "The stairway is blocked by a mountain of bones. Then the mountain stands up."),
                    new DialogueLine("boss", "LIVING... FLESH. THE KING... FORBIDS... PASSAGE."),
                    new DialogueLine("h0", B(0, "crypt")),
                    new DialogueLine("boss", "THEN... JOIN... THE PILE."),
                });

            case "region_drowned":
                return new Cutscene(id, "The Drowned Halls", "Region II", 1, KeeperSprite, KeeperName, KeeperTint, 1f, "menu", new[]
                {
                    new DialogueLine("narrator", "Below the crypt, black water has swallowed the old cathedral cellars. Somewhere in the dark, someone is singing."),
                    new DialogueLine("keeper", "The Drowned Halls. The Matriarch was a Keeper once, like me. She chose the water over the flame."),
                    new DialogueLine("h1", Q(1, "drowned")),
                    new DialogueLine("h0", "Is there anything left of her we can reach?"),
                    new DialogueLine("keeper", "Only her song. Do not listen to it too long."),
                    new DialogueLine("h2", Q(2, "drowned")),
                    new DialogueLine("keeper", $"The Ember still answers offerings. Give it gold and it will mend your wounds.", new[]
                    {
                        new DialogueChoice($"Offer {offer} gold (heal the party 50%)", "offer_heal", offer),
                        new DialogueChoice("Keep the gold", "none"),
                    }),
                });

            case "boss_drowned":
                return new Cutscene(id, "The Drowned Matriarch", "Queen of the Deep", 1, "matriarch", "The Drowned Matriarch", "#ffffff", 1.25f, "boss", new[]
                {
                    new DialogueLine("narrator", "The singing stops. The water parts, and she rises."),
                    new DialogueLine("boss", "Little sparks. You carry Ilsa's light. I can smell it on you, like smoke."),
                    new DialogueLine("boss", "The flame only ever asked us to burn. The water asks nothing. Stay. Sink. Rest."),
                    new DialogueLine("h0", B(0, "drowned")),
                    new DialogueLine("boss", "Then I will drown your little light myself."),
                });

            case "region_frost":
                return new Cutscene(id, "The Frozen Ossuary", "Region III", 2, KeeperSprite, KeeperName, KeeperTint, 1f, "menu", new[]
                {
                    new DialogueLine("narrator", "The black water freezes as you descend. Frost climbs the walls, and the bones in the alcoves are sheathed in ice."),
                    new DialogueLine("keeper", "The Frozen Ossuary. Vaelith rules here: a lich who cut her soul out and hid it in a jar, so that she could never die."),
                    new DialogueLine("h0", Q(0, "frost")),
                    new DialogueLine("keeper", "While that jar stands, your blows will barely touch her. Find it. Break it."),
                    new DialogueLine("h1", Q(1, "frost")),
                    new DialogueLine("keeper", "The Ember can still warm you. Will you feed it?", new[]
                    {
                        new DialogueChoice($"Offer {offer} gold (heal the party 50%)", "offer_heal", offer),
                        new DialogueChoice("Keep the gold", "none"),
                    }),
                });

            case "boss_frost":
                return new Cutscene(id, "Vaelith, the Frost Lich", "Keeper of the Ossuary", 2, "lich", "Vaelith", "#ffffff", 1.25f, "boss", new[]
                {
                    new DialogueLine("narrator", "A throne of ice. A figure in a crown of icicles, and beside her, a jar that glows with a cold blue heart."),
                    new DialogueLine("boss", "Warm blood. How long it has been. Come closer, little candles. Let me feel you go out."),
                    new DialogueLine("h0", B(0, "frost")),
                    new DialogueLine("boss", "Strike me all you like. I cannot die while my heart sleeps in the jar."),
                });

            case "region_storm":
                return new Cutscene(id, "The Storm Spire", "Region IV", 3, KeeperSprite, KeeperName, KeeperTint, 1f, "menu", new[]
                {
                    new DialogueLine("narrator", "The ice gives way to a spiral stair inside a tower of black stone. Thunder shakes the walls. Lightning falls without end."),
                    new DialogueLine("keeper", "Kharos, the Storm Tyrant. He was the Hollow King's general once. He chooses one prey, and his lightning never misses it."),
                    new DialogueLine("h2", Q(2, "storm")),
                    new DialogueLine("keeper", "When he brands one of you, protect them. Shield them, guard them, heal them. Or lose them."),
                    new DialogueLine("h1", Q(1, "storm")),
                    new DialogueLine("keeper", "The flame burns low. One more offering?", new[]
                    {
                        new DialogueChoice($"Offer {offer} gold (heal the party 50%)", "offer_heal", offer),
                        new DialogueChoice("Keep the gold", "none"),
                    }),
                });

            case "boss_storm":
                return new Cutscene(id, "Kharos, the Storm Tyrant", "General of the Hollow King", 3, "tyrant", "Kharos", "#ffffff", 1.25f, "boss", new[]
                {
                    new DialogueLine("narrator", "At the top of the spire, a giant in storm-black armor stands in the rain. Lightning coils around his spear."),
                    new DialogueLine("boss", "My king said you would come. He did not say you would be so small."),
                    new DialogueLine("h0", B(0, "storm")),
                    new DialogueLine("boss", "Choose who dies first. No? Then I will choose for you."),
                });

            case "region_cinder":
                return new Cutscene(id, "The Cinder Throne", "Region V", 4, KeeperSprite, KeeperName, KeeperTint, 1f, "menu", new[]
                {
                    new DialogueLine("narrator", "Past the storm, the stairs glow red. Below them, everything is burning."),
                    new DialogueLine("keeper", "The Cinder Throne, at last. He is feeding on the Ember's heart; that is why these halls burn."),
                    new DialogueLine("h2", Q(2, "cinder")),
                    new DialogueLine("h0", "When we reach him, what happens to you?"),
                    new DialogueLine("keeper", "If the flame is rekindled, I can finally rest. That is all a Keeper ever wants."),
                    new DialogueLine("h1", Q(1, "cinder")),
                    new DialogueLine("keeper", "One last offering, before the end?", new[]
                    {
                        new DialogueChoice($"Offer {offer} gold (heal the party 50%)", "offer_heal", offer),
                        new DialogueChoice("Keep the gold", "none"),
                    }),
                });

            case "boss_cinder":
                return new Cutscene(id, "The Hollow King", "Devourer of the Ember", 4, "hollow_king", "The Hollow King", "#ffffff", 1.25f, "boss", new[]
                {
                    new DialogueLine("narrator", "On a throne of cinders sits a crowned skeleton, and in its ribs, the stolen heart of the Last Ember beats."),
                    new DialogueLine("boss", "Ilsa's little moths. You crossed my crypt, my waters, my ice and my storm, only to bring me more fuel."),
                    new DialogueLine("h1", B(1, "cinder")),
                    new DialogueLine("boss", "Fuel speaks so bravely before it burns."),
                    new DialogueLine("h0", B(0, "cinder")),
                    new DialogueLine("boss", "Then come. Kneel, or be devoured."),
                });

            case "ending":
                return new Cutscene(id, "The Last Ember", "Rekindled", 4, KeeperSprite, KeeperName, KeeperTint, 1f, "menu", new[]
                {
                    new DialogueLine("narrator", "The Hollow King falls apart into ash. The heart of the Ember rolls free, flickers... and catches."),
                    new DialogueLine("keeper", "It burns. After all this time, it burns again."),
                    new DialogueLine("h0", "Then it's over?"),
                    new DialogueLine("keeper", "For a while. The dead will sleep. Thank you, all of you. Tell them up there that Ilsa kept the light."),
                    new DialogueLine("narrator", "The Keeper's light grows brighter, warmer... and then, quietly, it goes out. Above, the cathedral bells fall silent."),
                });
        }
        return null;
    }

    /// <summary>Applies a cutscene choice. Returns a line to show, or null.</summary>
    public static string? ApplyChoice(RunState run, DialogueChoice choice)
    {
        switch (choice.Action)
        {
            case "offer_heal":
                if (run.Gold < choice.Cost) return "You don't have enough gold.";
                run.Gold -= choice.Cost;
                run.HealParty(50);
                return "The Ember flares. Warmth floods back into your wounds.";
            default:
                return null;
        }
    }

    // ═════════════════════ Hero personalities ═════════════════════

    private static string Quip(string hero, string region) => (hero, region) switch
    {
        ("vanguard", "crypt") => "Then we go down. I swore an oath to this cathedral; I'll keep it.",
        ("vanguard", "drowned") => "Water or fire, my shield doesn't care.",
        ("vanguard", "cinder") => "Stay behind me. Whatever happens, stay behind me.",
        ("arcanist", "crypt") => "A king who steals a flame. How very predictable. Kings never could resist anything shiny.",
        ("arcanist", "drowned") => "Drowned halls. Marvelous. My robes will never forgive me.",
        ("arcanist", "cinder") => "Hm. My eyebrows have survived worse. Probably.",
        ("warden", "crypt") => "Rest easy, Keeper. We'll carry the light the rest of the way.",
        ("warden", "drowned") => "The water is so cold... Keep close, everyone. I'll keep you warm.",
        ("warden", "cinder") => "Whatever it costs, we bring it back. All of us.",
        ("shadowblade", "crypt") => "Robbing a king. Finally, a job I'm qualified for.",
        ("shadowblade", "drowned") => "A singing queen in the dark. I've been to worse taverns.",
        ("shadowblade", "cinder") => "Hot. Loud. Full of things that want to kill us. Feels like home.",
        ("ranger", "crypt") => "Restless dead, a giant bone monster... this is going to make a great story!",
        ("ranger", "drowned") => "Can you shoot something underwater? Guess I'll find out!",
        ("ranger", "cinder") => "Last stretch! I can almost see the end of the tunnel. Or that's just fire.",
        ("berserker", "crypt") => "Dead things that walk? Good. They won't mind being cut down twice.",
        ("berserker", "drowned") => "Cold water. Like home. I'll swim through their queen if I must.",
        ("berserker", "cinder") => "Fire and blood. This is where my sagas end, or begin.",
        ("hexer", "crypt") => "The dead whisper here. They have so much to say about their king.",
        ("hexer", "drowned") => "A queen who chose the water. I understand her better than I'd like.",
        ("hexer", "cinder") => "Every curse I know, I have saved for him.",
        ("vanguard", "frost") => "Cold steel, colder hearts. I have stood guard in worse.",
        ("vanguard", "storm") => "Then he'll have to go through my shield first.",
        ("arcanist", "frost") => "Ice and bones. Ah, my least favorite combination. Fire it is.",
        ("arcanist", "storm") => "A lightning tower. I wrote a paper on these. It ended badly for the author.",
        ("warden", "frost") => "Stay near my lantern. I won't let the cold take anyone.",
        ("warden", "storm") => "If he chooses one of us, he'll find all of us standing in the way.",
        ("shadowblade", "frost") => "A soul in a jar. I've stolen smaller things from better guards.",
        ("shadowblade", "storm") => "Lightning I can dodge. Mostly.",
        ("ranger", "frost") => "My fingers are numb. Good thing my arrows aren't!",
        ("ranger", "storm") => "Great, now I can't even hear myself aim.",
        ("berserker", "frost") => "Ice? I bathe in fjords at midwinter.",
        ("berserker", "storm") => "Thunder means the gods are watching. Let's give them a show.",
        ("hexer", "frost") => "A lich who hid her soul. Clever. I'll find it anyway.",
        ("hexer", "storm") => "Storms are only angry curses. I know angry curses.",
        _ => "Let's go.",
    };

    private static string BossRetort(string hero, string region) => (hero, region) switch
    {
        ("vanguard", "crypt") => "My oath forbids me to turn back. Stand aside, or fall.",
        ("vanguard", "drowned") => "I've heard your song. It doesn't move me. Raise your hand, Matriarch.",
        ("vanguard", "cinder") => "I've walked through your crypt, your water and your fire. I'm not kneeling now.",
        ("arcanist", "crypt") => "It talks! Badly, but it talks. Let's see if it burns as well.",
        ("arcanist", "drowned") => "Rest? Madam, I've had sixty years of naps. I'd rather have a fight.",
        ("arcanist", "cinder") => "I've read about you. Footnote. Page nine hundred. Very forgettable.",
        ("warden", "crypt") => "Poor thing. Let us put you back to sleep.",
        ("warden", "drowned") => "The flame asked you to protect people. You can still remember that.",
        ("warden", "cinder") => "That heart doesn't belong to you. Give it back.",
        ("shadowblade", "crypt") => "Big. Slow. My favorite kind.",
        ("shadowblade", "drowned") => "Pretty voice. Shame about everything else.",
        ("shadowblade", "cinder") => "Nice crown. I'll be taking that.",
        ("ranger", "crypt") => "Wow, you're huge! ...That makes you easier to hit.",
        ("ranger", "drowned") => "Sorry, lady, we've got somewhere to be!",
        ("ranger", "cinder") => "You're the last page of this story. Let's finish it.",
        ("berserker", "crypt") => "Big bones. I'll make a fine necklace out of you.",
        ("berserker", "drowned") => "Sing louder. I like a song while I fight.",
        ("berserker", "cinder") => "Kneel? I don't even know how.",
        ("hexer", "crypt") => "Hush now. I'll unmake you one bone at a time.",
        ("hexer", "drowned") => "Your song is lovely. Your death will be lovelier.",
        ("hexer", "cinder") => "You stole a flame. I'll steal everything else.",
        ("vanguard", "frost") => "Then I'll break the jar, and you with it.",
        ("vanguard", "storm") => "Choose me, then. I've held worse than lightning.",
        ("arcanist", "frost") => "Hiding your soul in pottery. Truly the height of necromancy.",
        ("arcanist", "storm") => "Small, yes. Also very, very flammable.",
        ("warden", "frost") => "You've been cold so long you've forgotten what warmth is for.",
        ("warden", "storm") => "You'll have to strike all of us. We stand together.",
        ("shadowblade", "frost") => "That jar of yours looks very breakable.",
        ("shadowblade", "storm") => "The bigger they are...",
        ("ranger", "frost") => "Sorry, your highness. We came for the jar.",
        ("ranger", "storm") => "Small means hard to hit!",
        ("berserker", "frost") => "I'll smash your jar and dance on the shards.",
        ("berserker", "storm") => "Choose me. I'm the one who hits back hardest.",
        ("hexer", "frost") => "Every soul can be found. Yours is screaming already.",
        ("hexer", "storm") => "Your lightning is loud. My curses are quiet. Quiet wins.",
        _ => "Enough talk.",
    };

    // ═════════════════════ In-battle barks ═════════════════════

    private static readonly Dictionary<string, string[]> StartBarks = new()
    {
        ["vanguard"] = new[] { "Form up!", "Behind me!", "For the Ember!" },
        ["arcanist"] = new[] { "Ah, volunteers.", "Stand back, this gets warm.", "Let's be quick, I'm old." },
        ["warden"] = new[] { "I'm with you all.", "Light keep us.", "Stay close!" },
        ["shadowblade"] = new[] { "Pick a target.", "This won't take long.", "Try to keep up." },
        ["ranger"] = new[] { "Ooh, new ones!", "I've got eyes on them!", "Let's do this!" },
        ["berserker"] = new[] { "BLOOD AND ASH!", "Who's first?", "Finally!" },
        ["hexer"] = new[] { "Let me curse them.", "Such fragile things.", "Suffer slowly." },
    };

    private static readonly Dictionary<string, string[]> KillBarks = new()
    {
        ["vanguard"] = new[] { "Rest now.", "One down." },
        ["arcanist"] = new[] { "Crispy.", "Elementary." },
        ["warden"] = new[] { "Sleep, poor soul.", "Be at peace." },
        ["shadowblade"] = new[] { "Too easy.", "Next." },
        ["ranger"] = new[] { "Bullseye!", "Got one!" },
        ["berserker"] = new[] { "MORE!", "Next skull!" },
        ["hexer"] = new[] { "Withered.", "As foretold." },
    };

    private static readonly Dictionary<string, string[]> HurtBarks = new()
    {
        ["vanguard"] = new[] { "I... can hold.", "Not yet!" },
        ["arcanist"] = new[] { "Ow! My everything!", "I need a moment..." },
        ["warden"] = new[] { "I'm hurt... heal me, or I'll heal myself.", "Light... give me strength." },
        ["shadowblade"] = new[] { "That one stung.", "Tch. Bleeding." },
        ["ranger"] = new[] { "Ahh! Okay, ow!", "Someone help!" },
        ["berserker"] = new[] { "Now I'm angry.", "Pain is fuel!" },
        ["hexer"] = new[] { "You'll pay for that.", "My veil... is torn." },
    };

    private static readonly string[] BreakBarks = { "It's open, hit it!", "Broken! Now!", "It's staggered!", "Now's our chance!" };

    public static string? StartBark(string heroId, Random rng) => Pick(StartBarks, heroId, rng);
    public static string? KillBark(string heroId, Random rng) => Pick(KillBarks, heroId, rng);
    public static string? HurtBark(string heroId, Random rng) => Pick(HurtBarks, heroId, rng);
    public static string BreakBark(Random rng) => BreakBarks[rng.Next(BreakBarks.Length)];

    private static string? Pick(Dictionary<string, string[]> pool, string id, Random rng) =>
        pool.TryGetValue(id, out var lines) ? lines[rng.Next(lines.Length)] : null;

    /// <summary>What a boss or elite says when the fight starts and when its second phase begins.</summary>
    public static string? EnemyBark(string enemyId, bool phaseTwo) => (enemyId, phaseTwo) switch
    {
        ("colossus", false) => "CRUSH... THEM.",
        ("colossus", true) => "BONES... BREAK... LOOSE!",
        ("matriarch", false) => "Sink, little sparks.",
        ("matriarch", true) => "Then let the abyss take you!",
        ("hollow_king", false) => "Kneel.",
        ("hollow_king", true) => "I will devour you as I devoured the flame!",
        ("gravemaw", false) => "*hungry gurgling*",
        ("gravemaw", true) => "*FRENZIED HOWL*",
        ("templar", false) => "Heretics. Judgment comes.",
        ("siren_queen", false) => "Come closer... listen...",
        ("champion", false) => "The deep claims all.",
        ("behemoth", false) => "*the ground rumbles*",
        ("behemoth", true) => "*its crust splits with a roar*",
        ("mimic", false) => "*hungry wooden creaking*",
        _ => null,
    };
}
