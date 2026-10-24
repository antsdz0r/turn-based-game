using System;
using Godot;
using TBRPG.Core;

namespace TBRPG.UI;

/// <summary>Optional parry: press SPACE (or click) when the shrinking ring meets the gold ring.</summary>
public partial class TimingPrompt : Control
{
    private const double MeetTime = 0.75;
    private const double PerfectWindow = 0.08;
    private const double GoodWindow = 0.18;

    private System.Threading.Tasks.TaskCompletionSource<DefenseQuality>? _tcs;
    private double _t;
    private bool _active;
    private Vector2 _center;

    public TimingPrompt()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    public System.Threading.Tasks.Task<DefenseQuality> Run(Vector2 center)
    {
        _center = center;
        _t = 0;
        _active = true;
        Visible = true;
        _tcs = new System.Threading.Tasks.TaskCompletionSource<DefenseQuality>();
        QueueRedraw();
        return _tcs.Task;
    }

    public override void _Process(double delta)
    {
        if (!_active || GetTree().Paused) return;
        _t += delta / Math.Max(0.01, Engine.TimeScale);
        QueueRedraw();
        if (_t > MeetTime + GoodWindow + 0.05) Finish(DefenseQuality.None);
    }

    public override void _Input(InputEvent @event)
    {
        if (!_active || GetTree().Paused) return;
        bool press = @event.IsActionPressed("ui_accept")
                     || @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left };
        if (!press) return;
        GetViewport().SetInputAsHandled();
        double diff = Math.Abs(_t - MeetTime);
        var quality = diff <= PerfectWindow ? DefenseQuality.Perfect : diff <= GoodWindow ? DefenseQuality.Good : DefenseQuality.None;
        Finish(quality);
    }

    private void Finish(DefenseQuality quality)
    {
        if (!_active) return;
        _active = false;
        Visible = false;
        _tcs?.TrySetResult(quality);
    }

    public override void _Draw()
    {
        if (!_active) return;
        float progress = (float)Math.Clamp(_t / MeetTime, 0, 1.3);
        float radius = Mathf.Lerp(110f, 26f, Math.Min(progress, 1f)) - Math.Max(0, progress - 1) * 40f;
        DrawArc(_center, 26, 0, Mathf.Tau, 48, Visuals.GoldBright, 3, true);
        DrawArc(_center, Math.Max(4, radius), 0, Mathf.Tau, 48, new Color(Visuals.Ember, 0.9f), 4, true);
        DrawString(ThemeDB.FallbackFont, _center + new Vector2(-40, 52), "SPACE!", HorizontalAlignment.Center, 80, 16, Visuals.GoldBright);
    }
}
