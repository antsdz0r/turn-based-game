using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Hero mastery: every hero's XP, what each level unlocks, the costume (shown side by side) and the
/// alternative ability. Loadout choices made here are the same ones the hero select screen uses.</summary>
public partial class MasteryScene : Screen
{
    private VBoxContainer _list = null!;

    protected override void Build()
    {
        Audio.Music(this, "camp");
        AddRegionBackdrop(Database.Region(0), 0.45f, torches: true);
        Glows.Add(Visuals.AddCampfire(this, 0.5f, 0.97f));

        var panel = Visuals.CenterPanel(this, 1000);
        var root = new VBoxContainer { CustomMinimumSize = new Vector2(980, 600) };
        root.AddThemeConstantOverride("separation", 8);
        panel.AddChild(root);
        root.AddChild(Visuals.Title("Hero Mastery", 34));
        root.AddChild(Visuals.Rich("[color=#a99f8f]Heroes earn mastery XP in every run they join (more for depth, bosses and victories). " +
                                   $"[color=#ffd76b]★ Adept ({Mastery.Thresholds[0]})[/color]: a new costume.  " +
                                   $"[color=#ffd76b]★★ Veteran ({Mastery.Thresholds[1]})[/color]: an alternative ability.  " +
                                   $"[color=#ffd76b]★★★ Mastered ({Mastery.Thresholds[2]})[/color]: start every run with an upgraded basic attack (+{Mastery.MasteredShards} shards).[/color]", 13));

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
        else base._UnhandledInput(@event);
    }

    private void Fill()
    {
        foreach (var child in _list.GetChildren()) child.QueueFree();
        var p = GameSession.Profile;
        foreach (var def in Database.Heroes)
        {
            bool unlocked = Meta.IsHeroUnlocked(p, def.Id);
            int level = Mastery.Level(p, def.Id), xp = Mastery.Xp(p, def.Id), next = Mastery.NextThreshold(p, def.Id);
            int prev = level == 0 ? 0 : Mastery.Thresholds[level - 1];
            var costume = Mastery.Costume(def.Id);
            var alt = Mastery.Alt(def.Id);

            var card = new PanelContainer();
            card.AddThemeStyleboxOverride("panel", Visuals.Box(new Color("#1a1520"), level >= Mastery.MaxLevel ? Visuals.GoldBright : new Color("#3a3147"),
                                                               level >= Mastery.MaxLevel ? 2 : 1, 6, 10));
            _list.AddChild(card);
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 14);
            card.AddChild(row);

            // Default look and costume, side by side (the costume is a silhouette until unlocked).
            var looks = new HBoxContainer();
            looks.AddThemeConstantOverride("separation", 4);
            var basic = Visuals.Icon($"Portraits/{def.Id}.png", 72);
            var dressed = Visuals.Icon($"Portraits/{costume?.Sprite ?? def.Id}.png", 72);
            if (!unlocked) basic.Modulate = new Color(0.3f, 0.3f, 0.35f);
            if (!Mastery.HasCostume(p, def.Id)) dressed.Modulate = new Color(0, 0, 0, 0.8f);
            looks.AddChild(basic);
            looks.AddChild(dressed);
            row.AddChild(looks);

            string stars = $"[color=#ffd76b]{new string('★', level)}[/color][color=#4a4058]{new string('☆', Mastery.MaxLevel - level)}[/color]";
            string bar = next > 0 ? Bar(xp - prev, next - prev) + $"  [color=#a99f8f]{xp}/{next} XP[/color]" : "[color=#ffd76b]Mastered[/color]";
            string costumeText = costume is null ? "" : Mastery.HasCostume(p, def.Id)
                ? $"[color=#7fd4a0]Costume: {costume.Name}[/color] [color=#a99f8f]— {costume.Description}[/color]"
                : $"[color=#5e566a]Costume (Adept): {costume.Name}[/color]";
            string altText = alt is null ? "" : Mastery.HasAlt(p, def.Id)
                ? $"[color=#7fd4a0]Alternative: {Database.Ability(alt.AbilityId).Name}[/color] [color=#a99f8f]instead of {Database.Ability(alt.Replaces).Name} — {Database.Ability(alt.AbilityId).Description}[/color]"
                : $"[color=#5e566a]Alternative ability (Veteran): {Database.Ability(alt.AbilityId).Name}, instead of {Database.Ability(alt.Replaces).Name}[/color]";
            var text = Visuals.Rich($"[font_size=18][color=#ffd76b]{def.Name}[/color][/font_size]  [color=#a99f8f]{def.Role}[/color]  {stars}  [color=#9fd8ff]{Mastery.LevelName(level)}[/color]\n" +
                                    $"{bar}\n{costumeText}\n{altText}" + (unlocked ? "" : "\n[color=#ffa040]Hero locked: unlock them first (Ember Shrine).[/color]"), 13);
            text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(text);

            // Loadout toggles (also on the hero select screen).
            var toggles = new VBoxContainer { CustomMinimumSize = new Vector2(170, 0), Alignment = BoxContainer.AlignmentMode.Center };
            toggles.AddThemeConstantOverride("separation", 6);
            row.AddChild(toggles);
            if (Mastery.HasCostume(p, def.Id))
            {
                var wear = Visuals.Button(p.CostumeOn.Contains(def.Id) ? "Wearing costume" : "Wear costume", () =>
                {
                    if (!p.CostumeOn.Remove(def.Id)) p.CostumeOn.Add(def.Id);
                    GameSession.SaveProfile();
                    Fill();
                }, new Vector2(170, 34), 13);
                toggles.AddChild(wear);
            }
            if (Mastery.HasAlt(p, def.Id) && alt is not null)
            {
                var use = Visuals.Button(p.AltOn.Contains(def.Id) ? $"Using {Database.Ability(alt.AbilityId).Name}" : $"Use {Database.Ability(alt.AbilityId).Name}", () =>
                {
                    if (!p.AltOn.Remove(def.Id)) p.AltOn.Add(def.Id);
                    GameSession.SaveProfile();
                    Fill();
                }, new Vector2(170, 34), 13);
                toggles.AddChild(use);
            }
        }
    }

    private static string Bar(int value, int goal)
    {
        int filled = (int)System.Math.Round(14.0 * value / System.Math.Max(1, goal));
        filled = System.Math.Clamp(filled, 0, 14);
        return $"[color=#9fd8ff]{new string('▰', filled)}[/color][color=#4a4058]{new string('▱', 14 - filled)}[/color]";
    }
}
