using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// The current region's route map. Rooms are joined by dotted trails; the party token stands where you are.
/// Click a room to select it (its trail lights up and the info panel shows what lies there), click again or
/// press "Travel" to walk there. Sealed gates cost gold and lead to golden rooms.
/// </summary>
public partial class MapView : Control
{
    private readonly RunState _run;
    private readonly System.Action<int> _onEnter;
    private readonly System.Action<MapNode?> _onSelect;
    private readonly Dictionary<int, Godot.Button> _buttons = new();
    private readonly Dictionary<int, Vector2> _jitter = new();
    private HashSet<int> _available = new();
    private HashSet<int> _downstream = new();
    private int _region;
    private List<MapNode> _nodes = new();
    private Control _token = null!;
    private int _selected = -1;
    private bool _travelling;
    private float _time;

    public MapView() : this(new RunState(), _ => { }, _ => { }) { }

    public MapView(RunState run, System.Action<int> onEnter, System.Action<MapNode?> onSelect)
    {
        _run = run;
        _onEnter = onEnter;
        _onSelect = onSelect;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public MapNode? Selected => _selected >= 0 ? _run.Map.Node(_selected) : null;
    public bool SelectedReachable => _selected >= 0 && _available.Contains(_selected);
    /// <summary>True when the party stands at this region's entrance (not on a room of it yet).</summary>
    private bool AtEntrance => _run.CurrentNode < 0 || _run.Map.Node(_run.CurrentNode).Region != _region;

    public override void _Ready()
    {
        _available = _run.AvailableNodes().Select(n => n.Id).ToHashSet();
        _region = _run.CurrentRegion;
        _nodes = _run.Map.InRegion(_region).ToList();
        var rng = new System.Random(_run.Seed + _region * 31);
        foreach (var node in _nodes)
            _jitter[node.Id] = new Vector2(rng.Next(-26, 27), rng.Next(-22, 23));

        foreach (var node in _nodes) AddChild(BuildButton(node));
        BuildToken();

        Resized += Layout;
        Layout();
        var first = _available.FirstOrDefault(id => _buttons.ContainsKey(id), -1);
        if (first >= 0)
        {
            _buttons[first].CallDeferred(Control.MethodName.GrabFocus);
            SelectNode(first);
        }
    }

    private Godot.Button BuildButton(MapNode node)
    {
        bool available = _available.Contains(node.Id);
        bool current = node.Id == _run.CurrentNode;
        bool boss = node.Type == NodeType.Boss;
        int size = boss ? 84 : 58;

        var button = new Godot.Button
        {
            Icon = Visuals.Tex(Visuals.NodeIcon(node.Type)),
            ExpandIcon = true,
            CustomMinimumSize = new Vector2(size, size),
            Size = new Vector2(size, size),
            TextureFilter = TextureFilterEnum.Nearest,
            TooltipText = MapScene.NodeDescription(node.Type),
            FocusMode = FocusModeEnum.All,
        };
        button.AddThemeConstantOverride("icon_max_width", size - 20);
        button.IconAlignment = HorizontalAlignment.Center;

        Color border = node.Golden ? Visuals.GoldBright
            : current ? Visuals.GoldBright
            : available ? Visuals.Ember
            : node.Visited ? new Color("#6a5a3a")
            : new Color("#3a3244");
        Color fill = node.Golden ? new Color("#3a2c10") : node.Visited ? new Color("#2a2218") : available ? new Color("#2b2230") : new Color("#16121b");
        var style = Visuals.Box(fill, border, available || node.Golden ? 3 : 2, size / 2, 6);
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("disabled", style);
        var hover = Visuals.Box(new Color("#3a2a20"), Visuals.GoldBright, 3, size / 2, 6);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeStyleboxOverride("focus", Visuals.Box(new Color(0, 0, 0, 0), Visuals.GoldBright, 2, size / 2, 6));
        bool lit = available || current || node.Visited;
        button.Modulate = lit ? Colors.White : new Color(0.6f, 0.58f, 0.66f);
        if (node.Visited && !current) button.Modulate = new Color(0.8f, 0.75f, 0.7f);

        int id = node.Id;
        button.Pressed += () =>
        {
            if (_travelling) return;
            if (_selected == id && _available.Contains(id)) TravelSelected();
            else SelectNode(id);
        };
        button.FocusEntered += () => { if (!_travelling && _selected != id) SelectNode(id); };
        button.MouseEntered += () => { if (!_travelling) Audio.Sfx(this, "blip", 0.1f, 0.15f); };
        _buttons[node.Id] = button;

        if (available)
        {
            var pulse = button.CreateTween().SetLoops();
            pulse.TweenProperty(button, "scale", new Vector2(1.1f, 1.1f), 0.6).SetTrans(Tween.TransitionType.Sine);
            pulse.TweenProperty(button, "scale", Vector2.One, 0.6).SetTrans(Tween.TransitionType.Sine);
        }
        if (node.Golden)
        {
            var sparkle = new CpuParticles2D
            {
                Amount = 8,
                Lifetime = 1.2,
                Position = new Vector2(size / 2f, size / 2f),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.SphereSurface,
                EmissionSphereRadius = size / 2f + 4,
                Gravity = new Vector2(0, -20),
                ScaleAmountMin = 2,
                ScaleAmountMax = 4,
                Color = Visuals.GoldBright,
            };
            button.AddChild(sparkle);
        }
        return button;
    }

    private void BuildToken()
    {
        var hero = _run.Party.FirstOrDefault()?.Id ?? "vanguard";
        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", Visuals.Box(new Color("#1a1420"), Visuals.GoldBright, 3, 28, 2));
        var portrait = Visuals.Portrait(hero, 44);
        portrait.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(portrait);
        // No ZIndex: the token is the map's last child, so it sits above the rooms but below any panel opened on top.
        var anchor = new Control { MouseFilter = MouseFilterEnum.Ignore };
        anchor.AddChild(panel);
        panel.Position = new Vector2(-26, -26);
        AddChild(anchor);
        _token = anchor;
        var bob = panel.CreateTween().SetLoops();
        bob.TweenProperty(panel, "position", new Vector2(-26, -34), 0.7).SetTrans(Tween.TransitionType.Sine);
        bob.TweenProperty(panel, "position", new Vector2(-26, -26), 0.7).SetTrans(Tween.TransitionType.Sine);
    }

    // ═════════════════════ Geometry ═════════════════════

    private Vector2 Entrance => new(Size.X * 0.03f, Size.Y * 0.5f);

    private Vector2 Center(MapNode node)
    {
        float x = Size.X * (0.1f + 0.9f * (node.LocalFloor + 0.5f) / DungeonMap.FloorsPerRegion);
        int count = _run.Map.CountOnFloor(node.Floor);
        float y = Size.Y * (node.Index + 0.5f) / count;
        var j = node.Type == NodeType.Boss ? Vector2.Zero : _jitter.GetValueOrDefault(node.Id);
        return new Vector2(x, y) + j;
    }

    private Vector2 TokenSpot => AtEntrance ? Entrance : Center(_run.Map.Node(_run.CurrentNode)) + new Vector2(0, -52);

    private Vector2 From(int nodeId) => nodeId < 0 ? Entrance : Center(_run.Map.Node(nodeId));

    /// <summary>A point along a gently curved trail between two places.</summary>
    private static Vector2 Trail(Vector2 a, Vector2 b, float bend, float k)
    {
        var mid = (a + b) / 2;
        var perp = (b - a).Orthogonal().Normalized();
        var control = mid + perp * bend;
        return a.Lerp(control, k).Lerp(control.Lerp(b, k), k);
    }

    private static float Bend(int a, int b) => (((a * 7 + b * 3) % 5 + 5) % 5 - 2) * 9f;

    private void Layout()
    {
        foreach (var node in _nodes)
        {
            var button = _buttons[node.Id];
            button.Size = button.CustomMinimumSize;
            button.PivotOffset = button.Size / 2;
            button.Position = Center(node) - button.Size / 2;
        }
        if (!_travelling) _token.Position = TokenSpot;
        QueueRedraw();
    }

    // ═════════════════════ Selection & travel ═════════════════════

    public void SelectNode(int id)
    {
        _selected = id;
        _downstream = new HashSet<int>();
        var stack = new Stack<int>();
        stack.Push(id);
        while (stack.Count > 0)
        {
            var n = _run.Map.Node(stack.Pop());
            foreach (var next in n.Next.Where(x => _run.Map.Node(x).Region == _region))
                if (_downstream.Add(next)) stack.Push(next);
        }
        _onSelect(Selected);
        QueueRedraw();
    }

    public void TravelSelected()
    {
        if (_travelling || _selected < 0 || !_available.Contains(_selected)) return;
        if (!_run.CanEnter(_selected))
        {
            Audio.Sfx(this, "miss", 0f, 0.6f);
            return;
        }
        _travelling = true;
        int target = _selected;
        if (_run.TollFor(target) > 0)
        {
            Audio.Sfx(this, "coin", 0f);
            Audio.Sfx(this, "lock", 0.05f, 0.7f);
        }

        Vector2 start = _token.Position;
        Vector2 end = Center(_run.Map.Node(target)) + new Vector2(0, -52);
        int fromId = AtEntrance ? -1 : _run.CurrentNode;
        float bend = Bend(fromId, target);
        double duration = Mathf.Clamp(start.DistanceTo(end) / 320f, 0.45f, 1.1f);
        int steps = 0;
        var walk = _token.CreateTween();
        walk.TweenMethod(Callable.From<float>(k =>
        {
            _token.Position = Trail(start, end, bend, k);
            int step = (int)(k * duration / 0.16);
            if (step != steps)
            {
                steps = step;
                Audio.Sfx(this, "blip", 0.2f, 0.12f);
            }
        }), 0f, 1f, duration).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        walk.TweenProperty(_buttons[target], "scale", new Vector2(1.3f, 1.3f), 0.12);
        walk.TweenCallback(Callable.From(() => _onEnter(target)));
    }

    public override void _Process(double delta)
    {
        _time += (float)delta;
        QueueRedraw();
    }

    // ═════════════════════ Drawing ═════════════════════

    public override void _Draw()
    {
        var dim = new Color(0.55f, 0.5f, 0.65f, 0.35f);
        var walked = new Color(Visuals.Gold, 0.9f);
        var open = new Color(Visuals.Ember, 0.95f);

        // Entrance: a small campfire and trails to the first rooms.
        DrawCircle(Entrance, 10, new Color(Visuals.Ember, 0.5f + 0.2f * Mathf.Sin(_time * 4)));
        DrawCircle(Entrance, 5, Visuals.GoldBright);
        foreach (var first in _nodes.Where(n => n.LocalFloor == 0))
        {
            bool isOpen = AtEntrance && _available.Contains(first.Id);
            DrawTrail(Entrance, Center(first), Bend(-1, first.Id), first.Visited ? walked : isOpen ? open : dim, isOpen || first.Visited);
        }

        foreach (var node in _nodes)
        {
            foreach (var nextId in node.Next)
            {
                var next = _run.Map.Node(nextId);
                if (next.Region != _region) continue;
                bool isOpen = node.Id == _run.CurrentNode && _available.Contains(nextId);
                bool wasWalked = node.Visited && next.Visited;
                bool ahead = _selected >= 0 && _downstream.Contains(nextId) && (node.Id == _selected || _downstream.Contains(node.Id));
                var color = wasWalked ? walked : isOpen ? open : ahead ? new Color(0.85f, 0.8f, 0.95f, 0.6f) : dim;
                float bend = Bend(node.Id, nextId);
                DrawTrail(Center(node), Center(next), bend, color, isOpen || wasWalked || ahead);
                if (node.Tolls.Contains(nextId)) DrawGate(Trail(Center(node), Center(next), bend, 0.5f), next.Region);
            }
        }

        // The selected route, animated from where the party stands to the selected room.
        if (SelectedReachable && !_travelling)
        {
            int fromId = AtEntrance ? -1 : _run.CurrentNode;
            Vector2 a = From(fromId), b = Center(_run.Map.Node(_selected));
            float bend = Bend(fromId, _selected);
            int dots = Mathf.Max(4, (int)(a.DistanceTo(b) / 12));
            for (int i = 0; i <= dots; i++)
            {
                float k = (i + _time * 3 % 1) / dots;
                if (k > 1) continue;
                DrawCircle(Trail(a, b, bend, k), 4.5f, new Color(Visuals.GoldBright, 0.95f));
            }
        }

        // Golden halos
        foreach (var node in _nodes.Where(n => n.Golden))
        {
            float r = 36 + 3 * Mathf.Sin(_time * 3 + node.Id);
            DrawArc(Center(node), r, 0, Mathf.Tau, 40, new Color(Visuals.GoldBright, 0.55f), 2f, true);
        }

        // Floor numerals
        for (int f = 0; f < DungeonMap.FloorsPerRegion; f++)
        {
            int global = _region * DungeonMap.FloorsPerRegion + f;
            float x = Size.X * (0.1f + 0.9f * (f + 0.5f) / DungeonMap.FloorsPerRegion);
            DrawString(ThemeDB.FallbackFont, new Vector2(x - 16, -8), DungeonMap.IsBossFloor(global) ? "BOSS" : ToRoman(global + 1),
                       HorizontalAlignment.Left, -1, 14, new Color(Visuals.Gold, 0.7f));
        }
    }

    private void DrawTrail(Vector2 a, Vector2 b, float bend, Color color, bool strong)
    {
        int dots = Mathf.Max(3, (int)(a.DistanceTo(b) / (strong ? 13 : 16)));
        for (int i = 1; i < dots; i++)
            DrawCircle(Trail(a, b, bend, (float)i / dots), strong ? 3.2f : 2.2f, color);
    }

    private void DrawGate(Vector2 at, int region)
    {
        int price = DungeonMap.TollPrice(region);
        bool afford = _run.Gold >= price;
        DrawCircle(at, 15, new Color("#1a1420"));
        DrawArc(at, 15, 0, Mathf.Tau, 24, afford ? Visuals.GoldBright : Visuals.Bad, 2f, true);
        DrawTextureRect(Visuals.Tex("UI/icon_coin.png"), new Rect2(at - new Vector2(10, 10), new Vector2(20, 20)), false);
        DrawString(ThemeDB.FallbackFont, at + new Vector2(-14, 32), $"{price}g", HorizontalAlignment.Left, -1, 14,
                   afford ? Visuals.GoldBright : Visuals.Bad);
    }

    private static string ToRoman(int n) => n switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X",
        11 => "XI", 12 => "XII", 13 => "XIII", 14 => "XIV", 15 => "XV", 16 => "XVI", 17 => "XVII", 18 => "XVIII",
        19 => "XIX", 20 => "XX", _ => n.ToString(),
    };
}
