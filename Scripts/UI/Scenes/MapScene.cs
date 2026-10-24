using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>The branching dungeon map. Only nodes linked from the current one can be entered.</summary>
public partial class MapScene : Screen
{
    protected override void Build()
    {
        var run = GameSession.Run;
        if (run is null)
        {
            GameSession.ChangeScene(GetTree(), GameSession.MenuScene);
            return;
        }
        var region = run.CurrentRegionDef;
        string intro = Story.RegionIntroId(run, run.CurrentRegion);
        if (!run.SeenScenes.Contains(intro))
        {
            GameSession.PlayCutscene(GetTree(), intro, GameSession.AfterCutscene.Map);
            return;
        }
        Audio.Music(this, "menu");
        AddRegionBackdrop(region, 0.45f, torches: false);

        // Top bar
        var top = new PanelContainer();
        top.SetAnchorsPreset(LayoutPreset.TopWide);
        top.AddThemeStyleboxOverride("panel", Visuals.FramedPanel(0.9f, 8));
        AddChild(top);
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 16);
        top.AddChild(bar);
        bar.AddChild(Visuals.Title(region.Name, 24));
        int floor = run.CurrentNode < 0 ? 0 : run.Floor + (run.NodeCompleted ? 1 : 0);
        var floorLabel = Visuals.Label($"Region {run.CurrentRegion + 1}/{DungeonMap.RegionCount} · Floor {System.Math.Min(floor + 1, DungeonMap.Floors)}/{DungeonMap.Floors}", 15, Visuals.Text);
        floorLabel.TooltipText = region.Subtitle;
        floorLabel.MouseFilter = MouseFilterEnum.Stop;
        bar.AddChild(floorLabel);
        if (run.Daily is not null && Daily.Modifiers.FirstOrDefault(m => m.Id == run.Daily) is { } dailyMod)
        {
            var dl = Visuals.Label($"Daily: {dailyMod.Name}", 15, Visuals.Ember);
            dl.TooltipText = dailyMod.Description;
            dl.MouseFilter = MouseFilterEnum.Stop;
            bar.AddChild(dl);
        }
        if (run.Weekly is not null)
        {
            var wl = Visuals.Label($"Weekly: {string.Join(" · ", run.Mutators.Select(m => Weekly.Get(m)?.Name ?? m))}", 14, Visuals.Ember);
            wl.TooltipText = string.Join("\n", run.Mutators.Select(m => Weekly.Get(m) is { } d ? $"{d.Name}: {d.Description}" : m));
            wl.MouseFilter = MouseFilterEnum.Stop;
            bar.AddChild(wl);
        }
        if (run.Loop > 0)
        {
            var el = Visuals.Label($"Endless loop {run.Loop + 1}", 15, Visuals.Bad);
            el.TooltipText = $"Enemies: +{(int)(Endless.HpPerLoop * 100 * run.Loop)}% HP, +{(int)(Endless.DamagePerLoop * 100 * run.Loop)}% damage. Depth {Endless.Depth(run)}.";
            el.MouseFilter = MouseFilterEnum.Stop;
            bar.AddChild(el);
        }
        if (run.Heat > 0)
        {
            var heat = Visuals.Label($"Heat {run.Heat}", 16, Visuals.Ember);
            heat.TooltipText = Heat.Describe(run.Heat);
            heat.MouseFilter = MouseFilterEnum.Stop;
            bar.AddChild(heat);
        }
        bar.AddChild(Visuals.Icon("UI/icon_coin.png", 22));
        bar.AddChild(Visuals.Label($"{run.Gold}", 18, Visuals.GoldBright));
        bar.AddChild(Visuals.Icon("UI/icon_bag.png", 22, "Consumables"));
        bar.AddChild(Visuals.Label($"{run.Consumables.Count}/{RunState.MaxConsumables}", 16, Visuals.Text));

        var relics = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        relics.AddThemeConstantOverride("separation", 2);
        foreach (var id in run.Relics.Where(Database.HasRelicDef))
        {
            var relic = Database.Relic(id);
            var icon = Visuals.Icon("UI/icon_relic.png", 24, $"{relic.Name}\n{relic.Description}{(relic.Curse is { } curse ? $"\nCurse: {curse}" : "")}");
            icon.Modulate = new Color(relic.ColorHex);
            relics.AddChild(icon);
        }
        bar.AddChild(relics);

