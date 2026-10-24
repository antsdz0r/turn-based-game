using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Paged rules guide, reachable from the main menu.</summary>
public partial class HowToPlay : Screen
{
    private static readonly (string Title, string Body)[] Pages =
    [
        ("The Descent",
            "Choose [b]three heroes[/b] and descend through [b]five regions[/b] of the Ashen Vault: " +
            "[color=#ffd76b]the Ashen Crypt[/color], [color=#8fd0ff]the Drowned Halls[/color], [color=#d8f0ff]the Frozen Ossuary[/color], " +
            "[color=#b0b8ff]the Storm Spire[/color] and [color=#ff8a2c]the Cinder Throne[/color]. " +
            "Each region has four floors, its own monsters, and a [b]boss[/b] with its own mechanic on its last floor.\n\n" +
            "On the map, glowing rooms are reachable. Battles and elites give gold, XP and a choice of rewards. " +
            "Beating a region's boss heals the party 40% and grants a relic choice. Defeat [color=#ffd76b]the Hollow King[/color] " +
            "on the Cinder Throne to win.\n\n" +
            "[color=#ffd76b]Gold[/color] buys gear at shops, opens [b]sealed gates[/b] on the map (they lead to golden rooms with better rewards), " +
            "rerolls battle rewards, and can be offered to the Ember between regions.\n\n" +
            "HP, gold, gear and upgrades carry over between rooms. A hero who falls is dragged back to 1 HP after a won battle. " +
            "If the whole party falls, the run ends."),
        ("Turns & Actions",
            "Everyone acts once per round in [b]speed order[/b], shown in the top bar. Slowed units act later.\n\n" +
            "On a hero's turn you can: [b]Attack[/b] (free, builds resource), use an [b]ability[/b] (costs resource, some have cooldowns), " +
            "[b]Defend[/b] (-50% damage until your next turn), use an [b]item[/b], or [b]switch rows[/b].\n\n" +
            "Every hero has a unique resource: [color=#e0b84a]Resolve[/color] (gained when hit), [color=#6f9be0]Mana[/color] (regenerates), " +
            "[color=#ffe27a]Faith[/color], [color=#ff6b9a]Combo[/color] (built by quick attacks, spent by finishers) and [color=#7fd4a0]Focus[/color].\n\n" +
            "Keys [b]1-9[/b] pick actions. [b]Tab/arrows + Enter[/b] pick targets. [b]Esc[/b] cancels or pauses."),
        ("Rows",
            "Each side has a [b]front[/b] and a [b]back[/b] row (at most 2 heroes in front).\n\n" +
            "• [b]Melee[/b] attacks can only reach the front row while anyone stands there. The back row is safe from melee.\n" +
            "• Ranged attacks and spells reach anyone.\n" +
            "• Some abilities need a row: Chain Lightning and Volley need room in the back; Shield Bash needs the front.\n" +
            "• Kael's [b]Intercept[/b] takes hits for an ally; [b]Challenge[/b] taunts every enemy. Maela's [b]Shepherd[/b] moves an ally."),
        ("Weaknesses & Breaks",
            "Damage is [b]Physical, Fire, Ice or Lightning[/b]. Enemies are weak (x1.5), resistant (x0.5) or immune to some types. " +
            "Physical damage is reduced by armor.\n\n" +
            "The icons under an enemy's HP bar are its [color=#ffa040]weaknesses[/color]; the gold pips are its [color=#ffd76b]break meter[/color]. " +
            "Every hit on a weakness removes a pip (multi-hit attacks remove one per hit).\n\n" +
            "At zero pips the enemy is [color=#ffa040][b]BROKEN[/b][/color]: its prepared action is cancelled, it skips its next turn " +
            "and takes [b]double damage[/b] until it recovers. Bosses can change weaknesses mid-fight!\n\n" +
            "[color=#ffd76b]Traits:[/color] every level-up lets a hero pick 1 of 3 traits that change how they play."),
        ("Intents",
            "The bubble above each enemy shows exactly what it will do this round and to whom. What you see is what happens.\n\n" +
            "If a target dies, moves rows or a hero taunts, the bubble updates immediately.\n\n" +
            "[color=#ffa040]Charging[/color] enemies spend a round winding up a big attack, then unleash it next round. " +
            "Break them, or use Nyx's [b]Sever[/b] to interrupt them.\n\n" +
            "Hover an enemy to outline the heroes it threatens. Hover one of your actions to preview its damage on every target."),
        ("Status Effects",
            string.Join("\n", new[]
            {
                StatusKind.Poison, StatusKind.Burn, StatusKind.Bleed, StatusKind.Regen, StatusKind.Stun, StatusKind.Shield,
                StatusKind.Marked, StatusKind.Weakened, StatusKind.Empowered, StatusKind.Taunt, StatusKind.Slowed,
                StatusKind.Defending, StatusKind.Riposte, StatusKind.Guarded,
            }.Select(k => $"{Visuals.Img(Visuals.StatusIcon(k), 18)} {Visuals.StatusDescription(new StatusEffect { Kind = k, Amount = 3, Turns = 2 })}"))),
        ("Between Runs",
            "Every run earns [color=#ffb85a]Ember Shards[/color]: floors reached, elites and bosses slain, and a big bonus for winning. " +
            "Spend them at the [b]Ember Shrine[/b] on permanent blessings (more gold, potions, a starting relic, veteran heroes...).\n\n" +
            "Two more heroes wait to be freed: [b]Brakka[/b] the Berserker (defeat the Bone Colossus) and [b]Sable[/b] the Hexer " +
            "(defeat the Drowned Matriarch). You can also buy them with shards.\n\n" +
            "The [b]Daily Challenge[/b] gives everyone the same party, route and twist for the day (+50% shards).\n\n" +
            "On the road: elites come with random [color=#ff6a4a]modifiers[/color] (Frenzied, Armored, Splitting, Vampiric, Thorned), " +
            "some chests bite back, and a few choices you make in the Crypt come back to you much later..."),
        ("Parry & Heat",
            "When an enemy attacks, two rings appear on the target. Press [b]SPACE[/b] (or click) as they meet:\n" +
            "[color=#8fb3ff]Perfect[/color] = -75% damage, [color=#bfeaff]Good[/color] = -40%. " +
            "You can turn this off in Settings; the game is balanced to be winnable without it.\n\n" +
            "Winning a run unlocks the next [color=#ff8a2c]Heat[/color] level (up to 5). Each level adds a modifier:\n" +
            string.Join("\n", Heat.Modifiers.Select((m, i) => $"  {i + 1}. {m}"))),
    ];

