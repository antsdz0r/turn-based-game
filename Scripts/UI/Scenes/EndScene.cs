using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Victory or defeat summary with run statistics. Start a new run or return to the menu.</summary>
public partial class EndScene : Screen
{
    protected override void Build()
    {
        var run = GameSession.EndedRun;
        bool won = GameSession.EndedWon;
        Audio.Music(this, won ? "menu" : "menu");
        AddBackdrop(won ? new Color(0.75f, 0.68f, 0.6f) : new Color(0.4f, 0.35f, 0.45f), torches: won);

        if (run is not null)
        {
            var floor = Visuals.AnchorPoint(this, 0.5f, 0.92f);
            for (int i = 0; i < run.Party.Count; i++)
            {
                var sprite = Visuals.UnitSprite(run.Party[i].Id, 2.25f, breathe: won);
                sprite.Position = new Vector2(-130 + i * 130, 0);
                if (!won) sprite.Modulate = new Color(0.45f, 0.45f, 0.5f, 0.8f);
                floor.AddChild(sprite);
            }
        }

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.CenterTop);
        panel.GrowHorizontal = GrowDirection.Both;
        panel.OffsetTop = 30;
        AddChild(panel);
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(560, 0) };
        box.AddThemeConstantOverride("separation", 8);
        panel.AddChild(box);

        var title = Visuals.Title(won ? "THE EMBER RE-KINDLED" : "FALLEN IN THE VAULT", 40);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        if (!won) title.AddThemeColorOverride("font_color", Visuals.Bad);
        box.AddChild(title);
        var flavor = Visuals.Label(won
            ? "The Hollow King crumbles to ash. The Last Ember burns again."
            : "The Last Ember flickers. Others will descend where you fell.", 15, Visuals.TextDim);
        flavor.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(flavor);
        box.AddChild(Visuals.Separator());

        if (run is not null)
        {
            var s = run.Stats;
            box.AddChild(Visuals.Rich(
                $"Time  [b]{Visuals.FormatTime(s.Seconds)}[/b]\n" +
                $"Floor reached  [b]{run.Floor + 1} / {DungeonMap.Floors}[/b]\n" +
                $"Battles won  [b]{s.BattlesWon}[/b]   ·   Elites slain  [b]{s.ElitesSlain}[/b]\n" +
                $"Enemies slain  [b]{s.EnemiesSlain}[/b]   ·   Damage dealt  [b]{s.DamageDealt}[/b]\n" +
                $"Gold earned  [b]{s.GoldEarned}[/b]   ·   Relics  [b]{run.Relics.Count}[/b]", 16));
        }
        var p = GameSession.Profile;
        var shards = Visuals.Rich($"[center]{Visuals.Img("UI/icon_fire.png", 18)} [color=#ffb85a][b]+{p.LastShardsEarned} Ember Shards[/b][/color]  " +
                                  $"[color=#a99f8f](total {p.Shards}, spend them at the Ember Shrine)[/color][/center]", 16);
        box.AddChild(shards);
        foreach (var u in Meta.HeroUnlocks.Where(u => run is not null && Meta.IsHeroUnlocked(p, u.HeroId) && run.BossesKilled > 0 && p.BossesDefeated.Contains(u.BossId)))
        {
            var hero = Visuals.Label($"New hero available: {Database.Hero(u.HeroId).Name}!", 16, Visuals.GoldBright);
            hero.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(hero);
        }
        if (run?.DailyDate is { } date)
        {
            var d = Visuals.Label($"Daily challenge {date}: best {(p.DailyBest.GetValueOrDefault(date) > DungeonMap.Floors ? "VICTORY" : $"floor {p.DailyBest.GetValueOrDefault(date)}")}", 15, Visuals.Text);
            d.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(d);
        }
        if (won && run is not null && run.Heat < Heat.Max && p.MaxHeatUnlocked > run.Heat)
        {
            var unlocked = Visuals.Label($"Heat {run.Heat + 1} unlocked! {Heat.Modifiers[run.Heat]}", 16, Visuals.Ember);
            unlocked.HorizontalAlignment = HorizontalAlignment.Center;
            box.AddChild(unlocked);
        }
        box.AddChild(Visuals.Separator());
        box.AddChild(Visuals.Rich($"[color=#a99f8f]All runs: {p.RunsWon} won of {p.RunsStarted}   ·   Deepest floor {p.BestFloor}" +
                                  (p.FastestWinSeconds > 0 ? $"   ·   Fastest win {Visuals.FormatTime(p.FastestWinSeconds)}" : "") + "[/color]", 14));

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 12);
        box.AddChild(buttons);
        var again = Visuals.Button("New Run", () => GameSession.ChangeScene(GetTree(), GameSession.HeroSelectScene), new Vector2(200, 50), 18);
        buttons.AddChild(again);
        buttons.AddChild(Visuals.Button("Main Menu", () => GameSession.ChangeScene(GetTree(), GameSession.MenuScene), new Vector2(200, 50), 18));
        again.CallDeferred(Control.MethodName.GrabFocus);

        Audio.Sfx(this, won ? "victory" : "defeat", 0f);
        Visuals.FadeIn(this, 1.0);
    }
}
