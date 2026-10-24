using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;

namespace TBRPG.Game;

/// <summary>
/// The current run, persistent profile, settings and scene flow. Static, so it survives scene changes.
/// Files: user://run.json (current run), user://profile.json (stats), user://settings.json.
/// </summary>
public static class GameSession
{
    private const string RunPath = "user://run.json";
    private const string ProfilePath = "user://profile.json";
    private const string SettingsPath = "user://settings.json";

    public const string MenuScene = "res://Scenes/MainMenu.tscn";
    public const string HeroSelectScene = "res://Scenes/HeroSelect.tscn";
    public const string MapScene = "res://Scenes/Map.tscn";
    public const string BattleScene = "res://Scenes/Battle.tscn";
    public const string RewardScene = "res://Scenes/Reward.tscn";
    public const string ShopScene = "res://Scenes/Shop.tscn";
    public const string RestScene = "res://Scenes/Rest.tscn";
    public const string EventScene = "res://Scenes/Event.tscn";
    public const string EndScene = "res://Scenes/End.tscn";
    public const string HowToPlayScene = "res://Scenes/HowToPlay.tscn";
    public const string CompendiumScene = "res://Scenes/Compendium.tscn";
    public const string CutsceneScene = "res://Scenes/Cutscene.tscn";
    public const string ShrineScene = "res://Scenes/EmberShrine.tscn";

    public enum AfterCutscene { Map, Node, End }

    /// <summary>The cutscene to play next and where to go when it ends.</summary>
    public static string? PendingCutscene { get; private set; }
    public static AfterCutscene CutsceneNext { get; private set; }
    /// <summary>Run used for names in cutscenes (the ending plays after the run is closed).</summary>
    public static RunState? StoryRun => Run ?? EndedRun;

    private static bool _initialized;

    public static RunState? Run { get; private set; }
    public static Profile Profile { get; private set; } = new();
    public static Settings Settings { get; private set; } = new();

