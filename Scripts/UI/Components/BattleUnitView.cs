using System;
using System.Linq;
using System.Text;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// One combatant on the battlefield: sprite (with flash/outline shader), intent bubble (enemies),
/// name, HP + shield bar, resource pips (heroes) or break pips + weaknesses (enemies), and status icons.
/// The control's origin is placed so that the unit's feet sit on the battlefield baseline.
/// </summary>
public partial class BattleUnitView : Control
{
    public const float ViewWidth = 118;

    public Combatant Unit { get; }
    public Node2D Body { get; } = new();
    public Sprite2D Sprite { get; } = new();
    public Sprite2D Shadow { get; } = new();
    public ShaderMaterial SpriteMaterial { get; } = new();
    public Vector2 Home { get; private set; }
    public float FeetY { get; }
    public float SpriteHeight { get; }

    private readonly PanelContainer _bubble = new();
    private readonly RichTextLabel _intent;
    private readonly Godot.Label _name;
    private readonly ProgressBar _hp = new();
    private readonly ProgressBar _shield = new();
    private readonly Godot.Label _hpText;
    private readonly HBoxContainer _sub = new();
    private readonly HFlowContainer _statuses = new();
    private readonly PanelContainer _previewPanel = new();
    private readonly RichTextLabel _preview;
    private Tween? _hpTween;
    private Tween? _chargeTween;
    private Tween? _dangerTween;
    private readonly Sprite2D _ring = new();
    private Tween? _ringTween;
    private readonly float _scale;

    public BattleUnitView() : this(new Combatant { DefId = "", Name = "", Sprite = "skeleton" }) { }

