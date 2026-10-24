using Godot;
using TBRPG.Core;

namespace TBRPG.UI;

/// <summary>Per-region lighting for battles: sprite ambient and rim light, background grade, light shafts.</summary>
public sealed record RegionLight(Color Ambient, Color Rim, Vector3 Shadow, Vector3 Highlight, Color Shaft, Color Bounce);

/// <summary>
/// The battle "look": colour-graded background, light shafts and a warm floor bounce behind the units, and
/// a foreground of silhouettes plus a vignette in front of them. Units are lit by their region (unit.gdshader).
/// </summary>
public static class BattleLook
{
    public static RegionLight For(string regionKey) => regionKey switch
    {
        "drowned" => new(new Color(0.78f, 0.9f, 0.95f), new Color("#6fd0c0"), new Vector3(0f, 0.04f, 0.08f), new Vector3(0.0f, 0.06f, 0.06f),
                         new Color(0.45f, 0.9f, 0.85f, 0.16f), new Color(0.3f, 0.8f, 0.75f, 0.10f)),
        "frost" => new(new Color(0.86f, 0.92f, 1f), new Color("#d8f4ff"), new Vector3(0.02f, 0.04f, 0.1f), new Vector3(0.05f, 0.07f, 0.1f),
                       new Color(0.85f, 0.95f, 1f, 0.18f), new Color(0.75f, 0.9f, 1f, 0.10f)),
        "storm" => new(new Color(0.82f, 0.82f, 0.96f), new Color("#d9c8ff"), new Vector3(0.04f, 0.02f, 0.12f), new Vector3(0.06f, 0.05f, 0.1f),
                       new Color(0.8f, 0.75f, 1f, 0.14f), new Color(0.7f, 0.65f, 1f, 0.08f)),
        "cinder" => new(new Color(1f, 0.86f, 0.8f), new Color("#ff8a3c"), new Vector3(0.08f, 0.0f, 0.02f), new Vector3(0.14f, 0.05f, -0.02f),
                        new Color(1f, 0.6f, 0.3f, 0.16f), new Color(1f, 0.5f, 0.2f, 0.16f)),
        "garden" => new(new Color(0.84f, 0.95f, 0.84f), new Color("#c8f08a"), new Vector3(0.02f, 0.06f, 0.04f), new Vector3(0.06f, 0.08f, 0.0f),
                        new Color(0.7f, 1f, 0.8f, 0.16f), new Color(0.6f, 0.9f, 0.4f, 0.10f)),
        "archive" => new(new Color(0.94f, 0.86f, 0.96f), new Color("#e0a0ff"), new Vector3(0.06f, 0.02f, 0.1f), new Vector3(0.12f, 0.07f, 0.0f),
                         new Color(0.95f, 0.7f, 1f, 0.15f), new Color(1f, 0.75f, 0.45f, 0.12f)),
        _ => new(new Color(0.86f, 0.86f, 0.98f), new Color("#ffa050"), new Vector3(0.05f, 0f, 0.12f), new Vector3(0.10f, 0.04f, -0.02f),
                 new Color(0.75f, 0.78f, 1f, 0.16f), new Color(1f, 0.62f, 0.3f, 0.12f)),
    };

    private static CanvasItemMaterial Additive() => new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };

    /// <summary>Grades the background and adds light shafts plus a floor bounce. Call before the units are added.</summary>
    public static void AddBehindUnits(Control parent, TextureRect? background, string regionKey)
    {
        var light = For(regionKey);
        var size = parent.GetViewportRect().Size;
        if (background is not null)
        {
            var grade = new ShaderMaterial { Shader = GD.Load<Shader>(Visuals.Art + "Shaders/grade.gdshader") };
            grade.SetShaderParameter("shadow_tint", light.Shadow);
            grade.SetShaderParameter("highlight_tint", light.Highlight);
            background.Material = grade;
        }

        // Three slanted light shafts from high windows, slowly breathing.
        var shaftTex = Visuals.Tex("FX/shaft.png");
        for (int i = 0; i < 3; i++)
        {
            var shaft = new Sprite2D
            {
                Texture = shaftTex,
                Material = Additive(),
                Centered = false,
                Position = new Vector2(size.X * (0.42f + 0.08f * i), -20),
                Scale = new Vector2(2.2f + 0.6f * (i % 2), size.Y * 0.8f / shaftTex.GetHeight()),
                Rotation = -0.32f,
                Modulate = light.Shaft * new Color(1, 1, 1, i == 1 ? 1f : 0.7f),
            };
            parent.AddChild(shaft);
            var breathe = shaft.CreateTween().SetLoops();
            double period = 3.5 + i * 1.3;
            breathe.TweenProperty(shaft, "modulate:a", light.Shaft.A * 0.45f, period).SetTrans(Tween.TransitionType.Sine);
            breathe.TweenProperty(shaft, "modulate:a", light.Shaft.A, period).SetTrans(Tween.TransitionType.Sine);
        }

        // A warm pool of light on the floor where the battle happens.
        var bounce = new Sprite2D
        {
            Texture = Visuals.MakeSoftCircle(Colors.White),
            Material = Additive(),
            Position = new Vector2(size.X / 2, size.Y * 0.62f),
            Scale = new Vector2(size.X / 64f * 0.9f, size.Y / 64f * 0.35f),
            Modulate = light.Bounce,
        };
        parent.AddChild(bounce);
    }

    /// <summary>Foreground silhouettes and the vignette. Call after the unit and effect layers.</summary>
    public static void AddInFront(Control parent, string regionKey)
    {
        string path = $"Backgrounds/{regionKey}_fg.png";
        if (ResourceLoader.Exists(Visuals.Art + path))
        {
            var fg = new TextureRect
            {
                Texture = Visuals.Tex(path),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            fg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            parent.AddChild(fg);
        }
        var vignette = new TextureRect
        {
            Texture = Visuals.Tex("FX/vignette.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        vignette.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        parent.AddChild(vignette);
    }
}
