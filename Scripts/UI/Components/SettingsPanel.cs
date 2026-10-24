using System;
using Godot;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Audio, display and gameplay settings. Saved immediately on every change.</summary>
public partial class SettingsPanel : PanelContainer
{
    private readonly Action _onClose;
    private string? _capturing;
    private Godot.Button? _captureButton;
    private VBoxContainer _keys = null!;

    public SettingsPanel() : this(() => { }) { }

    public SettingsPanel(Action onClose)
    {
        _onClose = onClose;
        ProcessMode = ProcessModeEnum.Always;
        CustomMinimumSize = new Vector2(440, 0);
        SetAnchorsPreset(LayoutPreset.Center);
        GrowHorizontal = GrowDirection.Both;
        GrowVertical = GrowDirection.Both;
    }

    public override void _Ready()
    {
        Visuals.FitToScreen(this);
        var s = GameSession.Settings;
        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(440, Math.Min(620, GetViewportRect().Size.Y - 60)),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        AddChild(scroll);
        var box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(box);

        var title = Visuals.Title("Settings", 32);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);
        box.AddChild(Visuals.Separator());

        box.AddChild(Visuals.Label("Music volume", 15, Visuals.Text));
        var music = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = s.Music, CustomMinimumSize = new Vector2(0, 22) };
        music.ValueChanged += v => { s.Music = (float)v; Audio.ApplyVolumes(); GameSession.SaveSettings(); };
        box.AddChild(music);

        box.AddChild(Visuals.Label("Sound effects volume", 15, Visuals.Text));
        var sfx = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = s.Sfx, CustomMinimumSize = new Vector2(0, 22) };
        sfx.ValueChanged += v => { s.Sfx = (float)v; Audio.Sfx(this, "blip", 0f); GameSession.SaveSettings(); };
        box.AddChild(sfx);

        var fullscreen = new CheckButton { Text = "Fullscreen", ButtonPressed = s.Fullscreen };
        fullscreen.Toggled += on => { s.Fullscreen = on; GameSession.ApplyDisplaySettings(); GameSession.SaveSettings(); };
        box.AddChild(fullscreen);

        var timing = new CheckButton
        {
            Text = "Timed parry (2-3 beats per enemy attack)",
            ButtonPressed = s.TimingDefense,
            TooltipText = "Every beat perfect: no damage and a counterattack. Every beat good: -40% damage. Miss one: full damage.\nTurn off for purely turn-based combat.",
        };
        timing.Toggled += on => { s.TimingDefense = on; GameSession.SaveSettings(); };
        box.AddChild(timing);

        box.AddChild(Visuals.Label("Battle animation speed", 15, Visuals.Text));
        var speeds = new HBoxContainer();
        speeds.AddThemeConstantOverride("separation", 8);
        box.AddChild(speeds);
        for (int i = 1; i <= 3; i++)
        {
            int speed = i;
            var button = new Godot.Button
            {
                Text = $"{speed}x",
                ToggleMode = true,
                ButtonPressed = s.BattleSpeed == speed,
                CustomMinimumSize = new Vector2(70, 36),
                ProcessMode = ProcessModeEnum.Always,
            };
            button.Pressed += () =>
            {
                s.BattleSpeed = speed;
                GameSession.SaveSettings();
                foreach (var child in speeds.GetChildren())
                    if (child is Godot.Button b) b.ButtonPressed = b == button;
            };
            speeds.AddChild(button);
        }

        // ── Accessibility ──
        box.AddChild(Visuals.Separator());
        box.AddChild(Visuals.Label("Accessibility", 20, Visuals.Gold, serif: true));
        box.AddChild(Visuals.Label("Text size (applies to screens opened from now on)", 15, Visuals.Text));
        var sizes = new HBoxContainer();
        sizes.AddThemeConstantOverride("separation", 8);
        box.AddChild(sizes);
        foreach (var (label, scale) in new[] { ("Normal", 1f), ("Large", 1.15f), ("Largest", 1.3f) })
        {
            var button = new Godot.Button { Text = label, ToggleMode = true, ButtonPressed = Math.Abs(s.TextScale - scale) < 0.01f,
                                            CustomMinimumSize = new Vector2(100, 36), ProcessMode = ProcessModeEnum.Always };
            button.Pressed += () =>
            {
                s.TextScale = scale;
                GameSession.SaveSettings();
                foreach (var child in sizes.GetChildren()) if (child is Godot.Button b) b.ButtonPressed = b == button;
            };
            sizes.AddChild(button);
        }
        var motion = new CheckButton
        {
            Text = "Reduce motion and flashes", ButtonPressed = s.ReduceMotion,
            TooltipText = "No screen shake, camera punches, hit-stops or full-screen lightning flashes.",
        };
        motion.Toggled += on => { s.ReduceMotion = on; GameSession.SaveSettings(); };
        box.AddChild(motion);
        var colorblind = new CheckButton
        {
            Text = "Colour-blind friendly colours", ButtonPressed = s.ColorblindMode,
            TooltipText = "Element colours and health bars use a palette that stays distinct for every kind of colour vision\n(heroes blue, enemies orange). Applies to the next battle.",
        };
        colorblind.Toggled += on => { s.ColorblindMode = on; GameSession.SaveSettings(); };
        box.AddChild(colorblind);

        // ── Controls ──
        box.AddChild(Visuals.Separator());
        box.AddChild(Visuals.Label("Controls (click, then press a key)", 20, Visuals.Gold, serif: true));
        _keys = new VBoxContainer();
        _keys.AddThemeConstantOverride("separation", 4);
        box.AddChild(_keys);
        FillKeys();
        box.AddChild(Visuals.Button("Reset controls", () => { Keybinds.Reset(); FillKeys(); }, new Vector2(0, 36), 14));

        box.AddChild(Visuals.Spacer(6));
        var back = Visuals.Button("Back", _onClose, new Vector2(0, 42));
        box.AddChild(back);
    }

    private void FillKeys()
    {
        foreach (var child in _keys.GetChildren()) child.QueueFree();
        foreach (var (id, label, _) in Keybinds.Actions)
        {
            var row = new HBoxContainer();
            var name = Visuals.Label(label, 14, Visuals.Text);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(name);
            var key = new Godot.Button { Text = Keybinds.Name(id), CustomMinimumSize = new Vector2(130, 30), ProcessMode = ProcessModeEnum.Always };
            string action = id;
            key.Pressed += () =>
            {
                if (_captureButton is not null) _captureButton.Text = Keybinds.Name(_capturing!);
                _capturing = action;
                _captureButton = key;
                key.Text = "Press a key...";
            };
            row.AddChild(key);
            _keys.AddChild(row);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_capturing is null || @event is not InputEventKey { Pressed: true, Echo: false } key) return;
        GetViewport().SetInputAsHandled();
        if (key.Keycode != Key.Escape) Keybinds.Bind(_capturing, key.Keycode);
        _capturing = null;
        _captureButton = null;
        FillKeys();
    }
}
