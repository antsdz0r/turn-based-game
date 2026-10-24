using System.Collections.Generic;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// Base for every screen: initializes the session, applies the theme, provides the atmospheric
/// backdrop, flickers torches and counts run play time. Subclasses implement Build().
/// </summary>
public partial class Screen : Control
{
    protected readonly List<Sprite2D> Glows = new();
    private double _time;
    protected PauseMenu? Pause { get; private set; }

    public override void _Ready()
    {
        GameSession.Init();
        GlobalKeys.Ensure(this);
        Theme = Visuals.Theme;
        Visuals.CoverScreen(this);
        Build();
    }

    /// <summary>Builds the screen. Called once from _Ready.</summary>
    protected virtual void Build() { }

    /// <summary>
    /// Keyboard play everywhere: if nothing has focus when an arrow / Tab / Enter key is pressed, focus the first
    /// usable button (overlays first, since they were added last). From there Godot's focus navigation takes over.
    /// </summary>
    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsPressed() || GetViewport().GuiGetFocusOwner() is not null) return;
        if (!(e.IsAction("ui_up") || e.IsAction("ui_down") || e.IsAction("ui_left") || e.IsAction("ui_right")
              || e.IsAction("ui_focus_next") || e.IsAction("ui_accept"))) return;
        if (Visuals.FirstFocusable(this) is { } button)
        {
            button.GrabFocus();
            GetViewport().SetInputAsHandled();
        }
    }

    protected void AddBackdrop(Color tint, bool torches = true, bool embers = true)
    {
        Visuals.AddBackground(this, tint);
        if (torches)
        {
            Glows.Add(Visuals.AddTorch(this, 0.2f, 0.24f));
            Glows.Add(Visuals.AddTorch(this, 0.8f, 0.24f));
        }
        Visuals.AddAtmosphere(this, embers);
    }

    /// <summary>Backdrop that matches a region: its background art, ambience and torches.</summary>
    /// <summary>The background image added by AddRegionBackdrop (battles colour-grade it).</summary>
    protected TextureRect? Backdrop { get; private set; }

    protected void AddRegionBackdrop(RegionDef region, float brightness = 1f, bool torches = true)
    {
        Backdrop = Visuals.AddBackground(this, new Color(brightness, brightness, brightness), region.Background);
        if (torches && region.Key != "drowned")
        {
            Glows.Add(Visuals.AddTorch(this, 0.2f, 0.24f));
            Glows.Add(Visuals.AddTorch(this, 0.8f, 0.24f));
        }
        Visuals.AddAtmosphere(this, embers: region.Key is "crypt" or "cinder");
        Visuals.AddRegionAmbience(this, region.Key);
    }

    /// <summary>Adds the Esc pause menu (must be called last so it draws on top).</summary>
    protected void AddPauseMenu(bool inRun)
    {
        Pause = new PauseMenu(inRun);
        AddChild(Pause);
    }

    public override void _Process(double delta)
    {
        _time += delta;
        Visuals.Flicker(Glows, _time);
        if (GameSession.Run is { } run && !GetTree().Paused) run.Stats.Seconds += delta / Mathf.Max(0.01, Engine.TimeScale);
    }
}
