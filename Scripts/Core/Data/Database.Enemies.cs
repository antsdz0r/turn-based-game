using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public static partial class Database
{
    private static Dictionary<Element, Affinity> Aff(Element[]? weak = null, Element[]? resist = null, Element[]? immune = null)
    {
        var d = new Dictionary<Element, Affinity>();
        foreach (var e in weak ?? Array.Empty<Element>()) d[e] = Affinity.Weak;
        foreach (var e in resist ?? Array.Empty<Element>()) d[e] = Affinity.Resist;
        foreach (var e in immune ?? Array.Empty<Element>()) d[e] = Affinity.Immune;
        return d;
    }

    private static readonly Dictionary<string, EnemyDef> EnemyTable = BuildEnemies().Concat(BuildRegionPack3()).Concat(BuildSecretBoss()).ToDictionary(e => e.Id);

    public static EnemyDef EnemyDefinition(string id) =>
        EnemyTable.TryGetValue(id, out var def) ? def : throw new KeyNotFoundException($"Unknown enemy id '{id}'");
    public static bool HasEnemy(string id) => EnemyTable.ContainsKey(id);
    public static IEnumerable<EnemyDef> AllEnemies => EnemyTable.Values;

    private static IEnumerable<EnemyDef> BuildEnemies()
    {
        yield return new EnemyDef
        {
            Id = "bones", Name = "Restless Bones", Sprite = "skeleton", Blurb = "Weak melee soldier. Always swings at the front row.",
            MaxHp = 20, Armor = 2, Speed = 3, Row = Row.Front, BreakMax = 2, Gold = 8,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "club", Name = "Bone Club", Melee = true, Power = 6, Pick = HeroPick.Frontline },
                new() { Id = "rattle", Name = "Rattle", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 5, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "archer", Name = "Bone Archer", Sprite = "skeleton_archer", Blurb = "Ranged. Targets your back row.",
            MaxHp = 15, Armor = 1, Speed = 6, Row = Row.Back, BreakMax = 2, Gold = 10,
            Affinities = Aff(weak: [Element.Fire, Element.Physical]),
            Moves =
            [
                new() { Id = "arrow", Name = "Black Arrow", Power = 7, Pick = HeroPick.Backline, Vfx = Vfx.Arrow },
                new() { Id = "pin", Name = "Pinning Volley", Target = MoveTarget.BackRow, Power = 4, Vfx = Vfx.Arrow,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "ghoul", Name = "Ghoul", Sprite = "ghoul", Blurb = "Bleeding claws, and a charged Gorge on the weakest hero.",
            MaxHp = 26, Armor = 0, Speed = 5, Row = Row.Front, BreakMax = 3, Gold = 12,
            Affinities = Aff(weak: [Element.Fire, Element.Physical], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "claws", Name = "Rending Claws", Melee = true, Power = 4, Hits = 2,
                        Applies = { new StatusApply(StatusKind.Bleed, 2, 2) } },
                new() { Id = "gorge", Name = "Gorge", Melee = true, Power = 14, Pick = HeroPick.LowestHp, Charged = true },
            ],
        };
        yield return new EnemyDef
        {
            Id = "acolyte", Name = "Ash Acolyte", Sprite = "cultist", Blurb = "Support. Heals and empowers its allies. Kill it first.",
            MaxHp = 18, Armor = 0, Speed = 4, Row = Row.Back, BreakMax = 2, Gold = 12,
            Affinities = Aff(weak: [Element.Lightning, Element.Physical], resist: [Element.Fire]),
            Moves =
            [
                new() { Id = "dark_mend", Name = "Dark Mend", Kind = MoveKind.Heal, Target = MoveTarget.Ally, Power = 10, Vfx = Vfx.Heal },
                new() { Id = "blessing", Name = "Ember Blessing", Kind = MoveKind.Empower, Target = MoveTarget.AllAllies, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2) } },
                new() { Id = "cinder_bolt", Name = "Cinder Bolt", Element = Element.Fire, Power = 6, Vfx = Vfx.Projectile },
            ],
        };
        yield return new EnemyDef
        {
            Id = "wraith", Name = "Hollow Wraith", Sprite = "wraith", Blurb = "Elusive caster. Physical attacks barely touch it; ice can't.",
            MaxHp = 20, Armor = 0, Speed = 7, Dodge = 20, Row = Row.Back, BreakMax = 3, Gold = 14,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Physical], immune: [Element.Ice]),
            Moves =
            [
                new() { Id = "chill", Name = "Chill Touch", Element = Element.Ice, Power = 6, Vfx = Vfx.Projectile,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "wail", Name = "Wail", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 4, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "bulwark", Name = "Cinder Bulwark", Sprite = "bulwark", Blurb = "Heavily armored protector. Shields its allies.",
            MaxHp = 34, Armor = 5, Speed = 2, Row = Row.Front, BreakMax = 4, Gold = 16,
            Affinities = Aff(weak: [Element.Lightning, Element.Ice], resist: [Element.Fire]),
            Moves =
            [
                new() { Id = "shield_wall", Name = "Shield Wall", Kind = MoveKind.Shield, Target = MoveTarget.AllAllies, Power = 6, Vfx = Vfx.Buff },
                new() { Id = "slam", Name = "Bulwark Slam", Melee = true, Power = 7, Pick = HeroPick.Frontline,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "brace", Name = "Brace", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 8, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "blight", Name = "Blightspitter", Sprite = "blight", Blurb = "Spreads stacking poison. Burn or freeze it fast.",
            MaxHp = 18, Armor = 1, Speed = 5, Row = Row.Back, BreakMax = 2, Gold = 12,
            Affinities = Aff(weak: [Element.Fire, Element.Ice]),
            Moves =
            [
                new() { Id = "spit", Name = "Venom Spit", Power = 3, Pick = HeroPick.Backline, Vfx = Vfx.Projectile,
                        Applies = { new StatusApply(StatusKind.Poison, 4, 1) } },
                new() { Id = "miasma", Name = "Miasma", Target = MoveTarget.AllHeroes, Power = 0, Vfx = Vfx.Cloud, Accuracy = 0,
                        Applies = { new StatusApply(StatusKind.Poison, 3, 1) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "caller", Name = "Grave Caller", Sprite = "necromancer", Blurb = "Summoner. Raises bones and marks a hero for death.",
            MaxHp = 24, Armor = 0, Speed = 4, Row = Row.Back, BreakMax = 3, Gold = 16,
            Affinities = Aff(weak: [Element.Physical, Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "raise", Name = "Raise Dead", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "bones", Vfx = Vfx.Buff },
                new() { Id = "soul_bolt", Name = "Soul Bolt", Element = Element.Ice, Power = 7, Vfx = Vfx.Projectile },
                new() { Id = "death_mark", Name = "Death Mark", Power = 2, Pick = HeroPick.LowestHp, Vfx = Vfx.Debuff, Accuracy = 0,
                        Applies = { new StatusApply(StatusKind.Marked, 0, 2) } },
            ],
        };

        // ───── Elites ─────
        yield return new EnemyDef
        {
            Id = "gravemaw", Name = "Gravemaw", Sprite = "ghoul", Tint = "#ff9a8a", Scale = 1.35f, IsElite = true,
            Blurb = "Elite. At half health it frenzies: new attacks, new weaknesses.",
            MaxHp = 96, Armor = 2, Speed = 5, Row = Row.Front, BreakMax = 4, Gold = 40,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "maul", Name = "Maul", Melee = true, Power = 9, Pick = HeroPick.Frontline },
                new() { Id = "howl", Name = "Grave Howl", Target = MoveTarget.AllHeroes, Power = 3, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "gorge_big", Name = "Gorge", Melee = true, Power = 16, Pick = HeroPick.LowestHp, Charged = true },
            ],
            Phase2Name = "Gravemaw (Frenzied)",
            Phase2Affinities = Aff(weak: [Element.Ice, Element.Physical], resist: [Element.Fire]),
            Phase2BreakMax = 3,
            Phase2Moves =
            [
                new() { Id = "frenzy_claws", Name = "Frenzied Claws", Melee = true, Power = 5, Hits = 3,
                        Applies = { new StatusApply(StatusKind.Bleed, 2, 2) } },
                new() { Id = "bloodlust", Name = "Bloodlust", Kind = MoveKind.Heal, Target = MoveTarget.Self, Power = 10, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2, OnSelf: true) } },
                new() { Id = "devour", Name = "Devour", Melee = true, Power = 20, Pick = HeroPick.LowestHp, Charged = true },
            ],
        };
        yield return new EnemyDef
        {
            Id = "templar", Name = "Fallen Templar", Sprite = "vanguard", Tint = "#ff8a6a", Scale = 1.2f, IsElite = true, FlipSprite = true,
            Blurb = "Elite. Cleaves the front row and charges a judgment on your weakest hero.",
            MaxHp = 52, Armor = 4, Speed = 4, Row = Row.Front, BreakMax = 4, Gold = 40,
            Affinities = Aff(weak: [Element.Ice, Element.Lightning], resist: [Element.Fire, Element.Physical]),
            Moves =
            [
                new() { Id = "cleave", Name = "Ashen Cleave", Target = MoveTarget.FrontRow, Melee = true, Power = 7 },
                new() { Id = "judgment", Name = "Burning Judgment", Element = Element.Fire, Power = 15, Pick = HeroPick.LowestHp,
                        Charged = true, Vfx = Vfx.Beam, Applies = { new StatusApply(StatusKind.Burn, 3, 2) } },
                new() { Id = "oath", Name = "Oath of Cinders", Kind = MoveKind.Shield, Target = MoveTarget.Self, Power = 10, Vfx = Vfx.Buff },
            ],
        };

        // ═════ Region 1 additions: the Ashen Crypt ═════
        yield return new EnemyDef
        {
            Id = "rats", Name = "Crypt Rats", Sprite = "rat", Blurb = "A squeaking swarm. Fast, fragile, and they bite to bleed.",
            MaxHp = 13, Armor = 0, Speed = 7, Row = Row.Front, BreakMax = 1, Gold = 6,
            Affinities = Aff(weak: [Element.Fire, Element.Physical]),
            Moves =
            [
                new() { Id = "gnaw", Name = "Gnaw", Melee = true, Power = 3, Hits = 2, Applies = { new StatusApply(StatusKind.Bleed, 1, 2, Chance: 50) } },
                new() { Id = "scurry", Name = "Scurry", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 3, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "moth", Name = "Grave Moth", Sprite = "moth", Blurb = "Flits out of reach (high dodge). Its dust weakens your attacks.",
            MaxHp = 15, Armor = 0, Speed = 9, Dodge = 25, Row = Row.Back, BreakMax = 2, Gold = 10,
            Affinities = Aff(weak: [Element.Lightning, Element.Fire], resist: [Element.Physical]),
            Moves =
            [
                new() { Id = "dust", Name = "Blinding Dust", Target = MoveTarget.AllHeroes, Power = 2, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "sip", Name = "Soul Sip", Element = Element.Ice, Power = 5, Pick = HeroPick.LowestHp, Vfx = Vfx.Projectile },
            ],
        };
        yield return new EnemyDef
        {
            Id = "colossus", Name = "The Bone Colossus", Sprite = "colossus", Scale = 1.35f, IsBoss = true, ShiftingWeakPoint = true,
            Epithet = "Guardian of the Ashen Crypt", MechanicHint = "Its core shifts every round: strike the exposed weak point.",
            Blurb = "Guardian of the Ashen Crypt. Every round its grave-fire core shifts and exposes a new WEAK POINT: hit that element for double damage. In phase 2 it exposes TWO weak points but hits harder. Each time it recovers from a break, its meter grows.",
            MaxHp = 122, Armor = 4, Speed = 3, Row = Row.Front, BreakMax = 4, Gold = 60,
            Affinities = Aff(weak: [Element.Physical, Element.Fire], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "bone_slam", Name = "Bone Slam", Melee = true, Power = 9, Pick = HeroPick.Frontline },
                new() { Id = "barrage", Name = "Rib Barrage", Target = MoveTarget.AllHeroes, Power = 4, Vfx = Vfx.Projectile },
                new() { Id = "quake", Name = "Grave Quake", Target = MoveTarget.FrontRow, Melee = true, Power = 13, Charged = true,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "reassemble", Name = "Reassemble", Kind = MoveKind.Heal, Target = MoveTarget.Self, Power = 12, Vfx = Vfx.Heal },
            ],
            Phase2Name = "The Bone Colossus (Unbound)",
            Phase2Affinities = Aff(weak: [Element.Lightning, Element.Ice], resist: [Element.Physical]),
            Phase2BreakMax = 4,
            Phase2Armor = 2,
            Phase2Moves =
            [
                new() { Id = "frenzy_slam", Name = "Frenzied Slams", Melee = true, Power = 6, Hits = 2, Pick = HeroPick.Frontline },
                new() { Id = "quake2", Name = "Grave Quake", Target = MoveTarget.AllHeroes, Power = 11, Charged = true,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "call_bones", Name = "Call the Bones", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "bones", Vfx = Vfx.Buff },
            ],
        };

        yield return new EnemyDef
        {
            Id = "mimic", Name = "Mimic", Sprite = "mimic", IsElite = true,
            Blurb = "That chest has teeth. Beat it and it coughs up everything it swallowed.",
            MaxHp = 64, Armor = 3, Speed = 6, Row = Row.Front, BreakMax = 3, Gold = 70,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Physical]),
            Moves =
            [
                new() { Id = "chomp", Name = "Chomp", Melee = true, Power = 11, Pick = HeroPick.Frontline },
                new() { Id = "tongue", Name = "Tongue Lash", Target = MoveTarget.AllHeroes, Power = 5,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
                new() { Id = "snap_shut", Name = "Snap Shut", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 12, Vfx = Vfx.Buff },
            ],
        };

        // ═════ Region 2: the Drowned Halls ═════
        yield return new EnemyDef
        {
            Id = "thrall", Name = "Drowned Thrall", Sprite = "thrall", Blurb = "A waterlogged soldier. Tough, slow, and it drags heroes down.",
            MaxHp = 28, Armor = 3, Speed = 3, Row = Row.Front, BreakMax = 3, Gold = 12,
            Affinities = Aff(weak: [Element.Lightning, Element.Fire], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "fist", Name = "Sodden Fist", Melee = true, Power = 7, Pick = HeroPick.Frontline },
                new() { Id = "grasp", Name = "Drowning Grasp", Melee = true, Power = 4, Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "brace_t", Name = "Brace", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 6, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "siren", Name = "Siren", Sprite = "siren", Blurb = "Sings to heal her allies and weaken yours. Silence her first.",
            MaxHp = 18, Armor = 0, Speed = 6, Row = Row.Back, BreakMax = 2, Gold = 14,
            Affinities = Aff(weak: [Element.Physical, Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "lament", Name = "Lament", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 3, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "hymn", Name = "Tidal Hymn", Kind = MoveKind.Heal, Target = MoveTarget.AllAllies, Power = 6, Vfx = Vfx.Heal },
                new() { Id = "dirge", Name = "Dirge", Element = Element.Ice, Power = 12, Pick = HeroPick.LowestHp, Charged = true, Vfx = Vfx.Beam },
            ],
        };
        yield return new EnemyDef
        {
            Id = "leech", Name = "Bog Leech", Sprite = "leech", Blurb = "Latches on and bleeds you, then swells to heal itself.",
            MaxHp = 22, Armor = 1, Speed = 4, Row = Row.Front, BreakMax = 2, Gold = 10,
            Affinities = Aff(weak: [Element.Fire, Element.Ice]),
            Moves =
            [
                new() { Id = "latch", Name = "Latch", Melee = true, Power = 4, Applies = { new StatusApply(StatusKind.Bleed, 3, 2) } },
                new() { Id = "engorge", Name = "Engorge", Kind = MoveKind.Heal, Target = MoveTarget.Self, Power = 9, Vfx = Vfx.Heal },
            ],
        };
        yield return new EnemyDef
        {
            Id = "siren_queen", Name = "Abyssal Siren", Sprite = "siren", Tint = "#b0a0ff", Scale = 1.3f, IsElite = true,
            Blurb = "Elite. Calls drowned thralls and charges a crushing dirge.",
            MaxHp = 88, Armor = 1, Speed = 6, Row = Row.Back, BreakMax = 4, Gold = 40,
            Affinities = Aff(weak: [Element.Physical, Element.Lightning], resist: [Element.Ice, Element.Fire]),
            Moves =
            [
                new() { Id = "lament_q", Name = "Abyssal Lament", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 5, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "call_thrall", Name = "Call the Drowned", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "thrall", Vfx = Vfx.Buff },
                new() { Id = "dirge_q", Name = "Black Dirge", Element = Element.Ice, Power = 17, Pick = HeroPick.LowestHp, Charged = true, Vfx = Vfx.Beam },
            ],
        };
        yield return new EnemyDef
        {
            Id = "champion", Name = "Drowned Champion", Sprite = "thrall", Tint = "#9ab8ff", Scale = 1.3f, IsElite = true,
            Blurb = "Elite. Swings an anchor through your front row.",
            MaxHp = 64, Armor = 4, Speed = 4, Row = Row.Front, BreakMax = 4, Gold = 40,
            Affinities = Aff(weak: [Element.Lightning, Element.Fire], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "anchor", Name = "Anchor Swing", Target = MoveTarget.FrontRow, Melee = true, Power = 8 },
                new() { Id = "undertow", Name = "Undertow", Melee = true, Power = 17, Pick = HeroPick.LowestHp, Charged = true,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "brace_c", Name = "Barnacle Guard", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 10, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "matriarch", Name = "The Drowned Matriarch", Sprite = "matriarch", Scale = 1.35f, IsBoss = true,
            Epithet = "Queen of the Drowned Halls", MechanicHint = "When the tide is charging, move the threatened row.",
            Blurb = "Queen of the Drowned Halls. Her RISING TIDE floods the front row: when she charges it, pull your heroes back! In phase 2 the tide turns: an UNDERTOW drags at the back row too, so watch which row she is charging.",
            MaxHp = 130, Armor = 2, Speed = 5, Row = Row.Front, BreakMax = 5, Gold = 80,
            Affinities = Aff(weak: [Element.Lightning, Element.Physical], resist: [Element.Ice, Element.Fire]),
            Moves =
            [
                new() { Id = "riptide", Name = "Riptide", Element = Element.Ice, Power = 9, Vfx = Vfx.Projectile },
                new() { Id = "wave", Name = "Tidal Wave", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 6, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "drown", Name = "Drown", Element = Element.Ice, Power = 18, Pick = HeroPick.LowestHp, Charged = true, Vfx = Vfx.Beam,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "flood", Name = "Rising Tide", Target = MoveTarget.FrontOnly, Element = Element.Ice, Power = 22, Charged = true,
                        Vfx = Vfx.Cloud, Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "call_deep", Name = "Call of the Deep", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "thrall", Vfx = Vfx.Buff },
            ],
            Phase2Name = "The Matriarch (Abyssal)",
            Phase2Affinities = Aff(weak: [Element.Fire, Element.Ice], resist: [Element.Lightning]),
            Phase2BreakMax = 5,
            Phase2Moves =
            [
                new() { Id = "depths", Name = "Crushing Depths", Target = MoveTarget.FrontRow, Melee = true, Power = 9, Hits = 2 },
                new() { Id = "abyss_wail", Name = "Abyssal Wail", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 8, Vfx = Vfx.Cloud },
                new() { Id = "drown2", Name = "Drown", Element = Element.Ice, Power = 22, Pick = HeroPick.LowestHp, Charged = true, Vfx = Vfx.Beam },
                new() { Id = "undertow", Name = "Undertow", Target = MoveTarget.BackOnly, Element = Element.Ice, Power = 20, Charged = true,
                        Vfx = Vfx.Cloud, Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "flood2", Name = "Abyssal Tide", Target = MoveTarget.FrontOnly, Element = Element.Ice, Power = 26, Charged = true,
                        Vfx = Vfx.Cloud, Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "tide_mend", Name = "Tide's Embrace", Kind = MoveKind.Heal, Target = MoveTarget.Self, Power = 14, Vfx = Vfx.Heal },
            ],
        };

        // ═════ Region 3: the Frozen Ossuary ═════
        yield return new EnemyDef
        {
            Id = "wight", Name = "Frost Wight", Sprite = "wight", Blurb = "A frozen knight. Its rime blade slows whoever it cuts. Melt it with fire.",
            MaxHp = 32, Armor = 3, Speed = 3, Row = Row.Front, BreakMax = 3, Gold = 14,
            Affinities = Aff(weak: [Element.Fire], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "rime_blade", Name = "Rime Blade", Melee = true, Element = Element.Ice, Power = 8, Pick = HeroPick.Frontline,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1, Chance: 50) } },
                new() { Id = "frost_guard", Name = "Frost Guard", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 8, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "wisp", Name = "Ice Wisp", Sprite = "wisp", Blurb = "A drifting mote of winter. Hard to hit, immune to ice, and its aura slows the party.",
            MaxHp = 15, Armor = 0, Speed = 8, Dodge = 25, Row = Row.Back, BreakMax = 2, Gold = 10,
            Affinities = Aff(weak: [Element.Fire, Element.Physical], immune: [Element.Ice]),
            Moves =
            [
                new() { Id = "frostbolt", Name = "Frostbolt", Element = Element.Ice, Power = 7, Vfx = Vfx.Projectile },
                new() { Id = "chill_aura", Name = "Chill Aura", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 2, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "hound", Name = "Bone Hound", Sprite = "hound", Blurb = "Hunts in packs. Fast, and its bites bleed.",
            MaxHp = 17, Armor = 1, Speed = 8, Row = Row.Front, BreakMax = 1, Gold = 9,
            Affinities = Aff(weak: [Element.Fire, Element.Physical]),
            Moves =
            [
                new() { Id = "hound_rend", Name = "Rend", Melee = true, Power = 4, Hits = 2, Applies = { new StatusApply(StatusKind.Bleed, 2, 2, Chance: 60) } },
                new() { Id = "hound_howl", Name = "Howl", Kind = MoveKind.Empower, Target = MoveTarget.AllAllies, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "revenant", Name = "Frost Revenant", Sprite = "wight", Tint = "#a0d8ff", Scale = 1.3f, IsElite = true,
            Blurb = "Elite. Cleaves the front row and charges a killing Permafrost.",
            MaxHp = 68, Armor = 4, Speed = 4, Row = Row.Front, BreakMax = 4, Gold = 45,
            Affinities = Aff(weak: [Element.Fire], resist: [Element.Ice, Element.Physical]),
            Moves =
            [
                new() { Id = "glacial_cleave", Name = "Glacial Cleave", Target = MoveTarget.FrontRow, Melee = true, Element = Element.Ice, Power = 9,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
                new() { Id = "permafrost", Name = "Permafrost", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 13, Charged = true, Vfx = Vfx.Cloud },
                new() { Id = "ice_wall", Name = "Ice Wall", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 12, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "alpha_hound", Name = "Alpha Bone Hound", Sprite = "hound", Tint = "#ffb0a0", Scale = 1.35f, IsElite = true,
            Blurb = "Elite. Leads the pack: it calls more hounds and savages the weak.",
            MaxHp = 90, Armor = 2, Speed = 9, Row = Row.Front, BreakMax = 3, Gold = 40,
            Affinities = Aff(weak: [Element.Fire, Element.Physical]),
            Moves =
            [
                new() { Id = "savage", Name = "Savage", Melee = true, Power = 6, Hits = 2, Pick = HeroPick.LowestHp,
                        Applies = { new StatusApply(StatusKind.Bleed, 3, 2) } },
                new() { Id = "call_pack", Name = "Call the Pack", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "hound", Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "phylactery", Name = "Phylactery", Sprite = "phylactery",
            Blurb = "The Lich's soul-jar. While it stands she takes 60% less damage, and it keeps mending her.",
            MaxHp = 30, Armor = 2, Speed = 2, Row = Row.Back, BreakMax = 2, Gold = 0,
            Affinities = Aff(weak: [Element.Physical, Element.Lightning], immune: [Element.Ice]),
            Moves =
            [
                new() { Id = "soul_tether", Name = "Soul Tether", Kind = MoveKind.Heal, Target = MoveTarget.Ally, Power = 7, Vfx = Vfx.Heal },
            ],
        };
        yield return new EnemyDef
        {
            Id = "lich", Name = "Vaelith, the Frost Lich", Sprite = "lich", Scale = 1.35f, IsBoss = true, ProtectedBy = "phylactery",
            Epithet = "Mistress of the Frozen Ossuary", MechanicHint = "Shatter the Phylactery first: while it stands, she is warded.",
            Blurb = "Mistress of the Frozen Ossuary. WARDED while her Phylactery stands (-60% damage): smash it first! Shattering it also tears at her soul (damage and 2 break pips). Her Glacial Prison freezes a hero solid, and in phase 2 she binds a new Phylactery once.",
            MaxHp = 130, Armor = 2, Speed = 5, Row = Row.Front, BreakMax = 5, Gold = 90,
            Affinities = Aff(weak: [Element.Fire, Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "frost_nova", Name = "Frost Nova", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 6, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
                new() { Id = "ice_spear", Name = "Ice Spear", Element = Element.Ice, Power = 13, Pick = HeroPick.LowestHp, Vfx = Vfx.Projectile },
                new() { Id = "glacial_prison", Name = "Glacial Prison", Element = Element.Ice, Power = 8, Pick = HeroPick.HighestHp, Charged = true,
                        Vfx = Vfx.Beam, Applies = { new StatusApply(StatusKind.Stun, 1, 1) } },
            ],
            Phase2Name = "Vaelith, Winter Unbound",
            Phase2Affinities = Aff(weak: [Element.Fire, Element.Physical], resist: [Element.Ice, Element.Lightning]),
            Phase2BreakMax = 5,
            Phase2Moves =
            [
                new() { Id = "blizzard", Name = "Blizzard", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 14, Charged = true, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 2) } },
                new() { Id = "ice_spear2", Name = "Ice Spear", Element = Element.Ice, Power = 15, Pick = HeroPick.LowestHp, Vfx = Vfx.Projectile },
                new() { Id = "rebind", Name = "Rebind Phylactery", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "phylactery", Vfx = Vfx.Buff },
                new() { Id = "glacial_prison2", Name = "Glacial Prison", Element = Element.Ice, Power = 10, Pick = HeroPick.HighestHp, Charged = true,
                        Vfx = Vfx.Beam, Applies = { new StatusApply(StatusKind.Stun, 1, 1) } },
            ],
        };

        // ═════ Region 4: the Storm Spire ═════
        yield return new EnemyDef
        {
            Id = "harpy", Name = "Storm Harpy", Sprite = "harpy", Blurb = "Dives at the back row from above. Quick and slippery.",
            MaxHp = 18, Armor = 0, Speed = 9, Dodge = 20, Row = Row.Back, BreakMax = 2, Gold = 11,
            Affinities = Aff(weak: [Element.Ice, Element.Physical], resist: [Element.Lightning]),
            Moves =
            [
                new() { Id = "talon_dive", Name = "Talon Dive", Power = 7, Pick = HeroPick.Backline, Vfx = Vfx.Melee },
                new() { Id = "shriek", Name = "Shriek", Target = MoveTarget.AllHeroes, Power = 2, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "construct", Name = "Spark Construct", Sprite = "construct", Blurb = "A brass war-machine full of lightning. Immune to it; freeze its gears.",
            MaxHp = 42, Armor = 5, Speed = 2, Row = Row.Front, BreakMax = 4, Gold = 16,
            Affinities = Aff(weak: [Element.Ice], resist: [Element.Fire], immune: [Element.Lightning]),
            Moves =
            [
                new() { Id = "piston", Name = "Piston Slam", Melee = true, Power = 10, Pick = HeroPick.Frontline },
                new() { Id = "overcharge", Name = "Overcharge", Target = MoveTarget.AllHeroes, Element = Element.Lightning, Power = 11, Charged = true, Vfx = Vfx.Beam },
                new() { Id = "static_field", Name = "Static Field", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 10, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "stormcaller", Name = "Stormcaller", Sprite = "stormcaller", Blurb = "Calls chain lightning on the whole party and quickens its allies.",
            MaxHp = 21, Armor = 0, Speed = 6, Row = Row.Back, BreakMax = 2, Gold = 14,
            Affinities = Aff(weak: [Element.Physical, Element.Ice], resist: [Element.Lightning]),
            Moves =
            [
                new() { Id = "chain_bolt", Name = "Chain Lightning", Target = MoveTarget.AllHeroes, Element = Element.Lightning, Power = 4, Vfx = Vfx.Beam },
                new() { Id = "thunderbolt", Name = "Thunderbolt", Element = Element.Lightning, Power = 10, Pick = HeroPick.LowestHp, Vfx = Vfx.Beam },
                new() { Id = "tailwind", Name = "Tailwind", Kind = MoveKind.Empower, Target = MoveTarget.AllAllies, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "thunderbird", Name = "Thunderbird", Sprite = "harpy", Tint = "#ffe09a", Scale = 1.35f, IsElite = true,
            Blurb = "Elite. A great storm-bird. Its Gale is charged: break it or brace.",
            MaxHp = 100, Armor = 1, Speed = 10, Dodge = 15, Row = Row.Back, BreakMax = 4, Gold = 45,
            Affinities = Aff(weak: [Element.Ice, Element.Physical], resist: [Element.Lightning]),
            Moves =
            [
                new() { Id = "thunder_dive", Name = "Thunder Dive", Element = Element.Lightning, Power = 12, Pick = HeroPick.Backline, Vfx = Vfx.Melee },
                new() { Id = "storm_wings", Name = "Storm Wings", Target = MoveTarget.AllHeroes, Element = Element.Lightning, Power = 6, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
                new() { Id = "gale", Name = "Gale", Target = MoveTarget.AllHeroes, Power = 14, Charged = true, Vfx = Vfx.Cloud },
            ],
        };
        yield return new EnemyDef
        {
            Id = "arc_golem", Name = "Arc Golem", Sprite = "construct", Tint = "#b0c8ff", Scale = 1.3f, IsElite = true,
            Blurb = "Elite. A siege engine. Slams the front row and overloads everything around it.",
            MaxHp = 80, Armor = 6, Speed = 2, Row = Row.Front, BreakMax = 5, Gold = 45,
            Affinities = Aff(weak: [Element.Ice], resist: [Element.Fire, Element.Physical], immune: [Element.Lightning]),
            Moves =
            [
                new() { Id = "arc_slam", Name = "Arc Slam", Target = MoveTarget.FrontRow, Melee = true, Element = Element.Lightning, Power = 10 },
                new() { Id = "overload", Name = "Overload", Target = MoveTarget.AllHeroes, Element = Element.Lightning, Power = 12, Charged = true, Vfx = Vfx.Beam },
            ],
        };
        yield return new EnemyDef
        {
            Id = "tyrant", Name = "Kharos, the Storm Tyrant", Sprite = "tyrant", Scale = 1.35f, IsBoss = true,
            Epithet = "Lord of the Storm Spire", MechanicHint = "The brand marks his next target: shield, cleanse or defend them.",
            Blurb = "Lord of the Storm Spire. His STORM BRAND marks a hero; the next Thunderstrike hits that hero for massive damage. Defend, protect or heal them! In phase 2 the brand forks across the back row and the strike hits every branded hero: cleanse the brand or spread out.",
            MaxHp = 110, Armor = 3, Speed = 6, Row = Row.Front, BreakMax = 5, Gold = 95,
            Affinities = Aff(weak: [Element.Ice, Element.Physical], resist: [Element.Lightning]),
            Moves =
            [
                new() { Id = "storm_brand", Name = "Storm Brand", Element = Element.Lightning, Power = 3, Pick = HeroPick.HighestHp, Vfx = Vfx.Debuff,
                        Applies = { new StatusApply(StatusKind.Marked, 0, 3) } },
                new() { Id = "thunderstrike", Name = "Thunderstrike", Element = Element.Lightning, Power = 22, Pick = HeroPick.Marked, Charged = true, Vfx = Vfx.Beam },
                new() { Id = "tempest", Name = "Tempest", Target = MoveTarget.AllHeroes, Element = Element.Lightning, Power = 5, Vfx = Vfx.Cloud },
            ],
            Phase2Name = "Kharos, Eye of the Storm",
            Phase2Affinities = Aff(weak: [Element.Fire, Element.Ice], resist: [Element.Lightning, Element.Physical]),
            Phase2BreakMax = 5,
            Phase2Moves =
            [
                new() { Id = "storm_brand2", Name = "Storm Brand", Element = Element.Lightning, Power = 4, Pick = HeroPick.HighestHp, Vfx = Vfx.Debuff,
                        Applies = { new StatusApply(StatusKind.Marked, 0, 4) } },
                // Escalation: the brand forks across the whole back row, and the strike hits EVERY branded hero.
                new() { Id = "forked_brand", Name = "Forked Brand", Target = MoveTarget.BackRow, Element = Element.Lightning, Power = 3, Accuracy = 100, Vfx = Vfx.Debuff,
                        Applies = { new StatusApply(StatusKind.Marked, 0, 4) } },
                new() { Id = "thunderstrike2", Name = "Thunderstrike", Target = MoveTarget.Branded, Element = Element.Lightning, Power = 16,
                        Charged = true, Vfx = Vfx.Beam },
                new() { Id = "cyclone", Name = "Cyclone", Target = MoveTarget.FrontRow, Melee = true, Power = 8, Hits = 2 },
                new() { Id = "call_harpies", Name = "Call the Harpies", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "harpy", Vfx = Vfx.Buff },
            ],
        };

        // ═════ Region 5: the Cinder Throne ═════
        yield return new EnemyDef
        {
            Id = "imp", Name = "Ember Imp", Sprite = "imp", Blurb = "Fast fire-flinger. Immune to fire; freeze it.",
            MaxHp = 15, Armor = 0, Speed = 8, Row = Row.Back, BreakMax = 1, Gold = 10,
            Affinities = Aff(weak: [Element.Ice, Element.Physical], immune: [Element.Fire]),
            Moves =
            [
                new() { Id = "firebolt", Name = "Firebolt", Element = Element.Fire, Power = 6, Vfx = Vfx.Projectile,
                        Applies = { new StatusApply(StatusKind.Burn, 2, 2) } },
                new() { Id = "cackle", Name = "Cackle", Kind = MoveKind.Empower, Target = MoveTarget.AllAllies, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "golem", Name = "Magma Golem", Sprite = "golem", Blurb = "Slow, armored, fire-immune. Its Eruption is charged: break it with ice.",
            MaxHp = 44, Armor = 6, Speed = 1, Row = Row.Front, BreakMax = 4, Gold = 18,
            Affinities = Aff(weak: [Element.Ice, Element.Lightning], resist: [Element.Physical], immune: [Element.Fire]),
            Moves =
            [
                new() { Id = "molten_slam", Name = "Molten Slam", Melee = true, Element = Element.Fire, Power = 10, Pick = HeroPick.Frontline },
                new() { Id = "eruption", Name = "Eruption", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 8, Charged = true, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Burn, 3, 2) } },
                new() { Id = "harden", Name = "Harden", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 10, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "behemoth", Name = "Magma Behemoth", Sprite = "golem", Tint = "#ffb080", Scale = 1.35f, IsElite = true,
            Blurb = "Elite. When its crust cracks (half HP) it loses its armor and its weaknesses change.",
            MaxHp = 78, Armor = 7, Speed = 2, Row = Row.Front, BreakMax = 5, Gold = 45,
            Affinities = Aff(weak: [Element.Ice], resist: [Element.Physical, Element.Lightning], immune: [Element.Fire]),
            Moves =
            [
                new() { Id = "molten_b", Name = "Molten Fist", Melee = true, Element = Element.Fire, Power = 12, Pick = HeroPick.Frontline },
                new() { Id = "eruption_b", Name = "Cataclysm", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 10, Charged = true, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Burn, 3, 2) } },
            ],
            Phase2Name = "Magma Behemoth (Cracked)",
            Phase2Affinities = Aff(weak: [Element.Ice, Element.Physical], immune: [Element.Fire]),
            Phase2BreakMax = 3,
            Phase2Armor = 0,
            Phase2Moves =
            [
                new() { Id = "rampage", Name = "Rampage", Target = MoveTarget.FrontRow, Melee = true, Element = Element.Fire, Power = 7, Hits = 2 },
                new() { Id = "meltdown", Name = "Meltdown", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 14, Charged = true, Vfx = Vfx.Cloud },
            ],
        };

        // ═════ New threats: one per region, each a target-priority puzzle ═════
        yield return new EnemyDef
        {
            Id = "bellringer", Name = "Bell Ringer", Sprite = "bellringer",
            Blurb = "A hunched sexton. Every third round its FUNERAL TOLL empowers every enemy; its dread toll weakens your party. Silence it first.",
            MaxHp = 30, Armor = 1, Speed = 3, Row = Row.Back, BreakMax = 2, Gold = 16,
            Affinities = Aff(weak: [Element.Physical, Element.Fire], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "toll", Name = "Funeral Toll", Kind = MoveKind.Empower, Target = MoveTarget.AllAllies, Vfx = Vfx.Buff,
                        Applies = { new StatusApply(StatusKind.Empowered, 0, 2) } },
                new() { Id = "dread_toll", Name = "Toll of Dread", Target = MoveTarget.AllHeroes, Power = 2, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 1) } },
                new() { Id = "bell_strike", Name = "Bell Strike", Power = 6, Vfx = Vfx.Projectile },
            ],
        };
        yield return new EnemyDef
        {
            Id = "shellback", Name = "Shellback", Sprite = "shellback",
            Blurb = "A barnacled crab with 6 armor. It hunkers behind its shell when hurt and charges a crushing pincer. Lightning cracks it; armor-piercing and the hammer help.",
            MaxHp = 50, Armor = 6, Speed = 2, Row = Row.Front, BreakMax = 4, Gold = 22,
            Affinities = Aff(weak: [Element.Lightning], resist: [Element.Ice]),
            Moves =
            [
                new() { Id = "pinch", Name = "Pinch", Melee = true, Power = 8, Pick = HeroPick.Frontline },
                new() { Id = "shell_up", Name = "Shell Up", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 10, Vfx = Vfx.Buff },
                new() { Id = "crush", Name = "Pincer Crush", Melee = true, Power = 16, Pick = HeroPick.Frontline, Charged = true,
                        Applies = { new StatusApply(StatusKind.Weakened, 0, 2) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "rime_sentinel", Name = "Rime Sentinel", Sprite = "rime_sentinel",
            Blurb = "An ice totem. It shields wounded allies and chills your party every round. Fire melts it.",
            MaxHp = 34, Armor = 2, Speed = 2, Row = Row.Back, BreakMax = 3, Gold = 18,
            Affinities = Aff(weak: [Element.Fire, Element.Physical], immune: [Element.Ice]),
            Moves =
            [
                new() { Id = "rime_ward", Name = "Rime Ward", Kind = MoveKind.Shield, Target = MoveTarget.Ally, Power = 7, Vfx = Vfx.Buff },
                new() { Id = "chill", Name = "Biting Chill", Target = MoveTarget.AllHeroes, Element = Element.Ice, Power = 2, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Slowed, 0, 1) } },
            ],
        };
        yield return new EnemyDef
        {
            Id = "ballista", Name = "Storm Ballista", Sprite = "ballista",
            Blurb = "A siege engine. Every third round it charges a BALLISTA BOLT that skewers your whole back row: move them forward, or wreck it first.",
            MaxHp = 36, Armor = 3, Speed = 3, Row = Row.Back, BreakMax = 3, Gold = 20,
            Affinities = Aff(weak: [Element.Ice, Element.Physical], resist: [Element.Lightning]),
            Moves =
            [
                new() { Id = "ballista_bolt", Name = "Ballista Bolt", Target = MoveTarget.BackOnly, Power = 18, Accuracy = 100, Charged = true, Vfx = Vfx.Arrow },
                new() { Id = "rattle_shot", Name = "Rattle Shot", Power = 6, Pick = HeroPick.Backline, Vfx = Vfx.Arrow },
                new() { Id = "reload", Name = "Reload", Kind = MoveKind.Defend, Target = MoveTarget.Self, Power = 6, Vfx = Vfx.Buff },
            ],
        };
        yield return new EnemyDef
        {
            Id = "ash_priest", Name = "Ash Priest", Sprite = "ash_priest",
            Blurb = "A priest of the Hollow King. Mends allies and swings a censer that sets your party burning. Immune to fire; ice puts it out.",
            MaxHp = 30, Armor = 1, Speed = 4, Row = Row.Back, BreakMax = 2, Gold = 18,
            Affinities = Aff(weak: [Element.Ice], immune: [Element.Fire]),
            Moves =
            [
                new() { Id = "ash_mend", Name = "Ash Mend", Kind = MoveKind.Heal, Target = MoveTarget.Ally, Power = 10, Vfx = Vfx.Heal },
                new() { Id = "censer", Name = "Censer Swing", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 3, Vfx = Vfx.Cloud,
                        Applies = { new StatusApply(StatusKind.Burn, 2, 2) } },
                new() { Id = "sear", Name = "Sear", Element = Element.Fire, Power = 7, Pick = HeroPick.LowestHp, Vfx = Vfx.Projectile },
            ],
        };

        // ───── Boss ─────
        yield return new EnemyDef
        {
            Id = "hollow_king", Name = "The Hollow King", Sprite = "hollow_king", Scale = 1.4f, IsBoss = true,
            Epithet = "Devourer of the Last Ember", MechanicHint = "He seals your best skill and eats his servants: everything you learned, at once.",
            Blurb = "He devoured the Last Ember's heart. He USURPS your heroes' best abilities (his bubble names the one he'll seal). In phase 2 everything you've learned returns: he eats his own minions (kill them first) and his stolen ember exposes a shifting WEAK POINT every round.",
            MaxHp = 160, Armor = 3, Speed = 5, Row = Row.Front, BreakMax = 5, Gold = 100,
            Affinities = Aff(weak: [Element.Ice, Element.Lightning], resist: [Element.Fire]),
            Moves =
            [
                new() { Id = "grave_blade", Name = "Grave Blade", Melee = true, Power = 10, Pick = HeroPick.Frontline },
                new() { Id = "crown", Name = "Crown of Cinders", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 9,
                        Charged = true, Vfx = Vfx.Cloud, Applies = { new StatusApply(StatusKind.Burn, 3, 2) } },
                new() { Id = "call_dead", Name = "Call the Dead", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "bones", Vfx = Vfx.Buff },
                new() { Id = "decree", Name = "Royal Decree", Power = 4, Pick = HeroPick.LowestHp, Vfx = Vfx.Debuff,
                        Applies = { new StatusApply(StatusKind.Marked, 0, 2), new StatusApply(StatusKind.Weakened, 0, 2) } },
                new() { Id = "usurp", Name = "Usurp", Kind = MoveKind.Usurp, Pick = HeroPick.HighestHp, Vfx = Vfx.Debuff },
            ],
            Phase2Name = "The Ember-Devoured King",
            Phase2ShiftingWeakPoint = true,
            Phase2Affinities = Aff(weak: [Element.Physical, Element.Ice], resist: [Element.Lightning], immune: [Element.Fire]),
            Phase2BreakMax = 6,
            Phase2Moves =
            [
                new() { Id = "rend", Name = "Cinder Rend", Target = MoveTarget.FrontRow, Melee = true, Element = Element.Fire, Power = 9, Hits = 2 },
                new() { Id = "ashfall", Name = "Ashfall", Target = MoveTarget.AllHeroes, Element = Element.Fire, Power = 16,
                        Charged = true, Vfx = Vfx.Cloud, Applies = { new StatusApply(StatusKind.Burn, 4, 2) } },
                new() { Id = "consume", Name = "Consume Servant", Kind = MoveKind.Consume, Target = MoveTarget.Self, Power = 24, Vfx = Vfx.Heal },
                new() { Id = "call_dead2", Name = "Call the Dead", Kind = MoveKind.Summon, Target = MoveTarget.Self, SummonId = "bones", Vfx = Vfx.Buff },
                new() { Id = "usurp2", Name = "Usurp", Kind = MoveKind.Usurp, Pick = HeroPick.HighestHp, Vfx = Vfx.Debuff },
            ],
        };
    }

    /// <summary>Creates a battle-ready enemy, scaled by floor (+8% HP per floor).</summary>
    public static Combatant CreateEnemy(string id, int floor, bool summoned = false, int heat = 0)
    {
        var def = EnemyDefinition(id);
        // Regular enemies have 25% less HP than their listed value (bosses 15% less): shorter, punchier fights.
        double trim = def.IsBoss ? 0.85 : 0.75;
        int hp = (int)Math.Round(def.MaxHp * trim * (1 + 0.03 * floor));
        int breakMax = def.BreakMax + (heat >= 2 && (def.IsElite || def.IsBoss) ? 1 : 0);
        return new Combatant
        {
            DefId = def.Id,
            Name = def.Name,
            Sprite = def.Sprite,
            Tint = def.Tint,
            Scale = def.Scale,
            FlipSprite = def.FlipSprite,
            IsBoss = def.IsBoss,
            IsElite = def.IsElite,
            Enemy = def,
            MaxHp = hp,
            Hp = hp,
            Armor = def.Armor,
            BaseSpeed = def.Speed,
            Dodge = def.Dodge,
            Crit = def.Crit,
            Row = def.Row,
            Slot = 0,
            BreakMax = breakMax,
            Break = breakMax,
            Affinities = new Dictionary<Element, Affinity>(def.Affinities),
            Moves = def.Moves,
            Summoned = summoned,
        };
    }

    // ═════════════════════════ Encounters ═════════════════════════

    public static readonly IReadOnlyList<RegionDef> Regions = new List<RegionDef>
    {
        new(0, "The Ashen Crypt", "Where the dead were laid to rest, and did not stay there.",
            "Backgrounds/crypt.png", "#ffffff", "battle", "bones",
            Easy: [["bones", "bones"], ["bones", "archer"], ["rats", "rats"], ["blight", "bones"], ["moth", "bones"]],
            Hard: [["ghoul", "bones", "archer"], ["ghoul", "moth"], ["rats", "rats", "blight"], ["bones", "bones", "archer"],
                   ["ghoul", "blight"], ["bones", "acolyte", "archer"], ["bones", "bones", "bellringer"], ["ghoul", "bellringer"]],
            Elites: [["gravemaw"], ["gravemaw", "rats"], ["templar", "archer"]],
            Boss: ["bones", "colossus"]),
        new(1, "The Drowned Halls", "Black water fills the old cathedral cellars. Something sings below.",
            "Backgrounds/drowned.png", "#ffffff", "battle_drowned", "thrall",
            Easy: [["thrall", "siren"], ["leech", "leech"], ["thrall", "wraith"], ["leech", "siren"], ["thrall", "caller"]],
            Hard: [["thrall", "thrall", "siren"], ["leech", "wraith", "caller"], ["thrall", "siren", "acolyte"],
                   ["leech", "thrall", "wraith"], ["bulwark", "siren", "leech"], ["shellback", "siren"], ["shellback", "thrall", "leech"]],
            Elites: [["siren_queen"], ["champion", "siren"], ["champion", "leech"]],
            Boss: ["thrall", "matriarch"]),
        new(2, "The Frozen Ossuary", "A catacomb buried in ice. The dead here do not rot; they wait.",
            "Backgrounds/frost.png", "#ffffff", "battle_frost", "wight",
            Easy: [["wight", "wisp"], ["hound", "hound"], ["wight", "hound"], ["wisp", "wisp", "hound"]],
            Hard: [["wight", "wight", "wisp"], ["hound", "hound", "wisp"], ["wight", "wraith", "wisp"], ["bulwark", "wisp", "hound"],
                   ["wight", "rime_sentinel"], ["hound", "hound", "rime_sentinel"]],
            Elites: [["revenant"], ["revenant", "wisp"], ["alpha_hound", "hound"]],
            Boss: ["wight", "lich", "phylactery"]),
        new(3, "The Storm Spire", "A tower of black stone where lightning never stops falling.",
            "Backgrounds/storm.png", "#ffffff", "battle_storm", "harpy",
            Easy: [["harpy", "harpy"], ["construct", "stormcaller"], ["harpy", "stormcaller"], ["construct", "harpy"]],
            Hard: [["construct", "harpy", "stormcaller"], ["construct", "construct", "harpy"], ["harpy", "harpy", "stormcaller"],
                   ["bulwark", "stormcaller", "harpy"], ["construct", "ballista"], ["harpy", "stormcaller", "ballista"]],
            Elites: [["thunderbird"], ["arc_golem", "stormcaller"], ["thunderbird", "harpy"]],
            Boss: ["construct", "tyrant"]),
        new(4, "The Cinder Throne", "The heart of the Last Ember, devoured. Everything here burns.",
            "Backgrounds/cinder.png", "#ffffff", "battle_cinder", "imp",
            Easy: [["imp", "bulwark"], ["imp", "imp"], ["golem", "imp"], ["bulwark", "acolyte"]],
            Hard: [["bulwark", "imp", "acolyte"], ["golem", "imp", "imp"], ["bulwark", "golem", "imp"],
                   ["golem", "bulwark"], ["golem", "acolyte", "blight"], ["golem", "ash_priest"], ["imp", "bulwark", "ash_priest"]],
            Elites: [["behemoth"], ["templar", "imp"], ["behemoth", "imp"], ["templar", "acolyte"]],
            Boss: ["imp", "hollow_king"]),
        GardenRegion,
        ArchiveRegion,
    };

    public static RegionDef Region(int index) => Regions[Math.Clamp(index, 0, Regions.Count - 1)];
    public static int RegionIndex(string key) => Regions.FirstOrDefault(r => r.Key == key)?.Index ?? 0;

    /// <summary>An encounter from the default region for this floor (tests and tools).</summary>
    public static string[] PickEncounter(NodeType type, int floor, Random rng) =>
        PickEncounter(type, Region(DungeonMap.RegionOf(floor)), floor, rng);

    public static string[] PickEncounter(NodeType type, RegionDef region, int floor, Random rng)
    {
        if (type == NodeType.Boss) return region.Boss;
        if (type == NodeType.Elite) return region.Elites[rng.Next(region.Elites.Length)];
        var pool = floor % DungeonMap.FloorsPerRegion == 0 ? region.Easy : region.Hard;
        return pool[rng.Next(pool.Length)];
    }

    public static List<Combatant> CreateEncounter(string[] ids, int floor, int heat = 0)
    {
        // Unknown ids are skipped instead of crashing the battle; if nothing is left, fall back to the region's pool.
        var valid = ids.Where(HasEnemy).ToArray();
        if (valid.Length == 0) valid = Region(DungeonMap.RegionOf(floor)).Easy[0];
        var units = valid.Select(id => CreateEnemy(id, floor, false, heat)).ToList();
        foreach (var row in new[] { Row.Front, Row.Back })
        {
            int slot = 0;
            foreach (var u in units.Where(u => u.Row == row)) u.Slot = slot++;
        }
        return units;
    }
}
