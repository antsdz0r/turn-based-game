using System.Collections.Generic;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>After a won battle: gold, XP and level-ups, then pick one of three rewards (or skip).</summary>
public partial class RewardScene : Screen
{
    private readonly List<Godot.Button> _choiceButtons = new();
    private Godot.Button _continue = null!;
    private bool _taken;
    private HBoxContainer _cards = null!;
    private Godot.Button _reroll = null!;
    private Godot.Label _goldLabel = null!;

    protected override void Build()
    {
        var run = GameSession.Run;
        var result = GameSession.LastResult;
        if (run is null || result is null)
        {
            GameSession.CompleteNode(GetTree());
            return;
        }
        Audio.Music(this, "menu");
        if (GameSession.Run is { } regionRun) AddRegionBackdrop(regionRun.NodeRegion, 0.5f, torches: true); else AddBackdrop(new Color(0.5f, 0.46f, 0.58f), torches: true);

        var panel = Visuals.CenterPanel(this, 860);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(box);

        var title = Visuals.Title("Spoils of Battle", 36);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);

        var gains = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        gains.AddThemeConstantOverride("separation", 10);
        gains.AddChild(Visuals.Icon("UI/icon_coin.png", 24));
        _goldLabel = Visuals.Label($"+{result.Gold} gold  (total {run.Gold})", 18, Visuals.GoldBright);
        gains.AddChild(_goldLabel);
        gains.AddChild(Visuals.Spacer(0, 20));
        gains.AddChild(Visuals.Label($"+{result.Xp} XP to every hero", 18, Visuals.Text));
        box.AddChild(gains);
        // Results go in a short scrolling box so the cards and buttons always stay on screen.
        if (result.Lines.Count > 0)
        {
            var linesScroll = new ScrollContainer
            {
                CustomMinimumSize = new Vector2(820, Mathf.Min(120, 26 * result.Lines.Count)),
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            var lines = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            lines.AddThemeConstantOverride("separation", 2);
            linesScroll.AddChild(lines);
            foreach (var line in result.Lines)
            {
                var text = Visuals.Rich(line, 14);
                text.CustomMinimumSize = new Vector2(800, 0);
                lines.AddChild(text);
            }
            box.AddChild(linesScroll);
        }
        if (result.Lines.Count > 0 && result.Lines.Exists(l => l.Contains("level"))) Audio.Sfx(this, "levelup", 0f);
        else Audio.Sfx(this, "coin", 0f);

        box.AddChild(Visuals.Separator());
        box.AddChild(Visuals.Label("Choose one reward:", 17, Visuals.Text));
        _cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _cards.AddThemeConstantOverride("separation", 12);
        box.AddChild(_cards);
        foreach (var option in result.Choices) _cards.AddChild(BuildCard(run, option));

        var bottom = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        bottom.AddThemeConstantOverride("separation", 12);
        box.AddChild(bottom);
        bottom.AddChild(Visuals.Button("Party & Gear", () => AddChild(new PartyPanel(() => { })), new Vector2(180, 46), 16, "UI/icon_bag.png"));
        _reroll = Visuals.Button("", () => Reroll(run, result), new Vector2(190, 46), 16, "UI/icon_coin.png");
        bottom.AddChild(_reroll);
        UpdateReroll(run, result);
        _continue = Visuals.Button("Skip reward", Continue, new Vector2(220, 46), 18);
        bottom.AddChild(_continue);

        AddPauseMenu(inRun: true);
        Visuals.FadeIn(this, 0.4);
        // Level-ups first: pick traits, then the spoils.
        if (TraitPicker.ShowIfPending(this, () => { })) Pause?.MoveToFront();
    }

    private Control BuildCard(RunState run, RewardOption option)
    {
        var card = new PanelContainer { CustomMinimumSize = new Vector2(250, 196) };
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        card.AddChild(box);

        string icon = option.Kind switch
        {
            RewardKind.Relic => "UI/icon_relic.png",
            RewardKind.Gold => "UI/icon_coin.png",
            RewardKind.Upgrade => "UI/icon_empowered.png",
            _ => Database.HasItem(option.Id) ? Visuals.ItemIcon(Database.Item(option.Id)) : "UI/icon_bag.png",
        };
        var image = Visuals.Icon(icon, 48);
        image.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        if (option.Kind == RewardKind.Relic && Database.HasRelicDef(option.Id)) image.Modulate = new Color(Database.Relic(option.Id).ColorHex);
        box.AddChild(image);

        var kind = Visuals.Label(option.Kind.ToString().ToUpperInvariant(), 12, Visuals.Ember);
        kind.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(kind);
        var name = Visuals.Label(option.Title, 18, Visuals.GoldBright, serif: true);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(name);
        var desc = Visuals.Rich($"[center]{option.Description}[/center]", 13);
        desc.CustomMinimumSize = new Vector2(226, 0);
        desc.SizeFlagsVertical = SizeFlags.ExpandFill;
        box.AddChild(desc);

        var take = Visuals.Button("Take", () =>
        {
            if (_taken || !run.ApplyReward(option)) return;
            _taken = true;
            Audio.Sfx(this, option.Kind == RewardKind.Gold ? "coin" : "levelup", 0f, 0.8f);
            foreach (var b in _choiceButtons) b.Disabled = true;
            _reroll.Disabled = true;
            GameSession.SaveRun();
            _continue.Text = "Continue";
        }, new Vector2(0, 40), 16);
        if (!run.CanTake(option))
        {
            take.Disabled = true;
            take.TooltipText = "Your satchel is full.";
        }
        _choiceButtons.Add(take);
        box.AddChild(take);
        return card;
    }

    private void UpdateReroll(RunState run, BattleResult result)
    {
        int price = run.RerollPrice(result);
        _reroll.Text = $"Reroll ({price}g)";
        _reroll.Disabled = _taken || run.Gold < price;
        _reroll.TooltipText = run.Gold < price ? $"Not enough gold (you have {run.Gold})." : "Pay gold for a fresh set of rewards.";
    }

    private void Reroll(RunState run, BattleResult result)
    {
        if (_taken || !run.RerollRewards(result)) return;
        Audio.Sfx(this, "coin", 0f);
        GameSession.SaveRun();
        _goldLabel.Text = $"+{result.Gold} gold  (total {run.Gold})";
        _choiceButtons.Clear();
        foreach (var child in _cards.GetChildren()) child.QueueFree();
        foreach (var option in result.Choices)
        {
            var card = BuildCard(run, option);
            card.Modulate = new Color(1, 1, 1, 0);
            card.Scale = new Vector2(0.9f, 0.9f);
            _cards.AddChild(card);
            var t = card.CreateTween();
            t.TweenProperty(card, "modulate:a", 1f, 0.2);
            t.Parallel().TweenProperty(card, "scale", Vector2.One, 0.2).SetTrans(Tween.TransitionType.Back);
        }
        UpdateReroll(run, result);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Enter / Space always moves on, even if a button somehow ends up out of view.
        if (@event.IsActionPressed("ui_accept") && GetViewport().GuiGetFocusOwner() is null)
        {
            GetViewport().SetInputAsHandled();
            Continue();
        }
    }

    private void Continue()
    {
        GameSession.LastResult = null;
        GameSession.CompleteNode(GetTree());
    }
}
