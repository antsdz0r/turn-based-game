using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Choose 3 heroes and set their rows (at most 2 in front).</summary>
public partial class HeroSelect : Screen
{
    private readonly List<string> _picked = new();
    private readonly Dictionary<string, Row> _rows = new();
    private readonly Dictionary<string, PanelContainer> _cards = new();
    private VBoxContainer _formation = null!;
    private Godot.Button _begin = null!;
    private int _heat;
    /// <summary>Card width: all heroes fit side by side in the 1280px layout (narrower as the roster grows).</summary>
    private static float CardWidth => Math.Clamp((1232f - 6 * (Database.Heroes.Count - 1)) / Database.Heroes.Count, 124f, 164f);
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
        var card = new PanelContainer { CustomMinimumSize = new Vector2(CardWidth, 0), MouseFilter = MouseFilterEnum.Stop };
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
            how.CustomMinimumSize = new Vector2(CardWidth - 24, 0);
            lbox.AddChild(how);
            _cards[def.Id] = card;
            return card;
        }
        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 4);
        card.AddChild(box);

        var prof = GameSession.Profile;
        int psize = CardWidth < 150 ? 68 : 80;
        var portrait = Visuals.Icon($"Portraits/{LookFor(def.Id)}.png", psize);
        portrait.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        box.AddChild(portrait);
        int mastery = Mastery.Level(prof, def.Id);
        int next = Mastery.NextThreshold(prof, def.Id);
        var masteryLabel = Visuals.Rich($"[center][color=#ffd76b]{new string('★', mastery)}[/color][color=#4a4058]{new string('☆', Mastery.MaxLevel - mastery)}[/color] " +
                                        $"[color=#9fd8ff]{Mastery.LevelName(mastery)}[/color][/center]", 11);
        masteryLabel.TooltipText = $"Hero mastery: {Mastery.Xp(prof, def.Id)}{(next > 0 ? $"/{next}" : "")} XP from runs with this hero.\n" +
                                   "Adept: a new costume.  Veteran: an alternative ability.  Mastered: start runs with an upgraded basic attack.";
        masteryLabel.MouseFilter = MouseFilterEnum.Stop;
        box.AddChild(masteryLabel);
        var name = Visuals.Label(def.Name, 16, Visuals.GoldBright, serif: true);
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(name);
        var role = Visuals.Label(def.Role.ToUpperInvariant(), 13, new Color(def.ColorHex).Lightened(0.3f));
        role.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(role);
        var blurb = Visuals.Rich($"[color=#a99f8f]{def.Blurb}[/color]", 12);
        blurb.CustomMinimumSize = new Vector2(CardWidth - 24, 0);
        box.AddChild(blurb);
        box.AddChild(Visuals.Rich(
            $"HP [b]{def.MaxHp}[/b]  Armor [b]{def.Armor}[/b]\nSpeed [b]{def.Speed}[/b]  " +
            $"Crit [b]{def.Crit}%[/b]  Dodge [b]{def.Dodge}%[/b]\n" +
            $"[color={def.Resource.ColorHex}]{def.Resource.Name}[/color] (max {def.Resource.Max})", 12));

        var abilities = new HFlowContainer { MouseFilter = MouseFilterEnum.Ignore };
        abilities.AddThemeConstantOverride("h_separation", 4);
        FillAbilities(abilities, def);
        box.AddChild(abilities);

        // Mastery loadout: costume and alternative ability, remembered in the profile.
        if (Mastery.HasCostume(prof, def.Id) && Mastery.Costume(def.Id) is { } costume)
        {
            var look = Visuals.Button(prof.CostumeOn.Contains(def.Id) ? costume.Name : "Default look", () => { }, new Vector2(0, 28), 11);
            look.TooltipText = $"{costume.Name}: {costume.Description}\nClick to switch.";
            look.Pressed += () =>
            {
                if (!prof.CostumeOn.Remove(def.Id)) prof.CostumeOn.Add(def.Id);
                look.Text = prof.CostumeOn.Contains(def.Id) ? costume.Name : "Default look";
                portrait.Texture = Visuals.Tex($"Portraits/{LookFor(def.Id)}.png");
                GameSession.SaveProfile();
            };
            box.AddChild(look);
        }
        if (Mastery.HasAlt(prof, def.Id) && Mastery.Alt(def.Id) is { } alt)
        {
            var altAbility = Database.Ability(alt.AbilityId);
            var swap = Visuals.Button(prof.AltOn.Contains(def.Id) ? $"Using {altAbility.Name}" : $"Using {Database.Ability(alt.Replaces).Name}", () => { }, new Vector2(0, 28), 11);
            swap.TooltipText = $"Alternative ability: {altAbility.Name}\n{altAbility.RulesText(false)}\nClick to swap it with {Database.Ability(alt.Replaces).Name}.";
            swap.Pressed += () =>
            {
                if (!prof.AltOn.Remove(def.Id)) prof.AltOn.Add(def.Id);
                swap.Text = prof.AltOn.Contains(def.Id) ? $"Using {altAbility.Name}" : $"Using {Database.Ability(alt.Replaces).Name}";
                FillAbilities(abilities, def);
                GameSession.SaveProfile();
            };
            box.AddChild(swap);
        }

        _cards[def.Id] = card;
        return card;
    }

    /// <summary>The portrait to show: the costume if it is unlocked and switched on.</summary>
    private static string LookFor(string heroId)
    {
        var prof = GameSession.Profile;
        return prof.CostumeOn.Contains(heroId) && Mastery.HasCostume(prof, heroId) && Mastery.Costume(heroId) is { } c ? c.Sprite : heroId;
    }

    private static void FillAbilities(HFlowContainer flow, HeroDef def)
    {
        foreach (var child in flow.GetChildren()) child.QueueFree();
        var prof = GameSession.Profile;
        var alt = prof.AltOn.Contains(def.Id) && Mastery.HasAlt(prof, def.Id) ? Mastery.Alt(def.Id) : null;
        foreach (var id in def.Abilities.Select(a => alt is not null && a == alt.Replaces ? alt.AbilityId : a).Prepend(def.BasicAbility))
        {
            var ability = Database.Ability(id);
            flow.AddChild(Visuals.Icon(Visuals.AbilityIcon(ability), 24, $"{ability.Name}\n{ability.RulesText(false)}"));
        }
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
            entry.AddChild(Visuals.Icon($"Portraits/{LookFor(id)}.png", 36));
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
        // Duo techniques this party unlocks (and the ones one hero away).
        var duos = Database.ActiveSynergies(_picked).ToList();
        var near = Database.Synergies.Where(syn => !duos.Contains(syn) && (_picked.Contains(syn.HeroA) || _picked.Contains(syn.HeroB))).ToList();
        if (duos.Count > 0 || near.Count > 0)
        {
            string Short(string id) => Database.Hero(id).Name.Split(' ')[0];
            var text = string.Join("   ", duos.Select(d => $"[color=#ffd76b]⚔ {Database.Ability(d.AbilityId).Name}[/color] [color=#a99f8f]({Short(d.HeroA)} + {Short(d.HeroB)})[/color]"));
            if (_picked.Count < 3 && near.Count > 0)
                text += (text.Length > 0 ? "   " : "") + "[color=#5e566a]Possible duos: " + string.Join(", ", near.Take(3).Select(d => $"{Short(d.HeroA)} + {Short(d.HeroB)}")) + "[/color]";
            var duoLabel = Visuals.Rich(text, 12);
            duoLabel.TooltipText = string.Join("\n\n", duos.Select(d => $"{Database.Ability(d.AbilityId).Name}: {d.Flavor}\n{Database.Ability(d.AbilityId).Description}"));
            duoLabel.MouseFilter = MouseFilterEnum.Stop;
            _formation.AddChild(duoLabel);
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
