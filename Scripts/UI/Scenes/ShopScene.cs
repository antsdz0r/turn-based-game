using System.Linq;
using Godot;
using TBRPG.Core;
using TBRPG.Game;

namespace TBRPG.UI;

/// <summary>Merchant: buy gear, consumables and relics; pay for healing or training.</summary>
public partial class ShopScene : Screen
{
    private VBoxContainer _box = null!;
    private Godot.Label _gold = null!;
    private bool _healed;

    protected override void Build()
    {
        var run = GameSession.Run ?? GameSession.EnsureRun();
        Audio.Music(this, "menu");
        if (GameSession.Run is { } regionRun) AddRegionBackdrop(regionRun.NodeRegion, 0.55f, torches: true); else AddBackdrop(new Color(0.55f, 0.5f, 0.45f));

        var merchant = Visuals.AnchorPoint(this, 0.12f, 0.82f);
        var sprite = Visuals.UnitSprite("cultist", 2.5f, flip: true);
        sprite.Modulate = new Color("#ffd9a0");
        merchant.AddChild(sprite);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        panel.OffsetLeft = 260;
        panel.OffsetRight = -40;
        panel.OffsetTop = 30;
        panel.OffsetBottom = -30;
        AddChild(panel);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        _box = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _box.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(_box);

        Fill(run);
        AddPauseMenu(inRun: true);
        Visuals.FadeIn(this, 0.4);
    }

    private void Fill(RunState run)
    {
        foreach (var child in _box.GetChildren()) child.QueueFree();

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        _box.AddChild(header);
        var title = Visuals.Title("The Ember Merchant", 32);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        header.AddChild(title);
        header.AddChild(Visuals.Icon("UI/icon_coin.png", 24));
        _gold = Visuals.Label($"{run.Gold}", 22, Visuals.GoldBright);
        header.AddChild(_gold);
        _box.AddChild(Visuals.Label("\"Coin for steel, coin for hope. The dead have no use for either.\"", 14, Visuals.TextDim));
        _box.AddChild(Visuals.Separator());

        foreach (var entry in run.GetShop())
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 10);
            string name, desc, icon;
            Color tint = Colors.White;
            if (entry.Kind == RewardKind.Relic)
            {
                var relic = Database.Relic(entry.Id);
                name = relic.Name;
                desc = "Relic: " + relic.Description;
                icon = "UI/icon_relic.png";
                tint = new Color(relic.ColorHex);
            }
            else
            {
                var item = Database.Item(entry.Id);
                name = item.Name;
                desc = $"{item.Kind}: {item.Description}";
                icon = Visuals.ItemIcon(item);
            }
            var image = Visuals.Icon(icon, 32);
            image.Modulate = tint;
            row.AddChild(image);
            var label = Visuals.Rich($"[b]{name}[/b]\n[color=#a99f8f]{desc}[/color]", 14);
            label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(label);
            bool can = run.CanBuy(entry, out var reason);
            var buy = Visuals.Button(entry.Sold ? "Sold" : $"Buy  {entry.Price}g", () =>
            {
                if (run.Buy(entry))
                {
                    Audio.Sfx(this, "coin", 0f);
                    GameSession.SaveRun();
                    Fill(run);
                }
            }, new Vector2(130, 40), 15);
            buy.Disabled = !can;
            buy.TooltipText = can ? "" : reason;
            row.AddChild(buy);
            _box.AddChild(row);
        }

        _box.AddChild(Visuals.Separator());
        _box.AddChild(Visuals.Label("Services", 18, Visuals.Gold, serif: true));

        var heal = Visuals.Button(_healed ? "Healed" : $"Bind wounds: heal the party 30%  ({run.HealServicePrice}g)", () =>
        {
            if (_healed || run.Gold < run.HealServicePrice) return;
            run.Gold -= run.HealServicePrice;
            run.HealParty(30);
            _healed = true;
            Audio.Sfx(this, "heal", 0f);
            GameSession.SaveRun();
            Fill(run);
        }, new Vector2(0, 42), 15, "UI/icon_heal_ally.png");
        heal.Disabled = _healed || run.Gold < run.HealServicePrice || run.Party.All(h => h.Hp >= h.MaxHp);
        _box.AddChild(heal);

        var train = Visuals.Button($"Training: upgrade one ability  ({run.UpgradeServicePrice}g)", () =>
        {
            AddChild(new UpgradePicker((heroId, abilityId) =>
            {
                if (run.Gold < run.UpgradeServicePrice || !run.Upgrade(heroId, abilityId)) return;
                run.Gold -= run.UpgradeServicePrice;
                Audio.Sfx(this, "levelup", 0f);
                GameSession.SaveRun();
                Fill(run);
            }, () => { }));
            Pause?.MoveToFront();
        }, new Vector2(0, 42), 15, "UI/icon_empowered.png");
        train.Disabled = run.Gold < run.UpgradeServicePrice;
        _box.AddChild(train);

        _box.AddChild(Visuals.Separator());
        var bottom = new HBoxContainer();
        bottom.AddThemeConstantOverride("separation", 12);
        _box.AddChild(bottom);
        bottom.AddChild(Visuals.Button("Party & Gear", () => AddChild(new PartyPanel(() => Fill(run))), new Vector2(180, 46), 16, "UI/icon_bag.png"));
        bottom.AddChild(Visuals.Button("Leave the shop", () => GameSession.CompleteNode(GetTree()), new Vector2(220, 46), 17));
    }
}
