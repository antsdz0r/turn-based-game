using System.Threading.Tasks;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// "Game feel" helpers for the battle screen: afterimages on fast moves, hit-stop, a small camera punch on big
/// hits, a squash on the struck unit, and enemy deaths that dissolve into embers.
/// </summary>
public partial class BattleScene
{
    /// <summary>A fading copy of the unit's current pose, drawn behind the units.</summary>
    private void Afterimage(BattleUnitView v, Color tint, float alpha, double life = 0.28)
    {
        if (!IsInstanceValid(v) || v.Sprite.Texture is null) return;
        var sprite = v.Sprite;
        var ghost = new Sprite2D
        {
            Texture = sprite.Texture,
            Offset = sprite.Offset,
            FlipH = sprite.FlipH,
            TextureFilter = TextureFilterEnum.Nearest,
            Modulate = new Color(tint.R, tint.G, tint.B, alpha),
        };
        _field.AddChild(ghost);
        _field.MoveChild(ghost, 0);
        ghost.GlobalPosition = sprite.GlobalPosition;
        ghost.GlobalRotation = sprite.GlobalRotation;
        ghost.GlobalScale = sprite.GlobalScale;
        var t = ghost.CreateTween();
        t.TweenProperty(ghost, "modulate:a", 0f, life);
        t.TweenCallback(Callable.From(ghost.QueueFree));
    }

    /// <summary>Leaves a trail of afterimages while a unit dashes.</summary>
    private async void TrailDuring(BattleUnitView v, double duration, Color tint, int count = 4)
    {
        for (int i = 0; i < count; i++)
        {
            if (!IsInstanceValid(this) || !IsInsideTree()) return;
            Afterimage(v, tint, 0.55f - i * 0.08f);
            await Wait(duration / count);
        }
    }

    /// <summary>Freezes the action for a split second (real time, whatever the battle speed).</summary>
    private async Task HitStop(double seconds)
    {
        if (GameSession.Settings.ReduceMotion) return;
        double speed = Engine.TimeScale;
        Engine.TimeScale = 0.05;
        await ToSignal(GetTree().CreateTimer(seconds, true, false, true), SceneTreeTimer.SignalName.Timeout);
        Engine.TimeScale = speed;
    }

