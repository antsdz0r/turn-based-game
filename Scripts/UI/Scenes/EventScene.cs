using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Handles both map events (choices with consequences) and treasure chests.</summary>
public partial class EventScene : Screen
{
    private VBoxContainer _box = null!;

    protected override void Build()
    {
        var run = GameSession.Run ?? GameSession.EnsureRun();
        if (run.ForcedEncounter is not null)
        {
            // An event fight was chosen before the game was closed: go straight to it.
            GameSession.ChangeScene(GetTree(), GameSession.BattleScene);
            return;
        }
        Audio.Music(this, "menu");
        if (GameSession.Run is { } regionRun) AddRegionBackdrop(regionRun.NodeRegion, 0.5f, torches: true); else AddBackdrop(new Color(0.48f, 0.45f, 0.56f));

        var panel = Visuals.CenterPanel(this, 680);
        _box = new VBoxContainer();
        _box.AddThemeConstantOverride("separation", 12);
        panel.AddChild(_box);

        bool mimic = run.Node is { Type: NodeType.Treasure, Golden: false } node && run.NodeRng(11).Next(100) < 16
                     && !run.SeenEvents.Contains($"opened_{node.Id}");
        if (mimic) BuildMimic(run);
        else if (run.Node?.Type == NodeType.Treasure) BuildTreasure(run);
        else BuildEvent(run);

        AddPauseMenu(inRun: true);
        Visuals.FadeIn(this, 0.4);
    }

    private void BuildTreasure(RunState run)
    {
        var title = Visuals.Title("A Sealed Chest", 32);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _box.AddChild(title);
        var chest = Visuals.Icon("UI/icon_node_treasure.png", 96);
        chest.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        _box.AddChild(chest);
        _box.AddChild(Visuals.Label("Ash coats the lid. Something inside still glints.", 15, Visuals.TextDim));
        Godot.Button? open = null;
        open = Visuals.Button("Open it", () =>
        {
            if (open is null) return;
            open.Visible = false;
            var lines = run.OpenTreasure();
            Audio.Sfx(this, "coin", 0f);
            var shake = chest.CreateTween();
            shake.TweenProperty(chest, "scale", new Vector2(1.15f, 1.15f), 0.1);
            shake.TweenProperty(chest, "scale", Vector2.One, 0.15);
            foreach (var line in lines) _box.AddChild(Visuals.Rich(line, 16));
            run.CompleteNode();
            GameSession.SaveRun();
            AddContinue();
        }, new Vector2(0, 48), 18);
        _box.AddChild(open);
    }

    /// <summary>A chest that turns out to have teeth.</summary>
    private void BuildMimic(RunState run)
    {
        var title = Visuals.Title("A Sealed Chest...?", 32);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _box.AddChild(title);
        var chest = Visuals.Icon("Units/mimic.png", 128);
        chest.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        chest.FlipH = true;
        _box.AddChild(chest);
        var breathe = chest.CreateTween().SetLoops();
        breathe.TweenProperty(chest, "scale", new Vector2(1.04f, 0.97f), 0.6).SetTrans(Tween.TransitionType.Sine);
        breathe.TweenProperty(chest, "scale", Vector2.One, 0.6).SetTrans(Tween.TransitionType.Sine);
        _box.AddChild(Visuals.Rich("[color=#c9bfae]As you reach for the lid, the chest [b]breathes[/b]. A long tongue slides out from under it, tasting the air.[/color]", 16));
        var choices = new VBoxContainer();
        choices.AddThemeConstantOverride("separation", 8);
        _box.AddChild(choices);
        choices.AddChild(Visuals.Button("Fight it  —  an elite battle. Mimics hoard relics and gold.", () =>
        {
            run.SeenEvents.Add($"opened_{run.CurrentNode}");
            run.ForcedEncounter = new() { "mimic" };
            run.ForcedElite = true;   // elite rewards: a relic among the choices
            GameSession.SaveRun();
            Audio.Sfx(this, "growl", 0f);
            GameSession.ChangeScene(GetTree(), GameSession.BattleScene);
        }, new Vector2(0, 44), 15));
        choices.AddChild(Visuals.Button("Back away slowly  —  nothing happens.", () =>
        {
            run.SeenEvents.Add($"opened_{run.CurrentNode}");
            choices.QueueFree();
            _box.AddChild(Visuals.Rich("[color=#ffd76b]The chest sighs, disappointed, and goes back to sleep.[/color]", 16));
            run.CompleteNode();
            GameSession.SaveRun();
            AddContinue();
        }, new Vector2(0, 44), 15));
    }

    private void BuildEvent(RunState run)
    {
        var rng = run.NodeRng(5);
        var ev = Events.Pick(rng, run);
        var title = Visuals.Title(ev.Title, 32);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _box.AddChild(title);
        _box.AddChild(Visuals.Rich($"[color=#c9bfae]{ev.Text}[/color]", 16));
        _box.AddChild(Visuals.Separator());

        var choices = new VBoxContainer();
        choices.AddThemeConstantOverride("separation", 8);
        _box.AddChild(choices);
        foreach (var choice in ev.Choices)
        {
            bool available = choice.Available(run);
            var button = Visuals.Button($"{choice.Label}  —  {choice.Hint}", () =>
            {
                string outcome = choice.Apply(run, run.NodeRng(6));
                if (!run.SeenEvents.Contains(ev.Id)) run.SeenEvents.Add(ev.Id);
                choices.QueueFree();
                Audio.Sfx(this, "turn", 0.02f, 0.7f);
                _box.AddChild(Visuals.Rich($"[color=#ffd76b]{outcome}[/color]", 16));
                if (run.ForcedEncounter is not null)
                {
                    // The choice started a fight: the node completes after the battle.
                    GameSession.SaveRun();
                    var fight = Visuals.Button("To battle!", () => GameSession.ChangeScene(GetTree(), GameSession.BattleScene), new Vector2(0, 46), 17);
                    _box.AddChild(fight);
                    fight.CallDeferred(Control.MethodName.GrabFocus);
                    return;
                }
                run.CompleteNode();
                GameSession.SaveRun();
                AddContinue();
            }, new Vector2(0, 44), 15);
            button.Alignment = HorizontalAlignment.Left;
            button.Disabled = !available;
            if (!available) button.TooltipText = "You can't do that right now.";
            choices.AddChild(button);
        }
    }

    private void AddContinue()
    {
        var cont = Visuals.Button("Continue", () => GameSession.CompleteNode(GetTree()), new Vector2(0, 46), 17);
        _box.AddChild(cont);
        cont.CallDeferred(Control.MethodName.GrabFocus);
    }
}
