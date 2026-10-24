using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// Battle screen. The rules (TBRPG.Core.Battle) resolve instantly and emit BattleEvents; this class
/// replays them with animation and sound, runs enemy turns (with the optional parry prompt), and
/// handles hero input. Input is ignored while anything is playing, so double actions are impossible.
/// </summary>
public partial class BattleScene : Screen
{
    private Battle _battle = null!;
    private RunState _run = null!;
    private readonly Dictionary<Combatant, BattleUnitView> _views = new();
    private readonly Queue<BattleEvent> _events = new();
    private bool _busy = true;
    private bool _ended;

    // Targeting
    private AbilityInstance? _pendingAbility;
    private int _pendingItem = -1;
    private List<Combatant> _targets = new();
    private Combatant? _hover;
    private AbilityInstance? _hoverAbility;

    // UI
    private Control _field = null!;
    private Control _fx = null!;
    private Tween? _shakeTween;
    private Godot.Label _roundLabel = null!;
    private HBoxContainer _turnBar = null!;
    private Godot.Button _speedButton = null!;
    private HBoxContainer _actionRow = null!;
    private VBoxContainer _heroBox = null!;
    private RichTextLabel _description = null!;
    private RichTextLabel _log = null!;
    private Godot.Label _banner = null!;
    private Godot.Label _toast = null!;
    private Tween? _toastTween;
    private TimingPrompt _timing = null!;
    private PanelContainer? _itemMenu;
    private readonly List<(Godot.Button button, Action action)> _hotkeys = new();
    private bool _introPlayed;
    private readonly Random _barkRng = new();
    private Combatant? _lastActor;
    private double _lastBark = -10;
    private readonly HashSet<Combatant> _hurtBarked = new();

