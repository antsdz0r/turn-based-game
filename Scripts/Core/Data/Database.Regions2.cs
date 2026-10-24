using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>The two newer regions: the Rotting Garden and the Silent Archive. A run draws three of the five
/// middle regions (see RunState.RollRegionOrder), so not every run looks the same.</summary>
public static partial class Database
{
    private static IEnumerable<EnemyDef> BuildRegionPack3()
    {
        // ═════ The Rotting Garden ═════
        yield return new EnemyDef
        {
            Id = "thornling", Name = "Thornling", Sprite = "thornling",
            Blurb = "A walking briar. When it Bristles, melee attackers get pricked back: hit it with spells or wait a turn. Burns well.",
            MaxHp = 26, Armor = 1, Speed = 4, Row = Row.Front, BreakMax = 2, Gold = 10,
            Affinities = Aff(weak: [Element.Fire], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "thorn_lash", Name = "Thorn Lash", Melee = true, Power = 7, Pick = HeroPick.Frontline,
                        Applies = { new StatusApply(StatusKind.Bleed, 2, 2, Chance: 50) } },
                new() { Id = "bristle", Name = "Bristle", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 4, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Riposte, 5, 1, OnSelf: true) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "spore_puffer", Name = "Spore Puffer", Sprite = "spore_puffer",
            Blurb = "A bloated mushroom. Poisons the whole party, and BURSTS when it dies, poisoning your front row. Kill it with ranged attacks, or accept the cloud.",
            MaxHp = 22, Armor = 0, Speed = 3, Row = Row.Back, BreakMax = 1, Gold = 10,
            Affinities = Aff(weak: [Element.Fire, Element.Physical]),
            Moves =
            [
                new() { Id = "spore_cloud", Name = "Spore Cloud", Target = MoveTarget.AllHeroes, Power = 2, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Poison, 3, 3) } },
                new() { Id = "puff", Name = "Puff", Power = 5, Vfx = Vfx.Projectile },
            ],
        };
        yield return new EnemyDef
        {
            Id = "strangler", Name = "Strangler Vine", Sprite = "strangler",
            Blurb = "Roots that grab and squeeze. Its Root Grasp Slows and Weakens; its Crushing Coil is charged: interrupt it or pull the front row back.",
            MaxHp = 36, Armor = 2, Speed = 2, Row = Row.Front, BreakMax = 3, Gold = 14,
            Affinities = Aff(weak: [Element.Fire], resist: [Element.Lightning]),
            Moves =
            [
                new() { Id = "root_grasp", Name = "Root Grasp", Melee = true, Power = 5, Pick = HeroPick.HighestHp,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 2), new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "crushing_coil", Name = "Crushing Coil", Target = MoveTarget.FrontOnly, Melee = true, Power = 12, Charged = true },
            ],
        };
        yield return new EnemyDef
        {
            Id = "stalker", Name = "Thicket Stalker", Sprite = "stalker",
            Blurb = "A lean predator that hides in the brush (15% dodge) and pounces on the weakest hero. Fast; freeze or burn it.",
            MaxHp = 26, Armor = 1, Speed = 7, Dodge = 15, Crit = 15, Row = Row.Front, BreakMax = 2, Gold = 12,
            Affinities = Aff(weak: [Element.Fire, Element.Ice]),
            Moves =
            [
                new() { Id = "pounce", Name = "Pounce", Melee = true, Power = 8, Pick = HeroPick.LowestHp },
                new() { Id = "stalk", Name = "Stalk", Kind = MoveKind.Empower, Target = MoveTarget.Self, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2, OnSelf: true) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "briar_witch", Name = "Briar Witch", Sprite = "briar_witch", IsElite = true,
            Blurb = "Elite. A hag of the garden. She wards her plants with bark shields, grows new Thornlings and curses your party to bleed. Burn her first.",
            MaxHp = 78, Armor = 1, Speed = 4, Row = Row.Back, BreakMax = 4, Gold = 45,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "bramble_curse", Name = "Bramble Curse", Target = MoveTarget.AllHeroes, Power = 3, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Bleed, 2, 2) } },
                new() { Id = "bark_ward", Name = "Bark Ward", Kind = MoveKind.Shield, Target = MoveTarget.Ally, Power = 9, Vfx = Vfx.Buff },
                new() { Id = "grow", Name = "Grow", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "thornling", Vfx = Vfx.Buff },
                new() { Id = "wither", Name = "Wither", Power = 9, Pick = HeroPick.LowestHp, Vfx = Vfx.Debuff,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 2) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "seed_pod", Name = "Seed Pod", Sprite = "seed_pod",
            Blurb = "One of the Mother Bloom's pods. While it lives, it feeds her every round. Fragile: pop it.",
            MaxHp = 14, Armor = 0, Speed = 1, Row = Row.Front, BreakMax = 1, Gold = 0,
            Affinities = Aff(weak: [Element.Fire, Element.Physical]),
            Moves =
            [
                new() { Id = "sap_flow", Name = "Sap Flow", Kind = MoveKind.Heal, Target = MoveTarget.Ally, Power = 4, Vfx = Vfx.Heal },
            ],
        };
        yield return new EnemyDef
        {
            Id = "mother_bloom", Name = "The Mother Bloom", Sprite = "mother_bloom", Scale = 1.35f, IsBoss = true,
            RegrowFrom = "seed_pod", RegrowPct = 4,
            Epithet = "Heart of the Rotting Garden", MechanicHint = "Her seed pods feed her every round: cut them down before you cut her.",
            Blurb = "The garden's rotten heart. Every living SEED POD heals her 4% of her HP each round, so pop the pods first. Her Bloom Burst is charged and poisons everyone. In phase 2 she starts EATING her pods for a big heal.",
            MaxHp = 150, Armor = 2, Speed = 4, Row = Row.Front, BreakMax = 5, Gold = 90,
            Affinities = Aff(weak: [Element.Fire], resist: [Element.Lightning]),
            Moves =
            [
                new() { Id = "petal_storm", Name = "Petal Storm", Target = MoveTarget.AllHeroes, Power = 6, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Poison, 2, 2) } },
                new() { Id = "vine_whip", Name = "Vine Whip", Melee = true, Power = 11, Pick = HeroPick.Frontline },
                new() { Id = "sow", Name = "Sow", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "seed_pod", Vfx = Vfx.Buff },
                new() { Id = "bloom_burst", Name = "Bloom Burst", Target = MoveTarget.AllHeroes, Power = 11, Charged = true, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Poison, 4, 3) } },
            ],
            Phase2Name = "The Mother Bloom, Ever-Hungry",
            Phase2Affinities = Aff(weak: [Element.Fire, Element.Physical], resist: [Element.Lightning]),
            Phase2BreakMax = 5,
            Phase2Moves =
            [
                new() { Id = "strangling_roots", Name = "Strangling Roots", Target = MoveTarget.FrontRow, Melee = true, Power = 8, Hits = 2 },
                new() { Id = "bloom_burst2", Name = "Bloom Burst", Target = MoveTarget.AllHeroes, Power = 15, Charged = true, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Poison, 4, 3) } },
                new() { Id = "devour_pod", Name = "Devour Pod", Kind = MoveKind.Consume, Target = MoveTarget.Self, Power = 26, Vfx = Vfx.Heal },
                new() { Id = "sow2", Name = "Sow", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "seed_pod", Vfx = Vfx.Buff },
            ],
        };

        // ═════ The Silent Archive ═════
        yield return new EnemyDef
        {
            Id = "tome", Name = "Hungering Tome", Sprite = "tome",
            Blurb = "A flying book with teeth. Slices with its pages and reads aloud to empower its allies. Paper burns.",
            MaxHp = 26, Armor = 0, Speed = 6, Dodge = 10, Row = Row.Back, BreakMax = 2, Gold = 12,
            Affinities = Aff(weak: [Element.Fire], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "page_cut", Name = "Page Cut", Power = 7, Vfx = Vfx.Projectile,
                        Applies = { new StatusApply(StatusKind.Bleed, 2, 2, Chance: 50) } },
                new() { Id = "read_aloud", Name = "Read Aloud", Kind = MoveKind.Empower, Target = MoveTarget.AllAllies, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "ink_wraith", Name = "Ink Wraith", Sprite = "ink_wraith",
            Blurb = "A ghost made of spilled ink. Blots out a hero's strength (Weaken) and drowns the party in black. Hard to hit with steel.",
            MaxHp = 32, Armor = 0, Speed = 5, Dodge = 15, Row = Row.Back, BreakMax = 2, Gold = 14,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Physical]),
            Moves =
            [
                new() { Id = "ink_blot", Name = "Ink Blot", Power = 6, Pick = HeroPick.HighestHp, Vfx = Vfx.Debuff,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 2) } },
                new() { Id = "drown_ink", Name = "Drown in Ink", Target = MoveTarget.AllHeroes, Power = 4, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "gargoyle", Name = "Gargoyle", Sprite = "gargoyle",
            Blurb = "A stone guardian with 6 armor. It Petrifies when hurt, and its charged Dive hits the weakest hero hard. Lightning cracks stone.",
            MaxHp = 54, Armor = 6, Speed = 2, Row = Row.Front, BreakMax = 4, Gold = 22,
            Affinities = Aff(weak: [Element.Lightning], resist: [Element.Fire, Element.Ice]),
            Moves =
            [
                new() { Id = "stone_claw", Name = "Stone Claw", Melee = true, Power = 11, Pick = HeroPick.Frontline },
                new() { Id = "petrify", Name = "Petrify", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 14, Vfx = Vfx.Buff },
                new() { Id = "dive", Name = "Dive", Power = 18, Pick = HeroPick.LowestHp, Charged = true },
            ],
        };
        yield return new EnemyDef
        {
            Id = "quill_swarm", Name = "Quill Swarm", Sprite = "quill_swarm",
            Blurb = "A cloud of enchanted quills: very fast and hard to hit (20% dodge). Stabs three times a turn. Spells and fire scatter it.",
            MaxHp = 20, Armor = 0, Speed = 8, Dodge = 20, Row = Row.Back, BreakMax = 1, Gold = 10,
            Affinities = Aff(weak: [Element.Fire], resist: [Element.Physical]),
            Moves =
            [
                new() { Id = "quill_stab", Name = "Quill Stab", Power = 3, Hits = 3, Vfx = Vfx.Arrow },
            ],
        };
        yield return new EnemyDef
        {
            Id = "index_golem", Name = "Index Golem", Sprite = "index_golem", IsElite = true,
            Blurb = "Elite. A golem of shelves and brass. It CATALOGUES a hero (Mark), then its charged Reshelve slams the Marked hero. Protect them, or break its charge with lightning.",
            MaxHp = 100, Armor = 5, Speed = 3, Row = Row.Front, BreakMax = 5, Gold = 50,
            Affinities = Aff(weak: [Element.Lightning, Element.Fire], resist: [Element.Physical]),
            Moves =
            [
                new() { Id = "catalogue", Name = "Catalogue", Power = 6, Pick = HeroPick.HighestHp, Vfx = Vfx.Beam,
                        Applies = { new StatusApply(StatusKind.Marked, 0, 2) } },
                new() { Id = "reshelve", Name = "Reshelve", Power = 20, Pick = HeroPick.Marked, Charged = true },
                new() { Id = "brass_fist", Name = "Brass Fist", Melee = true, Power = 10, Pick = HeroPick.Frontline },
                new() { Id = "bind_pages", Name = "Bind Pages", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 12, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "archivist", Name = "The Archivist", Sprite = "archivist", Scale = 1.35f, IsBoss = true, AdaptiveResist = true,
            Epithet = "Keeper of Forbidden Words", MechanicHint = "It learns: the element that hit it last deals 40% less damage. Vary your attacks.",
            Blurb = "A faceless librarian who has read every spell ever written. It ADAPTS: the last element that hit it deals 40% less damage, so keep changing elements. It calls Hungering Tomes, and its charged Forbidden Word weakens your party. In phase 2 it ERASES a hero with a huge charged blow.",
            MaxHp = 118, Armor = 2, Speed = 6, Row = Row.Front, BreakMax = 5, Gold = 100,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "index_bolt", Name = "Index Bolt", Element = Element.Lightning, Power = 12, Pick = HeroPick.LowestHp, Vfx = Vfx.Projectile },
                new() { Id = "redact", Name = "Redact", Power = 5, Pick = HeroPick.HighestHp, Vfx = Vfx.Debuff,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 2), new StatusApply(StatusKind.Marked, 0, 2) } },
                new() { Id = "summon_tomes", Name = "Open the Stacks", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "tome", Vfx = Vfx.Buff },
                new() { Id = "forbidden_word", Name = "Forbidden Word", Target = MoveTarget.AllHeroes, Power = 10, Charged = true, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 2) } },
            ],
            Phase2Name = "The Archivist, Unbound",
            Phase2Affinities = Aff(weak: [Element.Fire, Element.Physical], resist: [Element.Lightning]),
            Phase2BreakMax = 6,
            Phase2Moves =
            [
                new() { Id = "erase", Name = "Erase", Power = 21, Pick = HeroPick.HighestHp, Charged = true, Vfx = Vfx.Beam },
                new() { Id = "page_storm", Name = "Page Storm", Target = MoveTarget.AllHeroes, Power = 6, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Bleed, 2, 2) } },
                new() { Id = "index_bolt2", Name = "Index Bolt", Element = Element.Lightning, Power = 14, Pick = HeroPick.LowestHp, Vfx = Vfx.Projectile },
                new() { Id = "summon_tomes2", Name = "Open the Stacks", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "tome", Vfx = Vfx.Buff },
            ],
        };
    }

    /// <summary>The secret boss behind the Sealed Door (see Events.Rooms).</summary>
    private static IEnumerable<EnemyDef> BuildSecretBoss()
    {
        yield return new EnemyDef
        {
            Id = "ember_wyrm", Name = "The Ember Wyrm", Sprite = "ember_wyrm", Scale = 1.4f, IsBoss = true, ShiftingWeakPoint = true,
            Epithet = "What the Ember Made", MechanicHint = "Its molten scales crack in a new place every round: hit the weak point.",
            Blurb = "Secret boss. A dragon born from the Last Ember's first spark, sealed away by the Keepers. Immune to fire. Its scales expose a new WEAK POINT every round (two in phase 2). Its charged Inferno engulfs the whole party: shield up, or interrupt it.",
            MaxHp = 190, Armor = 4, Speed = 6, Row = Row.Front, BreakMax = 6, Gold = 150,
            Affinities = Aff(weak: [Element.Ice], resist: [Element.Physical], immune: [Element.Fire]),
            Moves =
            [
                new() { Id = "flame_breath", Name = "Flame Breath", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 8, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Burn, 3, 2) } },
                new() { Id = "wyrm_claw", Name = "Obsidian Claw", Melee = true, Power = 13, Pick = HeroPick.Frontline },
                new() { Id = "tail_sweep", Name = "Tail Sweep", Target = MoveTarget.FrontRow, Melee = true, Power = 7, Hits = 2 },
                new() { Id = "wing_gust", Name = "Wing Gust", Target = MoveTarget.AllHeroes, Power = 3, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
            ],
            Phase2Name = "The Ember Wyrm, Unbound",
            Phase2Affinities = Aff(weak: [Element.Ice, Element.Lightning], immune: [Element.Fire]),
            Phase2BreakMax = 6,
            Phase2Armor = 2,
            Phase2Moves =
            [
                new() { Id = "inferno", Name = "Inferno", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 17, Charged = true, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Burn, 4, 2) } },
                new() { Id = "wyrm_claw2", Name = "Obsidian Claw", Melee = true, Power = 15, Pick = HeroPick.LowestHp },
                new() { Id = "molten_roar", Name = "Molten Roar", Kind = MoveKind.Empower, Target = MoveTarget.Self, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2, OnSelf: true) } },
                new() { Id = "tail_sweep2", Name = "Tail Sweep", Target = MoveTarget.FrontRow, Melee = true, Power = 8, Hits = 2 },
            ],
        };
    }

    private static RegionDef GardenRegion => new(5, "The Rotting Garden", "The monks' gardens grew wild when the Ember dimmed. Now they are hungry.",
        "Backgrounds/garden.png", "#ffffff", "battle_garden", "thornling",
        Easy: [["thornling", "thornling"], ["thornling", "spore_puffer"], ["stalker", "spore_puffer"], ["strangler", "spore_puffer"]],
        Hard: [["thornling", "strangler", "spore_puffer"], ["stalker", "stalker", "spore_puffer"], ["strangler", "thornling", "thornling"],
               ["stalker", "thornling", "spore_puffer"], ["strangler", "spore_puffer", "spore_puffer"], ["thornling", "stalker", "acolyte"]],
        Elites: [["briar_witch"], ["briar_witch", "thornling"], ["briar_witch", "spore_puffer"]],
        Boss: ["seed_pod", "mother_bloom", "seed_pod"], NativeAct: 1);

    private static RegionDef ArchiveRegion => new(6, "The Silent Archive", "Every forbidden word the Keepers ever wrote, and something that read them all.",
        "Backgrounds/archive.png", "#ffffff", "battle_archive", "tome",
        Easy: [["tome", "tome"], ["gargoyle", "tome"], ["gargoyle", "quill_swarm"], ["gargoyle", "ink_wraith"]],
        Hard: [["gargoyle", "tome", "ink_wraith"], ["gargoyle", "gargoyle", "quill_swarm"], ["gargoyle", "ink_wraith", "quill_swarm"],
               ["bulwark", "tome", "quill_swarm"], ["gargoyle", "tome", "tome"], ["bulwark", "ink_wraith", "tome"]],
        Elites: [["index_golem"], ["index_golem", "tome"], ["index_golem", "quill_swarm"]],
        Boss: ["tome", "archivist"], NativeAct: 3);

    /// <summary>Regions that can fill the middle acts (1-3) of a run. The crypt always opens and the Cinder Throne always closes.</summary>
    public static readonly string[] MiddleRegions = ["drowned", "frost", "storm", "garden", "archive"];

    public static RegionDef RegionByKey(string key) => Regions.FirstOrDefault(r => r.Key == key) ?? Regions[0];

    /// <summary>
    /// A region played deeper (or shallower) than it was tuned for: +12% HP and +8% damage per act of difference.
    /// </summary>
    public static void ScaleForAct(IEnumerable<Combatant> enemies, RegionDef region, int act)
    {
        int diff = act - region.Act;
        if (diff == 0) return;
        foreach (var e in enemies)
        {
            e.MaxHp = Math.Max(1, (int)Math.Round(e.MaxHp * (1 + 0.12 * diff)));
            e.Hp = e.MaxHp;
            e.DamageMult *= 1 + 0.08 * diff;
        }
    }
}
