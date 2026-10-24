using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Choose 3 of the 5 heroes and set their rows (at most 2 in front).</summary>
public partial class HeroSelect : Screen
{
    private readonly List<string> _picked = new();
    private readonly Dictionary<string, Row> _rows = new();
    private readonly Dictionary<string, PanelContainer> _cards = new();
    private VBoxContainer _formation = null!;
    private Godot.Button _begin = null!;
    private int _heat;
    private Godot.Label _heatLabel = null!;

    protected override void Build()
    {
        Audio.Music(this, "menu");
        AddBackdrop(new Color(0.45f, 0.42f, 0.55f), torches: false);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 24;
        root.OffsetRight = -24;
        root.OffsetTop = 16;
        root.OffsetBottom = -16;
        root.AddThemeConstantOverride("separation", 10);
        AddChild(root);

        var title = Visuals.Title("Choose Your Party", 38);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        root.AddChild(title);
        var hint = Visuals.Label("Pick three heroes. Hover abilities for details. Front-row heroes take melee hits; the back row is safe from melee while the front stands.", 14, Visuals.TextDim);
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        root.AddChild(hint);

        var cards = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsVertical = SizeFlags.ExpandFill };
        cards.AddThemeConstantOverride("separation", 6);
        root.AddChild(cards);
        foreach (var def in Database.Heroes) cards.AddChild(BuildCard(def));

        var bottom = new HBoxContainer();
        bottom.AddThemeConstantOverride("separation", 16);
        root.AddChild(bottom);
        bottom.AddChild(Visuals.Button("Back", () => GameSession.ChangeScene(GetTree(), GameSession.MenuScene), new Vector2(140, 50)));
        _formation = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        bottom.AddChild(_formation);
        bottom.AddChild(BuildHeatPicker());
        _begin = Visuals.Button("Begin the Descent", Begin, new Vector2(260, 56), 20);
        bottom.AddChild(_begin);

