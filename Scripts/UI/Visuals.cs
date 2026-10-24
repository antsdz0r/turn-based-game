using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Shared look for every screen: palette, fonts, theme, icons, backdrop and small widgets.</summary>
public static class Visuals
{
    public const string Art = "res://Art/";
    /// <summary>Units are drawn at 64x64 (bosses 80x80) and shown at x2.</summary>
    public const float SpriteScale = 2f;
    /// <summary>Props (torch, campfire) are still 8-16px art shown at x4.</summary>
    public const float PropScale = 4f;

    // Palette: charcoal, midnight blue, muted violet, ember orange, gold.
    public static readonly Color Charcoal = new("#15121a");
    public static readonly Color Midnight = new("#141a2e");
    public static readonly Color Violet = new("#3a2d4f");
    public static readonly Color Ember = new("#ff8a2c");
    public static readonly Color Gold = new("#d9b44a");
    public static readonly Color GoldBright = new("#ffd76b");
    public static readonly Color Text = new("#ece4d4");
    public static readonly Color TextDim = new("#a99f8f");
    public static readonly Color Ink = new(0.05f, 0.03f, 0.07f);
    public static readonly Color Good = new("#6be07a");
    public static readonly Color Bad = new("#ff6b6b");

    private static readonly Dictionary<string, Texture2D> Textures = new();
    private static Theme? _theme;
    private static Font? _serif;

    public static Texture2D Tex(string path)
    {
        if (!Textures.TryGetValue(path, out var texture))
        {
            texture = GD.Load<Texture2D>(Art + path);
            Textures[path] = texture;
        }
        return texture;
    }

    /// <summary>An elegant serif from the OS (Windows ships Constantia/Georgia), falling back to the default font.</summary>
    public static Font Serif => _serif ??= new SystemFont
    {
        FontNames = new[] { "Constantia", "Georgia", "Palatino Linotype", "Book Antiqua", "Cambria", "Times New Roman", "serif" },
        FontWeight = 600,
    };

    // ───────────── Styles ─────────────

    public static StyleBoxFlat Box(Color background, Color border, int borderWidth = 2, int radius = 4, int margin = 8)
    {
        var box = new StyleBoxFlat { BgColor = background, BorderColor = border, AntiAliasing = true };
        box.SetBorderWidthAll(borderWidth);
        box.SetCornerRadiusAll(radius);
        box.SetContentMarginAll(margin);
        return box;
    }

    /// <summary>A plain dark panel with a thin gold border (for panels that recolor their border).</summary>
    public static StyleBoxFlat PanelStyle(float alpha = 0.94f, int margin = 16)
    {
        var box = Box(new Color(0.043f, 0.035f, 0.063f, alpha), new Color("#8a6640"), 2, 8, margin);
        box.ShadowColor = new Color(0, 0, 0, 0.5f);
        box.ShadowSize = 10;
        return box;
    }

    /// <summary>The ornate frame: dark glass, double gold rule and diamond studs on the corners (9-slice texture).</summary>
    public static StyleBoxTexture FramedPanel(float alpha = 0.94f, int margin = 16)
    {
        var box = new StyleBoxTexture
        {
            Texture = Tex("UI/frame.png"),
            ModulateColor = new Color(1, 1, 1, Mathf.Clamp(alpha + 0.05f, 0, 1)),
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch,
        };
        box.SetTextureMarginAll(18);
        box.SetExpandMarginAll(6);   // the corner studs sit just outside the panel
        box.SetContentMarginAll(margin + 2);
        return box;
    }

    public static Theme Theme => _theme ??= BuildTheme();

