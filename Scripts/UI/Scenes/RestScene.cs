using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Rest site: heal 35% (and revive the fallen), or train one ability.</summary>
public partial class RestScene : Screen
{
    private VBoxContainer _box = null!;
    private bool _done;

    protected override void Build()
    {
        var run = GameSession.Run ?? GameSession.EnsureRun();
        Audio.Music(this, "menu");
        if (GameSession.Run is { } regionRun) AddRegionBackdrop(regionRun.NodeRegion, 0.45f, torches: false); else AddBackdrop(new Color(0.42f, 0.38f, 0.48f), torches: false);
        Glows.Add(Visuals.AddCampfire(this, 0.5f, 0.86f));

        var fire = Visuals.AnchorPoint(this, 0.5f, 0.88f);
        float[] seats = [-250f, -150f, 170f];
        for (int i = 0; i < run.Party.Count && i < seats.Length; i++)
        {
            var sprite = Visuals.UnitSprite(run.Party[i].Id, 2f, flip: seats[i] > 0);
            sprite.Position = new Vector2(seats[i], 0);
            fire.AddChild(sprite);
        }

        // A solid, centered window above the campfire (it scales down if it ever doesn't fit).
        var panel = Visuals.CenterPanel(this, 640);
        panel.AddThemeStyleboxOverride("panel", Visuals.FramedPanel(1f, 16));
        _box = new VBoxContainer { CustomMinimumSize = new Vector2(620, 0) };
        _box.AddThemeConstantOverride("separation", 10);
        panel.AddChild(_box);
        Fill(run, "");

        AddPauseMenu(inRun: true);
        Visuals.FadeIn(this, 0.5);
    }

    private void Fill(RunState run, string result)
    {
        foreach (var child in _box.GetChildren()) child.QueueFree();
        var title = Visuals.Title("A Quiet Ember", 32);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _box.AddChild(title);
        _box.AddChild(Visuals.Label("The fire is small, but it is warm. Choose one:", 15, Visuals.TextDim));

        foreach (var hero in run.Party)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(Visuals.Portrait(hero.Id, 32));
            float pct = (float)hero.Hp / hero.MaxHp;
            row.AddChild(Visuals.Label($"{hero.Def.Name}   HP {hero.Hp}/{hero.MaxHp}", 15, pct < 0.35f ? Visuals.Bad : Visuals.Text));
            _box.AddChild(row);
        }

        if (!_done)
        {
            _box.AddChild(Visuals.Button($"Rest: every hero recovers {run.RestHealPct}% of max HP", () =>
            {
                run.HealParty(run.RestHealPct);
                _done = true;
                run.CompleteNode();
                Audio.Sfx(this, "heal", 0f);
                GameSession.SaveRun();
                Fill(run, "The party rests. Wounds close and spirits lift.");
            }, new Vector2(0, 46), 16, "UI/icon_node_rest.png"));
            _box.AddChild(Visuals.Button("Train: upgrade one ability for free", () =>
            {
                AddChild(new UpgradePicker((heroId, abilityId) =>
                {
                    if (!run.Upgrade(heroId, abilityId)) return;
                    _done = true;
                    run.CompleteNode();
                    Audio.Sfx(this, "levelup", 0f);
                    GameSession.SaveRun();
                    var hero = run.Hero(heroId);
                    Fill(run, $"{hero.Def.Name} practices until {Database.Ability(abilityId).Name} becomes {Database.Ability(abilityId).Name}+.");
                }, () => { }));
                Pause?.MoveToFront();
            }, new Vector2(0, 46), 16, "UI/icon_empowered.png"));
        }
        else
        {
            _box.AddChild(Visuals.Rich($"[color=#6be07a]{result}[/color]", 15));
            _box.AddChild(Visuals.Button("Continue", () => GameSession.CompleteNode(GetTree()), new Vector2(0, 46), 17));
        }
        _box.AddChild(Visuals.Button("Party & Gear", () => AddChild(new PartyPanel(() => Fill(run, result))), new Vector2(0, 40), 15, "UI/icon_bag.png"));
    }
}