        // Default suggestion: a balanced party.
        foreach (var id in new[] { "vanguard", "arcanist", "warden" }) Toggle(id, silent: true);
        Visuals.FadeIn(this);
    }

    private Control BuildHeatPicker()
    {
        int max = GameSession.Profile.MaxHeatUnlocked;
        _heat = max;
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(190, 0) };
        box.AddThemeConstantOverride("separation", 4);
        if (max <= 0)
        {
            var locked = Visuals.Label("Heat: locked\n(win a run to unlock)", 13, Visuals.TextDim);
            box.AddChild(locked);
            return box;
        }
        _heatLabel = Visuals.Label("", 16, Visuals.Ember, serif: true);
        box.AddChild(_heatLabel);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        box.AddChild(row);
        row.AddChild(Visuals.Button("-", () => SetHeat(_heat - 1), new Vector2(44, 34), 16));
        row.AddChild(Visuals.Button("+", () => SetHeat(_heat + 1), new Vector2(44, 34), 16));
        SetHeat(max);
        return box;
    }

    private void SetHeat(int heat)
    {
        _heat = Math.Clamp(heat, 0, GameSession.Profile.MaxHeatUnlocked);
        _heatLabel.Text = $"Heat {_heat} / {GameSession.Profile.MaxHeatUnlocked}";
        _heatLabel.TooltipText = Heat.Describe(_heat);
        _heatLabel.MouseFilter = MouseFilterEnum.Stop;
    }

    private PanelContainer BuildCard(HeroDef def)
    {
        bool locked = !Meta.IsHeroUnlocked(GameSession.Profile, def.Id);
        var card = new PanelContainer { CustomMinimumSize = new Vector2(164, 0), MouseFilter = MouseFilterEnum.Stop };
        card.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                if (locked) Audio.Sfx(this, "miss");
                else Toggle(def.Id);
            }
        };
        if (locked)
        {
            var unlock = Meta.HeroUnlocks.First(u => u.HeroId == def.Id);
            var lbox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            lbox.AddThemeConstantOverride("separation", 8);
            card.AddChild(lbox);
            var shadow = Visuals.Portrait(def.Id, 80);
            shadow.Modulate = new Color(0, 0, 0, 0.85f);
            shadow.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            lbox.AddChild(shadow);
            var q = Visuals.Label("LOCKED", 18, Visuals.TextDim, serif: true);
            q.HorizontalAlignment = HorizontalAlignment.Center;
            lbox.AddChild(q);
            var how = Visuals.Rich($"[center][color=#ffa040]{unlock.Condition}[/color]\n[color=#a99f8f]or unlock at the Ember Shrine ({unlock.Shards} shards)[/color][/center]", 12);
            how.CustomMinimumSize = new Vector2(140, 0);
            lbox.AddChild(how);
            _cards[def.Id] = card;
            return card;
        }
        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 4);
        card.AddChild(box);

        var portrait = Visuals.Portrait(def.Id, 80);
        portrait.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        box.AddChild(portrait);
        var name = Visuals.Label(def.Name, 16, Visuals.GoldBright, serif: true);
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(name);
        var role = Visuals.Label(def.Role.ToUpperInvariant(), 13, new Color(def.ColorHex).Lightened(0.3f));
        role.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(role);
        var blurb = Visuals.Rich($"[color=#a99f8f]{def.Blurb}[/color]", 12);
        blurb.CustomMinimumSize = new Vector2(140, 0);
        box.AddChild(blurb);
        box.AddChild(Visuals.Rich(
            $"HP [b]{def.MaxHp}[/b]  Armor [b]{def.Armor}[/b]  Speed [b]{def.Speed}[/b]\n" +
            $"Crit [b]{def.Crit}%[/b]  Dodge [b]{def.Dodge}%[/b]\n" +
            $"[color={def.Resource.ColorHex}]{def.Resource.Name}[/color] (max {def.Resource.Max})", 12));

        var abilities = new HFlowContainer { MouseFilter = MouseFilterEnum.Ignore };
        abilities.AddThemeConstantOverride("h_separation", 4);
        foreach (var id in def.Abilities.Prepend(def.BasicAbility))
        {
            var ability = Database.Ability(id);
            abilities.AddChild(Visuals.Icon(Visuals.AbilityIcon(ability), 24, $"{ability.Name}\n{ability.RulesText(false)}"));
        }
        box.AddChild(abilities);

        _cards[def.Id] = card;
        return card;
    }

    private void Toggle(string id, bool silent = false)
    {
        if (_picked.Contains(id))
        {
            _picked.Remove(id);
        }
        else
        {
            if (_picked.Count >= 3) _picked.RemoveAt(0);
            _picked.Add(id);
            var preferred = Database.Hero(id).PreferredRow;
            _rows[id] = preferred == Row.Front && _picked.Count(p => p != id && _rows.GetValueOrDefault(p) == Row.Front) >= Battle.MaxFrontRow
                ? Row.Back
                : preferred;
        }
        if (!silent) Audio.Sfx(this, "click", 0.05f);
        Refresh();
    }

    private void SetRow(string id, Row row)
    {
        _rows[id] = row;
        if (row == Row.Front)
        {
            var fronts = _picked.Where(p => p != id && _rows[p] == Row.Front).ToList();
            if (fronts.Count >= Battle.MaxFrontRow) _rows[fronts[0]] = Row.Back;
        }
        else if (_picked.All(p => _rows[p] == Row.Back))
        {
            // Someone has to hold the front.
            var other = _picked.FirstOrDefault(p => p != id);
            if (other is not null) _rows[other] = Row.Front;
        }
        Refresh();
    }

    private void Refresh()
    {
        if (_picked.Count > 0 && _picked.All(p => _rows[p] == Row.Back)) _rows[_picked[0]] = Row.Front;

        foreach (var (id, card) in _cards)
        {
            bool picked = _picked.Contains(id);
            var style = Visuals.PanelStyle(0.94f, 10);
            style.BorderColor = picked ? Visuals.GoldBright : new Color("#3a3147");
            style.SetBorderWidthAll(picked ? 3 : 2);
            card.AddThemeStyleboxOverride("panel", style);
            card.Modulate = picked || _picked.Count < 3 ? Colors.White : new Color(0.7f, 0.7f, 0.75f);
            if (!Meta.IsHeroUnlocked(GameSession.Profile, id)) card.Modulate = new Color(0.75f, 0.72f, 0.8f);
        }

        foreach (var child in _formation.GetChildren()) child.QueueFree();
        _formation.AddChild(Visuals.Label($"Formation ({_picked.Count}/3)  ·  max {Battle.MaxFrontRow} in the front row", 15, Visuals.Gold, serif: true));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _formation.AddChild(row);
        foreach (var id in _picked)
        {
            var def = Database.Hero(id);
            var entry = new HBoxContainer();
            entry.AddThemeConstantOverride("separation", 4);
            entry.AddChild(Visuals.Portrait(id, 36));
            var front = Visuals.Button("Front", () => SetRow(id, Row.Front), new Vector2(70, 34), 13);
            front.ToggleMode = true;
            front.ButtonPressed = _rows[id] == Row.Front;
            var back = Visuals.Button("Back", () => SetRow(id, Row.Back), new Vector2(70, 34), 13);
            back.ToggleMode = true;
            back.ButtonPressed = _rows[id] == Row.Back;
            front.TooltipText = back.TooltipText = $"{def.Name} prefers the {def.PreferredRow.ToString().ToLowerInvariant()} row.";
            entry.AddChild(front);
            entry.AddChild(back);
            row.AddChild(entry);
        }
        _begin.Disabled = _picked.Count != 3;
    }

    private void Begin()
    {
        if (_picked.Count != 3) return;
        var party = _picked.OrderBy(p => _rows[p]).Select(p => (p, _rows[p])).ToList();
        GameSession.StartRun(GetTree(), party, _heat);
    }
}
