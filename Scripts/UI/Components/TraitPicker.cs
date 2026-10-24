using System;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// Modal shown when a hero levels up: pick 1 of 3 traits. Chains through every hero with picks waiting,
/// then calls onDone. Picks are saved immediately, so quitting never loses or duplicates one.
/// </summary>
public partial class TraitPicker : Control
{
    private readonly Action _onDone;

    public TraitPicker() : this(() => { }) { }
    public TraitPicker(Action onDone)
    {
        _onDone = onDone;
        MouseFilter = MouseFilterEnum.Stop;
    }

    /// <summary>Adds a picker to the screen if any hero has a pick waiting. Returns true if one was shown.</summary>
    public static bool ShowIfPending(Control parent, Action onDone)
    {
        if (GameSession.Run?.HeroNeedingTrait is null) return false;
        var picker = new TraitPicker(onDone);
        parent.AddChild(picker);
        return true;
    }

    public override void _Ready()
    {
        Visuals.CoverScreen(this);
        Build();
    }

    private void Build()
    {
        foreach (var child in GetChildren()) child.QueueFree();
        var run = GameSession.Run;
        var hero = run?.HeroNeedingTrait;
        if (run is null || hero is null)
        {
            _onDone();
            QueueFree();
            return;
        }
        var choices = run.TraitChoices(hero);
        if (choices.Count == 0)
        {
            hero.PendingTraits = 0;
            GameSession.SaveRun();
            Build();
            return;
        }

        Visuals.Dimmer(this, 0.75f);
        var panel = Visuals.CenterPanel(this, 940);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 14);
        panel.AddChild(root);

        var head = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        head.AddThemeConstantOverride("separation", 16);
        root.AddChild(head);
        head.AddChild(Visuals.Portrait(hero.Id, 72));
        var titles = new VBoxContainer();
        head.AddChild(titles);
        titles.AddChild(Visuals.Label("LEVEL UP", 14, Visuals.Ember));
        titles.AddChild(Visuals.Title($"{hero.Def.Name} reached level {hero.Level - hero.PendingTraits + 1}", 28));
        titles.AddChild(Visuals.Label("Choose a trait. It lasts for the rest of the run.", 14, Visuals.TextDim));
        // Room to breathe on any screen: the whole picker scales down if needed (Visuals.FitToScreen).
        Audio.Sfx(this, "levelup", 0f, 0.8f);

        var cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        cards.AddThemeConstantOverride("separation", 14);
        root.AddChild(cards);
        int i = 0;
        foreach (var trait in choices)
        {
            var t = trait;
            var card = new PanelContainer { CustomMinimumSize = new Vector2(280, 230) };
            card.AddThemeStyleboxOverride("panel", Visuals.Box(new Color("#1d1726"), new Color("#8a6640"), 2, 10, 14));
            var box = new VBoxContainer();
            box.AddThemeConstantOverride("separation", 10);
            card.AddChild(box);
            var icon = Visuals.Icon("UI/icon_empowered.png", 40);
            icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            box.AddChild(icon);
            var name = Visuals.Label(t.Name, 21, Visuals.GoldBright, serif: true);
            name.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(name);
            var desc = Visuals.Rich($"[center]{t.Description}[/center]", 15);
            desc.CustomMinimumSize = new Vector2(250, 0);   // a real width, so wrapping can't make it huge
            desc.SizeFlagsVertical = SizeFlags.ExpandFill;
            box.AddChild(desc);
            var pick = Visuals.Button("Choose", () =>
            {
                if (!run.PickTrait(hero, t.Id)) return;
                Audio.Sfx(this, "buff", 0f, 0.9f);
                GameSession.SaveRun();
                Build();
            }, new Vector2(0, 44), 17);
            box.AddChild(pick);
            if (i++ == 0) pick.CallDeferred(Control.MethodName.GrabFocus);

            // Cards deal in one after another.
            card.Modulate = new Color(1, 1, 1, 0);
            var tween = card.CreateTween();
            tween.TweenInterval(0.08 * i);
            tween.TweenProperty(card, "modulate:a", 1f, 0.2);
            cards.AddChild(card);
        }

        if (hero.Traits.Count > 0)
        {
            var current = Visuals.Rich("[center][color=#a99f8f]Current traits: " +
                string.Join(", ", hero.Traits.Where(Database.HasTraitDef).Select(id => Database.Trait(id).Name)) + "[/color][/center]", 13);
            current.CustomMinimumSize = new Vector2(860, 0);
            root.AddChild(current);
        }
    }
}