        // Party summary
        foreach (var hero in run.Party)
        {
            var chip = new HBoxContainer();
            chip.AddThemeConstantOverride("separation", 4);
            chip.TooltipText = $"{hero.Def.Name} · Lv {hero.Level} · {hero.Row} row";
            chip.MouseFilter = MouseFilterEnum.Stop;
            chip.AddChild(Visuals.Portrait(hero.Id, 32));
            float pct = (float)hero.Hp / System.Math.Max(1, hero.MaxHp);
            var hp = Visuals.Label($"{hero.Hp}/{hero.MaxHp}", 13, pct < 0.35f ? Visuals.Bad : Visuals.Good);
            chip.AddChild(hp);
            bar.AddChild(chip);
        }
        bar.AddChild(Visuals.Button("Party & Gear", OpenParty, new Vector2(150, 38), 15, "UI/icon_bag.png"));
        bar.AddChild(Visuals.Button("Menu", () => Pause?.Open(), new Vector2(90, 38), 15));

        BuildInfoPanel();
        _map = new MapView(run, nodeId => GameSession.EnterNode(GetTree(), nodeId), ShowInfo);
        _map.SetAnchorsPreset(LayoutPreset.FullRect);
        _map.OffsetLeft = 40;
        _map.OffsetRight = -40;
        _map.OffsetTop = 104;
        _map.OffsetBottom = -168;
        AddChild(_map);
        _info.MoveToFront();

        // Legend
        var legend = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        legend.SetAnchorsPreset(LayoutPreset.BottomWide);
        legend.GrowVertical = GrowDirection.Begin;
        legend.OffsetBottom = -8;
        legend.AddThemeConstantOverride("separation", 18);
        AddChild(legend);
        foreach (var type in new[] { NodeType.Battle, NodeType.Elite, NodeType.Event, NodeType.Treasure, NodeType.Shop, NodeType.Rest, NodeType.Boss })
        {
            var entry = new HBoxContainer();
            entry.AddThemeConstantOverride("separation", 4);
            entry.AddChild(Visuals.Icon(Visuals.NodeIcon(type), 20));
            entry.AddChild(Visuals.Label(type.ToString(), 13, Visuals.TextDim));
            legend.AddChild(entry);
        }

        var hint = Visuals.Label("Pick a glowing room to plan your route, then Travel. Gold gates lead to golden rooms.", 15, Visuals.GoldBright);
        hint.SetAnchorsPreset(LayoutPreset.TopWide);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.OffsetTop = 64;
        AddChild(hint);

        bool enteringRegion = run.CurrentNode < 0 || (run.NodeCompleted && run.Node?.Type == NodeType.Boss);
        if (enteringRegion) ShowRegionBanner(region);

