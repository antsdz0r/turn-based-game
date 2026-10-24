using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// A short story scene: letterbox bars, a title card, the party on the left and the keeper or a boss on the
/// right, and a typewriter dialogue box. Click / Space / Enter advances; Esc or "Skip" ends it.
/// </summary>
public partial class CutsceneScene : Screen
{
    private const float CharsPerSecond = 48f;

    private Cutscene _scene = null!;
    private RunState _run = null!;
    private int _line = -1;
    private float _shown;
    private bool _typing;
    private bool _finished;
    private bool _choosing;
    private int _blipCounter;

    private readonly Dictionary<string, Sprite2D> _actors = new();
    private readonly Dictionary<string, Vector2> _homes = new();
    private PanelContainer _box = null!;
    private TextureRect _portrait = null!;
    private Godot.Label _name = null!;
    private RichTextLabel _text = null!;
    private Godot.Label _more = null!;
    private HBoxContainer _choices = null!;
    private Godot.Label _narration = null!;
    private Control _titleCard = null!;

    protected override void Build()
    {
        var run = GameSession.StoryRun;
        var scene = GameSession.PendingCutscene is { } id && run is not null ? Story.Get(id, run) : null;
        if (run is null || scene is null)
        {
            GameSession.FinishCutscene(GetTree());
            return;
        }
        _run = run;
        _scene = scene;

        Audio.Music(this, scene.Music);
        AddRegionBackdrop(Database.Region(scene.Region), 0.55f, torches: true);
        var size = GetViewportRect().Size;
        float baseline = size.Y * 0.66f;

        // Party on the left
        for (int i = 0; i < run.Party.Count; i++)
        {
            var sprite = Visuals.UnitSprite(run.Party[i].Sprite, 2.5f);
            var home = new Vector2(size.X * 0.14f + i * 130, baseline + (i % 2) * 14);
            sprite.Position = home + new Vector2(-400, 0);
            AddChild(sprite);
            _actors[$"h{i}"] = sprite;
            _homes[$"h{i}"] = home;
            var walk = sprite.CreateTween();
            walk.TweenInterval(0.2 + i * 0.12);
            walk.TweenProperty(sprite, "position", home, 0.7).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }

        // Keeper or boss on the right
        bool keeper = scene.RightSprite == Story.KeeperSprite;
        var right = Visuals.UnitSprite(scene.RightSprite, 2.5f * scene.RightScale, flip: keeper);
        var rightHome = new Vector2(size.X * 0.78f, baseline);
        right.Position = rightHome;
        right.Modulate = new Color(scene.RightTint) with { A = 0f };
        AddChild(right);
        string rightKey = keeper ? "keeper" : "boss";
        _actors[rightKey] = right;
        _homes[rightKey] = rightHome;
        var appear = right.CreateTween();
        appear.TweenInterval(0.6);
        appear.TweenProperty(right, "modulate:a", keeper ? 0.85f : 1f, 0.8);
        if (keeper)
        {
            var bob = right.CreateTween().SetLoops();
            bob.TweenProperty(right, "position", rightHome + new Vector2(0, -12), 1.4).SetTrans(Tween.TransitionType.Sine);
            bob.TweenProperty(right, "position", rightHome, 1.4).SetTrans(Tween.TransitionType.Sine);
            // A cold, ghostly halo behind the keeper.
            var halo = new Sprite2D
            {
                Texture = Visuals.MakeSoftCircle(new Color(0.6f, 0.85f, 1f, 0.35f)),
                Scale = new Vector2(5, 5),
                Position = rightHome + new Vector2(0, -80),
            };
            AddChild(halo);
            MoveChild(halo, right.GetIndex());
            var pulse = halo.CreateTween().SetLoops();
            pulse.TweenProperty(halo, "scale", new Vector2(5.6f, 5.6f), 1.6).SetTrans(Tween.TransitionType.Sine);
            pulse.TweenProperty(halo, "scale", new Vector2(5f, 5f), 1.6).SetTrans(Tween.TransitionType.Sine);
        }

        AddLetterbox(size);
        BuildDialogBox();
        BuildTitleCard();

        var skip = Visuals.Button("Skip  ▸▸", Finish, new Vector2(120, 36), 14);
        skip.SetAnchorsPreset(LayoutPreset.TopRight);
        skip.Position = new Vector2(size.X - 140, 14);
        skip.FocusMode = FocusModeEnum.None;
        AddChild(skip);

        Visuals.FadeIn(this, 0.5);
        GetTree().CreateTimer(2.1).Timeout += () => { if (IsInstanceValid(this) && _line < 0) Next(); };
    }

    private void AddLetterbox(Vector2 size)
    {
        foreach (bool top in new[] { true, false })
        {
            var bar = new ColorRect { Color = Colors.Black, Size = new Vector2(size.X, 64), MouseFilter = MouseFilterEnum.Ignore };
            bar.Position = new Vector2(0, top ? -64 : size.Y);
            AddChild(bar);
            bar.CreateTween().TweenProperty(bar, "position:y", top ? 0f : size.Y - 64, 0.6).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }
    }

