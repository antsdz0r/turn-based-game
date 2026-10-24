using System.Linq;
using System.Collections.Generic;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Title screen.</summary>
public partial class MainMenu : Screen
{
    private VBoxContainer _menu = null!;
    private Godot.Button? _newRun;
    private bool _confirmNew;

    protected override void Build()
    {
        Audio.Music(this, "menu");
        AddBackdrop(new Color(0.62f, 0.58f, 0.72f));

        var floor = Visuals.AnchorPoint(this, 0.5f, 0.9f);
        string[] party = ["ranger", "arcanist", "warden", "shadowblade", "vanguard", "hexer", "berserker"];
        for (int i = 0; i < party.Length; i++)
        {
            var sprite = Visuals.UnitSprite(party[i]);
            sprite.Position = new Vector2(-600 + i * 62, 0);
            if (!Meta.IsHeroUnlocked(GameSession.Profile, party[i])) sprite.Modulate = new Color(0, 0, 0, 0.7f);
            floor.AddChild(sprite);
        }
        var king = Visuals.UnitSprite("hollow_king", 2.5f);
        king.Position = new Vector2(400, 0);
        king.Modulate = new Color(0.75f, 0.7f, 0.85f, 0.95f);
        floor.AddChild(king);

        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.SetAnchorsPreset(LayoutPreset.FullRect);
        column.AddThemeConstantOverride("separation", 8);
        AddChild(column);
        column.AddChild(Visuals.Spacer(14));

        var title = Visuals.Title("ASHENBOUND", 72);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeConstantOverride("outline_size", 18);
        column.AddChild(title);
        var subtitle = Visuals.Label("—  THE LAST EMBER  —", 22, Visuals.Ember, serif: true);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(subtitle);
        column.AddChild(Visuals.Spacer(10));

        _menu = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        _menu.AddThemeConstantOverride("separation", 10);
        column.AddChild(_menu);

        var saved = GameSession.LoadRunFile();
        if (saved is not null)
        {
            var cont = Visuals.Button("Continue Run", () =>
            {
                if (!GameSession.ContinueRun(GetTree())) Audio.Sfx(this, "hurt");
            }, new Vector2(280, 46), 18);
            cont.TooltipText = $"Floor {saved.Floor + 1} · {saved.Gold} gold · {Visuals.FormatTime(saved.Stats.Seconds)} played";
            _menu.AddChild(cont);
        }
        _newRun = Visuals.Button("New Run", OnNewRun, new Vector2(280, 46), 18);
        _menu.AddChild(_newRun);
        var today = Daily.Today;
        var mod = Daily.ModifierFor(today);
        var daily = Visuals.Button("Daily Challenge", OnDaily, new Vector2(280, 46), 18, "UI/icon_charging.png");
        int best = GameSession.Profile.DailyBest.GetValueOrDefault(today);
        daily.TooltipText = $"{today}: \"{mod.Name}\" — {mod.Description}\nSame party and route for everyone today. +50% Ember Shards."
                            + (best > 0 ? $"\nYour best today: {(best > DungeonMap.Floors ? "VICTORY" : $"floor {best}")}" : "");
        _menu.AddChild(daily);
        _menu.AddChild(Visuals.Button($"Ember Shrine  ({GameSession.Profile.Shards})", () => GameSession.ChangeScene(GetTree(), GameSession.ShrineScene),
                                      new Vector2(280, 46), 18, "UI/icon_fire.png"));
        var extras = new HBoxContainer();
        extras.AddThemeConstantOverride("separation", 8);
        extras.AddChild(Visuals.Button("How to Play", () => GameSession.ChangeScene(GetTree(), GameSession.HowToPlayScene), new Vector2(136, 46), 16));
        extras.AddChild(Visuals.Button("Compendium", () => GameSession.ChangeScene(GetTree(), GameSession.CompendiumScene), new Vector2(136, 46), 16));
        _menu.AddChild(extras);
        _menu.AddChild(Visuals.Button("Settings", OpenSettings, new Vector2(280, 46), 18));
        _menu.AddChild(Visuals.Button("Quit", () => GetTree().Quit(), new Vector2(280, 46), 18));
        _menu.GetChild<Control>(0).CallDeferred(Control.MethodName.GrabFocus);

        var p = GameSession.Profile;
        string stats = p.RunsStarted == 0
            ? "The Last Ember is dying. Descend into the Ashen Vault and take back its heart."
            : (p.MaxHeatUnlocked > 0 ? $"Heat unlocked: {p.MaxHeatUnlocked}   ·   " : "") + $"Runs: {p.RunsStarted}   ·   Victories: {p.RunsWon}   ·   Deepest floor: {p.BestFloor}   ·   Foes slain: {p.TotalEnemiesSlain}"
              + (p.FastestWinSeconds > 0 ? $"   ·   Fastest win: {Visuals.FormatTime(p.FastestWinSeconds)}" : "");
        var footer = Visuals.Label(stats, 14, Visuals.TextDim);
        footer.HorizontalAlignment = HorizontalAlignment.Center;
        footer.SetAnchorsPreset(LayoutPreset.BottomWide);
        footer.GrowVertical = GrowDirection.Begin;
        footer.OffsetBottom = -10;
        AddChild(footer);

        // Never cut off: if the menu is taller than the window, shrink it from the top center.
        void FitMenu()
        {
            if (!IsInstanceValid(this) || !IsInstanceValid(column) || !IsInsideTree()) return;
            var view = GetViewportRect().Size;
            float need = column.GetCombinedMinimumSize().Y;
            float s = Mathf.Min(1f, (view.Y - 44) / Mathf.Max(1, need));
            column.PivotOffset = new Vector2(view.X / 2, 0);
            column.Scale = new Vector2(s, s);
        }
        Callable.From(FitMenu).CallDeferred();
        GetViewport().SizeChanged += FitMenu;

        Visuals.FadeIn(this, 1.0);
    }

    private void OnNewRun()
    {
        if (GameSession.HasSavedRun && !_confirmNew)
        {
            _confirmNew = true;
            if (_newRun is not null) _newRun.Text = "Abandon saved run?";
            return;
        }
        if (GameSession.LoadRunFile() is { } old)
        {
            GameSession.Profile.RecordRunEnd(old, won: false);
            GameSession.SaveProfile();
            GameSession.DeleteRun();
        }
        GameSession.ChangeScene(GetTree(), GameSession.HeroSelectScene);
    }

    private bool _confirmDaily;

    private void OnDaily()
    {
        if (GameSession.HasSavedRun && !_confirmDaily)
        {
            _confirmDaily = true;
            Audio.Sfx(this, "blip");
            if (_newRun is not null) _newRun.Text = "Press Daily again to abandon your run";
            return;
        }
        if (GameSession.LoadRunFile() is { } old)
        {
            GameSession.Profile.RecordRunEnd(old, won: false);
            GameSession.SaveProfile();
            GameSession.DeleteRun();
        }
        GameSession.StartDaily(GetTree());
    }

    private void OpenSettings()
    {
        _menu.Visible = false;
        SettingsPanel? panel = null;
        panel = new SettingsPanel(() =>
        {
            panel?.QueueFree();
            _menu.Visible = true;
        });
        AddChild(panel);
    }
}