    private int _page;
    private Godot.Label _title = null!;
    private RichTextLabel _body = null!;
    private Godot.Label _counter = null!;

    protected override void Build()
    {
        Audio.Music(this, "menu");
        AddBackdrop(new Color(0.45f, 0.42f, 0.55f), torches: true);
        var panel = Visuals.CenterPanel(this, 820);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(800, 460) };
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);

        _title = Visuals.Title("", 34);
        box.AddChild(_title);
        box.AddChild(Visuals.Separator());
        _body = Visuals.Rich("", 16, fit: false);
        _body.SizeFlagsVertical = SizeFlags.ExpandFill;
        box.AddChild(_body);

        var nav = new HBoxContainer();
        nav.AddThemeConstantOverride("separation", 10);
        box.AddChild(nav);
        nav.AddChild(Visuals.Button("Main Menu", () => GameSession.ChangeScene(GetTree(), GameSession.MenuScene), new Vector2(150, 44), 15));
        _counter = Visuals.Label("", 14, Visuals.TextDim);
        _counter.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _counter.HorizontalAlignment = HorizontalAlignment.Center;
        nav.AddChild(_counter);
        nav.AddChild(Visuals.Button("< Back", () => ShowPage(_page - 1), new Vector2(110, 44), 15));
        var next = Visuals.Button("Next >", () => ShowPage(_page + 1), new Vector2(110, 44), 15);
        nav.AddChild(next);
        next.CallDeferred(Control.MethodName.GrabFocus);
        ShowPage(0);
        Visuals.FadeIn(this, 0.3);
    }

    private void ShowPage(int page)
    {
        _page = (page + Pages.Length) % Pages.Length;
        _title.Text = Pages[_page].Title;
        _body.Text = Pages[_page].Body;
        _counter.Text = $"{_page + 1} / {Pages.Length}";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel")) GameSession.ChangeScene(GetTree(), GameSession.MenuScene);
        else if (@event.IsActionPressed("ui_right")) ShowPage(_page + 1);
        else if (@event.IsActionPressed("ui_left")) ShowPage(_page - 1);
    }
}