    private void BuildTitleCard()
    {
        var box = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        box.OffsetBottom = -180;
        var sub = Visuals.Label(_scene.Subtitle.ToUpperInvariant(), 16, Visuals.Ember);
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        var title = Visuals.Title(_scene.Title, 54);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(sub);
        box.AddChild(title);
        box.Modulate = new Color(1, 1, 1, 0);
        AddChild(box);
        _titleCard = box;
        var t = box.CreateTween();
        t.TweenInterval(0.3);
        t.TweenProperty(box, "modulate:a", 1f, 0.6);
        t.TweenInterval(1.0);
        t.TweenProperty(box, "modulate:a", 0f, 0.5);
    }

    private void BuildDialogBox()
    {
        _narration = Visuals.Label("", 22, new Color("#e8dcc4"), serif: true);
        _narration.HorizontalAlignment = HorizontalAlignment.Center;
        _narration.VerticalAlignment = VerticalAlignment.Center;
        _narration.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _narration.SetAnchorsPreset(LayoutPreset.CenterTop);
        _narration.Size = new Vector2(900, 0);
        _narration.CustomMinimumSize = new Vector2(900, 120);
        _narration.Position = new Vector2(GetViewportRect().Size.X / 2 - 450, 90);
        _narration.Modulate = new Color(1, 1, 1, 0);
        AddChild(_narration);

        _box = new PanelContainer { CustomMinimumSize = new Vector2(0, 150) };
        _box.SetAnchorsPreset(LayoutPreset.BottomWide);
        _box.GrowVertical = GrowDirection.Begin;
        _box.OffsetLeft = 120;
        _box.OffsetRight = -120;
        _box.OffsetBottom = -74;
        _box.AddThemeStyleboxOverride("panel", Visuals.Box(new Color(0.05f, 0.04f, 0.07f, 0.94f), Visuals.Gold, 2, 10, 14));
        _box.Modulate = new Color(1, 1, 1, 0);
        _box.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_box);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        _box.AddChild(row);
        _portrait = Visuals.Icon("Portraits/vanguard.png", 96);
        _portrait.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(_portrait);
        var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 6);
        row.AddChild(col);
        _name = Visuals.Label("", 20, Visuals.GoldBright, serif: true);
        col.AddChild(_name);
        // Fit to content: the box grows upward for long lines instead of clipping them.
        _text = Visuals.Rich("", 18, fit: true);
        _text.CustomMinimumSize = new Vector2(0, 84);
        _text.SizeFlagsVertical = SizeFlags.ExpandFill;
        col.AddChild(_text);
        _choices = new HBoxContainer { Visible = false };
        _choices.AddThemeConstantOverride("separation", 12);
        col.AddChild(_choices);
        _more = Visuals.Label("▼  click / space", 12, Visuals.TextDim);
        _more.HorizontalAlignment = HorizontalAlignment.Right;
        col.AddChild(_more);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!_typing) return;
        _shown += (float)delta * CharsPerSecond;
        int total = _text.GetTotalCharacterCount();
        int visible = Mathf.Min(total, (int)_shown);
        if (visible != _text.VisibleCharacters)
        {
            _text.VisibleCharacters = visible;
            _narration.VisibleCharacters = visible;
            if (++_blipCounter % 3 == 0 && CurrentLine?.Speaker != "narrator") Audio.Sfx(this, "blip", 0.15f, 0.25f);
        }
        if (visible >= total) CompleteLine();
    }

    /// <summary>The line on screen (can be a choice's result, which is not part of the script).</summary>
    private DialogueLine? _current;
    private DialogueLine? CurrentLine => _current;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_finished || _choosing) return;
        bool advance = @event.IsActionPressed("ui_accept")
                       || (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left });
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Finish();
            return;
        }
        if (!advance) return;
        GetViewport().SetInputAsHandled();
        if (_line < 0) { Next(); return; }
        if (_typing) CompleteLine();
        else Next();
    }

    private void CompleteLine()
    {
        _typing = false;
        _text.VisibleCharacters = -1;
        _narration.VisibleCharacters = -1;
        if (CurrentLine?.Choices is { Count: > 0 } choices) ShowChoices(choices);
        else _more.Visible = true;
    }

    private void Next()
    {
        if (_finished) return;
        _line++;
        if (_line >= _scene.Lines.Count)
        {
            Finish();
            return;
        }
        if (_titleCard.Modulate.A > 0) _titleCard.Modulate = new Color(1, 1, 1, 0);
        ShowLine(_scene.Lines[_line]);
    }

    private void ShowLine(DialogueLine line)
    {
        _current = line;
        _choices.Visible = false;
        _more.Visible = false;
        _shown = 0;
        _typing = true;
        bool narrator = line.Speaker == "narrator";

        _narration.CreateTween().TweenProperty(_narration, "modulate:a", narrator ? 1f : 0f, 0.25);
        _box.CreateTween().TweenProperty(_box, "modulate:a", narrator ? 0f : 1f, 0.2);
        if (narrator)
        {
            _narration.Text = line.Text;
            _narration.VisibleCharacters = 0;
            _text.Text = line.Text; // drives the character count
            _text.VisibleCharacters = 0;
        }
        else
        {
            _name.Text = SpeakerName(line.Speaker);
            _portrait.Texture = Visuals.Tex(SpeakerPortrait(line.Speaker));
            _portrait.FlipH = line.Speaker == "keeper";
            _portrait.Modulate = line.Speaker == "keeper" ? new Color(_scene.RightTint) : Colors.White;
            _text.Text = line.Speaker == "boss" ? $"[color=#ffb0a0]{line.Text}[/color]" : line.Text;
            _text.VisibleCharacters = 0;
            Audio.Sfx(this, "turn", 0.05f, 0.35f);
        }
        Spotlight(narrator ? null : line.Speaker);
    }

    /// <summary>The speaker is lit and hops; everyone else steps into the shadows.</summary>
    private void Spotlight(string? speaker)
    {
        foreach (var (key, sprite) in _actors)
        {
            bool active = speaker is null || key == speaker;
            float alpha = key == "keeper" ? 0.85f : 1f;
            var target = active ? new Color(1, 1, 1, alpha) : new Color(0.45f, 0.42f, 0.5f, alpha);
            if (key == "keeper") target *= new Color(_scene.RightTint);
            sprite.CreateTween().TweenProperty(sprite, "self_modulate", target, 0.2);
            if (key == speaker && key != "keeper")
            {
                var home = _homes[key];
                var hop = sprite.CreateTween();
                hop.TweenProperty(sprite, "position", home + new Vector2(0, -16), 0.1).SetEase(Tween.EaseType.Out);
                hop.TweenProperty(sprite, "position", home, 0.14).SetEase(Tween.EaseType.In);
            }
        }
        if (speaker == "boss" && _actors.TryGetValue("boss", out var boss))
        {
            // Bosses speak with weight: a rumble and a pulse.
            var pulse = boss.CreateTween();
            pulse.TweenProperty(boss, "scale", boss.Scale * new Vector2(1.06f, 0.95f), 0.08);
            pulse.TweenProperty(boss, "scale", boss.Scale, 0.2);
            Audio.Sfx(this, "growl", 0.1f, 0.6f);
            var shake = CreateTween();
            for (int i = 0; i < 5; i++) shake.TweenProperty(this, "position", new Vector2((float)GD.RandRange(-6.0, 6.0), (float)GD.RandRange(-4.0, 4.0)), 0.03);
            shake.TweenProperty(this, "position", Vector2.Zero, 0.05);
        }
    }

    private string SpeakerName(string speaker) => speaker switch
    {
        "keeper" => Story.KeeperName,
        "boss" => _scene.RightName,
        _ when speaker.StartsWith('h') && int.TryParse(speaker[1..], out int i) && i < _run.Party.Count => _run.Party[i].Def.Name,
        _ => "",
    };

    private string SpeakerPortrait(string speaker) => speaker switch
    {
        "keeper" or "boss" => $"Units/{_scene.RightSprite}.png",
        _ when speaker.StartsWith('h') && int.TryParse(speaker[1..], out int i) && i < _run.Party.Count => $"Portraits/{_run.Party[i].Sprite}.png",
        _ => "Portraits/vanguard.png",
    };

    private void ShowChoices(IReadOnlyList<DialogueChoice> choices)
    {
        _choosing = true;
        _choices.Visible = true;
        foreach (var child in _choices.GetChildren()) child.QueueFree();
        Godot.Button? first = null;
        foreach (var choice in choices)
        {
            var c = choice;
            var button = Visuals.Button(c.Text, () => Choose(c), new Vector2(0, 40), 15, c.Cost > 0 ? "UI/icon_coin.png" : null);
            if (c.Cost > _run.Gold)
            {
                button.Disabled = true;
                button.TooltipText = $"You have {_run.Gold} gold.";
            }
            _choices.AddChild(button);
            if (first is null && !button.Disabled) first = button;
        }
        first?.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void Choose(DialogueChoice choice)
    {
        _choices.Visible = false;
        var result = Story.ApplyChoice(_run, choice);
        if (choice.Cost > 0 && result is not null) Audio.Sfx(this, "coin", 0f);
        if (choice.Action == "offer_heal" && result is not null) Audio.Sfx(this, "heal", 0f);
        GameSession.SaveRun();
        // Let input settle so the click that chose doesn't also skip the result line.
        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            _choosing = false;
            if (result is null) { Next(); return; }
            ShowLine(new DialogueLine("narrator", result));
        };
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        var fade = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = MouseFilterEnum.Stop };
        fade.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(fade);
        var t = fade.CreateTween();
        t.TweenProperty(fade, "color:a", 1f, 0.5);
        t.TweenCallback(Callable.From(() => GameSession.FinishCutscene(GetTree())));
    }
}
