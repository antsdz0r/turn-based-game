using System;
using System.Collections.Generic;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// Parry, Clair Obscur style: an enemy attack is a sequence of 2-3 beats. For each beat a ring shrinks onto the
/// gold ring; press PARRY (or click) as they meet. Rhythms vary (fast, slow, and feints that pause mid-way), and a
/// press while no ring is closing, or too early, fails that beat, so mashing never works.
///   Every beat perfect  → PARRY: no damage, no afflictions, and the hero counterattacks.
///   Every beat at least good → BLOCK: -40% damage.
///   Any beat missed     → the attack lands in full.
/// </summary>
public partial class TimingPrompt : Control
{
    private const double PerfectWindow = 0.05;   // seconds either side of the meet
    private const double GoodWindow = 0.11;

    private enum Beat { Pending, Perfect, Good, Miss }

    private sealed class BeatInfo
    {
        public double Duration;     // shrink time
        public double Pause;        // feint: the ring stops half-way for this long
        public double Gap;          // wait before this beat's ring appears
        public Beat Result = Beat.Pending;
        public double Meet => Duration + Pause;
    }

    private readonly List<BeatInfo> _beats = new();
    private readonly Random _rng = new();
    private System.Threading.Tasks.TaskCompletionSource<DefenseQuality>? _tcs;
    private int _index;
    private double _t;               // time since the current beat's ring appeared (negative during its gap)
    private bool _active;
    private bool _flinched;          // pressed during the gap: this beat is already lost
    private Vector2 _center;
    private string _judgment = "";
    private Color _judgmentColor = Colors.White;
    private double _judgmentTime;

    public TimingPrompt()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>Runs a parry sequence of <paramref name="beats"/> beats and returns the overall result.</summary>
    public System.Threading.Tasks.Task<DefenseQuality> Run(Vector2 center, int beats = 2)
    {
        _center = center;
        _beats.Clear();
        for (int i = 0; i < Math.Clamp(beats, 1, 5); i++)
        {
            bool feint = i > 0 && _rng.Next(100) < 30;
            _beats.Add(new BeatInfo
            {
                Duration = 0.42 + _rng.NextDouble() * 0.38,
                Pause = feint ? 0.16 + _rng.NextDouble() * 0.14 : 0,
                Gap = i == 0 ? 0.15 : 0.08 + _rng.NextDouble() * 0.22,
            });
        }
        _index = 0;
        _t = -_beats[0].Gap;
        _flinched = false;
        _judgment = "";
        _active = true;
        Visible = true;
        _tcs = new System.Threading.Tasks.TaskCompletionSource<DefenseQuality>();
        QueueRedraw();
        return _tcs.Task;
    }

    private BeatInfo Current => _beats[_index];

    public override void _Process(double delta)
    {
        if (!_active || GetTree().Paused) return;
        double dt = delta / Math.Max(0.01, Engine.TimeScale);    // real time, whatever the battle speed
        _t += dt;
        _judgmentTime -= dt;
        if (Current.Result == Beat.Pending && _t > Current.Meet + GoodWindow) Judge(_flinched ? "FLINCHED" : "MISS", Beat.Miss);
        if (Current.Result != Beat.Pending && _t > Current.Meet + GoodWindow + 0.05) NextBeat();
        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        if (!_active || GetTree().Paused) return;
        bool press = (@event is InputEventKey { Pressed: true, Echo: false } key && Keybinds.Is(key, "parry"))
                     || @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left };
        if (!press) return;
        GetViewport().SetInputAsHandled();
        if (Current.Result != Beat.Pending) return;               // this beat is already decided: ignore
        if (_t < 0)
        {
            _flinched = true;                                      // pressed before the ring even appeared
            Judge("TOO EARLY", Beat.Miss);
            return;
        }
        double diff = Math.Abs(_t - Current.Meet);
        if (diff <= PerfectWindow) Judge("PERFECT", Beat.Perfect);
        else if (diff <= GoodWindow) Judge("GOOD", Beat.Good);
        else Judge(_t < Current.Meet ? "TOO EARLY" : "TOO LATE", Beat.Miss);
    }

    private void Judge(string text, Beat result)
    {
        Current.Result = result;
        _judgment = text;
        _judgmentTime = 0.45;
        _judgmentColor = result switch { Beat.Perfect => Visuals.GoldBright, Beat.Good => new Color("#8fb3ff"), _ => Visuals.Bad };
        Audio.Sfx(this, result == Beat.Perfect ? "parry" : result == Beat.Good ? "guard" : "miss", 0.02f, result == Beat.Perfect ? 1.0f + 0.08f * _index : 1f);
    }

    private void NextBeat()
    {
        if (_index + 1 >= _beats.Count)
        {
            Finish();
            return;
        }
        _index++;
        _t = -Current.Gap;
        _flinched = false;
    }

    private void Finish()
    {
        if (!_active) return;
        _active = false;
        Visible = false;
        bool allPerfect = _beats.TrueForAll(b => b.Result == Beat.Perfect);
        bool allGood = _beats.TrueForAll(b => b.Result is Beat.Perfect or Beat.Good);
        _tcs?.TrySetResult(allPerfect ? DefenseQuality.Perfect : allGood ? DefenseQuality.Good : DefenseQuality.None);
    }

    /// <summary>How far the ring has shrunk (0 = just appeared, 1 = meeting the gold ring). Feints hold at 0.5.</summary>
    private static double Progress(BeatInfo b, double t)
    {
        if (t <= 0) return 0;
        double half = b.Duration * 0.5;
        if (b.Pause > 0 && t > half) t = t < half + b.Pause ? half : t - b.Pause;
        return t / b.Duration;
    }

    public override void _Draw()
    {
        if (!_active) return;
        var b = Current;
        // Beat pips: which beat this is, and how the earlier ones went.
        for (int i = 0; i < _beats.Count; i++)
        {
            var pip = _center + new Vector2((i - (_beats.Count - 1) / 2f) * 18, -60);
            var c = _beats[i].Result switch
            {
                Beat.Perfect => Visuals.GoldBright,
                Beat.Good => new Color("#8fb3ff"),
                Beat.Miss => Visuals.Bad,
                _ => i == _index ? Colors.White : new Color(1, 1, 1, 0.3f),
            };
            DrawCircle(pip, i == _index ? 5 : 4, c);
        }
        DrawArc(_center, 26, 0, Mathf.Tau, 48, Visuals.GoldBright, 3, true);
        if (b.Result == Beat.Pending && _t >= 0)
        {
            float p = (float)Math.Min(1.3, Progress(b, _t));
            float radius = Mathf.Lerp(110f, 26f, Math.Min(p, 1f)) - Math.Max(0, p - 1) * 40f;
            bool holding = b.Pause > 0 && _t > b.Duration * 0.5 && _t < b.Duration * 0.5 + b.Pause;
            DrawArc(_center, Math.Max(4, radius), 0, Mathf.Tau, 48, holding ? new Color("#d9c8ff") : new Color(Visuals.Ember, 0.9f), 4, true);
        }
        string label = _judgmentTime > 0 ? _judgment : $"{Keybinds.Name("parry")}!";
        var color = _judgmentTime > 0 ? _judgmentColor : Visuals.GoldBright;
        DrawString(ThemeDB.FallbackFont, _center + new Vector2(-70, 56), label, HorizontalAlignment.Center, 140, 16, color);
    }
}
