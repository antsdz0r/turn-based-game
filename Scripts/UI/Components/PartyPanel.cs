using System;
using System.Linq;
using System.Text;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>
/// Character information, formation, equipment and inventory. Opened from the map and reward screens.
/// </summary>
public partial class PartyPanel : Control
{
    private readonly Action _onClose;
    private string _heroId = "";
    private HBoxContainer _content = null!;

    public PartyPanel() : this(() => { }) { }

    public PartyPanel(Action onClose)
    {
        _onClose = onClose;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        Visuals.CoverScreen(this);
        Visuals.Dimmer(this, 0.7f);
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.OffsetLeft = 30;
        panel.OffsetTop = 24;
        panel.OffsetRight = -30;
        panel.OffsetBottom = -24;
        AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        panel.AddChild(root);

        var header = new HBoxContainer();
        root.AddChild(header);
        var title = Visuals.Title("Party & Gear", 30);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        header.AddChild(Visuals.Button("Close", Close, new Vector2(120, 38)));

        _content = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 16);
        root.AddChild(_content);

        _heroId = GameSession.Run?.Party.FirstOrDefault()?.Id ?? "";
        Rebuild();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Close();
        }
    }

    private void Close()
    {
        _onClose();
        QueueFree();
    }

    private void Rebuild()
    {
        foreach (var child in _content.GetChildren()) child.QueueFree();
        var run = GameSession.Run;
        if (run is null) return;
        var hero = run.Party.FirstOrDefault(h => h.Id == _heroId) ?? run.Party[0];
        _heroId = hero.Id;

        _content.AddChild(BuildHeroList(run));
        _content.AddChild(BuildDetails(run, hero));
        _content.AddChild(BuildBag(run, hero));
    }

    private Control BuildHeroList(RunState run)
    {
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(210, 0) };
        column.AddThemeConstantOverride("separation", 8);
        foreach (var hero in run.Party)
        {
            var def = hero.Def;
            var button = Visuals.Button($"{def.Name}\nLv {hero.Level} · HP {hero.Hp}/{hero.MaxHp}", () =>
            {
                _heroId = hero.Id;
                Rebuild();
            }, new Vector2(210, 70), 14);
            button.Icon = Visuals.Tex($"Portraits/{hero.Id}.png");
            button.ExpandIcon = true;
            button.AddThemeConstantOverride("icon_max_width", 52);
            button.Alignment = HorizontalAlignment.Left;
            button.ToggleMode = true;
            button.ButtonPressed = hero.Id == _heroId;
            column.AddChild(button);
        }

        column.AddChild(Visuals.Separator());
        column.AddChild(Visuals.Label($"Gold: {run.Gold}", 16, Visuals.GoldBright));
        column.AddChild(Visuals.Label("Relics", 16, Visuals.Gold, serif: true));
        var relics = new HFlowContainer();
        relics.AddThemeConstantOverride("h_separation", 4);
        foreach (var id in run.Relics.Where(Database.HasRelicDef))
        {
            var relic = Database.Relic(id);
            var icon = Visuals.Icon("UI/icon_relic.png", 28, $"{relic.Name}\n{relic.Description}{(relic.Curse is { } curse ? $"\nCurse: {curse}" : "")}");
            icon.Modulate = new Color(relic.ColorHex);
            relics.AddChild(icon);
        }
        if (run.Relics.Count == 0) relics.AddChild(Visuals.Label("None yet", 13, Visuals.TextDim));
        column.AddChild(relics);
        return column;
    }

    private Control BuildDetails(RunState run, HeroState hero)
    {
        var def = hero.Def;
        var stats = hero.ComputeStats();
        var scroll = new ScrollContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(column);

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 12);
        column.AddChild(head);
        head.AddChild(Visuals.Portrait(hero.Id, 80));
        var names = new VBoxContainer();
        head.AddChild(names);
        names.AddChild(Visuals.Label(def.Name, 26, Visuals.GoldBright, serif: true));
        names.AddChild(Visuals.Label($"{def.Role}  ·  Level {hero.Level}", 15, Visuals.Text));
        string xpText = hero.Level >= HeroState.MaxLevel ? "Max level" : $"XP {hero.Xp} / {HeroState.XpForLevel(hero.Level + 1)}";
        names.AddChild(Visuals.Label(xpText, 13, Visuals.TextDim));

        var info = new StringBuilder();
        info.Append($"[color=#a99f8f]{def.Blurb}[/color]\n");
        info.Append($"HP [b]{hero.Hp}/{stats.MaxHp}[/b]   Armor [b]{stats.Armor}[/b]   Speed [b]{stats.Speed}[/b]   ");
        info.Append($"Crit [b]{stats.Crit}%[/b]   Dodge [b]{stats.Dodge}%[/b]   Damage [b]+{stats.PowerPct}%[/b]\n");
        if (stats.ElementPct.Count > 0)
            info.Append(string.Join("   ", stats.ElementPct.Select(e => $"[color=#{Visuals.ElementColor(e.Key).ToHtml(false)}]+{e.Value}% {e.Key}[/color]")) + "\n");
        info.Append($"[color={def.Resource.ColorHex}]{def.Resource.Name}[/color]: max {def.Resource.Max}, starts at {Math.Min(def.Resource.Max, def.Resource.Start + stats.StartResource)}, +{def.Resource.RegenPerTurn} per turn");
        if (def.Resource.OnBasic > 0) info.Append($", +{def.Resource.OnBasic} per basic attack");
        if (def.Resource.OnHit > 0) info.Append($", +{def.Resource.OnHit} when hit");
        if (def.Resource.OnCrit > 0) info.Append($", +{def.Resource.OnCrit} on crit");
        column.AddChild(Visuals.Rich(info.ToString(), 14));

        var rowBox = new HBoxContainer();
        rowBox.AddThemeConstantOverride("separation", 10);
        rowBox.AddChild(Visuals.Label($"Formation: {hero.Row.ToString().ToUpperInvariant()} row", 15, Visuals.Text));
        rowBox.AddChild(Visuals.Button(hero.Row == Row.Front ? "Move to back" : "Move to front", () =>
        {
            run.ToggleRow(hero.Id);
            GameSession.SaveRun();
            Rebuild();
        }, new Vector2(150, 34), 14, "UI/icon_move.png"));
        column.AddChild(rowBox);

        column.AddChild(Visuals.Separator());
        column.AddChild(Visuals.Label("Abilities", 18, Visuals.Gold, serif: true));
        foreach (var id in hero.AbilityIds.Prepend(def.BasicAbility))
        {
            var ability = Database.Ability(id);
            bool up = hero.Upgrades.Contains(id);
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(Visuals.Icon(Visuals.AbilityIcon(ability), 26));
            var text = Visuals.Rich($"[b]{ability.Name}{(up ? "+" : "")}[/b]  [color=#a99f8f]{ability.RulesText(up).Replace("\n", "  —  ")}[/color]", 13);
            text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(text);
            column.AddChild(row);
        }

        column.AddChild(Visuals.Separator());
        column.AddChild(Visuals.Label("Traits", 18, Visuals.Gold, serif: true));
        var traits = hero.Traits.Where(Database.HasTraitDef).Select(Database.Trait).ToList();
        column.AddChild(Visuals.Rich(traits.Count == 0
            ? "[color=#5e566a]None yet: every level-up lets you pick one.[/color]"
            : string.Join("\n", traits.Select(t => $"[b][color=#ffd76b]{t.Name}[/color][/b]  [color=#a99f8f]{t.Description}[/color]")), 13));

        column.AddChild(Visuals.Separator());
        column.AddChild(Visuals.Label("Equipment", 18, Visuals.Gold, serif: true));
        foreach (var slot in new[] { EquipSlot.Weapon, EquipSlot.Armor, EquipSlot.Trinket })
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            var itemId = hero.GetSlot(slot);
            row.AddChild(Visuals.Icon($"UI/icon_{slot.ToString().ToLowerInvariant()}.png", 24));
            string text = itemId is not null && Database.HasItem(itemId)
                ? $"[b]{Database.Item(itemId).Name}[/b]  [color=#a99f8f]{Database.Item(itemId).Description}[/color]"
                : $"[color=#5e566a]No {slot.ToString().ToLowerInvariant()}[/color]";
            var label = Visuals.Rich(text, 14);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(label);
            if (itemId is not null)
            {
                row.AddChild(Visuals.Button("Unequip", () =>
                {
                    run.Unequip(hero.Id, slot);
                    GameSession.SaveRun();
                    Rebuild();
                }, new Vector2(100, 32), 13));
            }
            column.AddChild(row);
        }
        return scroll;
    }

    private Control BuildBag(RunState run, HeroState hero)
    {
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(330, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(column);

        column.AddChild(Visuals.Label("Bag: equipment", 18, Visuals.Gold, serif: true));
        if (run.Equipment.Count == 0) column.AddChild(Visuals.Label("Empty", 13, Visuals.TextDim));
        foreach (var id in run.Equipment.ToList().Where(Database.HasItem))
        {
            var item = Database.Item(id);
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.AddChild(Visuals.Icon(Visuals.ItemIcon(item), 24));
            string kind = item.IsEquipment ? $"[color={Visuals.RarityHex(item)}]{item.Rarity}[/color] {item.Family ?? item.Kind.ToString()}" : item.Kind.ToString();
            var label = Visuals.Rich($"[b]{item.Name}[/b] [color=#a99f8f]({kind})[/color]\n[color=#a99f8f]{item.Description}[/color]", 13);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(label);
            row.AddChild(Visuals.Button("Equip", () =>
            {
                run.Equip(hero.Id, id);
                Audio.Sfx(this, "guard", 0.05f, 0.6f);
                GameSession.SaveRun();
                Rebuild();
            }, new Vector2(80, 32), 13));
            column.AddChild(row);
        }

        column.AddChild(Visuals.Separator());
        column.AddChild(Visuals.Label($"Satchel ({run.Consumables.Count}/{RunState.MaxConsumables})", 18, Visuals.Gold, serif: true));
        if (run.Consumables.Count == 0) column.AddChild(Visuals.Label("Empty", 13, Visuals.TextDim));
        for (int i = 0; i < run.Consumables.Count; i++)
        {
            int index = i;
            var item = Database.Item(run.Consumables[i]);
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);
            row.AddChild(Visuals.Icon(Visuals.ItemIcon(item), 24));
            var label = Visuals.Rich($"[b]{item.Name}[/b]\n[color=#a99f8f]{item.Description}[/color]", 13);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(label);
            if (item.HealAmount > 0)
            {
                var use = Visuals.Button("Use", () =>
                {
                    hero.Hp = Math.Min(hero.MaxHp, hero.Hp + item.HealAmount);
                    run.Consumables.RemoveAt(index);
                    Audio.Sfx(this, "heal", 0.02f);
                    GameSession.SaveRun();
                    Rebuild();
                }, new Vector2(70, 32), 13);
                use.TooltipText = $"Heal {hero.Def.Name} now.";
                use.Disabled = hero.Hp >= hero.MaxHp;
                row.AddChild(use);
            }
            column.AddChild(row);
        }
        return scroll;
    }
}
