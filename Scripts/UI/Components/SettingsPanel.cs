using System;
using Godot;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Audio, display and gameplay settings. Saved immediately on every change.</summary>
public partial class SettingsPanel : PanelContainer
{
    private readonly Action _onClose;

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
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 10);
        AddChild(box);

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
            Text = "Timed parry (press SPACE as enemy attacks land)",
            ButtonPressed = s.TimingDefense,
            TooltipText = "Perfect timing: -75% damage. Good timing: -40%. Turn off for purely turn-based combat.",
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

        box.AddChild(Visuals.Spacer(6));
        var back = Visuals.Button("Back", _onClose, new Vector2(0, 42));
        box.AddChild(back);
    }
}
