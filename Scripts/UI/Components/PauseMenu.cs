using Godot;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Esc menu: resume, settings, save &amp; quit to menu, abandon run. Pauses the tree while open.</summary>
public partial class PauseMenu : Control
{
    private readonly bool _inRun;
    private VBoxContainer _menu = null!;
    private SettingsPanel? _settings;
    private Godot.Button? _abandon;
    private bool _confirmAbandon;

    public PauseMenu() : this(false) { }

    public PauseMenu(bool inRun)
    {
        _inRun = inRun;
        ProcessMode = ProcessModeEnum.Always;
        Visible = false;
        MouseFilter = MouseFilterEnum.Stop;
    }

    public override void _Ready()
    {
        Visuals.CoverScreen(this);
        Visuals.Dimmer(this, 0.65f);
        var panel = Visuals.CenterPanel(this, 320);
        _menu = new VBoxContainer();
        _menu.AddThemeConstantOverride("separation", 10);
        panel.AddChild(_menu);

        var title = Visuals.Title("Paused", 34);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _menu.AddChild(title);
        _menu.AddChild(Visuals.Separator());
        _menu.AddChild(Visuals.Button("Resume", Close, new Vector2(280, 46)));
        _menu.AddChild(Visuals.Button("Settings", OpenSettings, new Vector2(280, 46)));
        if (_inRun)
        {
            _menu.AddChild(Visuals.Button("Save & Quit to Menu", () => GameSession.SaveAndQuitToMenu(GetTree()), new Vector2(280, 46)));
            _abandon = Visuals.Button("Abandon Run", Abandon, new Vector2(280, 46));
            _menu.AddChild(_abandon);
        }
        else
        {
            _menu.AddChild(Visuals.Button("Main Menu", () => GameSession.ChangeScene(GetTree(), GameSession.MenuScene), new Vector2(280, 46)));
        }
        _menu.AddChild(Visuals.Button("Quit Game", () => { GameSession.SaveRun(); GetTree().Quit(); }, new Vector2(280, 46)));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel")) return;
        GetViewport().SetInputAsHandled();
        if (_settings is not null) { CloseSettings(); return; }
        if (Visible) Close();
        else Open();
    }

    public void Open()
    {
        Visible = true;
        _confirmAbandon = false;
        if (_abandon is not null) _abandon.Text = "Abandon Run";
        GetTree().Paused = true;
        MoveToFront();
    }

    public void Close()
    {
        if (_settings is not null) CloseSettings();
        Visible = false;
        GetTree().Paused = false;
    }

    private void OpenSettings()
    {
        _menu.GetParent<Control>().Visible = false;
        _settings = new SettingsPanel(CloseSettings);
        AddChild(_settings);
    }

    private void CloseSettings()
    {
        _settings?.QueueFree();
        _settings = null;
        _menu.GetParent<Control>().Visible = true;
    }

    private void Abandon()
    {
        if (!_confirmAbandon)
        {
            _confirmAbandon = true;
            if (_abandon is not null) _abandon.Text = "Really abandon? Click again";
            return;
        }
        GameSession.EndRun(GetTree(), won: false);
    }
}
