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
                var sprite = Visuals.UnitSprite(run.Party[i].Sprite, 2.25f, breathe: won);
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

        var title = Visuals.Title(won ? (run?.Loop > 0 ? $"LOOP {run.Loop + 1} CONQUERED" : "THE EMBER RE-KINDLED") : "FALLEN IN THE VAULT", 40);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        if (!won) title.AddThemeColorOverride("font_color", Visuals.Bad);
        box.AddChild(title);
        var flavor = Visuals.Label(won
            ? "The Hollow King crumbles to ash. The Last Ember burns again."
            : "The Last Ember flickers. Others will descend where you fell.", 15, Visuals.TextDim);
        flavor.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(flavor);
        box.AddChild(Visuals.Separator());

        // Everything between the title and the buttons scrolls, so the buttons are always on screen.
        var frame = box;
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(600, Mathf.Max(200, GetViewportRect().Size.Y - 300)),
        };
        frame.AddChild(scroll);
        box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        box.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(box);

        if (run is not null)
        {
            var s = run.Stats;
            var record = GameSession.Profile.History.FirstOrDefault();
            string Name(string? id) => id is not null && Database.Heroes.Any(h => h.Id == id) ? Database.Hero(id).Name.Split(' ')[0] : "-";
            box.AddChild(Visuals.Rich(
                (record is not null ? $"[color=#ffb85a]Route[/color]  [b]{record.RouteText}[/b]\n" : "") +
                $"Time  [b]{Visuals.FormatTime(s.Seconds)}[/b]   ·   Floor  [b]{run.Floor + 1} / {DungeonMap.Floors}[/b]   ·   Heat  [b]{run.Heat}[/b]\n" +
                $"Battles won  [b]{s.BattlesWon}[/b]   ·   Elites slain  [b]{s.ElitesSlain}[/b]   ·   Enemies slain  [b]{s.EnemiesSlain}[/b]\n" +
                $"Damage dealt  [b]{s.DamageDealt}[/b]   ·   Gold earned  [b]{s.GoldEarned}[/b]   ·   Relics  [b]{run.Relics.Count}[/b]\n" +
                $"Combos  [b]{s.Combos}[/b]   ·   Breaks  [b]{s.Breaks}[/b]   ·   Bounties  [b]{s.Bounties}[/b]", 15));
            // Who carried the run.
            var mvp = s.HeroDamage.OrderByDescending(kv => kv.Value).FirstOrDefault();
            var stars = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            stars.AddThemeConstantOverride("separation", 10);
            box.AddChild(stars);
            if (mvp.Key is not null)
            {
                stars.AddChild(Visuals.Portrait(mvp.Key, 44));
                stars.AddChild(Visuals.Rich($"[color=#ffd76b]MVP[/color] [b]{Name(mvp.Key)}[/b]\n[color=#a99f8f]{mvp.Value} damage ({100 * mvp.Value / System.Math.Max(1, s.DamageDealt)}%)[/color]", 14));
            }
            if (s.BestHitBy is not null)
            {
                stars.AddChild(Visuals.Spacer(0, 20));
                stars.AddChild(Visuals.Portrait(s.BestHitBy, 44));
                stars.AddChild(Visuals.Rich($"[color=#ffd76b]Biggest hit[/color] [b]{s.BestHit}[/b]\n[color=#a99f8f]by {Name(s.BestHitBy)}[/color]", 14));
            }
            foreach (var child in stars.GetChildren()) if (child is RichTextLabel rich) rich.CustomMinimumSize = new Vector2(150, 0);
            if (!won && s.DefeatedBy is { Length: > 0 } killer)
            {
                var fell = Visuals.Label($"Fell to: {killer}", 14, Visuals.Bad);
                fell.HorizontalAlignment = HorizontalAlignment.Center;
                box.AddChild(fell);
            }
        }
        var p = GameSession.Profile;
        var shards = Visuals.Rich($"[center]{Visuals.Img("UI/icon_fire.png", 18)} [color=#ffb85a][b]+{p.LastShardsEarned} Ember Shards[/b][/color]  " +
                                  $"[color=#a99f8f](total {p.Shards}, spend them at the Ember Shrine)[/color][/center]", 16);
        box.AddChild(shards);
        foreach (var feat in p.LastFeats)
        {
            string unlock = feat.UnlocksItem is { } item && Database.HasItem(item) ? $"  [color=#9fd8ff]Unlocked: {Database.Item(item).Name}[/color]" : "";
            box.AddChild(Visuals.Rich($"[center][color=#ffd76b]Feat complete: {feat.Name}[/color]  [color=#ffb85a]+{feat.Shards}[/color]{unlock}[/center]", 15));
        }
        var pages = p.LoreNew.Select(Lore.Get).Where(pg => pg is not null).Select(pg => pg!.Title).ToList();
        if (pages.Count > 0)
            box.AddChild(Visuals.Rich($"[center][color=#9fd8ff]New lore: {string.Join(", ", pages)}[/color] [color=#a99f8f](Compendium → Lore)[/color][/center]", 14));
        // Mastery level-ups, one line per level reached (heroes grouped).
        foreach (var group in p.LastMastery.GroupBy(m => m.Level).OrderBy(g => g.Key))
        {
            string names = string.Join(", ", group.Select(m => Database.Hero(m.HeroId).Name.Split(' ')[0]));
            box.AddChild(Visuals.Rich($"[center][color=#9fd8ff]Mastery {group.Key} ({Mastery.LevelName(group.Key)}): {names}[/color]  [color=#a99f8f]unlocked {Mastery.RewardText(group.Key)} (main menu → Heroes)[/color][/center]", 14));
        }
        if (p.NewHeroes.Count > 0)
            box.AddChild(Visuals.Rich($"[center][color=#ffd76b]New hero{(p.NewHeroes.Count > 1 ? "es" : "")} available: {string.Join(", ", p.NewHeroes.Select(id => Database.Hero(id).Name))}![/color][/center]", 16));
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
        box = frame;
        box.AddChild(Visuals.Separator());
        box.AddChild(Visuals.Rich($"[color=#a99f8f]All runs: {p.RunsWon} won of {p.RunsStarted}   ·   Deepest floor {p.BestFloor}" +
                                  (p.FastestWinSeconds > 0 ? $"   ·   Fastest win {Visuals.FormatTime(p.FastestWinSeconds)}" : "") + "[/color]", 14));

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 12);
        box.AddChild(buttons);
        var again = Visuals.Button("New Run", () => GameSession.ChangeScene(GetTree(), GameSession.HeroSelectScene), new Vector2(200, 50), 18);
        if (won && run is not null && Endless.CanDescend(run))
        {
            var deeper = Visuals.Button(run.Loop == 0 ? "Descend Deeper" : $"Descend to Loop {run.Loop + 2}", () => GameSession.DescendDeeper(GetTree()),
                                        new Vector2(220, 50), 18, "UI/icon_node_boss.png");
            deeper.TooltipText = $"Endless mode: keep this party (gear, relics, levels) and go round the vault again.\n" +
                                 $"A new map and region order; enemies get +{(int)(Endless.HpPerLoop * 100)}% HP and +{(int)(Endless.DamagePerLoop * 100)}% damage per loop.\n" +
                                 $"Your deepest descent: {p.EndlessBest} floors.";
            buttons.AddChild(deeper);
        }
        buttons.AddChild(again);
        buttons.AddChild(Visuals.Button("Main Menu", () => GameSession.ChangeScene(GetTree(), GameSession.MenuScene), new Vector2(200, 50), 18));
        again.CallDeferred(Control.MethodName.GrabFocus);

        Audio.Sfx(this, won ? "victory" : "defeat", 0f);
        Visuals.FadeIn(this, 1.0);
    }
}