    /// <summary>Result of the last won battle (shown on the reward screen).</summary>
    public static BattleResult? LastResult { get; set; }
    /// <summary>The run that just ended (for the end screen).</summary>
    public static RunState? EndedRun { get; private set; }
    public static bool EndedWon { get; private set; }

    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        if (Godot.FileAccess.FileExists(ProfilePath))
            Profile = Profile.FromJson(Godot.FileAccess.GetFileAsString(ProfilePath));
        if (Godot.FileAccess.FileExists(SettingsPath))
            Settings = Settings.FromJson(Godot.FileAccess.GetFileAsString(SettingsPath));
        ApplyDisplaySettings();
    }

    // ═════════════════════════ Runs ═════════════════════════

    public static bool HasSavedRun => Godot.FileAccess.FileExists(RunPath) && LoadRunFile() is not null;

    public static RunState? LoadRunFile() =>
        Godot.FileAccess.FileExists(RunPath) ? RunState.FromJson(Godot.FileAccess.GetFileAsString(RunPath)) : null;

    public static void StartRun(SceneTree tree, IReadOnlyList<(string id, Row row)> heroes, int heat = 0)
    {
        int seed = (int)(Time.GetTicksUsec() % int.MaxValue) ^ (int)GD.Randi();
        Run = RunState.NewRun(heroes, Math.Abs(seed), Math.Clamp(heat, 0, Profile.MaxHeatUnlocked));
        Meta.ApplyStartBonuses(Run, Profile);
        Profile.RunsStarted++;
        SaveProfile();
        SaveRun();
        ChangeScene(tree, MapScene);
    }

    /// <summary>Today's daily challenge: fixed seed, party and modifier.</summary>
    public static void StartDaily(SceneTree tree)
    {
        Run = Daily.Create(Daily.Today);
        Profile.RunsStarted++;
        SaveProfile();
        SaveRun();
        ChangeScene(tree, MapScene);
    }

    public static bool ContinueRun(SceneTree tree)
    {
        var run = LoadRunFile();
        if (run is null) return false;
        Run = run;
        if (!run.NodeCompleted && run.Node is not null) GoToNode(tree);
        else ChangeScene(tree, MapScene);
        return true;
    }

    /// <summary>Lets a scene run on its own from the editor (F6) by creating a throwaway run.</summary>
    public static RunState EnsureRun()
    {
        if (Run is not null) return Run;
        Run = RunState.NewRun(new List<(string, Row)> { ("vanguard", Row.Front), ("arcanist", Row.Back), ("warden", Row.Back) },
                              (int)(GD.Randi() % 100000));
        var first = Run.Map.OnFloor(0).First();
        Run.Enter(first.Id);
        return Run;
    }

    public static void EnterNode(SceneTree tree, int nodeId)
    {
        if (Run is null || !Run.Enter(nodeId)) return;
        var node = Run.Node!;
        if (node.Golden && node.Type == NodeType.Event)
        {
            Run.Gold += 30;
            Run.Stats.GoldEarned += 30;
        }
        SaveRun();
        string bossScene = Story.BossIntroId(node.Region);
        if (node.Type == NodeType.Boss && !Run.SeenScenes.Contains(bossScene))
        {
            PlayCutscene(tree, bossScene, AfterCutscene.Node);
            return;
        }
        GoToNode(tree);
    }

    public static void PlayCutscene(SceneTree tree, string id, AfterCutscene next)
    {
        if (Run is not null && !Run.SeenScenes.Contains(id))
        {
            Run.SeenScenes.Add(id);
            SaveRun();
        }
        PendingCutscene = id;
        CutsceneNext = next;
        ChangeScene(tree, CutsceneScene);
    }

    public static void FinishCutscene(SceneTree tree)
    {
        PendingCutscene = null;
        switch (CutsceneNext)
        {
            case AfterCutscene.Node: SaveRun(); GoToNode(tree); break;
            case AfterCutscene.End: ChangeScene(tree, EndScene); break;
            default: SaveRun(); ChangeScene(tree, Run is null ? MenuScene : MapScene); break;
        }
    }

    private static void GoToNode(SceneTree tree)
    {
        var node = Run?.Node;
        if (node is null) { ChangeScene(tree, MapScene); return; }
        string scene = node.Type switch
        {
            NodeType.Battle or NodeType.Elite or NodeType.Boss => BattleScene,
            NodeType.Shop => ShopScene,
            NodeType.Rest => RestScene,
            _ => EventScene,
        };
        ChangeScene(tree, scene);
    }

    /// <summary>Marks the current node done, saves, and returns to the map.</summary>
    public static void CompleteNode(SceneTree tree)
    {
        if (Run is null) { ChangeScene(tree, MenuScene); return; }
        Run.CompleteNode();
        SaveRun();
        ChangeScene(tree, MapScene);
    }

    public static void BattleWon(SceneTree tree, Battle battle)
    {
        if (Run is null) return;
        bool boss = Run.Node?.Type == NodeType.Boss && DungeonMap.IsFinalRegion(Run.Node.Region);
        foreach (var e in battle.Enemies.Where(e => e.IsBoss)) Profile.RecordBoss(e.DefId);
        SaveProfile();
        LastResult = Run.ApplyVictory(battle);
        Run.CompleteNode(); // rewards are applied: never replay this battle on Continue
        if (boss)
        {
            EndRun(tree, won: true);
            return;
        }
        SaveRun();
        ChangeScene(tree, RewardScene);
    }

    public static void BattleLost(SceneTree tree, Battle battle)
    {
        if (Run is null) return;
        Run.Stats.EnemiesSlain += battle.EnemiesSlain;
        Run.Stats.DamageDealt += battle.DamageDealt;
        EndRun(tree, won: false);
    }

    public static void EndRun(SceneTree tree, bool won)
    {
        if (Run is null) return;
        Profile.RecordRunEnd(Run, won);
        SaveProfile();
        EndedRun = Run;
        EndedWon = won;
        Run = null;
        DeleteRun();
        if (won) PlayCutscene(tree, Story.EndingId, AfterCutscene.End);
        else ChangeScene(tree, EndScene);
    }

    /// <summary>Save and return to the main menu (the run can be continued).</summary>
    public static void SaveAndQuitToMenu(SceneTree tree)
    {
        SaveRun();
        Run = null;
        ChangeScene(tree, MenuScene);
    }

    public static void ChangeScene(SceneTree tree, string path)
    {
        tree.Paused = false;
        Engine.TimeScale = 1.0;
        tree.ChangeSceneToFile(path);
    }

    // ═════════════════════════ Files ═════════════════════════

    public static void SaveRun()
    {
        if (Run is null) return;
        WriteFile(RunPath, Run.ToJson());
    }

    public static void DeleteRun()
    {
        if (Godot.FileAccess.FileExists(RunPath)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(RunPath));
    }

    public static void SaveProfile() => WriteFile(ProfilePath, Profile.ToJson());

    public static void SaveSettings() => WriteFile(SettingsPath, Settings.ToJson());

    private static void WriteFile(string path, string content)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        if (file is null)
        {
            GD.PushWarning($"Could not write {path}: {Godot.FileAccess.GetOpenError()}");
            return;
        }
        file.StoreString(content);
    }

    public static void ApplyDisplaySettings()
    {
        var mode = Settings.Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
        if (DisplayServer.WindowGetMode() != mode) DisplayServer.WindowSetMode(mode);
    }

    public static void ToggleFullscreen()
    {
        Settings.Fullscreen = !Settings.Fullscreen;
        ApplyDisplaySettings();
        SaveSettings();
    }
}