    public BattleUnitView(Combatant unit)
    {
        Unit = unit;
        MouseFilter = MouseFilterEnum.Stop;
        _scale = Visuals.SpriteScale * unit.Scale;
        var texture = Visuals.Tex($"Units/{unit.Sprite}.png");
        SpriteHeight = texture.GetHeight() * _scale;
        FeetY = Mathf.Max(196, SpriteHeight + 70);
        Size = new Vector2(ViewWidth, FeetY + 92);
        CustomMinimumSize = Size;

        // Intent bubble (enemies only)
        _bubble.MouseFilter = MouseFilterEnum.Ignore;
        var bubbleStyle = Visuals.Box(new Color(0.08f, 0.06f, 0.1f, 0.92f), new Color("#a63d25"), 2, 8, 5);
        _bubble.AddThemeStyleboxOverride("panel", bubbleStyle);
        _bubble.Position = new Vector2(-16, FeetY - SpriteHeight - 68);
        _bubble.Size = new Vector2(ViewWidth + 32, 60);
        _bubble.CustomMinimumSize = _bubble.Size;
        _bubble.Visible = !unit.IsHero;
        _intent = Visuals.Rich("", 12);
        _intent.FitContent = false;
        _intent.CustomMinimumSize = new Vector2(ViewWidth + 22, 50);
        _bubble.AddChild(_intent);
        AddChild(_bubble);

        // Sprite
        Home = new Vector2(ViewWidth / 2, FeetY);
        Shadow.Texture = Visuals.MakeSoftCircle(new Color(0, 0, 0, 0.55f));
        Shadow.Scale = new Vector2(1.7f * unit.Scale, 0.4f * unit.Scale);
        Shadow.Position = Home;
        AddChild(Shadow);
        // Glowing ring under the feet of whoever is acting.
        _ring.Texture = Visuals.Tex("FX/circle.png");
        _ring.TextureFilter = TextureFilterEnum.Nearest;
        _ring.Position = Home + new Vector2(0, -2);
        _ring.Scale = new Vector2(3.6f, 3.6f) * unit.Scale;
        _ring.Modulate = unit.IsHero ? new Color("#ffd76b") : new Color("#ff6a4a");
        _ring.Visible = false;
        AddChild(_ring);
        Body.Position = Home;
        AddChild(Body);
        SpriteMaterial.Shader = GD.Load<Shader>(Visuals.Art + "Shaders/unit.gdshader");
        Sprite.Texture = texture;
        Sprite.Offset = new Vector2(0, -texture.GetHeight() / 2f);
        Sprite.Scale = new Vector2(_scale, _scale);
        Sprite.TextureFilter = TextureFilterEnum.Nearest;
        Sprite.Material = SpriteMaterial;
        Sprite.FlipH = unit.FlipSprite;
        Sprite.SelfModulate = new Color(unit.Tint);
        Body.AddChild(Sprite);

        // Info below the feet
        _name = Visuals.Label(unit.Name, 14, unit.IsBoss ? Visuals.GoldBright
            : EnemyMods.Get(unit.Modifier) is { } mod ? new Color(mod.ColorHex) : Visuals.Text);
        _name.HorizontalAlignment = HorizontalAlignment.Center;
        _name.Position = new Vector2(-30, FeetY + 6);
        _name.Size = new Vector2(ViewWidth + 60, 20);
        AddChild(_name);

        _hp.ShowPercentage = false;
        _hp.MouseFilter = MouseFilterEnum.Ignore;
        _hp.Position = new Vector2(8, FeetY + 28);
        _hp.Size = new Vector2(ViewWidth - 16, 10);
        _hp.MaxValue = unit.MaxHp;
        _hp.Value = unit.Hp;
        _hp.AddThemeStyleboxOverride("background", Visuals.Box(new Color("#0b0910"), new Color("#0b0910"), 1, 2, 0));
        if (GameSession.Settings.ColorblindMode)   // blue for heroes, orange for enemies: never confused, whatever the colour vision
            _hp.AddThemeStyleboxOverride("fill", Visuals.Box(new Color(unit.IsHero ? "#56b4e9" : "#e69f00"), new Color(0, 0, 0, 0), 0, 2, 0));
        else _hp.AddThemeStyleboxOverride("fill", BarFill(unit.IsHero ? "UI/bar_hero.png" : "UI/bar_enemy.png"));
        AddChild(_hp);

        _shield.ShowPercentage = false;
        _shield.MouseFilter = MouseFilterEnum.Ignore;
        _shield.Position = _hp.Position;
        _shield.Size = _hp.Size;
        _shield.MaxValue = unit.MaxHp;
        _shield.AddThemeStyleboxOverride("background", new StyleBoxEmpty());
        var shieldFill = BarFill("UI/bar_shield.png");
        shieldFill.ModulateColor = new Color(1, 1, 1, 0.6f);
        _shield.AddThemeStyleboxOverride("fill", shieldFill);
        AddChild(_shield);
        // Segment ticks over the bar make HP easier to read at a glance.
        var ticks = new TextureRect
        {
            Texture = Visuals.Tex("UI/bar_segments.png"),
            StretchMode = TextureRect.StretchModeEnum.Tile,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Position = _hp.Position + new Vector2(1, 1),
            Size = _hp.Size - new Vector2(2, 2),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(ticks);

        _hpText = Visuals.Label("", 12, new Color("#d8d0c0"), outline: 4);
        _hpText.HorizontalAlignment = HorizontalAlignment.Center;
        _hpText.Position = new Vector2(-20, FeetY + 38);
        _hpText.Size = new Vector2(ViewWidth + 40, 16);
        AddChild(_hpText);

        _sub.MouseFilter = MouseFilterEnum.Ignore;
        _sub.Alignment = BoxContainer.AlignmentMode.Center;
        _sub.Position = new Vector2(0, FeetY + 56);
        _sub.Size = new Vector2(ViewWidth, 14);
        _sub.AddThemeConstantOverride("separation", 2);
        AddChild(_sub);

        // Targeting preview badge (expected damage / healing), shown over the lower body.
        _previewPanel.MouseFilter = MouseFilterEnum.Ignore;
        _previewPanel.AddThemeStyleboxOverride("panel", Visuals.Box(new Color(0.04f, 0.03f, 0.06f, 0.9f), new Color("#6be07a"), 1, 4, 3));
        _previewPanel.Position = new Vector2(-16, FeetY - 40);
        _previewPanel.Size = new Vector2(ViewWidth + 32, 30);
        _previewPanel.Visible = false;
        _preview = Visuals.Rich("", 12);
        _preview.CustomMinimumSize = new Vector2(ViewWidth + 24, 0);
        _previewPanel.AddChild(_preview);

        _statuses.MouseFilter = MouseFilterEnum.Ignore;
        _statuses.Alignment = FlowContainer.AlignmentMode.Center;
        _statuses.Position = new Vector2(-10, FeetY + 72);
        _statuses.Size = new Vector2(ViewWidth + 20, 18);
        _statuses.AddThemeConstantOverride("h_separation", 1);
        AddChild(_statuses);
        AddChild(_previewPanel);
    }

    /// <summary>Shows (or hides with null) the expected result of the pending action on this unit.</summary>
    public void SetPreview(string? bbcode)
    {
        _previewPanel.Visible = !string.IsNullOrEmpty(bbcode);
        _preview.Text = bbcode is null ? "" : $"[center]{bbcode}[/center]";
        _previewPanel.ResetSize();
        _previewPanel.Position = new Vector2((ViewWidth - _previewPanel.Size.X) / 2, FeetY - 40);
    }

    public override void _Ready()
    {
        var idle = Sprite.CreateTween().SetLoops();
        double breath = 0.7 + GD.Randf() * 0.3;
        idle.TweenProperty(Sprite, "scale", new Vector2(_scale, _scale * 1.04f), breath).SetTrans(Tween.TransitionType.Sine);
        idle.TweenProperty(Sprite, "scale", new Vector2(_scale, _scale), breath).SetTrans(Tween.TransitionType.Sine);
        switch (Unit.IsHero ? Unit.DefId : Unit.Sprite)
        {
            case "moth" or "wraith" or "imp" or "wisp" or "harpy" or "phylactery" or "tome" or "ink_wraith" or "quill_swarm":
            {
                // Hovering units bob up and down.
                var hover = Sprite.CreateTween().SetLoops();
                double period = 0.45 + GD.Randf() * 0.2;
                hover.TweenProperty(Sprite, "position", new Vector2(0, -10), period).SetTrans(Tween.TransitionType.Sine);
                hover.TweenProperty(Sprite, "position", Vector2.Zero, period).SetTrans(Tween.TransitionType.Sine);
                break;
            }
            case "siren" or "matriarch" or "leech" or "thrall":
            {
                // Swaying, as if under water.
                var sway = Sprite.CreateTween().SetLoops();
                double period = 1.1 + GD.Randf() * 0.4;
                sway.TweenProperty(Sprite, "rotation", 0.05f, period).SetTrans(Tween.TransitionType.Sine);
                sway.TweenProperty(Sprite, "rotation", -0.05f, period).SetTrans(Tween.TransitionType.Sine);
                break;
            }
        }
        if (!Unit.IsAlive) SetDeadLook();
    }

    /// <summary>Point in the middle of the body, in canvas coordinates.</summary>
    public Vector2 Center => Body.GlobalPosition + new Vector2(0, -SpriteHeight * 0.45f);
    public Vector2 Head => Body.GlobalPosition + new Vector2(0, -SpriteHeight - 6);

    public void SetOutline(Color color) => SpriteMaterial.SetShaderParameter("outline_color", color);

    /// <summary>Region lighting (see unit.gdshader): ambient multiply, rim light on one side, feet in shadow.</summary>
    public void SetLighting(Color ambient, Color rim, float rimStrength, float rimSide)
    {
        // A sprite drawn mirrored has its texture's left and right swapped.
        if (Sprite.FlipH) rimSide = -rimSide;
        SpriteMaterial.SetShaderParameter("ambient", ambient);
        SpriteMaterial.SetShaderParameter("rim_color", rim);
        SpriteMaterial.SetShaderParameter("rim_strength", rimStrength);
        SpriteMaterial.SetShaderParameter("rim_side", rimSide);
        SpriteMaterial.SetShaderParameter("foot_shade", 0.2f);
    }

    public void SetDeadLook()
    {
        Body.Modulate = new Color(1, 1, 1, 0);
        Shadow.Modulate = new Color(1, 1, 1, 0);
        _name.Modulate = new Color(1, 1, 1, 0.35f);
        _bubble.Visible = false;
    }

    public void TweenHp(int hp)
    {
        _hpTween?.Kill();
        _hpTween = _hp.CreateTween();
        _hpTween.TweenProperty(_hp, "value", (double)hp, 0.3).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _hpText.Text = HpText(hp);
    }

    public void SetCharging(bool charging)
    {
        _chargeTween?.Kill();
        _chargeTween = null;
        if (!charging)
        {
            SpriteMaterial.SetShaderParameter("flash", 0f);
            return;
        }
        SpriteMaterial.SetShaderParameter("flash_color", Visuals.Ember);
        _chargeTween = Sprite.CreateTween().SetLoops();
        _chargeTween.TweenMethod(Callable.From<float>(x => SpriteMaterial.SetShaderParameter("flash", x)), 0f, 0.35f, 0.45);
        _chargeTween.TweenMethod(Callable.From<float>(x => SpriteMaterial.SetShaderParameter("flash", x)), 0.35f, 0f, 0.45);
    }

    private string HpText(int hp)
    {
        int shield = Unit.Amount(StatusKind.Shield);
        return shield > 0 ? $"{hp}/{Unit.MaxHp}  +{shield}" : $"{hp}/{Unit.MaxHp}";
    }

    public void SetActive(bool active)
    {
        if (active == _ring.Visible) return;
        _ring.Visible = active;
        _ringTween?.Kill();
        _ringTween = null;
        if (!active) return;
        var baseScale = new Vector2(3.6f, 3.6f) * Unit.Scale;
        _ringTween = _ring.CreateTween().SetLoops();
        _ringTween.TweenProperty(_ring, "scale", baseScale * 1.15f, 0.5).SetTrans(Tween.TransitionType.Sine);
        _ringTween.TweenProperty(_ring, "scale", baseScale, 0.5).SetTrans(Tween.TransitionType.Sine);
    }

    private static StyleBoxTexture BarFill(string texture)
    {
        var box = new StyleBoxTexture { Texture = Visuals.Tex(texture) };
        box.SetTextureMarginAll(1);
        return box;
    }

    /// <summary>Heroes below 30% HP pulse red so it's obvious who needs healing.</summary>
    private void SetDanger(bool danger)
    {
        if (danger == (_dangerTween is not null)) return;
        _dangerTween?.Kill();
        _dangerTween = null;
        Sprite.Modulate = Colors.White;
        if (!danger) return;
        _dangerTween = Sprite.CreateTween().SetLoops();
        _dangerTween.TweenProperty(Sprite, "modulate", new Color(1f, 0.55f, 0.55f), 0.5).SetTrans(Tween.TransitionType.Sine);
        _dangerTween.TweenProperty(Sprite, "modulate", Colors.White, 0.5).SetTrans(Tween.TransitionType.Sine);
    }

    /// <summary>Redraws everything from the model. HP is skipped while an event animation owns it.</summary>
    public void Refresh(Battle battle, bool hp)
    {
        var u = Unit;
        _name.Text = u.Name;
        SetDanger(u.IsHero && u.IsAlive && u.HealthPct < 30);
        if (hp)
        {
            _hpTween?.Kill();
            _hp.MaxValue = u.MaxHp;
            _hp.Value = u.Hp;
            _hpText.Text = HpText(u.Hp);
        }
        _shield.MaxValue = u.MaxHp;
        _shield.Value = Math.Min(u.MaxHp, u.Amount(StatusKind.Shield));

        foreach (var child in _sub.GetChildren()) child.QueueFree();
        if (u.IsHero && u.ResourceDef is not null)
        {
            var color = new Color(u.ResourceDef.ColorHex);
            for (int i = 0; i < u.MaxResource; i++)
            {
                _sub.AddChild(new ColorRect
                {
                    Color = i < u.Resource ? color : new Color(0.15f, 0.13f, 0.18f),
                    CustomMinimumSize = new Vector2(Math.Min(12, (ViewWidth - 20) / u.MaxResource - 2), 6),
                    MouseFilter = MouseFilterEnum.Ignore,
                });
            }
        }
        else if (!u.IsHero && u.IsAlive)
        {
            for (int i = 0; i < u.BreakMax; i++)
            {
                var pip = Visuals.Icon(u.Broken ? "UI/pip_broken.png" : i < u.Break ? "UI/pip_on.png" : "UI/pip_off.png", 11);
                pip.MouseFilter = MouseFilterEnum.Ignore;
                _sub.AddChild(pip);
            }
            _sub.AddChild(Visuals.Spacer(0, 4));
            foreach (var element in u.Weaknesses) _sub.AddChild(Visuals.Icon(Visuals.ElementIcon(element), 14));
        }

        foreach (var child in _statuses.GetChildren()) child.QueueFree();
        foreach (var s in u.Statuses)
        {
            var icon = Visuals.Icon(Visuals.StatusIcon(s.Kind), 16);
            // Every status icon explains itself on hover (effect, duration, stacking rule).
            icon.MouseFilter = MouseFilterEnum.Stop;
            icon.TooltipText = Visuals.StatusDescription(s);
            _statuses.AddChild(icon);
            if (s.Kind is StatusKind.Poison or StatusKind.Burn or StatusKind.Bleed or StatusKind.Regen)
                _statuses.AddChild(Visuals.Label(s.Amount.ToString(), 11, Visuals.Text, outline: 3));
        }

        if (!u.IsHero)
        {
            _bubble.Visible = u.IsAlive;
            _intent.Text = IntentText(battle);
            SetCharging(u.IsAlive && (u.Intent is { Charging: true } || u.ChargedMove is not null));
        }
        TooltipText = BuildTooltip(battle);
        if (!u.IsAlive) SetDeadLook();
    }

    private string IntentText(Battle battle)
    {
        var e = Unit;
        if (!e.IsAlive) return "";
        if (e.Has(StatusKind.Stun)) return $"{Visuals.Img(Visuals.StatusIcon(StatusKind.Stun))} [color=#ffd76b][b]Stunned[/b][/color]\n[color=#a99f8f]skips its turn[/color]";
        if (battle.IsDoomed(e))
            return $"{Visuals.Img("UI/icon_heal_ally.png", 13)} [color=#ff7070][b]Doomed![/b][/color]\n[color=#a99f8f]will be devoured, won't act[/color]";
        // No plan right now: it already acted this round, or it just arrived (summons and split shards act next round).
        if (e.Intent is not { } intent) return "[color=#7d7590]Waiting\nacts next round[/color]";
        var move = intent.Move;
        string weakPoint = e.ExposedElement is Element ex
            ? e.ExposedElement2 is Element ex2
                ? $"\n{Visuals.Img(Visuals.ElementIcon(ex), 13)}{Visuals.Img(Visuals.ElementIcon(ex2), 13)} [color=#ffd76b]Weak points: {ex} + {ex2}[/color]"
                : $"\n{Visuals.Img(Visuals.ElementIcon(ex), 13)} [color=#ffd76b]Weak point: {ex}[/color]"
            : "";
        if (battle.IsWarded(e)) weakPoint += $"\n{Visuals.Img("UI/icon_shield.png", 13)} [color=#8fd0ff]Warded: break the Phylactery![/color]";
        if (e.Enemy?.AdaptiveResist == true && e.AdaptedTo is { } adapted)
            weakPoint += $"\n{Visuals.Img(Visuals.ElementIcon(adapted), 13)} [color=#c070ff]Adapted: {adapted} (-40%)[/color]";
        if (battle.FeedersOf(e) is > 0 and var pods)
            weakPoint += $"\n{Visuals.Img("UI/icon_regen.png", 13)} [color=#b3d05a]Fed by {pods} pod{(pods > 1 ? "s" : "")} (+{e.Enemy!.RegrowPct * pods}%/round)[/color]";
        if (intent.Charging)
        {
            string advice = move.Target switch
            {
                MoveTarget.FrontOnly => "Pull the front row back!",
                MoveTarget.BackOnly => "Move the back row forward!",
                MoveTarget.Branded => "Hits every BRANDED hero: cleanse, shield or defend them!",
                _ => "Break it!",
            };
            return $"{Visuals.Img("UI/icon_charging.png")} [color=#ffa040][b]Charging![/b][/color]\n[color=#ffa040]{move.Name}[/color] [color=#a99f8f]next round. {advice}[/color]{weakPoint}";
        }

        var targets = battle.IntentTargets(e, intent);
        string first = targets.FirstOrDefault()?.Name.Split(' ')[0] ?? "—";
        string who = move.Target switch
        {
            MoveTarget.Hero or MoveTarget.Ally => first,
            MoveTarget.FrontRow => "front row",
            MoveTarget.FrontOnly => targets.Count == 0 ? "nobody (front row empty)" : "FRONT ROW",
            MoveTarget.BackOnly => targets.Count == 0 ? "nobody (back row empty)" : "BACK ROW",
            MoveTarget.BackRow => "back row",
            MoveTarget.AllHeroes => "ALL",
            MoveTarget.Branded => targets.Count == 0 ? "branded (none)" : $"BRANDED ({targets.Count})",
            MoveTarget.AllAllies => "allies",
            _ => "self",
        };
        int power = battle.IntentPower(move, e);
        string hits = move.Hits > 1 ? $"x{move.Hits}" : "";
        var line = new StringBuilder();
        switch (move.Kind)
        {
            case MoveKind.Attack:
                if (move.Power > 0) line.Append($"{Visuals.Img(Visuals.ElementIcon(move.Element), 13)}[b]{power}{hits}[/b] ");
                line.Append($"→ [color=#ffb0b0]{who}[/color]");
                break;
            case MoveKind.Heal: line.Append($"{Visuals.Img("UI/icon_heal_ally.png", 13)}Heal {power} → {who}"); break;
            case MoveKind.Shield: line.Append($"{Visuals.Img("UI/icon_shield.png", 13)}Shield {power} → {who}"); break;
            case MoveKind.Empower: line.Append($"{Visuals.Img("UI/icon_empowered.png", 13)}Empower {who}"); break;
            case MoveKind.Defend: line.Append($"{Visuals.Img("UI/icon_defending.png", 13)}Defend +{power}"); break;
            case MoveKind.Summon: line.Append($"{Visuals.Img("UI/icon_summon.png", 13)}Summon"); break;
            case MoveKind.Consume: line.Append($"{Visuals.Img("UI/icon_heal_ally.png", 13)}Devour [color=#ffb0b0]{first}[/color], heal {power}"); break;
            case MoveKind.Usurp:
                string sealedName = targets.FirstOrDefault() is { } victim && battle.UsurpTarget(victim) is { } sealedSkill ? sealedSkill.Def.Name : "best skill";
                line.Append($"{Visuals.Img("UI/icon_weakened.png", 13)}Seal [color=#ffb0b0]{first}[/color]'s [b]{sealedName}[/b]");
                break;
        }
        foreach (var apply in move.Applies) line.Append(Visuals.Img(Visuals.StatusIcon(apply.Kind), 13));
        string name = move.Melee ? $"[b]{move.Name}[/b] [color=#a99f8f](melee)[/color]" : $"[b]{move.Name}[/b]";
        return $"{name}\n{line}{weakPoint}";
    }

    private string BuildTooltip(Battle battle)
    {
        var u = Unit;
        var sb = new StringBuilder();
        sb.AppendLine($"{u.Name}   HP {u.Hp}/{u.MaxHp}");
        sb.AppendLine($"Armor {u.Armor}  ·  Speed {u.Speed}  ·  Crit {u.Crit}%  ·  Dodge {u.Dodge}%  ·  {u.Row} row");
        if (u.IsHero && u.ResourceDef is not null) sb.AppendLine($"{u.ResourceDef.Name}: {u.Resource}/{u.MaxResource}");
        if (u.IsHero && u.WeaponEffect is not null) sb.AppendLine("Weapon: " + Database.WeaponEffectText(u.WeaponEffect));
        if (u.IsHero && u.Traits.Count > 0)
            sb.AppendLine("Traits: " + string.Join(", ", u.Traits.Where(Database.HasTraitDef).Select(t => Database.Trait(t).Name)));
        if (!u.IsHero)
        {
            sb.AppendLine(Visuals.AffinityText(u));
            sb.AppendLine(u.Broken ? "BROKEN: stunned, takes double damage." : $"Break meter: {u.Break}/{u.BreakMax} (hit weaknesses to deplete)");
            if (u.IsBoss) sb.AppendLine("Bosses can't be stunned by abilities, and their break meter grows (up to +2) each time they recover.");
            if (EnemyMods.Get(u.Modifier) is { } mod) sb.AppendLine(mod.Description);
            foreach (var combo in battle.PrimedCombos(u))
                sb.AppendLine($"COMBO READY: hit it with {combo.Payoff} (another hero) for {combo.Name}.");
            if (u.Enemy is not null) sb.AppendLine(u.Enemy.Blurb);
        }
        foreach (var s in u.Statuses) sb.AppendLine("• " + Visuals.StatusDescription(s));
        return sb.ToString().TrimEnd();
    }
}