    protected override void Build()
    {
        _run = GameSession.Run ?? GameSession.EnsureRun();
        var node = _run.Node;
        AddRegionBackdrop(_run.NodeRegion, node?.Type == NodeType.Boss ? 0.8f : 1f);

        _field = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _field.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_field);
        _fx = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _fx.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_fx);

        BuildTopBar(node);
        BuildBottomPanel();

        _banner = Visuals.Title("", 54);
        _banner.SetAnchorsPreset(LayoutPreset.FullRect);
        _banner.OffsetBottom = -200;
        _banner.HorizontalAlignment = HorizontalAlignment.Center;
        _banner.VerticalAlignment = VerticalAlignment.Center;
        _banner.Modulate = new Color(1, 1, 1, 0);
        AddChild(_banner);

        _toast = Visuals.Label("", 16, Visuals.Bad);
        _toast.SetAnchorsPreset(LayoutPreset.BottomWide);
        _toast.HorizontalAlignment = HorizontalAlignment.Center;
        _toast.OffsetTop = -214;
        _toast.OffsetBottom = -190;
        _toast.Modulate = new Color(1, 1, 1, 0);
        AddChild(_toast);

        _timing = new TimingPrompt();
        _timing.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_timing);

        AddPauseMenu(inRun: true);

        // Battle
        var setup = _run.CreateBattle();
        _battle = new Battle(setup);
        _battle.Happened += e => _events.Enqueue(e);
        _battle.Logged += line => _log.AppendText(line + "\n");
        foreach (var unit in _battle.Heroes.Concat(_battle.Enemies)) AddView(unit);
        LayoutUnits(animate: false);
        GetViewport().SizeChanged += () => { if (IsInstanceValid(this) && IsInsideTree()) LayoutUnits(animate: false); };

        bool boss = _battle.Enemies.Any(e => e.IsBoss);
        Audio.Music(this, boss ? "boss" : _run.NodeRegion.Music);
        Visuals.FadeIn(this, 0.4);

        if (GameSession.Profile.MarkSeen(_battle.Enemies.Select(e => e.DefId))) GameSession.SaveProfile();

        _battle.Start();
        RunLoop();
        if (!GameSession.Profile.TutorialSeen) ShowTips();
    }

    /// <summary>First-battle tips. Shown once per profile (also in How to Play).</summary>
    private void ShowTips()
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(640, 0) };
        panel.SetAnchorsPreset(LayoutPreset.CenterTop);
        panel.GrowHorizontal = GrowDirection.Both;
        panel.OffsetTop = 70;
        AddChild(panel);
        Pause?.MoveToFront();
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        panel.AddChild(box);
        box.AddChild(Visuals.Title("First battle: the essentials", 22));
        box.AddChild(Visuals.Rich(
            "• Turn order (top bar) is decided by [b]speed[/b]. The gold-outlined hero is acting.\n" +
            "• Enemy bubbles show their [b]next action and target[/b]. Hover an enemy to see who it threatens.\n" +
            "• Hitting a [color=#ffa040]weakness[/color] (icons under enemy HP) chips its [color=#ffd76b]break pips[/color]. " +
            "At zero it is [b]BROKEN[/b]: stunned, its attack cancelled, [b]double[/b] damage taken.\n" +
            "• [color=#ffa040]Charging[/color] enemies unleash a big attack next round: break or interrupt them!\n" +
            "• Melee can only reach the [b]front row[/b] while it stands. Hover actions to preview damage.\n" +
            "• Press [b]SPACE[/b] when the rings meet to parry (turn off in Settings).", 14));
        box.AddChild(Visuals.Button("Got it", () =>
        {
            GameSession.Profile.TutorialSeen = true;
            GameSession.SaveProfile();
            panel.QueueFree();
        }, new Vector2(0, 40), 16));
    }

    public override void _ExitTree() => Engine.TimeScale = 1.0;

    // ═════════════════════════ Flow ═════════════════════════

    private double Speed => GameSession.Settings.BattleSpeed;

    /// <summary>Plays queued events, runs enemy turns until a hero must act, then unlocks input.</summary>
    private async void RunLoop()
    {
        _busy = true;
        ClearTargeting();
        RefreshUi();
        try
        {
            Engine.TimeScale = Speed;
            if (!_introPlayed)
            {
                _introPlayed = true;
                await PlayIntro();
            }
            await PlayEvents();
            while (_battle.State == BattleState.EnemyTurn && IsInsideTree())
            {
                var enemy = _battle.Current;
                RefreshUi();
                await Wait(0.15);
                var defense = DefenseQuality.None;
                if (_battle.EnemyAttackPending && GameSession.Settings.TimingDefense && enemy is not null)
                {
                    var targets = _battle.PendingAttackTargets;
                    if (targets.Count > 0 && _views.TryGetValue(targets[0], out var targetView))
                    {
                        PopText(_views[enemy], enemy.Intent?.Move.Name ?? "", Visuals.Text, 18, -20);
                        Engine.TimeScale = 1.0;
                        defense = await _timing.Run(targetView.Center);
                        Engine.TimeScale = Speed;
                    }
                }
                _battle.ExecuteEnemyTurn(defense);
                await PlayEvents();
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"Battle playback error: {ex}");
        }
        finally
        {
            Engine.TimeScale = 1.0;
        }

        if (!IsInsideTree()) return;
        if (_battle.IsOver)
        {
            ShowResult();
            return;
        }
        _busy = false;
        RefreshUi();
    }

    private void Execute(Func<bool> command)
    {
        if (_busy || _ended) return;
        CloseItemMenu();
        ClearTargeting();
        if (!command())
        {
            Toast("That action isn't possible right now.");
            RefreshUi();
            return;
        }
        RunLoop();
    }

    private Combatant? ActiveHero => _battle.State == BattleState.HeroTurn ? _battle.Current : null;

    private void OnAbility(AbilityInstance ability)
    {
        var hero = ActiveHero;
        if (_busy || hero is null) return;
        CloseItemMenu();
        if (!_battle.CanUse(hero, ability, out var reason))
        {
            Toast(reason);
            return;
        }
        if (ability.Def.NeedsTarget)
        {
            _pendingAbility = ability;
            _pendingItem = -1;
            _targets = _battle.ValidTargets(hero, ability).ToList();
            _hover = null;
            Audio.Sfx(this, "blip", 0f);
            RefreshUi();
            return;
        }
        Execute(() => _battle.UseAbility(hero, ability, null));
    }

    private void OnItem(int index)
    {
        var hero = ActiveHero;
        if (_busy || hero is null) return;
        CloseItemMenu();
        if (!_battle.CanUseItem(hero, index, out var reason))
        {
            Toast(reason);
            return;
        }
        var item = Database.Item(_battle.Consumables[index]);
        if (item.TargetsAlly)
        {
            _pendingItem = index;
            _pendingAbility = null;
            _targets = _battle.ItemTargets(item).ToList();
            _hover = null;
            RefreshUi();
            return;
        }
        Execute(() => _battle.UseItem(hero, index, null));
    }

    private void OnUnitClicked(Combatant unit)
    {
        var hero = ActiveHero;
        if (_busy || hero is null) return;
        if (_pendingAbility is null && _pendingItem < 0) return;
        if (!_targets.Contains(unit))
        {
            ClearTargeting();
            RefreshUi();
            return;
        }
        ConfirmTarget(hero, unit);
    }

    private void ConfirmTarget(Combatant hero, Combatant target)
    {
        if (_pendingAbility is { } ability) Execute(() => _battle.UseAbility(hero, ability, target));
        else if (_pendingItem >= 0)
        {
            int index = _pendingItem;
            Execute(() => _battle.UseItem(hero, index, target));
        }
    }

    private void ClearTargeting()
    {
        _hoverAbility = null;
        _pendingAbility = null;
        _pendingItem = -1;
        _targets.Clear();
        _hover = null;
    }

    public override void _Input(InputEvent @event)
    {
        bool cancel = @event.IsActionPressed("ui_cancel")
                      || @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right };
        if (!cancel) return;
        if (_itemMenu is not null)
        {
            CloseItemMenu();
            GetViewport().SetInputAsHandled();
        }
        else if (_pendingAbility is not null || _pendingItem >= 0)
        {
            ClearTargeting();
            RefreshUi();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_busy || _ended || @event is not InputEventKey { Pressed: true, Echo: false } key) return;

        bool targeting = _pendingAbility is not null || _pendingItem >= 0;
        if (targeting && _targets.Count > 0)
        {
            if (key.Keycode is Key.Tab or Key.Right or Key.Down or Key.Left or Key.Up)
            {
                int dir = key.Keycode is Key.Left or Key.Up ? -1 : 1;
                int index = _hover is null ? 0 : (_targets.IndexOf(_hover) + dir + _targets.Count) % _targets.Count;
                _hover = _targets[index];
                UpdateOutlines();
                GetViewport().SetInputAsHandled();
                return;
            }
            if (key.Keycode is Key.Enter or Key.KpEnter or Key.Space && ActiveHero is { } hero)
            {
                ConfirmTarget(hero, _hover ?? _targets[0]);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        long code = (long)key.Keycode;
        int number = code >= (long)Key.Key1 && code <= (long)Key.Key9 ? (int)(code - (long)Key.Key1) : -1;
        if (number >= 0 && number < _hotkeys.Count)
        {
            var (button, action) = _hotkeys[number];
            if (button.Disabled) Toast(button.TooltipText.Split('\n').LastOrDefault() ?? "");
            else action();
            GetViewport().SetInputAsHandled();
        }
    }

    // ═════════════════════════ Event playback ═════════════════════════

    private async Task PlayEvents()
    {
        while (_events.Count > 0 && IsInsideTree())
        {
            var e = _events.Dequeue();
            switch (e)
            {
                case RoundStartEvent r:
                    RefreshUi(hp: false);
                    await ShowBanner($"Round {r.Round}", 0.22);
                    break;
                case TurnStartEvent t:
                    RefreshUi(hp: false);
                    if (_views.TryGetValue(t.Actor, out var actorView)) Pulse(actorView);
                    break;
                case ActionEvent a:
                    _lastActor = a.Actor;
                    await PlayAction(a);
                    break;
                case HitEvent h: await PlayHit(h); break;
                case MissEvent m:
                    Audio.Sfx(this, "miss");
                    PopText(View(m.Target), "MISS", Visuals.TextDim, 24, 10);
                    await Wait(0.25);
                    break;
                case TickEvent tick: await PlayTick(tick); break;
                case HealEvent heal: await PlayHeal(heal); break;
                case ShieldEvent s:
                    View(s.Target).Refresh(_battle, hp: false);
                    PopText(View(s.Target), $"+{s.Amount} shield", new Color("#8fb3ff"), 18, 0);
                    await Wait(0.15);
                    break;
                case StatusEvent st:
                    View(st.Target).Refresh(_battle, hp: false);
                    if (st.Applied && st.Kind is not (StatusKind.Shield or StatusKind.Defending))
                    {
                        PopText(View(st.Target), Visuals.StatusName(st.Kind), StatusColor(st.Kind), 16, -30);
                        await Wait(0.12);
                    }
                    break;
                case TextEvent text:
                    PopText(View(text.Target), text.Text, new Color(text.ColorHex), 20, -30);
                    await Wait(0.3);
                    break;
                case BreakDamageEvent bd:
                    Audio.Sfx(this, "lock", 0.05f, 0.7f);
                    View(bd.Target).Refresh(_battle, hp: false);
                    Burst(View(bd.Target).Center + new Vector2(0, -30), Visuals.GoldBright, 10, Vector2.Up, 80, 160, 0.4);
                    break;
                case BreakEvent b:
                    await PlayBreak(b);
                    if (_barkRng.Next(100) < 45 && RandomHero() is { } cheer) Say(cheer, Story.BreakBark(_barkRng));
                    break;
                case DeathEvent d:
                    await PlayDeath(d.Unit);
                    if (!d.Unit.IsHero && _lastActor is { IsHero: true } killer && _barkRng.Next(100) < 35)
                        Say(killer, Story.KillBark(killer.DefId, _barkRng));
                    break;
                case SummonEvent s: await PlaySummon(s.Unit); break;
                case ChargeEvent c:
                    Audio.Sfx(this, "charge", 0.02f);
                    View(c.Unit).Refresh(_battle, hp: false);
                    PopText(View(c.Unit), $"Charging {c.MoveName}!", Visuals.Ember, 20, -40);
                    Burst(View(c.Unit).Center, Visuals.Ember, 30, Vector2.Up, 40, 140, 0.8, gravity: -80);
                    await Wait(0.6);
                    break;
                case PhaseChangeEvent p:
                    Audio.Sfx(this, "break", 0.02f);
                    Shake(18);
                    Flash(View(p.Unit), Visuals.Ember, 1f);
                    View(p.Unit).Refresh(_battle, hp: false);
                    await ShowBanner(p.Title, 0.9, Visuals.Ember);
                    Say(p.Unit, Story.EnemyBark(p.Unit.DefId, true), force: true);
                    break;
                case MoveRowEvent:
                    LayoutUnits(animate: true);
                    await Wait(0.2);
                    break;
                case IntentsChangedEvent:
                    foreach (var v in _views.Values.Where(v => !v.Unit.IsHero)) v.Refresh(_battle, hp: false);
                    break;
                case BattleEndEvent: break;
            }
        }
    }

    /// <summary>Heroes march in from the left, enemies rise from the dark; bosses get a title card.</summary>
    private async Task PlayIntro()
    {
        foreach (var v in _views.Values)
        {
            if (!v.Unit.IsAlive) continue;
            float from = v.Unit.IsHero ? -260 : 260;
            v.Body.Position = v.Home + new Vector2(from, 0);
            var t = v.Body.CreateTween();
            t.TweenInterval(GD.RandRange(0.0, 0.18));
            t.TweenProperty(v.Body, "position", v.Home, 0.45).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }
        await Wait(0.65);
        if (RandomHero() is { } talker && _barkRng.Next(100) < 75) Say(talker, Story.StartBark(talker.DefId, _barkRng), force: true);
        var elite = _battle.Enemies.FirstOrDefault(e => e.IsElite);
        if (elite is not null && Story.EnemyBark(elite.DefId, false) is { } eliteLine)
        {
            await Wait(0.5);
            Say(elite, eliteLine, force: true);
        }
        var boss = _battle.Enemies.FirstOrDefault(e => e.IsBoss);
        if (boss is not null && _views.TryGetValue(boss, out var bossView))
        {
            Audio.Sfx(this, "growl", 0f, 1f);
            Shake(14f);
            Flash(bossView, Visuals.Ember, 0.9f);
            var roar = bossView.Body.CreateTween();
            roar.TweenProperty(bossView.Body, "scale", new Vector2(1.18f, 0.88f), 0.12);
            roar.TweenProperty(bossView.Body, "scale", new Vector2(0.95f, 1.08f), 0.12);
            roar.TweenProperty(bossView.Body, "scale", Vector2.One, 0.2);
            Burst(bossView.Center, Visuals.Ember, 50, Vector2.Up, 80, 260, 1.0, gravity: -60);
            await ShowBanner(boss.Name, 1.2, Visuals.Ember);
            Say(boss, Story.EnemyBark(boss.DefId, false), force: true);
        }
    }

    /// <summary>A speech bubble over a unit's head. Rate-limited so the battlefield never gets chatty.</summary>
    private void Say(Combatant unit, string? text, bool force = false, double hold = 1.8)
    {
        if (string.IsNullOrEmpty(text) || !_views.TryGetValue(unit, out var v) || !unit.IsAlive) return;
        double now = Time.GetTicksMsec() / 1000.0;
        if (!force && now - _lastBark < 2.5) return;
        _lastBark = now;

        bool enemy = !unit.IsHero;
        var bubble = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, ZIndex = 20 };
        bubble.AddThemeStyleboxOverride("panel", Visuals.Box(enemy ? new Color("#2a0f12") : new Color("#f3ead6"),
            enemy ? new Color("#ff6a4a") : new Color("#5a4a3a"), 2, 10, 8));
        var label = Visuals.Label(text, 15, enemy ? new Color("#ffd0c0") : new Color("#2a2018"), outline: 0);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(Mathf.Min(260, 14 + text.Length * 8), 0);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        bubble.AddChild(label);
        _fx.AddChild(bubble);
        float lift = enemy ? 110 : 30;
        bubble.ResetSize();
        bubble.Position = v.Head + new Vector2(-bubble.Size.X / 2, -bubble.Size.Y - lift);
        bubble.PivotOffset = new Vector2(bubble.Size.X / 2, bubble.Size.Y);
        bubble.Scale = new Vector2(0.3f, 0.3f);
        bubble.Modulate = new Color(1, 1, 1, 0);
        Audio.Sfx(this, "blip", 0.1f, 0.3f);
        var t = bubble.CreateTween();
        t.TweenProperty(bubble, "scale", Vector2.One, 0.18).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(bubble, "modulate:a", 1f, 0.12);
        t.TweenInterval(hold);
        t.TweenProperty(bubble, "modulate:a", 0f, 0.3);
        t.Parallel().TweenProperty(bubble, "position:y", bubble.Position.Y - 12, 0.3);
        t.TweenCallback(Callable.From(bubble.QueueFree));
    }

    private Combatant? RandomHero() =>
        _battle.Heroes.Where(h => h.IsAlive).OrderBy(_ => _barkRng.Next()).FirstOrDefault();

    /// <summary>Surviving heroes hop and cheer.</summary>
    private void PlayVictoryHops()
    {
        int i = 0;
        foreach (var v in _views.Values.Where(v => v.Unit.IsHero && v.Unit.IsAlive))
        {
            var t = v.Body.CreateTween().SetLoops(3);
            t.TweenInterval(i++ * 0.08);
            t.TweenProperty(v.Body, "position", v.Home + new Vector2(0, -26), 0.16).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            t.TweenProperty(v.Body, "position", v.Home, 0.16).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            t.TweenInterval(0.25);
            Burst(v.Center + new Vector2(0, -40), Visuals.GoldBright, 14, Vector2.Up, 60, 160, 0.7);
        }
    }

    private async Task ShowBanner(string title, double hold, Color? color = null)
    {
        _banner.Text = title;
        _banner.AddThemeColorOverride("font_color", color ?? Visuals.GoldBright);
        _banner.PivotOffset = _banner.Size / 2;
        var t = CreateTween();
        t.TweenProperty(_banner, "modulate:a", 1f, 0.12);
        t.Parallel().TweenProperty(_banner, "scale", Vector2.One, 0.25).From(new Vector2(1.4f, 1.4f))
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.TweenInterval(hold);
        t.TweenProperty(_banner, "modulate:a", 0f, 0.18);
        await ToSignal(t, Tween.SignalName.Finished);
    }

    private async Task PlayAction(ActionEvent a)
    {
        var actor = View(a.Actor);
        float dir = a.Actor.IsHero ? 1f : -1f;
        var color = Visuals.ElementColor(a.Element);
        if (a.Big) await PowerUp(actor, a, color);
        else PopText(actor, a.Name, new Color("#f2ead8"), 17, -14);
        if (!a.Actor.IsHero && a.Vfx is not (Vfx.Melee or Vfx.Buff)) await CastFlourish(actor, a, color);

        switch (a.Vfx)
        {
            case Vfx.Melee:
            {
                await MeleeStrike(actor, a, dir, color);
                break;
            }
            case Vfx.Projectile:
            case Vfx.Arrow:
            {
                Audio.Sfx(this, CastSound(a));
                await CastHop(actor, color);
                var flights = a.Targets.Select(t => FireProjectile(actor, View(t), a, dir)).ToList();
                await Task.WhenAll(flights);
                break;
            }
            case Vfx.Beam:
            {
                Audio.Sfx(this, "holy", 0.03f, 0.8f);
                await CastHop(actor, color);
                foreach (var target in a.Targets) SpawnBeam(View(target), color);
                await Wait(0.2);
                break;
            }
            case Vfx.Heal:
            {
                Audio.Sfx(this, "heal", 0.02f);
                // Devour: the servant's soul is torn out and streams into its master.
                foreach (var t in a.Targets.Where(t => t != a.Actor && !t.IsHero && t.Summoned))
                {
                    if (!_views.TryGetValue(t, out var servant)) continue;
                    Audio.Sfx(this, "dark_cast", 0.05f, 0.9f);
                    Flash(servant, new Color("#ff4040"), 1f);
                    for (int i = 0; i < 4; i++) SoundWave(servant.Center, actor.Center, new Color("#ff5a5a"), i * 0.07);
                    await Wait(0.35);
                }
                await CastHop(actor, Visuals.Good);
                break;
            }
            case Vfx.Debuff:
            {
                Audio.Sfx(this, "dark_cast", 0.05f);
                await CastHop(actor, new Color("#b07cff"));
                foreach (var target in a.Targets)
                    Burst(View(target).Center, new Color("#b07cff"), 24, Vector2.Zero, 40, 120, 0.6, gravity: -40);
                await Wait(0.2);
                break;
            }
            case Vfx.Cloud:
            {
                Audio.Sfx(this, a.Actor.IsHero ? "fire_cast" : "growl", 0.05f);
                await CastHop(actor, color);
                foreach (var target in a.Targets)
                    Burst(View(target).Center, a.Element == Element.Physical ? new Color("#9acd4a") : color, 34, Vector2.Zero, 20, 90, 0.9, gravity: -30);
                await Wait(0.25);
                break;
            }
            case Vfx.Buff:
            {
                var buffColor = a.Actor.IsHero ? new Color("#8fb3ff") : new Color("#ff7a5a");
                Audio.Sfx(this, a.Actor.IsHero ? "guard" : "growl", 0.05f, 0.8f);
                var t = actor.Body.CreateTween();
                t.TweenProperty(actor.Body, "scale", new Vector2(1.15f, 1.15f), 0.12).SetTrans(Tween.TransitionType.Back);
                t.TweenProperty(actor.Body, "scale", Vector2.One, 0.2);
                Flash(actor, buffColor, 0.6f);
                Burst(actor.Center, buffColor, 24, Vector2.Up, 120, 260, 0.6);
                await Wait(0.3);
                break;
            }
        }
        if (a.Big) BigImpact(a, color);
    }

    private async Task PlayHit(HitEvent h)
    {
        var v = View(h.Target);
        bool heroHurt = h.Target.IsHero;
        Color color = heroHurt ? new Color("#ff6b6b") : h.Element is Element el ? Visuals.ElementColor(el) : Colors.White;

        if (h.Defense == DefenseQuality.Perfect)
        {
            Audio.Sfx(this, "parry", 0.03f);
            PopText(v, "PARRY!", new Color("#8fb3ff"), 30, -60);
        }
        else if (h.Defense == DefenseQuality.Good)
        {
            Audio.Sfx(this, "guard", 0.05f, 0.7f);
            PopText(v, "BLOCK", new Color("#bfeaff"), 24, -60);
        }
        Audio.Sfx(this, heroHurt ? "hurt" : HitSound(h.Element), 0.1f);

        Flash(v, Colors.White, 1f);
        if (h.Damage > 0) Knockback(v, heavy: h.Crit || h.Damage >= 12);
        Burst(v.Center, color, h.Crit ? 30 : 16, Vector2.Up, 120, 280, 0.45);

        string number = h.Immune ? "IMMUNE" : h.Damage == 0 && h.Absorbed > 0 ? $"({h.Absorbed})" : h.Damage.ToString();
        if (h.Damage > 0 || h.Immune || h.Absorbed > 0)
            PopText(v, h.Crit ? number + "!" : number, h.Immune ? Visuals.TextDim : color, h.Crit ? 44 : 32, 20,
                    (float)GD.RandRange(-22.0, 22.0));
        if (h.Crit) PopText(v, "CRITICAL", Visuals.GoldBright, 16, -6);
        if (h.Weak) PopText(v, "WEAK!", new Color("#ffa040"), 18, 44);
        if (h.Resisted) PopText(v, "resisted", Visuals.TextDim, 15, 44);

        Shake(h.Crit || h.Damage >= 12 ? 11f : 4f);
        if (heroHurt && h.Target.IsAlive && h.HpAfter * 100 < h.Target.MaxHp * 30 && _hurtBarked.Add(h.Target))
            Say(h.Target, Story.HurtBark(h.Target.DefId, _barkRng), force: true);
        v.TweenHp(h.HpAfter);
        v.Refresh(_battle, hp: false);
        await Wait(0.3);
    }

    private async Task PlayTick(TickEvent tick)
    {
        var v = View(tick.Target);
        var color = StatusColor(tick.Kind);
        Audio.Sfx(this, tick.Kind == StatusKind.Burn ? "fire_hit" : "hurt", 0.1f, 0.5f);
        Flash(v, color, 0.7f);
        Burst(v.Center, color, 14, Vector2.Up, 30, 90, 0.6, gravity: -60);
        PopText(v, tick.Damage.ToString(), color, 26, 20);
        v.TweenHp(tick.HpAfter);
        v.Refresh(_battle, hp: false);
        await Wait(0.3);
    }

    private async Task PlayHeal(HealEvent h)
    {
        var v = View(h.Target);
        Flash(v, Visuals.Good, 0.6f);
        Burst(v.Center, Visuals.Good, 22, Vector2.Up, 40, 120, 0.9, gravity: -120);
        if (h.Amount > 0) PopText(v, $"+{h.Amount}", Visuals.Good, 30, 20);
        v.TweenHp(h.HpAfter);
        v.Refresh(_battle, hp: false);
        if (h.Target.IsAlive && v.Body.Modulate.A < 0.5f)
        {
            // Revived
            var t = v.Body.CreateTween();
            t.TweenProperty(v.Body, "modulate:a", 1f, 0.4);
            t.Parallel().TweenProperty(v.Shadow, "modulate:a", 1f, 0.4);
            t.Parallel().TweenProperty(v.Body, "position", v.Home, 0.4);
            t.Parallel().TweenProperty(v.Body, "rotation", 0f, 0.4);
        }
        await Wait(0.35);
    }

    private async Task PlayBreak(BreakEvent b)
    {
        var v = View(b.Target);
        Audio.Sfx(this, "break", 0.02f);
        Flash(v, Visuals.Ember, 1f);
        Shake(16f);
        Burst(v.Center, Visuals.Ember, 50, Vector2.Up, 150, 380, 0.7);
        PopText(v, "BREAK!", new Color("#ffa040"), 52, -50);
        if (b.Interrupted) PopText(v, "Attack interrupted", Visuals.GoldBright, 16, -90);
        v.Refresh(_battle, hp: false);
        double speed = Engine.TimeScale;
        Engine.TimeScale = speed * 0.3; // hit stop
        await Wait(0.05);
        Engine.TimeScale = speed;
        await Wait(0.5);
    }

    private async Task PlayDeath(Combatant unit)
    {
        var v = View(unit);
        Audio.Sfx(this, "death");
        Flash(v, new Color("#ff3b3b"), 1f);
        v.TweenHp(0);
        var t = v.Body.CreateTween();
        for (int i = 0; i < 3; i++)
        {
            t.TweenProperty(v.Body, "modulate:a", 0.25f, 0.05);
            t.TweenProperty(v.Body, "modulate:a", 1f, 0.05);
        }
        t.TweenProperty(v.Body, "rotation", v.Unit.IsHero ? -0.5f : 0.5f, 0.45).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        t.Parallel().TweenProperty(v.Body, "modulate:a", 0f, 0.45);
        t.Parallel().TweenProperty(v.Body, "position", v.Home + new Vector2(0, 14), 0.45);
        t.Parallel().TweenProperty(v.Shadow, "modulate:a", 0f, 0.45);
        Burst(v.Center, new Color("#9a8fb0"), 30, Vector2.Up, 30, 110, 1.0, gravity: -60);
        await ToSignal(t, Tween.SignalName.Finished);
        v.Refresh(_battle, hp: false);
        if (!unit.IsHero)
        {
            v.Visible = false;
            LayoutUnits(animate: true);
        }
    }

    private async Task PlaySummon(Combatant unit)
    {
        Audio.Sfx(this, "summon", 0.02f);
        var v = AddView(unit);
        LayoutUnits(animate: true);
        v.Refresh(_battle, hp: true);
        v.Body.Modulate = new Color(1, 1, 1, 0);
        v.Body.Position = v.Home + new Vector2(0, 30);
        await Wait(0.3);
        Burst(v.Center, new Color("#58ff8a"), 40, Vector2.Up, 60, 200, 0.8, gravity: -100);
        var t = v.Body.CreateTween();
        t.TweenProperty(v.Body, "modulate:a", 1f, 0.5);
        t.Parallel().TweenProperty(v.Body, "position", v.Home, 0.5).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        await ToSignal(t, Tween.SignalName.Finished);
    }

    // ═════════════════════════ Effects ═════════════════════════

    private static string CastSound(ActionEvent a)
    {
        if (a.Vfx == Vfx.Arrow) return "arrow";
        return a.Element switch
        {
            Element.Fire => "fire_cast",
            Element.Ice => "ice_cast",
            Element.Lightning => "holy",
            _ => a.Actor.IsHero ? "arrow" : "dark_cast",
        };
    }

    private static string HitSound(Element? element) => element switch
    {
        Element.Fire => "fire_hit",
        Element.Ice => "ice_hit",
        Element.Lightning => "lock",
        _ => "slash_hit",
    };

    private static Color StatusColor(StatusKind kind) => kind switch
    {
        StatusKind.Poison => new Color("#8fd14a"),
        StatusKind.Burn => new Color("#ff7a3d"),
        StatusKind.Bleed => new Color("#ff4a5a"),
        StatusKind.Regen => Visuals.Good,
        StatusKind.Marked => new Color("#ff6a6a"),
        StatusKind.Weakened => new Color("#b07cff"),
        StatusKind.Empowered => new Color("#ffb347"),
        StatusKind.Slowed => new Color("#7fd4ff"),
        StatusKind.Taunt => new Color("#ff9a5a"),
        StatusKind.Stun => Visuals.GoldBright,
        _ => new Color("#8fb3ff"),
    };

    private async Task CastHop(BattleUnitView actor, Color color)
    {
        SpawnMagicCircle(actor, color);
        var t = actor.Body.CreateTween();
        t.TweenProperty(actor.Body, "position", actor.Home + new Vector2(0, -14), 0.1).SetEase(Tween.EaseType.Out);
        t.TweenProperty(actor.Body, "position", actor.Home, 0.15).SetEase(Tween.EaseType.In);
        Burst(actor.Center + new Vector2(0, -30), color, 14, Vector2.Up, 40, 120, 0.5, gravity: -80);
        await Wait(0.15);
    }

    private async Task FireProjectile(BattleUnitView from, BattleUnitView to, ActionEvent a, float dir)
    {
        string texture = a.Vfx == Vfx.Arrow ? "FX/arrow.png" : a.Element switch
        {
            Element.Fire => "FX/fireball.png",
            Element.Ice => "FX/shard.png",
            Element.Lightning => "FX/sparkle.png",
            _ => a.Actor.IsHero ? "FX/arrow.png" : "FX/darkbolt.png",
        };
        var color = a.Vfx == Vfx.Arrow ? new Color("#cfc6aa")
            : a.Element == Element.Physical && !a.Actor.IsHero ? new Color("#b06cff")
            : Visuals.ElementColor(a.Element);

        Vector2 start = from.Center + new Vector2(40 * dir, -10);
        Vector2 end = to.Center;
        var projectile = new Sprite2D
        {
            Texture = Visuals.Tex(texture),
            Scale = new Vector2(4, 4),
            TextureFilter = TextureFilterEnum.Nearest,
            Position = start,
            Rotation = (end - start).Angle(),
            Modulate = a.Element == Element.Lightning ? new Color("#d9b8ff") : Colors.White,
        };
        _fx.AddChild(projectile);

        var trail = Particles(color, 30, 0.35, Vector2.Zero, 0, 20, gravity: 0);
        trail.OneShot = false;
        trail.Explosiveness = 0f;
        trail.LocalCoords = false;
        trail.Scale = new Vector2(0.25f, 0.25f);
        projectile.AddChild(trail);
        trail.Emitting = true;

        // Arrows and bolts fly on a curved path (quadratic bezier) and turn along it.
        float lift = a.Vfx == Vfx.Arrow ? 110f : 55f;
        Vector2 control = (start + end) / 2 + new Vector2(0, -lift - Mathf.Abs(end.X - start.X) * 0.08f);
        Vector2 Bezier(float k) => start.Lerp(control, k).Lerp(control.Lerp(end, k), k);
        double flight = a.Vfx == Vfx.Arrow ? 0.34 : 0.3;
        var t = projectile.CreateTween();
        t.TweenMethod(Callable.From<float>(k =>
        {
            if (!IsInstanceValid(projectile)) return;
            var p = Bezier(k);
            var ahead = Bezier(Mathf.Min(1f, k + 0.04f));
            projectile.Position = p;
            if (ahead != p) projectile.Rotation = (ahead - p).Angle();
            if (a.Vfx != Vfx.Arrow) projectile.RotationDegrees += Mathf.Sin(k * 20) * 8;
        }), 0f, 1f, flight).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        await ToSignal(t, Tween.SignalName.Finished);

        trail.Emitting = false;
        trail.Reparent(_fx);
        GetTree().CreateTimer(0.6).Timeout += () => { if (IsInstanceValid(trail)) trail.QueueFree(); };
        projectile.QueueFree();
    }

    private void SpawnSlash(BattleUnitView target, bool byHero, Color color)
    {
        var slash = new Sprite2D
        {
            Texture = Visuals.Tex("FX/slash.png"),
            Scale = new Vector2(5, 5),
            TextureFilter = TextureFilterEnum.Nearest,
            Position = target.Center,
            FlipH = !byHero,
            Modulate = byHero ? color : new Color("#ffb0b0"),
        };
        _fx.AddChild(slash);
        var t = slash.CreateTween();
        t.TweenProperty(slash, "scale", new Vector2(6, 6), 0.18);
        t.Parallel().TweenProperty(slash, "modulate:a", 0f, 0.22);
        t.TweenCallback(Callable.From(slash.QueueFree));
    }

    private void SpawnBeam(BattleUnitView target, Color color)
    {
        var star = new Sprite2D
        {
            Texture = Visuals.Tex("FX/sparkle.png"),
            Scale = Vector2.Zero,
            TextureFilter = TextureFilterEnum.Nearest,
            Position = target.Center,
            Modulate = color,
        };
        _fx.AddChild(star);
        var t = star.CreateTween();
        t.TweenProperty(star, "scale", new Vector2(9, 9), 0.18).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(star, "rotation", Mathf.Pi / 4, 0.4);
        t.TweenProperty(star, "modulate:a", 0f, 0.2);
        t.TweenCallback(Callable.From(star.QueueFree));

        var rain = Particles(color, 30, 0.6, Vector2.Down, 120, 260, gravity: 300);
        rain.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
        rain.EmissionRectExtents = new Vector2(30, 4);
        rain.Spread = 5;
        AddOneShot(rain, target.Center + new Vector2(0, -120), 1.0);
    }

    private void Flash(BattleUnitView v, Color color, float strength)
    {
        v.SpriteMaterial.SetShaderParameter("flash_color", color);
        var t = v.Sprite.CreateTween();
        t.TweenMethod(Callable.From<float>(x => v.SpriteMaterial.SetShaderParameter("flash", x)), strength, 0f, 0.3);
    }

    private void Pulse(BattleUnitView v)
    {
        var t = v.Body.CreateTween();
        t.TweenProperty(v.Body, "scale", new Vector2(1.08f, 1.08f), 0.1);
        t.TweenProperty(v.Body, "scale", Vector2.One, 0.15);
    }

    private void Knockback(BattleUnitView v, bool heavy = false)
    {
        float push = (v.Unit.IsHero ? -16 : 16) * (heavy ? 1.8f : 1f);
        float tilt = (v.Unit.IsHero ? -0.16f : 0.16f) * (heavy ? 1.6f : 1f);
        var t = v.Body.CreateTween();
        t.TweenProperty(v.Body, "position", v.Home + new Vector2(push, 0), 0.05);
        t.Parallel().TweenProperty(v.Body, "rotation", tilt, 0.05);
        t.Parallel().TweenProperty(v.Body, "scale", new Vector2(1.14f, 0.86f), 0.05);
        t.TweenProperty(v.Body, "position", v.Home, 0.25).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(v.Body, "rotation", 0f, 0.3).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(v.Body, "scale", Vector2.One, 0.3).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
    }

    // ───────────── Powerful attacks ─────────────

    private ColorRect? _dim;

    /// <summary>Screen darkens, energy converges on the attacker, its name flashes: something big is coming.</summary>
    private async Task PowerUp(BattleUnitView actor, ActionEvent a, Color color)
    {
        var glow = a.Actor.IsHero ? (a.Element == Element.Physical ? Visuals.GoldBright : color) : (a.Element == Element.Physical ? Visuals.Ember : color);
        _dim ??= CreateDim();
        _dim.CreateTween().TweenProperty(_dim, "color:a", 0.55f, 0.18);
        Audio.Sfx(this, "charge", 0.03f, 0.9f);
        PopText(actor, a.Name.ToUpperInvariant() + "!", glow, 30, -30);

        var converge = new CpuParticles2D
        {
            Amount = 50,
            Lifetime = 0.45,
            OneShot = true,
            Explosiveness = 0.15f,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.SphereSurface,
            EmissionSphereRadius = 120 * a.Actor.Scale,
            Gravity = Vector2.Zero,
            RadialAccelMin = -520,
            RadialAccelMax = -380,
            ScaleAmountMin = 3,
            ScaleAmountMax = 6,
            Color = glow,
        };
        AddOneShot(converge, actor.Center, 1.0);

        var t = actor.Body.CreateTween();
        t.TweenProperty(actor.Body, "scale", new Vector2(1.22f, 1.22f), 0.3).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenMethod(Callable.From<float>(x => actor.SpriteMaterial.SetShaderParameter("flash", x)), 0f, 0.7f, 0.3);
        actor.SpriteMaterial.SetShaderParameter("flash_color", glow);
        t.TweenProperty(actor.Body, "scale", Vector2.One, 0.12);
        t.Parallel().TweenMethod(Callable.From<float>(x => actor.SpriteMaterial.SetShaderParameter("flash", x)), 0.7f, 0f, 0.12);
        await Wait(0.42);
    }

    private ColorRect CreateDim()
    {
        var dim = new ColorRect { Color = new Color(0.02f, 0f, 0.04f, 0f), MouseFilter = MouseFilterEnum.Ignore };
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(dim);
        MoveChild(dim, _field.GetIndex()); // behind the units, over the background
        return dim;
    }

    /// <summary>Shockwaves, a white flash, a hard shake and a short hit-stop when a big attack lands.</summary>
    private async void BigImpact(ActionEvent a, Color color)
    {
        var wave = a.Actor.IsHero ? Visuals.GoldBright : Visuals.Ember;
        foreach (var target in a.Targets)
        {
            if (!_views.TryGetValue(target, out var v)) continue;
            Shockwave(v.Body.GlobalPosition, wave, v.Unit.Scale);
            Shockwave(v.Center, color, 0.7f * v.Unit.Scale, 0.08);
        }
        Shake(15f);
        var flash = new ColorRect { Color = new Color(1, 1, 1, 0.35f), MouseFilter = MouseFilterEnum.Ignore };
        flash.SetAnchorsPreset(LayoutPreset.FullRect);
        _fx.AddChild(flash);
        var f = flash.CreateTween();
        f.TweenProperty(flash, "color:a", 0f, 0.25);
        f.TweenCallback(Callable.From(flash.QueueFree));

        double speed = Engine.TimeScale;
        Engine.TimeScale = speed * 0.25; // hit-stop
        await ToSignal(GetTree().CreateTimer(0.06, true, false, true), SceneTreeTimer.SignalName.Timeout);
        Engine.TimeScale = speed;
        if (_dim is not null && IsInstanceValid(_dim)) _dim.CreateTween().TweenProperty(_dim, "color:a", 0f, 0.4);
    }

    /// <summary>An expanding ground ring.</summary>
    private void Shockwave(Vector2 position, Color color, float scale = 1f, double delay = 0)
    {
        var ring = new Sprite2D
        {
            Texture = Visuals.Tex("FX/circle.png"),
            Scale = new Vector2(1, 1) * scale,
            TextureFilter = TextureFilterEnum.Nearest,
            Position = position,
            Modulate = new Color(color, 0.95f),
        };
        _fx.AddChild(ring);
        var t = ring.CreateTween();
        if (delay > 0) t.TweenInterval(delay);
        t.TweenProperty(ring, "scale", new Vector2(9, 9) * scale, 0.4).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(ring, "modulate:a", 0f, 0.4);
        t.TweenCallback(Callable.From(ring.QueueFree));
    }

    // ───────────── Monster flavor ─────────────

    /// <summary>A short, sprite-specific flourish before an enemy casts or shoots.</summary>
    private async Task CastFlourish(BattleUnitView actor, ActionEvent a, Color color)
    {
        var body = actor.Body;
        switch (a.Actor.Sprite)
        {
            case "imp":
            {
                // Cackling spin-hop.
                var t = body.CreateTween();
                t.TweenProperty(body, "position", actor.Home + new Vector2(0, -40), 0.16).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);
                t.Parallel().TweenProperty(body, "rotation", -Mathf.Tau, 0.32);
                t.TweenProperty(body, "position", actor.Home, 0.16).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
                await ToSignal(t, Tween.SignalName.Finished);
                body.Rotation = 0;
                break;
            }
            case "siren" or "matriarch":
            {
                // A song: rings of sound roll toward the targets.
                Audio.Sfx(this, "ice_cast", 0.15f, 0.6f);
                var sway = body.CreateTween();
                sway.TweenProperty(body, "rotation", 0.12f, 0.12);
                sway.TweenProperty(body, "rotation", -0.12f, 0.2);
                sway.TweenProperty(body, "rotation", 0f, 0.12);
                foreach (var target in a.Targets.Take(3))
                {
                    if (!_views.TryGetValue(target, out var tv)) continue;
                    for (int i = 0; i < 3; i++) SoundWave(actor.Center, tv.Center, new Color("#9fe0ff"), i * 0.09);
                }
                await Wait(0.32);
                break;
            }
            case "moth" or "wraith" or "wisp" or "harpy":
            {
                // Flutter up, then cast.
                var t = body.CreateTween();
                for (int i = 0; i < 3; i++)
                {
                    t.TweenProperty(body, "position", actor.Home + new Vector2(0, -20 - i * 6), 0.06);
                    t.TweenProperty(body, "position", actor.Home + new Vector2(0, -10 - i * 6), 0.06);
                }
                t.TweenProperty(body, "position", actor.Home, 0.12);
                await Wait(0.22);
                break;
            }
            case "golem" or "construct":
            {
                // Stomp: the ground cracks.
                var t = body.CreateTween();
                t.TweenProperty(body, "scale", new Vector2(1.12f, 0.88f), 0.1);
                t.TweenProperty(body, "scale", Vector2.One, 0.15);
                Shockwave(body.GlobalPosition, new Color("#ff8a2c"), a.Actor.Scale);
                Burst(body.GlobalPosition, new Color("#8a6a50"), 20, Vector2.Up, 60, 160, 0.6, gravity: 300);
                Shake(5f);
                await Wait(0.18);
                break;
            }
            case "necromancer" or "cultist" or "hollow_king" or "colossus" or "lich" or "stormcaller" or "tyrant" or "hexer":
            {
                // Arms raised: the body rises and glows.
                var t = body.CreateTween();
                t.TweenProperty(body, "scale", new Vector2(0.94f, 1.1f), 0.14).SetTrans(Tween.TransitionType.Back);
                t.TweenProperty(body, "scale", Vector2.One, 0.14);
                Flash(actor, color, 0.5f);
                await Wait(0.14);
                break;
            }
        }
    }

    private void SoundWave(Vector2 from, Vector2 to, Color color, double delay)
    {
        var ring = new Sprite2D
        {
            Texture = Visuals.Tex("FX/circle.png"),
            Scale = new Vector2(1.2f, 3.2f),
            TextureFilter = TextureFilterEnum.Nearest,
            Position = from,
            Rotation = (to - from).Angle() + Mathf.Pi / 2,
            Modulate = new Color(color, 0f),
        };
        _fx.AddChild(ring);
        var t = ring.CreateTween();
        t.TweenInterval(delay);
        t.TweenProperty(ring, "modulate:a", 0.85f, 0.05);
        t.TweenProperty(ring, "position", to, 0.32).SetTrans(Tween.TransitionType.Sine);
        t.Parallel().TweenProperty(ring, "scale", new Vector2(2.2f, 5.5f), 0.32);
        t.TweenProperty(ring, "modulate:a", 0f, 0.1);
        t.TweenCallback(Callable.From(ring.QueueFree));
    }

    /// <summary>Weaponless monsters: rats and leeches pounce, moths swoop, golems leap and slam.</summary>
    private static string BeastStyle(string sprite) => sprite switch
    {
        "rat" or "leech" or "ghoul" or "hound" or "mimic" => "pounce",
        "moth" or "wraith" or "wisp" or "harpy" => "swoop",
        "golem" or "matriarch" or "construct" => "slam",
        _ => "lunge",
    };

    private async Task BeastStrike(BattleUnitView actor, ActionEvent a, float dir, string style)
    {
        var body = actor.Body;
        var first = a.Targets.Select(t => _views.TryGetValue(t, out var tv) ? tv : null).FirstOrDefault(tv => tv is not null);
        Vector2 reach = first is null ? new Vector2(80 * dir, 0) : first.Body.GlobalPosition - body.GlobalPosition + actor.Home - body.Position;
        // Stop a little in front of the target.
        Vector2 landing = actor.Home + new Vector2(reach.X - dir * 70 * a.Actor.Scale, 0);

        async Task Arc(Vector2 from, Vector2 to, float height, double time)
        {
            Vector2 control = (from + to) / 2 + new Vector2(0, -height);
            var arc = body.CreateTween();
            arc.TweenMethod(Callable.From<float>(k =>
            {
                if (IsInstanceValid(body)) body.Position = from.Lerp(control, k).Lerp(control.Lerp(to, k), k);
            }), 0f, 1f, time);
            await ToSignal(arc, Tween.SignalName.Finished);
        }

        if (style == "pounce")
        {
            var crouch = body.CreateTween();
            crouch.TweenProperty(body, "scale", new Vector2(1.2f, 0.78f), 0.12);
            await ToSignal(crouch, Tween.SignalName.Finished);
            Audio.Sfx(this, "growl", 0.2f, 0.5f);
            body.Scale = new Vector2(0.85f, 1.2f);
            body.Rotation = 0.35f * dir;
            await Arc(actor.Home, landing, 70, 0.2);
            body.Rotation = 0;
            body.Scale = new Vector2(1.2f, 0.85f);
            foreach (var t in a.Targets) if (_views.TryGetValue(t, out var tv)) SpawnClaw(tv, a.Actor.IsHero);
            await Wait(0.08);
            var settle = body.CreateTween();
            settle.TweenProperty(body, "scale", Vector2.One, 0.12);
            await Arc(landing, actor.Home, 40, 0.24);
        }
        else if (style == "swoop")
        {
            var up = body.CreateTween();
            up.TweenProperty(body, "position", actor.Home + new Vector2(-20 * dir, -70), 0.16).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            await ToSignal(up, Tween.SignalName.Finished);
            Audio.Sfx(this, "swing", 0.1f, 0.7f);
            body.Rotation = 0.5f * dir;
            await Arc(body.Position, landing + new Vector2(0, -30), -40, 0.14);
            foreach (var t in a.Targets) if (_views.TryGetValue(t, out var tv)) SpawnClaw(tv, a.Actor.IsHero);
            body.Rotation = 0;
            await Arc(body.Position, actor.Home, 80, 0.3);
        }
        else // slam
        {
            var rise = body.CreateTween();
            rise.TweenProperty(body, "position", actor.Home + new Vector2(10 * dir, -80), 0.24).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            rise.Parallel().TweenProperty(body, "scale", new Vector2(0.9f, 1.15f), 0.24);
            await ToSignal(rise, Tween.SignalName.Finished);
            await Wait(0.05);
            var fall = body.CreateTween();
            fall.TweenProperty(body, "position", actor.Home + new Vector2(reach.X * 0.45f, 0), 0.09).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            await ToSignal(fall, Tween.SignalName.Finished);
            body.Scale = new Vector2(1.3f, 0.75f);
            Audio.Sfx(this, "break", 0.1f, 0.6f);
            Shake(12f);
            Shockwave(body.GlobalPosition, new Color("#c8a070"), a.Actor.Scale * 1.3f);
            Burst(body.GlobalPosition, new Color("#8a7060"), 30, Vector2.Up, 80, 220, 0.7, gravity: 400);
            foreach (var t in a.Targets) if (_views.TryGetValue(t, out var tv)) Shockwave(tv.Body.GlobalPosition, new Color("#ffb070"), tv.Unit.Scale, 0.06);
            var back = body.CreateTween();
            back.TweenInterval(0.1);
            back.TweenProperty(body, "scale", Vector2.One, 0.2).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
            back.Parallel().TweenProperty(body, "position", actor.Home, 0.3).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            await Wait(0.12);
        }
    }

    // ───────────── Melee: wind-up, weapon swing, swoosh, impact ─────────────

    /// <summary>Which weapon a sprite swings (null = it fights with claws/fists).</summary>
    private static string? WeaponFor(string sprite) => sprite switch
    {
        "vanguard" or "knight" => "FX/weapon_sword.png",
        "shadowblade" => "FX/weapon_dagger.png",
        "warden" or "cleric" or "bulwark" => "FX/weapon_mace.png",
        "skeleton" or "colossus" or "thrall" => "FX/weapon_club.png",
        "hollow_king" => "FX/weapon_greatsword.png",
        "berserker" => "FX/weapon_axe.png",
        "wight" => "FX/weapon_sword.png",
        "tyrant" => "FX/weapon_greatsword.png",
        "hexer" => "FX/weapon_dagger.png",
        "ranger" or "skeleton_archer" => "FX/weapon_dagger.png",
        "arcanist" or "mage" or "cultist" or "necromancer" => "FX/weapon_mace.png",
        _ => null,
    };

    private async Task MeleeStrike(BattleUnitView actor, ActionEvent a, float dir, Color color)
    {
        var body = actor.Body;
        string? weaponPath = WeaponFor(a.Actor.Sprite);
        // Templar uses the vanguard sprite but carries a greatsword.
        if (a.Actor.DefId == "templar") weaponPath = "FX/weapon_greatsword.png";
        bool big = weaponPath == "FX/weapon_greatsword.png" || a.Actor.IsBoss || a.Big;
        if (weaponPath is null && BeastStyle(a.Actor.Sprite) is var style && style != "lunge")
        {
            await BeastStrike(actor, a, dir, style);
            return;
        }

        Sprite2D? weapon = null;
        float unitScale = Visuals.SpriteScale * a.Actor.Scale;
        Vector2 hand = new(dir * actor.SpriteHeight * 0.22f, -actor.SpriteHeight * 0.42f);
        if (weaponPath is not null)
        {
            var tex = Visuals.Tex(weaponPath);
            weapon = new Sprite2D
            {
                Texture = tex,
                Offset = new Vector2(0, -tex.GetHeight() / 2f + 6), // pivot at the hilt
                Scale = new Vector2(unitScale * 0.8f, unitScale * 0.8f),
                TextureFilter = TextureFilterEnum.Nearest,
                Position = hand,
                Rotation = dir * 0.3f,
                FlipH = dir < 0,
                Modulate = new Color(1, 1, 1, 0),
                ZIndex = 1,
            };
            body.AddChild(weapon);
        }

        // 1) Wind-up: lean back, crouch, raise the weapon behind the head.
        double windup = big ? 0.22 : 0.15;
        var t = body.CreateTween();
        t.TweenProperty(body, "position", actor.Home + new Vector2(-16 * dir, 2), windup).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(body, "scale", new Vector2(0.9f, 1.1f), windup);
        t.Parallel().TweenProperty(body, "rotation", -0.12f * dir, windup);
        if (weapon is not null)
        {
            t.Parallel().TweenProperty(weapon, "modulate:a", 1f, windup * 0.6);
            t.Parallel().TweenProperty(weapon, "rotation", -1.9f * dir, windup).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }
        await ToSignal(t, Tween.SignalName.Finished);

        // 2) Lunge and swing through.
        Audio.Sfx(this, "swing", 0.08f, big ? 1f : 0.85f);
        double swing = big ? 0.16 : 0.11;
        var lunge = body.CreateTween();
        lunge.TweenProperty(body, "position", actor.Home + new Vector2(84 * dir, -10), swing).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        lunge.Parallel().TweenProperty(body, "scale", new Vector2(1.15f, 0.9f), swing);
        lunge.Parallel().TweenProperty(body, "rotation", 0.14f * dir, swing);
        if (weapon is not null)
        {
            lunge.Parallel().TweenProperty(weapon, "rotation", 2.2f * dir, swing).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
            SpawnSwoosh(body, hand, weapon.Texture.GetHeight() * unitScale * 0.8f, dir, swing, a.Actor.IsHero ? color : new Color("#ffb0b0"));
        }
        await ToSignal(lunge, Tween.SignalName.Finished);

        // 3) Impact.
        foreach (var target in a.Targets)
        {
            if (weapon is null) SpawnClaw(View(target), a.Actor.IsHero);
            else SpawnSlash(View(target), a.Actor.IsHero, color);
        }
        if (big) Shake(6f);

        // 4) Recover.
        var back = body.CreateTween();
        back.TweenInterval(0.06);
        back.TweenProperty(body, "position", actor.Home, 0.24).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
        back.Parallel().TweenProperty(body, "scale", Vector2.One, 0.24);
        back.Parallel().TweenProperty(body, "rotation", 0f, 0.24);
        if (weapon is not null)
        {
            back.Parallel().TweenProperty(weapon, "modulate:a", 0f, 0.2);
            back.TweenCallback(Callable.From(weapon.QueueFree));
        }
    }

    /// <summary>A fading arc that follows the weapon tip through its swing.</summary>
    private void SpawnSwoosh(Node2D body, Vector2 pivot, float radius, float dir, double duration, Color color)
    {
        var arc = new Line2D
        {
            Width = 14,
            DefaultColor = new Color(color, 0.75f),
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round,
            Antialiased = true,
            ZIndex = 1,
            WidthCurve = new Curve(),
        };
        arc.WidthCurve.AddPoint(new Vector2(0, 0.1f));
        arc.WidthCurve.AddPoint(new Vector2(1, 1f));
        body.AddChild(arc);
        const int segments = 14;
        float from = -1.9f * dir, to = 2.2f * dir;
        var tween = arc.CreateTween();
        tween.TweenMethod(Callable.From<float>(k =>
        {
            if (!IsInstanceValid(arc)) return;
            arc.ClearPoints();
            int count = Math.Max(2, (int)(segments * k));
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(from, to, k * i / (count - 1)) - Mathf.Pi / 2;
                arc.AddPoint(pivot + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }), 0f, 1f, duration);
        tween.TweenProperty(arc, "modulate:a", 0f, 0.18);
        tween.TweenCallback(Callable.From(arc.QueueFree));
    }

    /// <summary>Three raking claw marks (for beasts and fists).</summary>
    private void SpawnClaw(BattleUnitView target, bool byHero)
    {
        Audio.Sfx(this, "slash_hit", 0.1f, 0.6f);
        for (int i = 0; i < 3; i++)
        {
            var claw = new Sprite2D
            {
                Texture = Visuals.Tex("FX/claw.png"),
                Scale = new Vector2(4.5f, 4.5f),
                TextureFilter = TextureFilterEnum.Nearest,
                Position = target.Center + new Vector2((i - 1) * 16, (i - 1) * -6),
                FlipH = !byHero,
                Modulate = new Color(byHero ? "#ffe0c0" : "#ff8080", 0f),
            };
            _fx.AddChild(claw);
            var t = claw.CreateTween();
            t.TweenInterval(i * 0.04);
            t.TweenProperty(claw, "modulate:a", 1f, 0.04);
            t.Parallel().TweenProperty(claw, "position", claw.Position + new Vector2(0, 18), 0.12);
            t.TweenProperty(claw, "modulate:a", 0f, 0.2);
            t.TweenCallback(Callable.From(claw.QueueFree));
        }
    }

    /// <summary>A glowing rune ring under a caster's feet.</summary>
    private void SpawnMagicCircle(BattleUnitView actor, Color color)
    {
        var ring = new Sprite2D
        {
            Texture = Visuals.Tex("FX/circle.png"),
            Scale = new Vector2(1.5f, 1.5f) * actor.Unit.Scale,
            TextureFilter = TextureFilterEnum.Nearest,
            Position = actor.Body.GlobalPosition + new Vector2(0, -2),
            Modulate = new Color(color, 0f),
        };
        _fx.AddChild(ring);
        var t = ring.CreateTween();
        t.TweenProperty(ring, "modulate:a", 0.95f, 0.08);
        t.Parallel().TweenProperty(ring, "scale", new Vector2(4.2f, 4.2f) * actor.Unit.Scale, 0.35).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.TweenProperty(ring, "modulate:a", 0f, 0.3);
        t.TweenCallback(Callable.From(ring.QueueFree));
        var sparks = Particles(color, 16, 0.6, Vector2.Up, 40, 120, gravity: -60);
        sparks.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
        sparks.EmissionRectExtents = new Vector2(40 * actor.Unit.Scale, 4);
        AddOneShot(sparks, actor.Body.GlobalPosition, 0.9);
    }

    private void Shake(float strength)
    {
        _shakeTween?.Kill();
        _shakeTween = CreateTween();
        for (int i = 0; i < 6; i++)
        {
            var offset = new Vector2((float)GD.RandRange(-1.0, 1.0), (float)GD.RandRange(-1.0, 1.0)) * strength * (1 - i / 6f);
            _shakeTween.TweenProperty(_field, "position", offset, 0.03);
        }
        _shakeTween.TweenProperty(_field, "position", Vector2.Zero, 0.04);
    }

    private void PopText(BattleUnitView v, string text, Color color, int size, float yOffset = 0, float xOffset = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        var label = Visuals.Label(text, size, color, outline: Math.Max(6, size / 4));
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        _fx.AddChild(label);
        label.Size = new Vector2(300, size * 1.6f);
        label.PivotOffset = label.Size / 2;
        var start = v.Head + new Vector2(-150 + xOffset, -size + yOffset);
        label.Position = start;

        var t = label.CreateTween();
        t.TweenProperty(label, "scale", Vector2.One, 0.2).From(new Vector2(1.6f, 1.6f))
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        t.Parallel().TweenProperty(label, "position", start + new Vector2(0, -42), 0.9)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        t.TweenProperty(label, "modulate:a", 0f, 0.25);
        t.TweenCallback(Callable.From(label.QueueFree));
    }

    private void Burst(Vector2 position, Color color, int amount, Vector2 direction, float minSpeed, float maxSpeed,
                       double lifetime, float gravity = 500)
    {
        AddOneShot(Particles(color, amount, lifetime, direction, minSpeed, maxSpeed, gravity), position, lifetime + 0.3);
    }

    private void AddOneShot(CpuParticles2D particles, Vector2 position, double freeAfter)
    {
        particles.Position = position;
        _fx.AddChild(particles);
        particles.Emitting = true;
        GetTree().CreateTimer(freeAfter).Timeout += () => { if (IsInstanceValid(particles)) particles.QueueFree(); };
    }

    private static CpuParticles2D Particles(Color color, int amount, double lifetime, Vector2 direction,
                                            float minSpeed, float maxSpeed, float gravity) => new()
    {
        Emitting = false,
        OneShot = true,
        Amount = Math.Max(1, amount),
        Lifetime = lifetime,
        Explosiveness = 0.9f,
        Direction = direction == Vector2.Zero ? Vector2.Up : direction,
        Spread = direction == Vector2.Zero ? 180f : 70f,
        InitialVelocityMin = minSpeed,
        InitialVelocityMax = maxSpeed,
        Gravity = new Vector2(0, gravity),
        ScaleAmountMin = 3f,
        ScaleAmountMax = 7f,
        Color = color,
        ColorRamp = new Gradient
        {
            Colors = new[] { Colors.White, new Color(1, 1, 1, 0) },
            Offsets = new[] { 0.4f, 1f },
        },
    };

    private void Toast(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Audio.Sfx(this, "blip", 0f, 0.6f);
        _toast.Text = text;
        _toastTween?.Kill();
        _toast.Modulate = Colors.White;
        _toastTween = _toast.CreateTween();
        _toastTween.TweenInterval(1.6);
        _toastTween.TweenProperty(_toast, "modulate:a", 0f, 0.4);
    }

    private SignalAwaiter Wait(double seconds) => ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

    // ═════════════════════════ Views & layout ═════════════════════════

    private BattleUnitView View(Combatant unit) => _views.TryGetValue(unit, out var view) ? view : AddView(unit);

    private BattleUnitView AddView(Combatant unit)
    {
        var view = new BattleUnitView(unit);
        view.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) OnUnitClicked(unit);
        };
        view.MouseEntered += () => { _hover = unit; UpdateOutlines(); };
        view.MouseExited += () => { if (_hover == unit) _hover = null; UpdateOutlines(); };
        _field.AddChild(view);
        _views[unit] = view;
        return view;
    }

    /// <summary>Heroes on the left (front row nearest the middle), enemies mirrored on the right.</summary>
    private void LayoutUnits(bool animate)
    {
        var size = GetViewportRect().Size;
        float center = size.X / 2;
        float baseline = size.Y - 284;
        const float rowGap = 34;

        void Place(IEnumerable<Combatant> units, float startX, float dir)
        {
            float x = startX;
            var list = units.ToList();
            // Squeeze the spacing so a full side always fits between the center and the screen edge.
            float room = (dir > 0 ? size.X - startX : startX) - BattleUnitView.ViewWidth / 2 - 12;
            float needed = list.Sum(u => 122 + (u.Scale - 1) * 60) - 122 + rowGap;
            float step = list.Count <= 1 || needed <= room ? 122 : Mathf.Max(78, 122 - (needed - room) / Mathf.Max(1, list.Count - 1));
            foreach (var row in new[] { Row.Front, Row.Back })
            {
                foreach (var unit in list.Where(u => u.Row == row).OrderBy(u => u.Slot))
                {
                    var view = View(unit);
                    float extra = (unit.Scale - 1) * 60;
                    x += dir * extra / 2;
                    var target = new Vector2(x - BattleUnitView.ViewWidth / 2, baseline - view.FeetY);
                    if (animate) view.CreateTween().TweenProperty(view, "position", target, 0.3).SetTrans(Tween.TransitionType.Quad);
                    else view.Position = target;
                    x += dir * (step + extra / 2);
                }
                x += dir * rowGap;
            }
        }

        Place(_battle.Heroes, center - 95, -1);
        // Fallen enemies leave the field, so summons always appear on-screen.
        Place(_battle.Enemies.Where(e => e.IsAlive), center + 95, 1);
    }

    private void UpdateOutlines()
    {
        var current = _battle.Current;
        var hero = ActiveHero;
        bool targeting = _pendingAbility is not null || _pendingItem >= 0;

        // Hovering an enemy (outside targeting) outlines the heroes its intent will hit.
        var threatened = new List<Combatant>();
        if (!targeting && _hover is { IsHero: false, IsAlive: true } enemy && enemy.Intent is { Charging: false } intent && intent.Move.HitsHeroes)
            threatened.AddRange(_battle.IntentTargets(enemy, intent));

        // Damage preview: for the pending ability, or the ability button under the mouse.
        var previewAbility = _pendingAbility ?? _hoverAbility;
        var previewTargets = _pendingAbility is not null ? _targets
            : _hoverAbility is not null && hero is not null ? _battle.ValidTargets(hero, _hoverAbility).ToList()
            : new List<Combatant>();

        foreach (var (unit, view) in _views)
        {
            Color outline = new(0, 0, 0, 0);
            if (_targets.Contains(unit)) outline = unit == _hover ? Colors.White : new Color("#6be07a");
            else if (threatened.Contains(unit)) outline = new Color("#ff3b3b");
            else if (!_busy && unit == current && unit.IsHero) outline = Visuals.GoldBright;
            else if (unit == current && !unit.IsHero) outline = new Color("#ff6a4a");
            view.SetOutline(outline);

            bool show = !_busy && hero is not null && previewAbility is not null && previewTargets.Contains(unit);
            view.SetPreview(show ? PreviewText(hero!, previewAbility!, unit) : null);
        }
    }

    private string? PreviewText(Combatant hero, AbilityInstance ability, Combatant target)
    {
        var p = _battle.Preview(hero, ability, target);
        var parts = new List<string>();
        if (p.Immune) parts.Add("[color=#a99f8f]IMMUNE[/color]");
        else if (p.Damage > 0) parts.Add($"[b]{p.Damage}[/b]");
        if (p.Damage > 0 && p.HitChance < 100) parts.Add($"[color=#c9bfae]{p.HitChance}%[/color]");
        if (p.Weak) parts.Add("[color=#ffa040]WEAK[/color]");
        if (p.Resisted) parts.Add("[color=#a99f8f]resist[/color]");
        if (p.Breaks) parts.Add("[color=#ffd76b][b]BREAKS![/b][/color]");
        if (p.Lethal && p.Damage > 0) parts.Add("[color=#ff6b6b]KILL[/color]");
        if (p.Heal > 0) parts.Add($"[color=#6be07a]+{Math.Min(p.Heal, target.MaxHp - target.Hp)}[/color]");
        if (p.Shield > 0) parts.Add($"[color=#8fb3ff]+{p.Shield} shield[/color]");
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    // ═════════════════════════ UI ═════════════════════════

    private void BuildTopBar(MapNode? node)
    {
        var top = new PanelContainer();
        top.SetAnchorsPreset(LayoutPreset.TopWide);
        top.AddThemeStyleboxOverride("panel", Visuals.FramedPanel(0.9f, 6));
        AddChild(top);
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 14);
        top.AddChild(bar);

        string title = node?.Type switch
        {
            NodeType.Boss => "Boss",
            NodeType.Elite => "Elite",
            _ => "Battle",
        };
        var titleLabel = Visuals.Title($"{title} · F{(node?.Floor ?? 0) + 1}", 20);
        titleLabel.TooltipText = $"{_run.NodeRegion.Name}, floor {(node?.Floor ?? 0) + 1}";
        titleLabel.MouseFilter = MouseFilterEnum.Stop;
        bar.AddChild(titleLabel);
        _roundLabel = Visuals.Label("", 15, Visuals.Text);
        bar.AddChild(_roundLabel);
        bar.AddChild(Visuals.Label("Turn order:", 13, Visuals.TextDim));
        // The turn order lives in a clipping box: however many units there are, it can never push
        // the Speed/Menu buttons off the screen.
        var turnClip = new Control { ClipContents = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(100, 38), MouseFilter = MouseFilterEnum.Ignore };
        bar.AddChild(turnClip);
        _turnBar = new HBoxContainer();
        _turnBar.AddThemeConstantOverride("separation", 4);
        turnClip.AddChild(_turnBar);

        _speedButton = Visuals.Button($"Speed {GameSession.Settings.BattleSpeed}x", () =>
        {
            GameSession.Settings.BattleSpeed = GameSession.Settings.BattleSpeed % 3 + 1;
            GameSession.SaveSettings();
            _speedButton.Text = $"Speed {GameSession.Settings.BattleSpeed}x";
            if (_busy) Engine.TimeScale = Speed;
        }, new Vector2(110, 34), 14);
        _speedButton.TooltipText = "Animation speed (1x / 2x / 3x).";
        bar.AddChild(_speedButton);
        bar.AddChild(Visuals.Button("Menu", () => Pause?.Open(), new Vector2(80, 34), 14));
    }

    private void BuildBottomPanel()
    {
        var bottom = new PanelContainer { CustomMinimumSize = new Vector2(0, 176) };
        bottom.SetAnchorsPreset(LayoutPreset.BottomWide);
        bottom.GrowVertical = GrowDirection.Begin;
        bottom.AddThemeStyleboxOverride("panel", Visuals.FramedPanel(0.94f, 8));
        AddChild(bottom);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);
        bottom.AddChild(row);

        _heroBox = new VBoxContainer { CustomMinimumSize = new Vector2(150, 0) };
        _heroBox.AddThemeConstantOverride("separation", 4);
        row.AddChild(_heroBox);

        var middle = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        middle.AddThemeConstantOverride("separation", 6);
        row.AddChild(middle);
        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 8);
        middle.AddChild(actions);
        _actionRow = new HBoxContainer();
        _actionRow.AddThemeConstantOverride("separation", 6);
        actions.AddChild(_actionRow);
        _utilityBox = new VBoxContainer();
        _utilityBox.AddThemeConstantOverride("separation", 3);
        actions.AddChild(_utilityBox);
        _description = Visuals.Rich("", 13, fit: false);
        _description.SizeFlagsVertical = SizeFlags.ExpandFill;
        middle.AddChild(_description);

        _log = Visuals.Rich("", 12, fit: false);
        _log.ScrollFollowing = true;
        _log.CustomMinimumSize = new Vector2(230, 0);
        _log.SizeFlagsVertical = SizeFlags.ExpandFill;
        _log.MouseFilter = MouseFilterEnum.Stop;
        row.AddChild(_log);
    }

    private void RefreshUi(bool hp = true)
    {
        if (!IsInsideTree()) return;
        foreach (var view in _views.Values)
        {
            view.Refresh(_battle, hp && !_busy);
            view.SetActive(view.Unit == _battle.Current && view.Unit.IsAlive);
        }
        UpdateOutlines();
        _roundLabel.Text = $"Round {_battle.Round}";
        RebuildTurnBar();
        RebuildActions();
    }

    private void RebuildTurnBar()
    {
        foreach (var child in _turnBar.GetChildren()) child.QueueFree();
        foreach (var unit in _battle.TurnOrder.Take(12))
        {
            bool current = unit == _battle.Current;
            var chip = new PanelContainer { MouseFilter = MouseFilterEnum.Stop, TooltipText = $"{unit.Name} (speed {unit.Speed})" };
            chip.AddThemeStyleboxOverride("panel", Visuals.Box(new Color("#141118"),
                current ? Visuals.GoldBright : unit.IsHero ? new Color("#4f8a5a") : new Color("#8a3a3a"), current ? 2 : 1, 4, 1));
            TextureRect image;
            if (unit.IsHero) image = Visuals.Portrait(unit.DefId, current ? 36 : 28);
            else
            {
                var tex = Visuals.Tex($"Units/{unit.Sprite}.png");
                image = new TextureRect
                {
                    Texture = new AtlasTexture { Atlas = tex, Region = HeadRegion(tex) },
                    CustomMinimumSize = new Vector2(current ? 36 : 28, current ? 36 : 28),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = TextureFilterEnum.Nearest,
                    MouseFilter = MouseFilterEnum.Ignore,
                };
            }
            chip.AddChild(image);
            _turnBar.AddChild(chip);
        }
    }

    /// <summary>Square crop around a unit's head for the turn-order chips (works for any sprite size).</summary>
    private static Rect2 HeadRegion(Texture2D tex)
    {
        float w = tex.GetWidth(), s = w * 0.42f;
        return new Rect2(w / 2f - s / 2f, w * 0.02f, s, s);
    }

    private void RebuildActions()
    {
        foreach (var child in _actionRow.GetChildren()) child.QueueFree();
        foreach (var child in _utilityBox.GetChildren()) child.QueueFree();
        foreach (var child in _heroBox.GetChildren()) child.QueueFree();
        _hotkeys.Clear();

        var hero = ActiveHero;
        if (_ended) return;
        if (_busy || hero is null)
        {
            var actor = _battle.Current;
            _description.Text = actor is { IsHero: false } ? $"[color=#ff9b9b]{actor.Name}[/color] acts..." : "";
            return;
        }

        // Hero panel
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 8);
        head.AddChild(Visuals.Portrait(hero.DefId, 56));
        var info = new VBoxContainer();
        info.AddChild(Visuals.Label(hero.Name.Split(' ')[0], 18, Visuals.GoldBright, serif: true));
        info.AddChild(Visuals.Label($"{hero.Hero?.Role} · {hero.Row} row", 12, Visuals.TextDim));
        head.AddChild(info);
        _heroBox.AddChild(head);
        if (hero.ResourceDef is { } res)
            _heroBox.AddChild(Visuals.Rich($"[color={res.ColorHex}]{res.Name} {hero.Resource}/{hero.MaxResource}[/color]", 14));
        _heroBox.AddChild(Visuals.Rich($"HP {hero.Hp}/{hero.MaxHp}", 13));

        bool targeting = _pendingAbility is not null || _pendingItem >= 0;
        if (hero.Basic is { } basic) AddAbilityButton(hero, basic, "Attack");
        foreach (var ability in hero.Abilities) AddAbilityButton(hero, ability, null);

        AddActionButton("Defend", "UI/icon_defend.png", "Take -50% damage until your next turn and gain 1 resource.",
            _battle.CanDefend(hero), "", () => Execute(() => _battle.Defend(hero)));
        _battle.CanMove(hero, out var moveReason);
        AddActionButton(hero.Row == Row.Front ? "Fall back" : "Advance", "UI/icon_move.png",
            "Switch rows (uses your turn). If the front row is full, you swap places with a front-row ally.",
            _battle.CanMove(hero, out _), moveReason, () => Execute(() => _battle.Move(hero)));
        AddActionButton($"Items ({_battle.Consumables.Count})", "UI/icon_bag.png", "Use a consumable (uses your turn).",
            _battle.Consumables.Count > 0, "Your satchel is empty.", OpenItemMenu);

        if (targeting)
        {
            string what = _pendingAbility?.DisplayName ?? (_pendingItem >= 0 && _pendingItem < _battle.Consumables.Count
                ? Database.Item(_battle.Consumables[_pendingItem]).Name : "");
            _description.Text = $"[color=#6be07a][b]Choose a target for {what}.[/b][/color]  Click a highlighted unit, or Tab + Enter.  Right-click / Esc to cancel.";
        }
        else
        {
            _description.Text = $"[color=#ffd76b]{hero.Name}'s turn.[/color] Hover an action for details. Keys [b]1-9[/b] select actions.";
        }
    }

    // ───────────── Action bar: ability cards + utility buttons ─────────────

    private VBoxContainer _utilityBox = null!;

    /// <summary>One-line summary of what an ability does, with icons (shown on its card).</summary>
    private string AbilitySummary(Combatant hero, AbilityInstance ability)
    {
        var def = ability.Def;
        bool up = ability.Upgraded;
        var parts = new List<string>();
        if (def.Power > 0 || def.PowerPerResource > 0)
        {
            int power = def.EffectivePower(up) + (def.PowerPerResource > 0 ? hero.Resource * def.PowerPerResource : 0);
            string hits = def.HitCount > 1 ? $"x{def.HitCount}" : "";
            var color = Visuals.ElementColor(def.Element).ToHtml(false);
            parts.Add($"{Visuals.Img(Visuals.ElementIcon(def.Element), 12)}[color=#{color}][b]{power}{hits}[/b][/color]"
                      + (def.Target == TargetKind.AllEnemies ? " all" : ""));
        }
        if (def.Heal > 0) parts.Add($"{Visuals.Img("UI/icon_heal_ally.png", 12)}[color=#6be07a]{def.EffectiveHeal(up)}[/color]" + (def.Target == TargetKind.AllAllies ? " all" : ""));
        if (def.Shield > 0) parts.Add($"{Visuals.Img("UI/icon_shield.png", 12)}[color=#8fb3ff]{def.EffectiveShield(up)}[/color]");
        foreach (var a in def.Applies.Take(2)) parts.Add(Visuals.Img(Visuals.StatusIcon(a.Kind), 12));
        if (def.LifestealPct > 0) parts.Add("[color=#ff6a8a]drain[/color]");
        if (def.SelfDamage > 0) parts.Add($"[color=#ff6b6b]-{def.SelfDamage}HP[/color]");
        return parts.Count == 0 ? "[color=#a99f8f]utility[/color]" : string.Join(" ", parts);
    }

    private void AddAbilityButton(Combatant hero, AbilityInstance ability, string? labelOverride)
    {
        var def = ability.Def;
        bool usable = _battle.CanUse(hero, ability, out var reason);
        string details = $"[b]{ability.DisplayName}[/b]  {ability.Def.RulesText(ability.Upgraded).Replace("\n", "\n[color=#a99f8f]")}[/color]";
        int index = _hotkeys.Count + 1;
        Action action = () => OnAbility(ability);

        Color accent = def.Heal > 0 ? new Color("#6be07a")
            : def.Power == 0 && def.PowerPerResource == 0 && def.Shield > 0 ? new Color("#8fb3ff")
            : def.Power == 0 && def.PowerPerResource == 0 ? new Color("#c070ff")
            : Visuals.ElementColor(def.Element);
        bool selected = _pendingAbility == ability;

        var card = new Godot.Button
        {
            CustomMinimumSize = new Vector2(112, 92),
            Disabled = !usable,
            ToggleMode = true,
            ButtonPressed = selected,
            FocusMode = FocusModeEnum.None,
            TooltipText = $"[{index}] " + StripBbcode(details) + (usable ? "" : "\n" + reason),
        };
        card.AddThemeStyleboxOverride("normal", Visuals.Box(new Color("#1d1726"), accent.Darkened(0.35f), 2, 8, 6));
        card.AddThemeStyleboxOverride("hover", Visuals.Box(new Color("#282033"), accent, 3, 8, 6));
        card.AddThemeStyleboxOverride("pressed", Visuals.Box(new Color("#3a2a18"), Visuals.GoldBright, 3, 8, 6));
        card.AddThemeStyleboxOverride("hover_pressed", Visuals.Box(new Color("#3a2a18"), Visuals.GoldBright, 3, 8, 6));
        card.AddThemeStyleboxOverride("disabled", Visuals.Box(new Color("#141019"), new Color("#282033"), 2, 8, 6));

        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.SetAnchorsPreset(LayoutPreset.FullRect);
        box.OffsetLeft = 6; box.OffsetRight = -6; box.OffsetTop = 4; box.OffsetBottom = -4;
        box.AddThemeConstantOverride("separation", 2);
        card.AddChild(box);

        // Top: hotkey badge + cost gems
        var top = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddChild(top);
        var key = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        key.AddThemeStyleboxOverride("panel", Visuals.Box(new Color("#0b0910"), new Color("#554a6d"), 1, 3, 1));
        key.AddChild(Visuals.Label($" {index} ", 11, Visuals.TextDim));
        top.AddChild(key);
        top.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        string resColor = hero.ResourceDef?.ColorHex ?? "#ffd76b";
        string costText = def.PowerPerResource > 0 ? $"[color={resColor}]ALL[/color]"
            : def.Cost > 0 ? $"[color={resColor}]{new string('◆', def.Cost)}[/color]"
            : "[color=#7fd4a0]FREE[/color]";
        var costLabel = Visuals.Rich($"[right]{costText}[/right]", 12);
        costLabel.CustomMinimumSize = new Vector2(60, 0);
        top.AddChild(costLabel);

        // Middle: icon + name
        var mid = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        mid.AddThemeConstantOverride("separation", 4);
        box.AddChild(mid);
        var icon = Visuals.Icon(Visuals.AbilityIcon(def), 26);
        icon.MouseFilter = MouseFilterEnum.Ignore;
        mid.AddChild(icon);
        var name = Visuals.Label(labelOverride ?? ability.DisplayName, 13, usable ? Visuals.GoldBright : Visuals.TextDim, serif: true);
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.CustomMinimumSize = new Vector2(72, 0);
        name.VerticalAlignment = VerticalAlignment.Center;
        mid.AddChild(name);

        // Bottom: what it does
        var summary = Visuals.Rich(AbilitySummary(hero, ability), 11);
        summary.CustomMinimumSize = new Vector2(100, 0);
        box.AddChild(summary);

        if (ability.CooldownLeft > 0)
        {
            var shade = new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = MouseFilterEnum.Ignore };
            shade.SetAnchorsPreset(LayoutPreset.FullRect);
            card.AddChild(shade);
            var cd = Visuals.Label($"{ability.CooldownLeft}", 34, Visuals.Text, serif: true);
            cd.SetAnchorsPreset(LayoutPreset.FullRect);
            cd.HorizontalAlignment = HorizontalAlignment.Center;
            cd.VerticalAlignment = VerticalAlignment.Center;
            cd.MouseFilter = MouseFilterEnum.Ignore;
            card.AddChild(cd);
        }
        else if (!usable) card.Modulate = new Color(0.75f, 0.72f, 0.8f);

        card.Pressed += () =>
        {
            Audio.Sfx(this, "click", 0.05f);
            action();
        };
        card.MouseEntered += () =>
        {
            card.PivotOffset = card.Size / 2;
            if (usable) card.CreateTween().TweenProperty(card, "scale", new Vector2(1.06f, 1.06f), 0.08);
            Audio.Sfx(this, "blip", 0.1f, 0.15f);
            if (_pendingAbility is null && _pendingItem < 0)
                _description.Text = details + (usable ? "" : $"\n[color=#ff6b6b]{reason}[/color]");
            if (usable)
            {
                _hoverAbility = ability;
                UpdateOutlines();
            }
        };
        card.MouseExited += () =>
        {
            card.CreateTween().TweenProperty(card, "scale", Vector2.One, 0.08);
            if (_hoverAbility is null) return;
            _hoverAbility = null;
            UpdateOutlines();
        };
        if (selected)
        {
            var pulse = card.CreateTween().SetLoops();
            pulse.TweenProperty(card, "modulate", new Color(1.25f, 1.15f, 0.9f), 0.4);
            pulse.TweenProperty(card, "modulate", Colors.White, 0.4);
        }
        _actionRow.AddChild(card);
        _hotkeys.Add((card, action));
    }

    /// <summary>Compact utility button (Defend / Move / Items) in the column next to the cards.</summary>
    private void AddActionButton(string text, string icon, string details, bool enabled, string reason, Action action,
                                 bool selected = false, AbilityInstance? ability = null)
    {
        int index = _hotkeys.Count + 1;
        var button = new Godot.Button
        {
            Text = $"{index}  {text}",
            Icon = Visuals.Tex(icon),
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(138, 29),
            Disabled = !enabled,
            FocusMode = FocusModeEnum.None,
            TextureFilter = TextureFilterEnum.Nearest,
            Alignment = HorizontalAlignment.Left,
            TooltipText = $"[{index}] " + StripBbcode(details) + (enabled ? "" : "\n" + reason),
        };
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeConstantOverride("icon_max_width", 18);
        button.Pressed += () =>
        {
            Audio.Sfx(this, "click", 0.05f);
            action();
        };
        button.MouseEntered += () =>
        {
            if (_pendingAbility is null && _pendingItem < 0)
                _description.Text = details + (enabled ? "" : $"\n[color=#ff6b6b]{reason}[/color]");
        };
        _utilityBox.AddChild(button);
        _hotkeys.Add((button, action));
    }

    private static string StripBbcode(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");

    private void OpenItemMenu()
    {
        var hero = ActiveHero;
        if (_busy || hero is null) return;
        CloseItemMenu();
        _itemMenu = new PanelContainer();
        _itemMenu.SetAnchorsPreset(LayoutPreset.CenterBottom);
        _itemMenu.GrowHorizontal = GrowDirection.Both;
        _itemMenu.GrowVertical = GrowDirection.Begin;
        _itemMenu.OffsetBottom = -186;
        AddChild(_itemMenu);
        Pause?.MoveToFront();
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 6);
        _itemMenu.AddChild(box);
        box.AddChild(Visuals.Label("Use an item", 18, Visuals.GoldBright, serif: true));
        for (int i = 0; i < _battle.Consumables.Count; i++)
        {
            int index = i;
            var item = Database.Item(_battle.Consumables[i]);
            bool ok = _battle.CanUseItem(hero, i, out var reason);
            var button = Visuals.Button($"{item.Name} — {item.Description}", () => OnItem(index), new Vector2(460, 40), 14, Visuals.ItemIcon(item));
            button.Alignment = HorizontalAlignment.Left;
            button.Disabled = !ok;
            button.TooltipText = ok ? item.Description : reason;
            box.AddChild(button);
        }
        box.AddChild(Visuals.Button("Cancel", CloseItemMenu, new Vector2(0, 36), 14));
    }

    private void CloseItemMenu()
    {
        _itemMenu?.QueueFree();
        _itemMenu = null;
    }

    // ═════════════════════════ End ═════════════════════════

    private void ShowResult()
    {
        if (_ended) return;
        _ended = true;
        _busy = true;
        RefreshUi();
        bool won = _battle.State == BattleState.Victory;
        Audio.Sfx(this, won ? "victory" : "defeat", 0f);
        if (won) PlayVictoryHops();

        Visuals.Dimmer(this, 0.45f);
        var panel = Visuals.CenterPanel(this, 420);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        panel.AddChild(box);
        var title = Visuals.Title(won ? "VICTORY" : "DEFEAT", 56);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        if (!won) title.AddThemeColorOverride("font_color", Visuals.Bad);
        box.AddChild(title);
        var summary = Visuals.Label(won
            ? $"Enemies slain: {_battle.EnemiesSlain}   ·   Damage dealt: {_battle.DamageDealt}"
            : "The Last Ember gutters as your party falls...", 15, Visuals.Text);
        summary.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(summary);
        var button = won
            ? Visuals.Button(_run.Node?.Type == NodeType.Boss ? "Claim victory" : "Claim rewards", () => GameSession.BattleWon(GetTree(), _battle), new Vector2(0, 50), 20)
            : Visuals.Button("Accept fate", () => GameSession.BattleLost(GetTree(), _battle), new Vector2(0, 50), 20);
        box.AddChild(button);
        button.CallDeferred(Control.MethodName.GrabFocus);
        Pause?.MoveToFront();
    }
}