    /// <summary>A quick zoom toward the centre of the battlefield and back.</summary>
    private void CameraPunch(float amount = 0.03f)
    {
        if (GameSession.Settings.ReduceMotion) return;
        foreach (var layer in new[] { _field, _fx })
        {
            layer.PivotOffset = layer.Size / 2;
            var t = layer.CreateTween();
            t.TweenProperty(layer, "scale", Vector2.One * (1 + amount), 0.05);
            t.TweenProperty(layer, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }
    }

    /// <summary>The struck unit squashes and springs back.</summary>
    private static void HitSquash(BattleUnitView v, bool heavy)
    {
        var t = v.Body.CreateTween();
        float k = heavy ? 0.2f : 0.12f;
        t.TweenProperty(v.Body, "scale", new Vector2(1 + k, 1 - k), 0.05);
        t.TweenProperty(v.Body, "scale", new Vector2(1 - k * 0.4f, 1 + k * 0.4f), 0.08);
        t.TweenProperty(v.Body, "scale", Vector2.One, 0.12).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    /// <summary>Enemies don't just fall over: they flare white, collapse and blow away as embers.</summary>
    private async Task DissolveDeath(BattleUnitView v)
    {
        Flash(v, new Color("#fff2d0"), 1f);
        var ember = new Color("#ff8a2c");
        var t = v.Body.CreateTween();
        t.TweenProperty(v.Body, "scale", new Vector2(1.15f, 0.9f), 0.08);
        t.TweenProperty(v.Body, "scale", new Vector2(1.35f, 0.08f), 0.45).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        t.Parallel().TweenProperty(v.Body, "modulate", new Color(ember.R, ember.G * 0.8f, ember.B * 0.6f, 0f), 0.45);
        t.Parallel().TweenProperty(v.Shadow, "modulate:a", 0f, 0.45);
        for (int i = 0; i < 3; i++)
        {
            Burst(v.Center + new Vector2((float)GD.RandRange(-20.0, 20.0), 10 * i), i == 1 ? new Color("#ffd27a") : ember,
                  22, Vector2.Up, 40, 170, 1.1, gravity: -140);
            await Wait(0.12);
        }
        await ToSignal(t, Tween.SignalName.Finished);
    }

    // ═════════════════════════ Monster voices ═════════════════════════

    private double _lastVoice = -10;
    private static readonly System.Collections.Generic.Dictionary<string, int> VoiceClipCounts = new()
    {
        ["talk"] = 6, ["attack"] = 3, ["hurt"] = 2, ["death"] = 2,
    };

    /// <summary>Plays the enemy's voice for this moment (rate-limited, except deaths and bosses).</summary>
    private void PlayVoice(Combatant unit, VoiceCue cue)
    {
        if (unit.IsHero) return;
        double now = Time.GetTicksMsec() / 1000.0;
        if (cue != VoiceCue.Death && !unit.IsBoss && now - _lastVoice < 0.9) return;
        _lastVoice = now;
        string key = cue.ToString().ToLowerInvariant();
        string voice = MobLines.Voice(unit.DefId);
        // Creatures have 4 "talk" clips, speakers 6: try a random index and fall back to 0.
        int count = VoiceClipCounts[key];
        string clip = $"{voice}_{key}_{_barkRng.Next(count)}";
        if (!ResourceLoader.Exists($"res://Audio/Voice/{clip}.ogg")) clip = $"{voice}_{key}_0";
        // Small and big versions of the same monster sound different: pitch follows the sprite scale.
        float pitch = Mathf.Clamp(1.15f - 0.25f * unit.Scale, 0.75f, 1.2f) * (float)GD.RandRange(0.95, 1.05);
        Audio.Voice(this, clip, unit.IsBoss ? 1.1f : 0.85f, pitch);
    }

    /// <summary>A monster says something (bubble + voice) with the given chance.</summary>
    private void MobSpeak(Combatant unit, MobCue cue, int chancePercent)
    {
        if (unit.IsHero || !unit.IsAlive || _barkRng.Next(100) >= chancePercent) return;
        if (MobLines.Line(unit.DefId, cue, _barkRng) is { } line) Say(unit, line);
    }

    // ═════════════════════════ Omen & bounty panel ═════════════════════════

    private PanelContainer? _objectivePanel;
    private RichTextLabel? _objectives;
    private bool _bountyAnnounced;

    /// <summary>A small panel under the top bar: this fight's omen (battlefield condition) and bounty (bonus objective).</summary>
    private void BuildObjectives()
    {
        if (_battle.Omen is null && _battle.Bounty is null) return;
        _objectivePanel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        _objectivePanel.AddThemeStyleboxOverride("panel", Visuals.Box(new Color(0.07f, 0.05f, 0.09f, 0.88f), new Color("#8a6640"), 2, 8, 8));
        _objectivePanel.Position = new Vector2(12, 62);
        _objectives = Visuals.Rich("", 13);
        _objectives.CustomMinimumSize = new Vector2(300, 0);
        _objectivePanel.AddChild(_objectives);
        AddChild(_objectivePanel);
        Pause?.MoveToFront();
        if (Omens.Get(_battle.Omen) is { } omen)
        {
            _objectivePanel.TooltipText = omen.Description;
            Hint("omen", $"This fight has an [b]omen[/b]: [color={omen.ColorHex}]{omen.Name}[/color]. Omens change the battlefield for both sides; read the panel in the top-left.");
        }
        else if (_battle.Bounty is not null)
            Hint("bounty", "This fight has a [b]bounty[/b]: an optional objective. Meet it and win for extra gold.");
        UpdateObjectives();
    }

    private void UpdateObjectives()
    {
        if (_objectives is null) return;
        var lines = new System.Collections.Generic.List<string>();
        if (Omens.Get(_battle.Omen) is { } omen)
            lines.Add($"[color={omen.ColorHex}][b]Omen: {omen.Name}[/b][/color]\n[color=#c9bfae]{omen.Description}[/color]");
        if (Bounties.Get(_battle.Bounty) is { } bounty)
        {
            bool met = _battle.BountyMet;
            bool failed = !met && bounty.Id is "swift" or "untouched" && (_battle.Round > 4 || _battle.HeroFalls > 0);
            string mark = met ? "[color=#6be07a]✔[/color]" : failed ? "[color=#ff6b6b]✘[/color]" : "[color=#a99f8f]…[/color]";
            lines.Add($"[color=#ffd76b][b]Bounty: {bounty.Name}[/b][/color] {mark} [color=#a99f8f](+{bounty.Gold}g)[/color]\n" +
                      $"[color=#c9bfae]{bounty.Description}  {_battle.BountyProgress}[/color]");
            if (met && !_bountyAnnounced && bounty.Id is "combo" or "breaker" or "exploit")
            {
                _bountyAnnounced = true;
                Toast($"Bounty complete: {bounty.Name}! (+{bounty.Gold} gold on victory)");
            }
        }
        _objectives.Text = string.Join("\n", lines);
    }

    // ───────────── Cinematics: finishers and the killing blow ─────────────

    private ColorRect? _cineDim;
    private Godot.Label? _cineTitle;

    /// <summary>Before a finisher: the world dims, the camera leans in on the hero, time slows and the move's name slams in.</summary>
    private async Task FinisherIntro(ActionEvent a)
    {
        if (!_views.TryGetValue(a.Actor, out var actor)) return;
        var vp = GetViewportRect().Size;
        var color = a.Element == Element.Physical ? Visuals.GoldBright : Visuals.ElementColor(a.Element);
        _cineDim = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = MouseFilterEnum.Ignore, Size = vp };
        AddChild(_cineDim);
        MoveChild(_cineDim, _field.GetIndex());   // over the background, under the units: the world darkens, the fighters stay lit
        _cineTitle = Visuals.Title(a.Name.ToUpperInvariant(), 54);
        _cineTitle.AddThemeColorOverride("font_color", color);
        _cineTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _cineTitle.Size = new Vector2(vp.X, 80);
        _cineTitle.Position = new Vector2(0, vp.Y * 0.18f);
        _cineTitle.PivotOffset = new Vector2(vp.X / 2, 40);
        _cineTitle.Scale = new Vector2(2.2f, 2.2f);
        _cineTitle.Modulate = new Color(1, 1, 1, 0);
        AddChild(_cineTitle);
        Audio.Sfx(this, "charge", 0f, 0.8f);

        var t = CreateTween().SetParallel();
        t.TweenProperty(_cineDim, "color:a", 0.65f, 0.18);
        t.TweenProperty(_cineTitle, "scale", Vector2.One, 0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.TweenProperty(_cineTitle, "modulate:a", 1f, 0.12);
        if (!GameSession.Settings.ReduceMotion)
            foreach (var layer in new[] { _field, _fx })
            {
                layer.PivotOffset = actor.Body.GlobalPosition - layer.GlobalPosition;
                t.TweenProperty(layer, "scale", Vector2.One * 1.12f, 0.3).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            }
        Flash(actor, color, 0.9f);
        Afterimage(actor, color, 0.6f, 0.45);
        await Wait(0.38);
        if (!GameSession.Settings.ReduceMotion) Engine.TimeScale = Speed * 0.55;   // the strike itself plays in slow motion
    }

    private void FinisherOutro()
    {
        Engine.TimeScale = Speed;
        var t = CreateTween().SetParallel();
        foreach (var layer in new[] { _field, _fx }) t.TweenProperty(layer, "scale", Vector2.One, 0.25).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        if (_cineDim is { } dim)
        {
            t.TweenProperty(dim, "color:a", 0f, 0.25);
            t.Chain().TweenCallback(Callable.From(dim.QueueFree));
        }
        if (_cineTitle is { } title)
        {
            var ft = title.CreateTween();
            ft.TweenProperty(title, "modulate:a", 0f, 0.3);
            ft.TweenCallback(Callable.From(title.QueueFree));
        }
        _cineDim = null;
        _cineTitle = null;
    }

    /// <summary>The killing blow of the battle: a long hit-freeze, a push-in on the victim and a white flash.</summary>
    private async Task KillCam(BattleUnitView victim)
    {
        if (GameSession.Settings.ReduceMotion)
        {
            await Wait(0.15);
            return;
        }
        var vp = GetViewportRect().Size;
        foreach (var layer in new[] { _field, _fx })
        {
            layer.PivotOffset = victim.Body.GlobalPosition - layer.GlobalPosition;
            var z = layer.CreateTween();
            z.TweenProperty(layer, "scale", Vector2.One * 1.18f, 0.12).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        }
        var flash = new ColorRect { Color = new Color(1, 1, 1, 0.55f), MouseFilter = MouseFilterEnum.Ignore, Size = vp };
        AddChild(flash);
        var f = flash.CreateTween();
        f.TweenProperty(flash, "color:a", 0f, 0.35);
        f.TweenCallback(Callable.From(flash.QueueFree));
        await HitStop(0.28);
        Engine.TimeScale = Speed * 0.4;
        await Wait(0.25);
        Engine.TimeScale = Speed;
        foreach (var layer in new[] { _field, _fx })
        {
            var z = layer.CreateTween();
            z.TweenProperty(layer, "scale", Vector2.One, 0.4).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        }
    }
}

public enum VoiceCue { Talk, Attack, Hurt, Death }
