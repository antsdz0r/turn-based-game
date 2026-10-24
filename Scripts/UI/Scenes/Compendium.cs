using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Reference of heroes, enemies (unlocked by meeting them), relics and items.</summary>
public partial class Compendium : Screen
{
    private VBoxContainer _list = null!;

    protected override void Build()
    {
        Audio.Music(this, "menu");
        AddBackdrop(new Color(0.45f, 0.42f, 0.55f), torches: false);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.OffsetLeft = 40;
        panel.OffsetRight = -40;
        panel.OffsetTop = 24;
        panel.OffsetBottom = -24;
        AddChild(panel);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);
        root.AddChild(header);
        var title = Visuals.Title("Compendium", 32);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        header.AddChild(Visuals.Button("Heroes", ShowHeroes, new Vector2(110, 40), 15));
        header.AddChild(Visuals.Button("Enemies", ShowEnemies, new Vector2(110, 40), 15));
        header.AddChild(Visuals.Button("Relics", ShowRelics, new Vector2(110, 40), 15));
        header.AddChild(Visuals.Button("Items", ShowItems, new Vector2(110, 40), 15));
        header.AddChild(Visuals.Button("Main Menu", () => GameSession.ChangeScene(GetTree(), GameSession.MenuScene), new Vector2(130, 40), 15));

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        root.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_list);

        ShowHeroes();
        Visuals.FadeIn(this, 0.3);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel")) GameSession.ChangeScene(GetTree(), GameSession.MenuScene);
    }

    private void Clear()
    {
        foreach (var child in _list.GetChildren()) child.QueueFree();
    }

    private void AddEntry(Control image, string bbcode)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);
        image.SizeFlagsVertical = SizeFlags.ShrinkBegin;
        row.AddChild(image);
        var text = Visuals.Rich(bbcode, 14);
        text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(text);
        _list.AddChild(row);
        _list.AddChild(Visuals.Separator());
    }

    private static TextureRect SpriteImage(string sprite, bool flip = false, string tint = "#ffffff")
    {
        var image = Visuals.Icon($"Units/{sprite}.png", 96);
        image.FlipH = flip;
        image.Modulate = new Color(tint);
        return image;
    }

    private void ShowHeroes()
    {
        Clear();
        foreach (var def in Database.Heroes)
        {
            var sb = new StringBuilder();
            int wins = GameSession.Profile.HeroWins.GetValueOrDefault(def.Id);
            sb.Append($"[font_size=20][color=#ffd76b]{def.Name}[/color][/font_size]  [color=#a99f8f]{def.Role}  ·  victories: {wins}[/color]\n");
            sb.Append($"{def.Blurb}\n");
            sb.Append($"HP {def.MaxHp} · Armor {def.Armor} · Speed {def.Speed} · Crit {def.Crit}% · Dodge {def.Dodge}% · prefers the {def.PreferredRow.ToString().ToLowerInvariant()} row\n");
            sb.Append($"[color={def.Resource.ColorHex}]{def.Resource.Name}[/color]: max {def.Resource.Max}, start {def.Resource.Start}, +{def.Resource.RegenPerTurn}/turn\n");
            foreach (var id in def.Abilities.Prepend(def.BasicAbility))
            {
                var a = Database.Ability(id);
                sb.Append($"{Visuals.Img(Visuals.AbilityIcon(a), 16)} [b]{a.Name}[/b] — {a.Description} [color=#a99f8f]({a.RulesText(false).Split('\n').Last()})[/color]\n");
            }
            sb.Append("[color=#ffd76b]Traits:[/color] [color=#a99f8f]" + string.Join(", ", Database.TraitsFor(def.Id).Select(t => t.Name)) + "[/color]");
            AddEntry(Visuals.Portrait(def.Id, 96), sb.ToString().TrimEnd());
        }
    }

    private void ShowEnemies()
    {
        Clear();
        var seen = GameSession.Profile.SeenEnemies;
        _list.AddChild(Visuals.Label($"Encountered: {seen.Count} / {Database.AllEnemies.Count()}", 15, Visuals.TextDim));
        foreach (var def in Database.AllEnemies)
        {
            if (!seen.Contains(def.Id))
            {
                var unknown = SpriteImage(def.Sprite, def.FlipSprite);
                unknown.Modulate = new Color(0, 0, 0, 0.8f);
                AddEntry(unknown, "[font_size=20][color=#5e566a]???[/color][/font_size]\n[color=#5e566a]Not yet encountered.[/color]");
                continue;
            }
            var sb = new StringBuilder();
            string tag = def.IsBoss ? "  [color=#ff8a2c]BOSS[/color]" : def.IsElite ? "  [color=#ffa040]ELITE[/color]" : "";
            sb.Append($"[font_size=20][color=#ffd76b]{def.Name}[/color][/font_size]{tag}\n");
            sb.Append($"{def.Blurb}\n");
            sb.Append($"HP {def.MaxHp} · Armor {def.Armor} · Speed {def.Speed} · Dodge {def.Dodge}% · {def.Row} row · Break {def.BreakMax}\n");
            sb.Append(AffinityLine(def.Affinities) + "\n");
            sb.Append(MovesLine(def.Moves));
            if (def.Phase2Moves is not null)
            {
                sb.Append($"\n[color=#ffa040]Phase 2 ({def.Phase2Name}):[/color] " + AffinityLine(def.Phase2Affinities ?? def.Affinities) + "\n");
                sb.Append(MovesLine(def.Phase2Moves));
            }
            AddEntry(SpriteImage(def.Sprite, def.FlipSprite, def.Tint), sb.ToString());
        }
    }

    private static string AffinityLine(System.Collections.Generic.Dictionary<Element, Affinity> affinities)
    {
        string Icons(Affinity kind) => string.Join("", affinities.Where(a => a.Value == kind).Select(a => Visuals.Img(Visuals.ElementIcon(a.Key), 16)));
        var parts = new System.Collections.Generic.List<string>();
        if (affinities.Any(a => a.Value == Affinity.Weak)) parts.Add("[color=#ffa040]Weak[/color] " + Icons(Affinity.Weak));
        if (affinities.Any(a => a.Value == Affinity.Resist)) parts.Add("Resists " + Icons(Affinity.Resist));
        if (affinities.Any(a => a.Value == Affinity.Immune)) parts.Add("Immune " + Icons(Affinity.Immune));
        return parts.Count == 0 ? "No weaknesses" : string.Join("   ", parts);
    }

    private static string MovesLine(System.Collections.Generic.List<EnemyMoveDef> moves) =>
        "Moves: " + string.Join(", ", moves.Select(m =>
            $"[b]{m.Name}[/b]{(m.Charged ? " [color=#ffa040](charged)[/color]" : "")}{(m.Melee ? " (melee)" : "")}"));

    private void ShowRelics()
    {
        Clear();
        foreach (var relic in Database.AllRelics)
        {
            var icon = Visuals.Icon("UI/icon_relic.png", 48);
            icon.Modulate = new Color(relic.ColorHex);
            AddEntry(icon, $"[font_size=18][color={relic.ColorHex}]{relic.Name}[/color][/font_size]\n{relic.Description}");
        }
    }

    private void ShowItems()
    {
        Clear();
        foreach (var item in Database.AllItems.OrderBy(i => i.Kind).ThenBy(i => i.Tier))
        {
            string tier = item.IsEquipment ? $"  ·  tier {item.Tier}" : "";
            AddEntry(Visuals.Icon(Visuals.ItemIcon(item), 48),
                     $"[font_size=18][color=#ffd76b]{item.Name}[/color][/font_size]  [color=#a99f8f]{item.Kind}{tier}  ·  {item.Price}g[/color]\n{item.Description}");
        }
    }
}
