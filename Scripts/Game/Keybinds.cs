using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TBRPG.Game;

/// <summary>Rebindable keys (Settings → Controls). Stored in Settings.Keys as Godot keycodes.</summary>
public static class Keybinds
{
    public static readonly IReadOnlyList<(string Id, string Label, Key Default)> Actions = new List<(string, string, Key)>
    {
        ("parry", "Parry (timed defense)", Key.Space),
        ("confirm", "Confirm target", Key.Enter),
        ("next_target", "Next target", Key.Tab),
        ("speed", "Battle speed", Key.S),
        ("action_1", "Action 1", Key.Key1),
        ("action_2", "Action 2", Key.Key2),
        ("action_3", "Action 3", Key.Key3),
        ("action_4", "Action 4", Key.Key4),
        ("action_5", "Action 5", Key.Key5),
        ("action_6", "Action 6", Key.Key6),
        ("action_7", "Action 7", Key.Key7),
        ("action_8", "Action 8", Key.Key8),
        ("action_9", "Action 9", Key.Key9),
    };

    public static Key Get(string action)
    {
        if (GameSession.Settings.Keys.TryGetValue(action, out long code)) return (Key)code;
        return Actions.FirstOrDefault(a => a.Id == action).Default;
    }

    public static bool Is(InputEventKey key, string action)
    {
        var bound = Get(action);
        if (key.Keycode == bound) return true;
        return action == "confirm" && bound == Key.Enter && key.Keycode == Key.KpEnter;
    }

    /// <summary>The action number (0-8) a key triggers, or -1.</summary>
    public static int ActionIndex(InputEventKey key)
    {
        for (int i = 0; i < 9; i++) if (key.Keycode == Get($"action_{i + 1}")) return i;
        return -1;
    }

    public static string Name(string action) => OS.GetKeycodeString(Get(action)).ToUpperInvariant();

    public static void Bind(string action, Key key)
    {
        // A key does one thing: steal it from whatever action had it.
        foreach (var (id, _, _) in Actions)
            if (id != action && Get(id) == key) GameSession.Settings.Keys[id] = (long)Get(action);
        GameSession.Settings.Keys[action] = (long)key;
        GameSession.SaveSettings();
    }

    public static void Reset()
    {
        GameSession.Settings.Keys.Clear();
        GameSession.SaveSettings();
    }
}
