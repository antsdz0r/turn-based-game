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
    protected void AddRegionBackdrop(RegionDef region, float brightness = 1f, bool torches = true)
    {
        Visuals.AddBackground(this, new Color(brightness, brightness, brightness), region.Background);
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