        AddPauseMenu(inRun: true);
        Visuals.FadeIn(this, 0.4);
        if (TraitPicker.ShowIfPending(this, () => { })) Pause?.MoveToFront();
    }

    /// <summary>A big title card that fades in and out when the party enters a new region.</summary>
    private void ShowRegionBanner(RegionDef region)
    {
        var banner = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        banner.SetAnchorsPreset(LayoutPreset.FullRect);
        var strip = new ColorRect { Color = new Color(0, 0, 0, 0.7f), CustomMinimumSize = new Vector2(0, 150), MouseFilter = MouseFilterEnum.Ignore };
        var inner = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        inner.SetAnchorsPreset(LayoutPreset.FullRect);
        strip.AddChild(inner);
        var small = Visuals.Label($"REGION {(GameSession.Run?.CurrentRegion ?? region.Index) + 1}", 16, Visuals.Ember);
        small.HorizontalAlignment = HorizontalAlignment.Center;
        var title = Visuals.Title(region.Name, 46);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        var sub = Visuals.Label(region.Subtitle, 17, Visuals.TextDim);
        sub.HorizontalAlignment = HorizontalAlignment.Center;
        inner.AddChild(small);
        inner.AddChild(title);
        inner.AddChild(sub);
        banner.AddChild(strip);
        banner.Modulate = new Color(1, 1, 1, 0);
        AddChild(banner);
        Audio.Sfx(this, "charge", 0f, 0.7f);
        var tween = banner.CreateTween();
        tween.TweenInterval(0.3);
        tween.TweenProperty(banner, "modulate:a", 1f, 0.6);
        tween.TweenInterval(2.2);
        tween.TweenProperty(banner, "modulate:a", 0f, 0.8);
        tween.TweenCallback(Callable.From(banner.QueueFree));
    }

    private MapView _map = null!;
    private PanelContainer _info = null!;
    private TextureRect _infoIcon = null!;
    private Godot.Label _infoTitle = null!;
    private RichTextLabel _infoText = null!;
    private Godot.Button _travel = null!;

    private void BuildInfoPanel()
    {
        _info = new PanelContainer { CustomMinimumSize = new Vector2(760, 112) };
        _info.SetAnchorsPreset(LayoutPreset.CenterBottom);
        _info.GrowHorizontal = GrowDirection.Both;
        _info.GrowVertical = GrowDirection.Begin;
        _info.OffsetBottom = -40;
        _info.AddThemeStyleboxOverride("panel", Visuals.FramedPanel(0.92f, 8));
        AddChild(_info);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);
        _info.AddChild(row);
        _infoIcon = Visuals.Icon("UI/icon_coin.png", 56);
        _infoIcon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(_infoIcon);
        var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(col);
        _infoTitle = Visuals.Title("", 22);
        col.AddChild(_infoTitle);
        _infoText = Visuals.Rich("", 14);
        _infoText.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        col.AddChild(_infoText);
        _travel = Visuals.Button("Travel", () => _map.TravelSelected(), new Vector2(170, 56), 18);
        _travel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(_travel);
        _info.Visible = false;
    }

    private void ShowInfo(MapNode? node)
    {
        var run = GameSession.Run;
        if (node is null || run is null) { _info.Visible = false; return; }
        _info.Visible = true;
        _infoIcon.Texture = Visuals.Tex(Visuals.NodeIcon(node.Type));
        var parts = NodeDescription(node.Type).Split('\n');
        _infoTitle.Text = (node.Golden ? "Golden " : "") + parts[0];
        _infoTitle.AddThemeColorOverride("font_color", node.Golden ? Visuals.GoldBright : Visuals.Text);
        var text = new System.Text.StringBuilder(parts.Length > 1 ? parts[1] : "");
        if (node.Golden) text.Append("\n[color=#ffd76b]" + GoldenText(node.Type) + "[/color]");
        var next = node.Next.Select(run.Map.Node).Where(n => n.Region == node.Region).Select(n => n.Type.ToString()).Distinct().ToList();
        if (next.Count > 0) text.Append($"\n[color=#a99f8f]Leads to: {string.Join(", ", next)}[/color]");

        bool reachable = run.IsReachable(node.Id);
        int toll = reachable ? run.TollFor(node.Id) : 0;
        if (toll > 0)
            text.Append($"\n{Visuals.Img("UI/icon_coin.png", 14)} [color={(run.Gold >= toll ? "#ffd76b" : "#ff6b6b")}]Sealed gate: pay {toll} gold to pass (you have {run.Gold}).[/color]");
        _infoText.Text = text.ToString();

        _travel.Disabled = !reachable || !run.CanEnter(node.Id);
        _travel.Text = !reachable ? (node.Visited ? "Visited" : "Out of reach") : toll > 0 ? $"Pay {toll}g & go" : "Travel";
        _travel.TooltipText = reachable && toll > run.Gold ? "Not enough gold." : "";
    }

    private static string GoldenText(NodeType type) => type switch
    {
        NodeType.Battle or NodeType.Elite => "Golden: +50% gold and a fourth reward to choose from.",
        NodeType.Treasure => "Golden vault: a guaranteed relic and double gold.",
        NodeType.Rest => "Golden camp: rest heals 20% more.",
        NodeType.Shop => "Golden shop: everything 25% off.",
        NodeType.Event => "Golden: 30 gold waits at the door.",
        _ => "",
    };

    private void OpenParty()
    {
        AddChild(new PartyPanel(() => GameSession.ChangeScene(GetTree(), GameSession.MapScene)));
        Pause?.MoveToFront();
    }

    public static string NodeDescription(NodeType type) => type switch
    {
        NodeType.Battle => "Battle\nThe restless dead bar the way.",
        NodeType.Elite => "Elite battle\nA deadly foe. Guaranteed relic among the rewards.",
        NodeType.Treasure => "Treasure\nA sealed chest: gold and gear or a relic.",
        NodeType.Shop => "Shop\nA wandering merchant. Gear, potions, relics, healing and training.",
        NodeType.Rest => "Rest site\nHeal 35% or train (upgrade an ability).",
        NodeType.Event => "Unknown\nSomething waits in the dark. Choices and consequences.",
        _ => "Boss\nThe guardian of this region. Defeat it to descend further.",
    };
}
