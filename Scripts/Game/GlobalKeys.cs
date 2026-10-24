using Godot;

namespace TBRPG.Game;

/// <summary>App-wide hotkeys that work on every screen: F11 or Alt+Enter toggles fullscreen.</summary>
public partial class GlobalKeys : Node
{
    private static GlobalKeys? _instance;

    public static void Ensure(Node from)
    {
        if (_instance is not null && IsInstanceValid(_instance)) return;
        _instance = new GlobalKeys { ProcessMode = ProcessModeEnum.Always, Name = "GlobalKeys" };
        from.GetTree().Root.CallDeferred(Node.MethodName.AddChild, _instance);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.F11 || (key.Keycode == Key.Enter && key.AltPressed))
        {
            GameSession.ToggleFullscreen();
            GetViewport().SetInputAsHandled();
        }
    }
}
