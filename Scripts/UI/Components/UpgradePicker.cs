using System;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Modal: choose a hero, then one of their not-yet-upgraded abilities.</summary>
public partial class UpgradePicker : Control
{
    private readonly Action<string, string> _onPick;
    private readonly Action _onCancel;
    private VBoxContainer _list = null!;
    private string _heroId = "";

    public UpgradePicker() : this((_, _) => { }, () => { }) { }

    public UpgradePicker(Action<string, string> onPick, Action onCancel)
    {
        _onPick = onPick;
        _onCancel = onCancel;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        Visuals.CoverScreen(this);
        Visuals.Dimmer(this, 0.7f);
        var panel = Visuals.CenterPanel(this, 900);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        var title = Visuals.Title("Upgrade an Ability", 28);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(title);
        var note = Visuals.Label("Upgraded: +35% damage/healing/shield, +1 status potency and duration, -1 cooldown. Many abilities also gain a twist (shown below).", 13, Visuals.TextDim);
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        note.CustomMinimumSize = new Vector2(860, 0);
        note.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(note);

        var heroes = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        heroes.AddThemeConstantOverride("separation", 8);
        root.AddChild(heroes);
        var run = GameSession.Run;
        if (run is null) return;
        _heroId = run.Party[0].Id;
        foreach (var hero in run.Party)
        {
            var button = Visuals.Button(hero.Def.Name, () => { _heroId = hero.Id; Fill(); }, new Vector2(200, 44), 14);
            button.Icon = Visuals.Tex($"Portraits/{hero.Id}.png");
            button.ExpandIcon = true;
            heroes.AddChild(button);
        }

        // Scrollable list of full ability cards, so nothing is ever cut off.
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(860, 380),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        root.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_list);
        root.AddChild(Visuals.Button("Cancel", () => { _onCancel(); QueueFree(); }, new Vector2(0, 40)));
        Fill();
    }

    private void Fill()
    {
        foreach (var child in _list.GetChildren()) child.QueueFree();
        var run = GameSession.Run;
        if (run is null) return;
        var hero = run.Hero(_heroId);
        foreach (var id in hero.AbilityIds.Prepend(hero.Def.BasicAbility))
        {
            var ability = Database.Ability(id);
            bool done = hero.Upgrades.Contains(id);

            var card = new PanelContainer();
            card.AddThemeStyleboxOverride("panel", Visuals.Box(new Color("#1a1520"), done ? new Color("#3a3244") : new Color("#6a5a3a"), 1, 6, 10));
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);
            card.AddChild(row);
            var icon = Visuals.Icon(Visuals.AbilityIcon(ability), 40);
            icon.SizeFlagsVertical = SizeFlags.ShrinkBegin;
            row.AddChild(icon);

            var text = Visuals.Rich(
                $"[font_size=17][color=#ffd76b]{ability.Name}[/color]{(done ? " [color=#7fd4a0](upgraded)[/color]" : "")}[/font_size]\n" +
                $"[color=#a99f8f]Now:[/color] {ability.RulesText(done)}" +
                (done ? "" : $"\n[color=#7fd4a0]Upgraded:[/color] {ability.RulesText(true)}"), 13);
            text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            text.CustomMinimumSize = new Vector2(640, 0);
            row.AddChild(text);

            var button = Visuals.Button(done ? "Done" : "Upgrade", () =>
            {
                _onPick(hero.Id, id);
                QueueFree();
            }, new Vector2(120, 44), 15);
            button.Disabled = done;
            button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            row.AddChild(button);
            _list.AddChild(card);
        }
    }
}