    private static Theme BuildTheme()
    {
        var theme = new Theme();
        // Card-like buttons: violet glass, gold edge on hover.
        theme.SetStylebox("normal", "Button", Box(new Color("#1d1726"), new Color("#554a6d"), 2, 8, 8));
        theme.SetStylebox("hover", "Button", Box(new Color("#282033"), new Color("#ffb85a"), 2, 8, 8));
        theme.SetStylebox("pressed", "Button", Box(new Color("#3a2a18"), new Color("#ffe09a"), 2, 8, 8));
        theme.SetStylebox("hover_pressed", "Button", Box(new Color("#3a2a18"), new Color("#ffe09a"), 2, 8, 8));
        theme.SetStylebox("disabled", "Button", Box(new Color("#141019"), new Color("#282033"), 2, 8, 8));
        theme.SetStylebox("focus", "Button", Box(new Color(0, 0, 0, 0), new Color("#ffb85a", 0.7f), 2, 8, 8));
        theme.SetColor("font_color", "Button", Text);
        theme.SetColor("font_hover_color", "Button", Colors.White);
        theme.SetColor("font_focus_color", "Button", Colors.White);
        theme.SetColor("font_pressed_color", "Button", GoldBright);
        theme.SetColor("font_hover_pressed_color", "Button", GoldBright);
        theme.SetColor("font_disabled_color", "Button", new Color("#5e566a"));
        theme.SetColor("icon_disabled_color", "Button", new Color(1, 1, 1, 0.3f));
        theme.SetFontSize("font_size", "Button", 16);
        theme.SetConstant("icon_max_width", "Button", 24);

        theme.SetStylebox("panel", "PanelContainer", FramedPanel());
        theme.SetStylebox("panel", "TooltipPanel", Box(new Color("#0b0910"), new Color("#8a6640"), 2, 6, 10));
        theme.SetColor("font_color", "TooltipLabel", Text);
        theme.SetFontSize("font_size", "TooltipLabel", 14);
        theme.SetColor("font_color", "Label", Text);
        theme.SetColor("default_color", "RichTextLabel", Text);

        theme.SetStylebox("background", "ProgressBar", Box(new Color("#0b0910"), new Color("#000000"), 1, 2, 0));
        theme.SetStylebox("fill", "ProgressBar", Box(Good, new Color(0, 0, 0, 0), 0, 2, 0));

        theme.SetStylebox("slider", "HSlider", Box(new Color("#231e2c"), new Color("#4e4260"), 1, 3, 3));
        theme.SetStylebox("grabber_area", "HSlider", Box(Ember, new Color(0, 0, 0, 0), 0, 3, 3));
        theme.SetStylebox("grabber_area_highlight", "HSlider", Box(GoldBright, new Color(0, 0, 0, 0), 0, 3, 3));
        theme.SetColor("font_color", "CheckButton", Text);
        theme.SetColor("font_hover_color", "CheckButton", Colors.White);
        theme.SetStylebox("normal", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("hover", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("pressed", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("hover_pressed", "CheckButton", new StyleBoxEmpty());
        theme.SetStylebox("focus", "CheckButton", new StyleBoxEmpty());
        return theme;
    }

    // ───────────── Widgets ─────────────

    public static Godot.Label Label(string text, int size, Color color, bool serif = false, int outline = -1)
    {
        var label = new Godot.Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        int outlineSize = outline >= 0 ? outline : Math.Max(4, size / 5);
        if (outlineSize > 0)
        {
            label.AddThemeConstantOverride("outline_size", outlineSize);
            label.AddThemeColorOverride("font_outline_color", Ink);
        }
        if (serif) label.AddThemeFontOverride("font", Serif);
        return label;
    }

    public static Godot.Label Title(string text, int size = 40) => Label(text, size, GoldBright, serif: true, outline: Math.Max(6, size / 5));

    public static RichTextLabel Rich(string bbcode, int size = 15, bool fit = true)
    {
        var rich = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = fit,
            ScrollActive = !fit,
            Text = bbcode,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        rich.AddThemeFontSizeOverride("normal_font_size", size);
        rich.AddThemeFontSizeOverride("bold_font_size", size);
        return rich;
    }

    /// <summary>A themed button that plays a click and runs the action.</summary>
    public static Godot.Button Button(string text, Action onPressed, Vector2? minSize = null, int fontSize = 17, string? icon = null)
    {
        var button = new Godot.Button
        {
            Text = text,
            CustomMinimumSize = minSize ?? new Vector2(220, 46),
            FocusMode = Control.FocusModeEnum.All,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        };
        if (icon is not null)
        {
            button.Icon = Tex(icon);
            button.ExpandIcon = true;
        }
        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.Pressed += () =>
        {
            Audio.Sfx(button, "click", 0.05f);
            onPressed();
        };
        return button;
    }

    public static TextureRect Icon(string path, int size = 20, string? tooltip = null)
    {
        return new TextureRect
        {
            Texture = Tex(path),
            CustomMinimumSize = new Vector2(size, size),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            TooltipText = tooltip ?? "",
            MouseFilter = tooltip is null ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Stop,
        };
    }

    public static TextureRect Portrait(string heroId, int size = 64)
    {
        var rect = Icon($"Portraits/{heroId}.png", size);
        return rect;
    }

    public static Control Spacer(float height = 0, float width = 0) =>
        new() { CustomMinimumSize = new Vector2(width, height), MouseFilter = Control.MouseFilterEnum.Ignore };

    public static HSeparator Separator()
    {
        var sep = new HSeparator();
        sep.AddThemeStyleboxOverride("separator", Box(new Color(Gold, 0.35f), new Color(0, 0, 0, 0), 0, 0, 0));
        sep.AddThemeConstantOverride("separation", 10);
        return sep;
    }

    /// <summary>A centered PanelContainer for modal dialogs.</summary>
    public static PanelContainer CenterPanel(Control parent, float minWidth)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(minWidth, 0) };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        parent.AddChild(panel);
        FitToScreen(panel);
        return panel;
    }

    /// <summary>
    /// Keeps a centered panel fully on screen: if its content grows taller (or wider) than the window,
    /// it is scaled down around its center instead of being cut off.
    /// </summary>
    public static void FitToScreen(Control panel, float margin = 16)
    {
        void Fit()
        {
            if (!GodotObject.IsInstanceValid(panel) || !panel.IsInsideTree()) return;
            var view = panel.GetViewportRect().Size;
            // Panels only ever grow on their own: shrink back to the content's real size first.
            var min = panel.GetCombinedMinimumSize();
            if (min.X > 0 && min.Y > 0 && (panel.Size - min).Length() > 0.5f)
            {
                panel.Size = min;   // fires Resized → Fit again with the right size
                return;
            }
            var size = panel.Size;
            if (size.X <= 0 || size.Y <= 0) return;
            float s = Mathf.Min(1f, Mathf.Min((view.Y - margin * 2) / size.Y, (view.X - margin * 2) / size.X));
            panel.PivotOffset = size / 2;
            panel.Scale = new Vector2(s, s);
            // Center on the window itself, not on the parent (which may not be sized yet).
            panel.GlobalPosition = (view - size) / 2;
        }
        panel.Resized += Fit;
        // Wrapped text can report a huge height for a frame before it knows its width: re-fit whenever
        // the content's minimum size changes, and once more after layout has settled.
        panel.MinimumSizeChanged += Fit;
        Callable.From(Fit).CallDeferred();
        void Settle() => panel.GetTree().CreateTimer(0.05, true, false, true).Timeout += Fit;
        if (panel.IsInsideTree()) Settle();
        else panel.Ready += Settle;
        OnWindowResized(panel, Fit);
    }

    /// <summary>
    /// Makes a screen or overlay exactly cover the window, now and whenever the window changes size
    /// (fullscreen switches happen after a scene is built, and anchors alone don't always catch it).
    /// </summary>
    public static void CoverScreen(Control c)
    {
        void Apply()
        {
            if (!GodotObject.IsInstanceValid(c) || !c.IsInsideTree()) return;
            c.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
            c.GlobalPosition = Vector2.Zero;
            c.Size = c.GetViewportRect().Size;
        }
        Apply();
        Callable.From(Apply).CallDeferred();
        OnWindowResized(c, Apply);
    }

    /// <summary>Runs an action when the window resizes, for as long as the control lives.</summary>
    public static void OnWindowResized(Control c, Action action)
    {
        if (!c.IsInsideTree()) { c.Ready += () => OnWindowResized(c, action); return; }
        var viewport = c.GetViewport();
        viewport.SizeChanged += action;
        c.TreeExiting += () => { if (GodotObject.IsInstanceValid(viewport)) viewport.SizeChanged -= action; };
    }

    public static ColorRect Dimmer(Control parent, float alpha = 0.6f)
    {
        var dim = new ColorRect { Color = new Color(0, 0, 0, alpha), MouseFilter = Control.MouseFilterEnum.Stop };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(dim);
        return dim;
    }

    // ───────────── Game vocabulary ─────────────

    public static Color ElementColor(Element element) => element switch
    {
        Element.Fire => new Color("#ff7a3d"),
        Element.Ice => new Color("#7fd4ff"),
        Element.Lightning => new Color("#c9a0ff"),
        _ => new Color("#e6ecf4"),
    };

    public static string ElementIcon(Element element) => $"UI/icon_{element.ToString().ToLowerInvariant()}.png";

    public static string StatusIcon(StatusKind kind) => $"UI/icon_{kind.ToString().ToLowerInvariant()}.png";

    public static string ItemIcon(ItemDef item) => $"UI/icon_{item.Icon}.png";

    public static string NodeIcon(NodeType type) => $"UI/icon_node_{type.ToString().ToLowerInvariant()}.png";

    public static string AbilityIcon(AbilityDef def)
    {
        if (def.Heal > 0 || def.Applies.Any(a => a.Kind == StatusKind.Regen)) return "UI/icon_heal_ally.png";
        if (def.Protect) return "UI/icon_guarded.png";
        if (def.SwitchTargetRow) return "UI/icon_move.png";
        if (def.Applies.Any(a => a.Kind == StatusKind.Riposte)) return "UI/icon_riposte.png";
        if (def.Applies.Any(a => a.Kind == StatusKind.Taunt)) return "UI/icon_taunt.png";
        if (def.Shield > 0 && def.Power == 0) return "UI/icon_shield.png";
        if (def.Power == 0 && def.Applies.Any(a => a.Kind == StatusKind.Marked)) return "UI/icon_marked.png";
        if (def.Applies.Any(a => a.Kind == StatusKind.Poison)) return "UI/icon_poison.png";
        if (def.Applies.Any(a => a.Kind == StatusKind.Bleed)) return "UI/icon_bleed.png";
        if (def.ElementSequence is not null) return "UI/icon_empowered.png";
        return ElementIcon(def.Element);
    }

    public static string StatusName(StatusKind kind) => kind switch
    {
        StatusKind.Marked => "Marked",
        StatusKind.Weakened => "Weakened",
        StatusKind.Empowered => "Empowered",
        _ => kind.ToString(),
    };

    public static string StatusDescription(StatusEffect s) => s.Kind switch
    {
        StatusKind.Poison => $"Poison {s.Amount}: takes {s.Amount} damage at turn start, then -1.",
        StatusKind.Burn => $"Burn {s.Amount}: takes {s.Amount} damage at turn start ({s.Turns} turns).",
        StatusKind.Bleed => $"Bleed {s.Amount}: takes {s.Amount} damage at turn start, ignoring armor and shields ({s.Turns} turns).",
        StatusKind.Regen => $"Regen {s.Amount}: heals {s.Amount} at turn start ({s.Turns} turns).",
        StatusKind.Stun => "Stunned: skips the next turn.",
        StatusKind.Shield => $"Shield {s.Amount}: absorbs damage ({s.Turns} turns).",
        StatusKind.Marked => $"Marked: takes +25% damage; attackers get +20% crit, +10% accuracy ({s.Turns} turns).",
        StatusKind.Weakened => $"Weakened: deals -25% damage ({s.Turns} turns).",
        StatusKind.Empowered => $"Empowered: deals +25% damage ({s.Turns} turns).",
        StatusKind.Taunt => $"Taunting: enemies must target this hero ({s.Turns} turns).",
        StatusKind.Slowed => $"Slowed: -3 speed ({s.Turns} turns).",
        StatusKind.Defending => "Defending: takes -50% damage until its next turn.",
        StatusKind.Riposte => $"Riposte: counters melee attackers for {s.Amount}.",
        StatusKind.Guarded => "Protected: attacks hit the protector instead.",
        _ => s.Kind.ToString(),
    };

    public static string AffinityText(Combatant unit)
    {
        var parts = new List<string>();
        var weak = unit.Affinities.Where(a => a.Value == Affinity.Weak).Select(a => a.Key.ToString()).ToList();
        var resist = unit.Affinities.Where(a => a.Value == Affinity.Resist).Select(a => a.Key.ToString()).ToList();
        var immune = unit.Affinities.Where(a => a.Value == Affinity.Immune).Select(a => a.Key.ToString()).ToList();
        if (weak.Count > 0) parts.Add("Weak: " + string.Join(", ", weak));
        if (resist.Count > 0) parts.Add("Resists: " + string.Join(", ", resist));
        if (immune.Count > 0) parts.Add("Immune: " + string.Join(", ", immune));
        return parts.Count == 0 ? "No weaknesses" : string.Join("  ·  ", parts);
    }

    public static string Img(string path, int size = 16) => $"[img={size}x{size}]{Art}{path}[/img]";

    public static string FormatTime(double seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }

    // ───────────── Backdrop ─────────────

    public static Texture2D MakeSoftCircle(Color color) => new GradientTexture2D
    {
        Width = 64,
        Height = 64,
        Fill = GradientTexture2D.FillEnum.Radial,
        FillFrom = new Vector2(0.5f, 0.5f),
        FillTo = new Vector2(0.5f, 0f),
        Gradient = new Gradient
        {
            Colors = new[] { color, new Color(color.R, color.G, color.B, 0) },
            Offsets = new[] { 0f, 1f },
        },
    };

    public static TextureRect AddBackground(Control parent, Color tint, string texture = "Backgrounds/crypt.png")
    {
        var background = new TextureRect
        {
            Texture = Tex(texture),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = tint,
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(background);

        // Midnight-blue wash from the top for depth.
        var wash = new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Width = 4,
                Height = 64,
                FillFrom = new Vector2(0, 0),
                FillTo = new Vector2(0, 1),
                Gradient = new Gradient
                {
                    Colors = new[] { new Color(Midnight, 0.65f), new Color(Midnight, 0f), new Color(0, 0, 0, 0.45f) },
                    Offsets = new[] { 0f, 0.55f, 1f },
                },
            },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        wash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(wash);
        return background;
    }

    /// <summary>Wall torch with a particle flame and a glow (returned, to flicker it every frame).</summary>
    public static Sprite2D AddTorch(Control parent, float anchorX, float anchorY)
    {
        var anchor = AnchorPoint(parent, anchorX, anchorY);
        var glow = Glow(new Color("#ff9a3c"), 4f, new Vector2(0, -24));
        anchor.AddChild(glow);
        anchor.AddChild(new Sprite2D
        {
            Texture = Tex("Props/torch.png"),
            Scale = new Vector2(PropScale, PropScale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        });
        anchor.AddChild(Flame(new Vector2(0, -34), 28, 4));
        return glow;
    }

    public static Sprite2D AddCampfire(Control parent, float anchorX, float anchorY)
    {
        var anchor = AnchorPoint(parent, anchorX, anchorY);
        var glow = Glow(new Color("#ff8a2c"), 9f, new Vector2(0, -20));
        anchor.AddChild(glow);
        anchor.AddChild(new Sprite2D
        {
            Texture = Tex("Props/campfire.png"),
            Scale = new Vector2(PropScale, PropScale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        });
        var flame = Flame(new Vector2(0, -10), 60, 22);
        flame.ScaleAmountMin = 6;
        flame.ScaleAmountMax = 12;
        flame.Lifetime = 0.8;
        anchor.AddChild(flame);
        return glow;
    }

    public static void Flicker(List<Sprite2D> glows, double time)
    {
        for (int i = 0; i < glows.Count; i++)
        {
            if (!GodotObject.IsInstanceValid(glows[i])) continue;
            float flicker = 0.55f + 0.08f * Mathf.Sin((float)time * 9f + i * 2f) + 0.06f * GD.Randf();
            glows[i].Modulate = new Color(1, 1, 1, flicker);
        }
    }

    /// <summary>Slow-drifting dust and rising embers across the whole screen.</summary>
    public static void AddAtmosphere(Control parent, bool embers = true)
    {
        var size = parent.GetViewportRect().Size;
        var dust = new CpuParticles2D
        {
            Amount = 36,
            Lifetime = 9,
            Preprocess = 9,
            Position = size / 2,
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = size / 2,
            Direction = Vector2.Up,
            Spread = 180,
            InitialVelocityMin = 3,
            InitialVelocityMax = 10,
            Gravity = new Vector2(0, -4),
            ScaleAmountMin = 2,
            ScaleAmountMax = 3,
            Color = new Color(1f, 0.9f, 0.7f, 0.22f),
        };
        parent.AddChild(dust);
        if (!embers) return;
        var ember = new CpuParticles2D
        {
            Amount = 24,
            Lifetime = 7,
            Preprocess = 7,
            Position = new Vector2(size.X / 2, size.Y + 10),
            EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
            EmissionRectExtents = new Vector2(size.X / 2, 4),
            Direction = Vector2.Up,
            Spread = 20,
            InitialVelocityMin = 25,
            InitialVelocityMax = 60,
            Gravity = new Vector2(6, -6),
            ScaleAmountMin = 2,
            ScaleAmountMax = 4,
            ColorRamp = new Gradient
            {
                Colors = new[] { new Color("#ffd36a"), new Color("#ff8a2c"), new Color(1f, 0.3f, 0.1f, 0f) },
                Offsets = new[] { 0f, 0.5f, 1f },
            },
        };
        parent.AddChild(ember);
    }

    /// <summary>Extra particles that give each region its own feel: rising bubbles under water, a storm of embers in the Cinder Throne.</summary>
    public static void AddRegionAmbience(Control parent, string region)
    {
        var size = parent.GetViewportRect().Size;
        // Two layers of fog drifting along the floor seam, at different speeds.
        Color fogTint = region switch
        {
            "drowned" => new Color(0.6f, 1f, 1f, 0.5f),
            "cinder" => new Color(1f, 0.55f, 0.4f, 0.35f),
            "frost" => new Color(0.9f, 0.97f, 1f, 0.65f),
            "storm" => new Color(0.6f, 0.62f, 0.8f, 0.5f),
            _ => new Color(0.85f, 0.85f, 1f, 0.45f),
        };
        for (int layer = 0; layer < 2; layer++)
        {
            var fog = new TextureRect
            {
                Texture = Tex("FX/fog.png"),
                StretchMode = TextureRect.StretchModeEnum.Tile,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Modulate = fogTint * new Color(1, 1, 1, layer == 0 ? 1f : 0.6f),
                Size = new Vector2(size.X + 512, 128),
                Position = new Vector2(-512 * layer * 0.5f, size.Y * 0.5f - 40 + layer * 28),
                TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            };
            parent.AddChild(fog);
            float start = fog.Position.X;
            var drift = fog.CreateTween().SetLoops();
            drift.TweenProperty(fog, "position:x", start - 512, layer == 0 ? 40.0 : 26.0).From(start);
        }
        if (region == "frost")
        {
            var snow = new CpuParticles2D
            {
                Amount = 90, Lifetime = 9, Preprocess = 9,
                Position = new Vector2(size.X / 2, -10),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                EmissionRectExtents = new Vector2(size.X / 2 + 60, 4),
                Direction = new Vector2(0.25f, 1), Spread = 15,
                InitialVelocityMin = 20, InitialVelocityMax = 50,
                Gravity = new Vector2(4, 6),
                ScaleAmountMin = 2, ScaleAmountMax = 4,
                Color = new Color(0.92f, 0.96f, 1f, 0.8f),
            };
            parent.AddChild(snow);
        }
        else if (region == "storm")
        {
            var rain = new CpuParticles2D
            {
                Amount = 140, Lifetime = 0.9, Preprocess = 1,
                Position = new Vector2(size.X / 2, -20),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                EmissionRectExtents = new Vector2(size.X / 2 + 100, 4),
                Direction = new Vector2(-0.25f, 1), Spread = 3,
                InitialVelocityMin = 700, InitialVelocityMax = 900,
                Gravity = Vector2.Zero,
                ScaleAmountMin = 1, ScaleAmountMax = 2,
                Color = new Color(0.6f, 0.7f, 0.9f, 0.45f),
            };
            parent.AddChild(rain);
            // Lightning: the whole screen flashes every few seconds.
            var flash = new ColorRect { Color = new Color(0.85f, 0.9f, 1f, 0f), MouseFilter = Control.MouseFilterEnum.Ignore };
            flash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            parent.AddChild(flash);
            var storm = flash.CreateTween().SetLoops();
            storm.TweenInterval(4.5);
            storm.TweenProperty(flash, "color:a", 0.35f, 0.04);
            storm.TweenProperty(flash, "color:a", 0.05f, 0.08);
            storm.TweenProperty(flash, "color:a", 0.25f, 0.05);
            storm.TweenProperty(flash, "color:a", 0f, 0.35);
            storm.TweenInterval(2.7);
            storm.TweenProperty(flash, "color:a", 0.2f, 0.05);
            storm.TweenProperty(flash, "color:a", 0f, 0.3);
        }
        else if (region == "drowned")
        {
            var bubbles = new CpuParticles2D
            {
                Amount = 30,
                Lifetime = 8,
                Preprocess = 8,
                Position = new Vector2(size.X / 2, size.Y + 10),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                EmissionRectExtents = new Vector2(size.X / 2, 4),
                Direction = Vector2.Up,
                Spread = 8,
                InitialVelocityMin = 30,
                InitialVelocityMax = 70,
                Gravity = new Vector2(0, -8),
                ScaleAmountMin = 0.08f,
                ScaleAmountMax = 0.22f,
                Texture = Tex("FX/circle.png"),
                Color = new Color(0.6f, 0.9f, 1f, 0.35f),
            };
            parent.AddChild(bubbles);
            // A slow, dark-teal haze that breathes.
            var haze = new ColorRect { Color = new Color(0.05f, 0.25f, 0.3f, 0.0f), MouseFilter = Control.MouseFilterEnum.Ignore };
            haze.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            parent.AddChild(haze);
            var tween = haze.CreateTween().SetLoops();
            tween.TweenProperty(haze, "color:a", 0.14f, 3.5).SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(haze, "color:a", 0.02f, 3.5).SetTrans(Tween.TransitionType.Sine);
        }
        else if (region == "cinder")
        {
            var storm = new CpuParticles2D
            {
                Amount = 60,
                Lifetime = 4,
                Preprocess = 4,
                Position = new Vector2(size.X / 2, size.Y + 10),
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
                EmissionRectExtents = new Vector2(size.X / 2, 4),
                Direction = new Vector2(0.3f, -1),
                Spread = 25,
                InitialVelocityMin = 70,
                InitialVelocityMax = 160,
                Gravity = new Vector2(20, -10),
                ScaleAmountMin = 2,
                ScaleAmountMax = 5,
                ColorRamp = new Gradient
                {
                    Colors = new[] { new Color("#fff0a0"), new Color("#ff6a1c"), new Color(0.6f, 0.1f, 0.05f, 0f) },
                    Offsets = new[] { 0f, 0.4f, 1f },
                },
            };
            parent.AddChild(storm);
            var heat = new ColorRect { Color = new Color(1f, 0.3f, 0.05f, 0.0f), MouseFilter = Control.MouseFilterEnum.Ignore };
            heat.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            parent.AddChild(heat);
            var tween = heat.CreateTween().SetLoops();
            tween.TweenProperty(heat, "color:a", 0.09f, 1.6).SetTrans(Tween.TransitionType.Sine);
            tween.TweenProperty(heat, "color:a", 0.02f, 1.6).SetTrans(Tween.TransitionType.Sine);
        }
    }

    /// <summary>A unit sprite with its feet at the origin (menus, story, camp).</summary>
    public static Sprite2D UnitSprite(string sprite, float scale = SpriteScale, bool breathe = true, bool flip = false)
    {
        var texture = Tex($"Units/{sprite}.png");
        var node = new Sprite2D
        {
            Texture = texture,
            Offset = new Vector2(0, -texture.GetHeight() / 2f),
            Scale = new Vector2(scale, scale),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            FlipH = flip,
        };
        if (breathe)
        {
            var idle = node.CreateTween().SetLoops();
            double breath = 0.7 + GD.Randf() * 0.3;
            idle.TweenProperty(node, "scale", new Vector2(scale, scale * 1.04f), breath)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            idle.TweenProperty(node, "scale", new Vector2(scale, scale), breath)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
        return node;
    }

    public static Control AnchorPoint(Control parent, float anchorX, float anchorY)
    {
        var anchor = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        anchor.SetAnchor(Side.Left, anchorX);
        anchor.SetAnchor(Side.Right, anchorX);
        anchor.SetAnchor(Side.Top, anchorY);
        anchor.SetAnchor(Side.Bottom, anchorY);
        parent.AddChild(anchor);
        return anchor;
    }

    public static void FadeIn(Control parent, double duration = 0.5)
    {
        var black = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
        black.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(black);
        var t = black.CreateTween();
        t.TweenProperty(black, "modulate:a", 0f, duration);
        t.TweenCallback(Callable.From(black.QueueFree));
    }

    private static Sprite2D Glow(Color color, float scale, Vector2 position) => new()
    {
        Texture = MakeSoftCircle(color),
        Scale = new Vector2(scale, scale),
        Position = position,
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
    };

    private static CpuParticles2D Flame(Vector2 position, int amount, float radius) => new()
    {
        Amount = amount,
        Lifetime = 0.55,
        Position = position,
        Direction = Vector2.Up,
        Spread = 12,
        InitialVelocityMin = 20,
        InitialVelocityMax = 50,
        Gravity = new Vector2(0, -90),
        ScaleAmountMin = 4,
        ScaleAmountMax = 8,
        EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere,
        EmissionSphereRadius = radius,
        ColorRamp = new Gradient
        {
            Colors = new[] { new Color("#fff2a8"), new Color("#ffb03a"), new Color("#e0442a"), new Color(0.5f, 0.1f, 0.05f, 0) },
            Offsets = new[] { 0f, 0.3f, 0.7f, 1f },
        },
    };
}
