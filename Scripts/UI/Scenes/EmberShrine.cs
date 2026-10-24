using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Spend Ember Shards on permanent upgrades and on unlocking new heroes.</summary>
public partial class EmberShrine : Screen
{
    private VBoxContainer _list = null!;
    private Godot.Label _shards = null!;

    protected override void Build()
    {
        Audio.Music(this, "menu");
        AddRegionBackdrop(Database.Region(0), 0.5f, torches: true);
        Glows.Add(Visuals.AddCampfire(this, 0.5f, 0.97f));

        var panel = Visuals.CenterPanel(this, 900);
        var root = new VBoxContainer { CustomMinimumSize = new Vector2(880, 560) };
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 12);
        root.AddChild(head);
        var title = Visuals.Title("The Ember Shrine", 34);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        head.AddChild(title);
        head.AddChild(Visuals.Icon("UI/icon_fire.png", 28));
        _shards = Visuals.Label("", 22, Visuals.GoldBright, serif: true);
        head.AddChild(_shards);
        root.AddChild(Visuals.Label("Every run leaves Ember Shards behind: floors reached, elites and bosses slain, and victories. Spend them here. Upgrades last forever.", 14, Visuals.TextDim));

        var scroll = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        root.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_list);

        root.AddChild(Visuals.Button("Back", () => GameSession.ChangeScene(GetTree(), GameSession.MenuScene), new Vector2(160, 44), 16));
        Fill();
        Visuals.FadeIn(this, 0.4);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel")) GameSession.ChangeScene(GetTree(), GameSession.MenuScene);
        else base._UnhandledInput(@event);   // arrows / Tab focus the first button
    }

    private void Fill()
    {
        var p = GameSession.Profile;
        _shards.Text = $"{p.Shards} shards";
        foreach (var child in _list.GetChildren()) child.QueueFree();

        _list.AddChild(Visuals.Label("Heroes", 20, Visuals.Gold, serif: true));
        foreach (var u in Meta.HeroUnlocks)
        {
            var def = Database.Hero(u.HeroId);
            bool unlocked = Meta.IsHeroUnlocked(p, u.HeroId);
            var row = Row(Visuals.Portrait(u.HeroId, 56),
                $"[font_size=18][color=#ffd76b]{def.Name}[/color][/font_size]  [color=#a99f8f]{def.Role}[/color]\n{def.Blurb}\n" +
                (unlocked ? "[color=#7fd4a0]Unlocked[/color]" : $"[color=#ffa040]{u.Condition}[/color] to unlock, or buy with shards."));
            var buy = Visuals.Button(unlocked ? "Unlocked" : $"Unlock ({u.Shards})", () =>
            {
                if (!Meta.BuyHero(p, u)) return;
                Audio.Sfx(this, "levelup", 0f);
                GameSession.SaveProfile();
                Fill();
            }, new Vector2(170, 44), 15);
            buy.Disabled = unlocked || p.Shards < u.Shards;
            row.AddChild(buy);
            if (!unlocked) ((TextureRect)row.GetChild(0)).Modulate = new Color(0.35f, 0.35f, 0.4f);
        }

        _list.AddChild(Visuals.Separator());
        int done = Feats.All.Count(f => p.FeatsClaimed.Contains(f.Id));
        _list.AddChild(Visuals.Label($"Feats  ({done}/{Feats.All.Count})", 20, Visuals.Gold, serif: true));
        _list.AddChild(Visuals.Label("Long-term goals across runs. Each pays shards once (at the end of the run that completes it); some unlock new weapons.", 13, Visuals.TextDim));
        foreach (var f in Feats.All.OrderBy(f => p.FeatsClaimed.Contains(f.Id)))
        {
            bool claimed = p.FeatsClaimed.Contains(f.Id);
            int progress = Feats.Progress(p, f);
            string bar = f.Goal > 1 ? $"  [color=#a99f8f]{progress}/{f.Goal}[/color] " + ProgressBar(progress, f.Goal) : "";
            string reward = $"[color=#ffb85a]+{f.Shards} shards[/color]"
                            + (f.UnlocksItem is { } item && Database.HasItem(item) ? $"  ·  [color=#9fd8ff]Unlocks {Database.Item(item).Name}[/color]" : "");
            var row = Row(Visuals.Icon(claimed ? "UI/icon_fire.png" : f.UnlocksItem is not null ? "UI/icon_weapon.png" : "UI/icon_relic.png", 40),
                $"[font_size=17][color={(claimed ? "#7fd4a0" : "#ffd76b")}]{f.Name}[/color][/font_size]{bar}\n[color=#c9bfae]{f.Description}[/color]\n{(claimed ? "[color=#7fd4a0]Complete[/color]" : reward)}");
            if (claimed) ((TextureRect)row.GetChild(0)).Modulate = new Color(1f, 0.85f, 0.6f);
            else if (progress == 0) ((TextureRect)row.GetChild(0)).Modulate = new Color(0.45f, 0.45f, 0.5f);
        }

        _list.AddChild(Visuals.Separator());
        _list.AddChild(Visuals.Label("Blessings", 20, Visuals.Gold, serif: true));
        foreach (var u in Meta.Upgrades)
        {
            int level = Meta.Level(p, u.Id);
            int cost = Meta.NextCost(p, u);
            string pips = string.Concat(Enumerable.Range(0, u.MaxLevel).Select(i => i < level ? "◆" : "◇"));
            var row = Row(Visuals.Icon("UI/icon_empowered.png", 40),
                $"[font_size=17][color=#ffd76b]{u.Name}[/color][/font_size]  [color=#ffb85a]{pips}[/color]\n[color=#c9bfae]{u.Description}[/color]");
            var buy = Visuals.Button(cost < 0 ? "Maxed" : $"Buy ({cost})", () =>
            {
                if (!Meta.Buy(p, u)) return;
                Audio.Sfx(this, "coin", 0f);
                Audio.Sfx(this, "buff", 0.05f, 0.7f);
                GameSession.SaveProfile();
                Fill();
            }, new Vector2(170, 44), 15);
            buy.Disabled = cost < 0 || p.Shards < cost;
            row.AddChild(buy);
        }
    }

    private static string ProgressBar(int value, int goal)
    {
        int filled = (int)System.Math.Round(10.0 * value / System.Math.Max(1, goal));
        return $"[color=#ffb85a]{new string('▰', filled)}[/color][color=#4a4058]{new string('▱', 10 - filled)}[/color]";
    }

        private HBoxContainer Row(Control icon, string bbcode)
    {
        var card = new PanelContainer();
        card.AddThemeStyleboxOverride("panel", Visuals.Box(new Color("#1a1520"), new Color("#3a3147"), 1, 6, 10));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);
        card.AddChild(row);
        icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(icon);
        var text = Visuals.Rich(bbcode, 14);
        text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(text);
        _list.AddChild(card);
        return row;
    }
}
