using System;
using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

public sealed class BattleSetup
{
    public required List<Combatant> Heroes { get; init; }
    public required List<Combatant> Enemies { get; init; }
    /// <summary>Relic ids owned by the party (read by the rules).</summary>
    public HashSet<string> Relics { get; init; } = new();
    /// <summary>The party's consumables. Shared with the run: using one removes it.</summary>
    public List<string> Consumables { get; init; } = new();
    public int Floor { get; init; }
    public int Seed { get; init; }
    /// <summary>Difficulty level 0-5 (see Heat).</summary>
    public int Heat { get; init; }
    /// <summary>Daily challenge modifier id, or null.</summary>
    public string? Daily { get; init; }
    /// <summary>Battlefield condition for this fight (see Omens), or null.</summary>
    public string? Omen { get; init; }
    /// <summary>Optional objective for this fight (see Bounties), or null.</summary>
    public string? Bounty { get; init; }
    /// <summary>Weekly challenge mutators that the battle rules read (frenzy, ironclad, war drums).</summary>
    public HashSet<string> Mutators { get; init; } = new();
}

/// <summary>
/// Turn-based battle with an explicit state machine:
///
///   NotStarted → (round start: enemies pick intents, turn order by speed)
///             → HeroTurn  (waits for UseAbility / Defend / UseItem / Move)
///             → EnemyTurn (waits for ExecuteEnemyTurn, after the UI's optional parry input)
///             → ... → Victory | Defeat
///
/// Every command validates that it is legal; illegal commands do nothing and return false,
/// so double clicks, dead heroes and stale targets can never break the battle.
/// </summary>
public sealed partial class Battle
{
    public const int MaxEnemies = 4;
    public const int MaxFrontRow = 2;
    /// <summary>A perfect parry's counterattack: this many times the parrying hero's basic attack power.</summary>
    public const double ParryCounterMult = 1.5;

    /// <summary>
    /// How many beats the parry prompt has for an attack (Clair Obscur style: every beat must be timed):
    /// 2 for ordinary attacks, 3 for elites, bosses, multi-hit and charged attacks.
    /// </summary>
    public static int ParryBeats(Combatant enemy, EnemyMoveDef move) =>
        enemy.IsBoss || enemy.IsElite || move.Hits >= 2 || move.Charged ? 3 : 2;

    private readonly List<Combatant> _heroes;
    private readonly List<Combatant> _enemies;
    private readonly Queue<Combatant> _queue = new();
    private readonly Random _rng;

    public HashSet<string> Relics { get; }
    public HashSet<string> Mutators { get; }
    public List<string> Consumables { get; }
    public int Floor { get; }
    public int Heat { get; }
    public double EnemyPowerMult { get; }

    public BattleState State { get; private set; } = BattleState.NotStarted;
    public int Round { get; private set; }
    public Combatant? Current { get; private set; }

    public int EnemiesSlain { get; private set; }
    public int DamageDealt { get; private set; }

    public event Action<BattleEvent>? Happened;
    public event Action<string>? Logged;

    public Battle(BattleSetup setup)
    {
        _heroes = setup.Heroes;
        _enemies = setup.Enemies;
        Relics = setup.Relics;
        Consumables = setup.Consumables;
        Floor = setup.Floor;
        Heat = setup.Heat;
        // Enemies hit harder (and have less HP, see Database.CreateEnemy): fights are short and decisive.
        EnemyPowerMult = (1 + 0.028 * setup.Floor + (Heat >= 2 ? 0.10 : 0)) * 1.15;
        _rng = new Random(setup.Seed);
        Daily = setup.Daily;
        Omen = setup.Omen;
        Bounty = setup.Bounty;
        Mutators = setup.Mutators;
        if (Daily == "frenzy" || Mutators.Contains("frenzy"))
        {
            EnemyPowerMult *= 1.3;
            foreach (var e in _enemies) e.MaxHp = e.Hp = Math.Max(1, (int)Math.Round(e.MaxHp * 0.7));
        }
        if (Relics.Contains("thorn_pact")) foreach (var h in _heroes) h.Armor = Math.Max(0, h.Armor - 2);
        NormalizeSlots(_heroes);
        NormalizeSlots(_enemies);
    }

    public IReadOnlyList<Combatant> Heroes => _heroes;
    public IReadOnlyList<Combatant> Enemies => _enemies;
    public IEnumerable<Combatant> AliveHeroes => _heroes.Where(h => h.IsAlive);
    public IEnumerable<Combatant> AliveEnemies => _enemies.Where(e => e.IsAlive);
    public bool IsOver => State is BattleState.Victory or BattleState.Defeat;

    /// <summary>The current actor followed by everyone still to act this round.</summary>
    public IReadOnlyList<Combatant> TurnOrder
    {
        get
        {
            var list = new List<Combatant>();
            if (Current is not null && Current.IsAlive) list.Add(Current);
            list.AddRange(_queue.Where(c => c.IsAlive));
            return list;
        }
    }

    public bool HasRelic(string id) => Relics.Contains(id);

    // ═════════════════════════ Flow ═════════════════════════

    public void Start()
    {
        if (State != BattleState.NotStarted) return;
        Log("[b]The battle begins.[/b]");
        foreach (var hero in AliveHeroes)
        {
            if (HasRelic("ember_heart")) ApplyStatus(hero, new StatusApply(StatusKind.Regen, 2, 3), -1);
            if (HasRelic("warding_bell")) AddShield(hero, 6, 2);
        }
        if (Heat >= 5)
            foreach (var enemy in AliveEnemies) AddShield(enemy, 5, 2);
        foreach (var enemy in AliveEnemies.Where(e => e.Modifier == "armored")) AddShield(enemy, 10, 3);
        if (HasRelic("hollow_lantern"))
            foreach (var hero in AliveHeroes) hero.Hp = Math.Max(1, Math.Min(hero.Hp, hero.MaxHp * 4 / 5));
        if (Mutators.Contains("ironclad")) foreach (var enemy in AliveEnemies) AddShield(enemy, 6, 2);
        if (Mutators.Contains("war_drums"))
            foreach (var enemy in AliveEnemies) ApplyStatus(enemy, new StatusApply(StatusKind.Empowered, 0, 2), enemy.Uid);
        foreach (var hero in AliveHeroes.Where(h => HasTrait(h, "brakka_warcry")))
            ApplyStatus(hero, new StatusApply(StatusKind.Empowered, 0, 2), hero.Uid);
        if (HasRelic("childs_lantern"))
            foreach (var hero in AliveHeroes) HealUnit(hero, Math.Max(1, hero.MaxHp / 10), fromHero: true);
        if (AliveHeroes.Any(h => HasTrait(h, "kael_rally")))
            foreach (var hero in AliveHeroes) AddShield(hero, 5, 2);
        if (AliveHeroes.Any(h => HasTrait(h, "maela_blessing")))
            foreach (var hero in AliveHeroes) ApplyStatus(hero, new StatusApply(StatusKind.Regen, 2, 3), hero.Uid);
        Advance();
    }

    /// <summary>Runs turns until a decision is needed (a hero's turn or an enemy's turn) or the battle ends.</summary>
    private void Advance()
    {
        int guard = 0;
        while (guard++ < 500)
        {
            if (CheckEnd()) return;
            if (_queue.Count == 0) BeginRound();

            var actor = _queue.Dequeue();
            if (!actor.IsAlive) continue;

            Current = actor;
            StartTurn(actor);
            if (CheckEnd()) return;
            if (!actor.IsAlive)
            {
                Current = null;
                continue;
            }

            if (actor.Has(StatusKind.Stun))
            {
                actor.Remove(StatusKind.Stun);
                Emit(new TextEvent(actor, "Stunned", "#ffd76b"));
                Log($"{Name(actor)} is stunned and loses the turn.");
                // No stun-locks: after losing a turn, a unit shrugs off new stuns until the end of next round.
                _steadfastUntil[actor.Uid] = Round + 1;
                actor.Intent = null;
                EndTurn(actor);
                continue;
            }

            if (!actor.IsHero && IsDoomed(actor))
            {
                // Its master is about to devour it: as shown on its bubble, it doesn't act.
                Emit(new TextEvent(actor, "Cowers", "#ff9b9b"));
                Log($"{Name(actor)} cowers before its master.");
                actor.Intent = null;
                EndTurn(actor);
                continue;
            }

            State = actor.IsHero ? BattleState.HeroTurn : BattleState.EnemyTurn;
            Emit(new TurnStartEvent(actor));
            return;
        }
        // Safety net: should never happen, but never soft-lock.
        State = AliveHeroes.Any() ? BattleState.Victory : BattleState.Defeat;
    }

    private void BeginRound()
    {
        Round++;
        foreach (var enemy in AliveEnemies.ToList()) ChooseIntent(enemy);

        var order = _heroes.Concat(_enemies)
            .Where(c => c.IsAlive)
            .OrderByDescending(c => c.Speed)
            .ThenBy(c => c.IsHero ? 0 : 1)
            .ThenBy(c => c.Row)
            .ThenBy(c => c.Slot)
            .ToList();
        foreach (var c in order) _queue.Enqueue(c);

        Emit(new RoundStartEvent(Round));
        Log($"[color=#8fb3ff]— Round {Round} —[/color]");

        // Bone Colossus: its core shifts and exposes a new weak point every round.
        foreach (var boss in AliveEnemies.Where(e => e.Enemy is { } d && (d.ShiftingWeakPoint || (d.Phase2ShiftingWeakPoint && e.Phase >= 2))))
        {
            // Phase 2 escalation: its cracked shell exposes TWO weak points, but it attacks harder.
            var options = new[] { Element.Physical, Element.Fire, Element.Ice, Element.Lightning }
                .Where(el => el != boss.ExposedElement && boss.AffinityTo(el) != Affinity.Immune)
                .OrderBy(_ => _rng.Next()).ToList();
            boss.ExposedElement = options[0];
            boss.ExposedElement2 = boss.Enemy!.ShiftingWeakPoint && boss.Phase >= 2 && options.Count > 1 ? options[1] : null;
            string exposedText = boss.ExposedElement2 is { } second ? $"{boss.ExposedElement} and {second}" : $"{boss.ExposedElement}";
            Emit(new TextEvent(boss, $"Weak point: {exposedText}!", "#ffd76b"));
            Log($"  [color=#ffd76b]{boss.Name}'s core shifts: it is exposed to {exposedText}![/color]");
            Emit(new IntentsChangedEvent());
        }

        OmenRoundStart();
        RegionRoundStart();
        if (HasRelic("doom_clock") && Round >= 7)
        {
            Log("  [color=#d8d6e6]The Doom Clock strikes.[/color]");
            foreach (var hero in AliveHeroes.ToList())
            {
                Emit(new TextEvent(hero, "Doom!", "#d8d6e6"));
                DotDamage(hero, StatusKind.Bleed, 4, useShield: true);
            }
        }

        // Storm Crown: every third round, lightning strikes every enemy.
        if (HasRelic("storm_crown") && Round % 3 == 0 && AliveHeroes.FirstOrDefault() is { } caller)
        {
            Emit(new TextEvent(caller, "Storm Crown!", "#d9b8ff"));
            Log("  [color=#d9b8ff]The Storm Crown calls down lightning![/color]");
            foreach (var enemy in AliveEnemies.ToList())
                Strike(caller, enemy, 6, Element.Lightning, 0, 0, false, false, 0, DefenseQuality.None);
        }
    }

    private void StartTurn(Combatant actor)
    {
        actor.Remove(StatusKind.Defending);
        actor.Remove(StatusKind.Riposte);
        foreach (var hero in _heroes)
            hero.Statuses.RemoveAll(s => s.Kind == StatusKind.Guarded && s.SourceUid == actor.Uid);

        TickStatuses(actor);
        if (!actor.IsAlive) return;

        foreach (var ability in actor.Abilities)
            if (ability.CooldownLeft > 0) ability.CooldownLeft--;
        if (actor.IsHero && actor.ResourceDef is not null)
            GainResource(actor, actor.ResourceDef.RegenPerTurn
                                + (HasTrait(actor, "orrin_focus") ? 1 : 0) + (HasTrait(actor, "maela_devotion") ? 1 : 0)
                                + (HasTrait(actor, "sable_harvest") ? 1 : 0) + (HasTrait(actor, "corvin_brewmaster") ? 1 : 0)
                                + (HasTrait(actor, "isolde_attune") ? 1 : 0));
    }

    private void EndTurn(Combatant actor)
    {
        foreach (var status in actor.Statuses.ToList())
        {
            if (status.Kind is StatusKind.Marked or StatusKind.Weakened or StatusKind.Empowered
                or StatusKind.Taunt or StatusKind.Slowed or StatusKind.Shield)
            {
                status.Turns--;
                if (status.Turns <= 0)
                {
                    actor.Statuses.Remove(status);
                    Emit(new StatusEvent(actor, status.Kind, false));
                }
            }
        }

        if (actor.Broken && !actor.Has(StatusKind.Stun))
        {
            actor.Broken = false;
            actor.TimesBroken++;
            // Bosses steel themselves: each break makes the next one take one more pip (up to +2).
            if (actor.IsBoss && actor.TimesBroken <= MaxBossHardening)
            {
                actor.BreakMax++;
                Log($"  [color=#c9bfae]{actor.Name} steels itself: its break meter grows to {actor.BreakMax}.[/color]");
            }
            actor.Break = actor.BreakMax;
            Emit(new TextEvent(actor, actor.IsBoss && actor.TimesBroken <= MaxBossHardening ? "Steeled (+1 break pip)" : "Recovered", "#c9bfae"));
        }

        Current = null;
    }

    private bool CheckEnd()
    {
        if (IsOver) return true;
        if (!AliveEnemies.Any())
        {
            State = BattleState.Victory;
            Current = null;
            Emit(new BattleEndEvent(true));
            Log("[color=#ffd76b][b]Victory![/b][/color]");
            return true;
        }
        if (!AliveHeroes.Any())
        {
            State = BattleState.Defeat;
            Current = null;
            Emit(new BattleEndEvent(false));
            Log("[color=#ff6b6b][b]The party has fallen.[/b][/color]");
            return true;
        }
        return false;
    }

    // ═════════════════════════ Hero commands ═════════════════════════

    public bool IsHeroTurn(Combatant hero) =>
        State == BattleState.HeroTurn && Current == hero && hero.IsAlive;

    public bool CanUse(Combatant hero, AbilityInstance ability, out string reason)
    {
        reason = "";
        var def = ability.Def;
        if (!IsHeroTurn(hero)) { reason = "Not this hero's turn."; return false; }
        if (ability.CooldownLeft > 0) { reason = $"Ready in {ability.CooldownLeft} turn(s)."; return false; }
        if (EffectiveCost(hero, def) > hero.Resource) { reason = $"Needs {EffectiveCost(hero, def)} {hero.ResourceDef?.Name}."; return false; }
        if (def.MinResource > hero.Resource) { reason = $"Needs at least {def.MinResource} {hero.ResourceDef?.Name}."; return false; }
        var requires = def.EffectiveRequires(ability.Upgraded);
        if (requires == RowRequirement.Front && hero.Row != Row.Front) { reason = "Must be in the FRONT row."; return false; }
        if (requires == RowRequirement.Back && hero.Row != Row.Back) { reason = "Must be in the BACK row."; return false; }
        if (def.Target is TargetKind.SingleEnemy or TargetKind.SingleAlly or TargetKind.AllEnemies && ValidTargets(hero, ability).Count == 0)
        { reason = "No valid target."; return false; }
        return true;
    }

    public IReadOnlyList<Combatant> ValidTargets(Combatant hero, AbilityInstance ability)
    {
        var def = ability.Def;
        return def.Target switch
        {
            // Warden's Pike: melee attacks reach past the front row.
            TargetKind.SingleEnemy or TargetKind.AllEnemies => TargetableEnemies(def.Melee && !HasWeapon(hero, "reach")).ToList(),
            TargetKind.SingleAlly => AliveHeroes.Where(h => !def.Protect || h != hero).ToList(),
            TargetKind.AllAllies => AliveHeroes.ToList(),
            _ => new List<Combatant> { hero },
        };
    }

    /// <summary>Melee can only reach the front row while it has a living unit.</summary>
    public IEnumerable<Combatant> TargetableEnemies(bool melee)
    {
        var alive = AliveEnemies.ToList();
        if (melee && alive.Any(e => e.Row == Row.Front)) return alive.Where(e => e.Row == Row.Front);
        return alive;
    }

    public IEnumerable<Combatant> TargetableHeroes(bool melee)
    {
        var alive = AliveHeroes.ToList();
        if (melee && alive.Any(h => h.Row == Row.Front)) return alive.Where(h => h.Row == Row.Front);
        return alive;
    }

    private bool UseAbilityCore(Combatant hero, AbilityInstance ability, Combatant? target)
    {
        if (!CanUse(hero, ability, out _)) return false;
        var def = ability.Def;
        bool up = ability.Upgraded;
        var valid = ValidTargets(hero, ability);
        if (def.NeedsTarget && (target is null || !valid.Contains(target))) return false;

        // Pay
        int spent = 0;
        if (def.PowerPerResource > 0)
        {
            spent = hero.Resource;
            hero.Resource = 0;
        }
        else hero.Resource -= EffectiveCost(hero, def);
        if (def.Cooldown > 0) ability.CooldownLeft = def.EffectiveCooldown(up) + 1;
        int selfDamage = def.SelfDamage + (def.Cost > 0 && HasWeapon(hero, "blood_price") ? 2 : 0);
        if (selfDamage > 0)
        {
            hero.Hp = Math.Max(1, hero.Hp - selfDamage);
            Emit(new TickEvent(hero, StatusKind.Bleed, selfDamage, hero.Hp));
        }
        int dealtBefore = DamageDealt;

        var targets = def.Target switch
        {
            TargetKind.SingleEnemy or TargetKind.SingleAlly => new List<Combatant> { target! },
            TargetKind.Self => new List<Combatant> { hero },
            _ => valid.ToList(),
        };

        bool big = def.Cost >= 3 || (def.PowerPerResource > 0 && hero.Resource + def.Cost >= 4);
        bool finisher = (def.PowerPerResource > 0 && spent >= 4) || def.Id.StartsWith("duo_") || def.Id is "meteor" or "trinity_cut" or "prismatic";
        Emit(new ActionEvent(hero, ability.DisplayName, def.Vfx, def.Element, targets, big, finisher));
        Log($"[color=#8fd18f]{hero.Name}[/color] uses [b]{ability.DisplayName}[/b].");
        if (def.Cost >= 3 && HasTrait(hero, "orrin_echo") && _rng.Next(100) < 30)
        {
            GainResource(hero, def.Cost);
            Emit(new TextEvent(hero, "Spell Echo!", "#6fd0c0"));
            Log("  The spell echoes: its cost is refunded.");
        }

        foreach (var t in targets)
        {
            bool landed = true;
            if (def.Power > 0 && def.TargetsEnemies)
            {
                landed = false;
                int power = AbilityPower(hero, def, up, t, spent);
                for (int i = 0; i < HitsFor(hero, def) && t.IsAlive; i++)
                {
                    var hit = Strike(hero, t, power, def.HitElement(i), def.Accuracy, def.CritBonus, def.EffectiveIgnoresArmor(up),
                                     def.Melee, def.EffectiveBreakBonus(up), DefenseQuality.None);
                    if (hit is not null) landed = true;
                }
            }

            if (def.EffectiveInterrupt(up) && landed && t.IsAlive && (t.ChargedMove is not null || t.Intent is { Charging: true }))
            {
                t.ChargedMove = null;
                t.Intent = null;
                Emit(new TextEvent(t, "Interrupted!", "#ffa040"));
                Log($"  {t.Name}'s attack is interrupted!");
            }

            if (landed && t.IsAlive)
            {
                foreach (var apply in def.EffectiveApplies(up).Where(a => !a.OnSelf))
                    ApplyStatus(t, apply, hero.Uid);
                if (def.Power > 0 && def.TargetsEnemies)
                {
                    if (HasTrait(hero, "maela_holyfire")) ApplyStatus(t, new StatusApply(StatusKind.Burn, 2, 2), hero.Uid);
                    if (HasTrait(hero, "nyx_venom")) ApplyStatus(t, new StatusApply(StatusKind.Poison, 2, 3), hero.Uid);
                }
            }
            if (def.Heal > 0) HealUnit(t, (int)Math.Round(def.EffectiveHeal(up) * (HasTrait(hero, "corvin_medic") ? 1.3 : 1.0)), fromHero: true);
            if (def.Shield > 0)
                AddShield(t, (int)Math.Round(def.EffectiveShield(up) * (HasTrait(hero, "maela_ward") ? 1.5 : 1.0)), 2);
            if (def.EffectiveCleanse(up)) Cleanse(t);
            if (def.SwitchTargetRow && t.IsAlive) SwitchRow(t);
            if (def.Protect && t.IsAlive && t != hero)
            {
                t.Remove(StatusKind.Guarded);
                t.Statuses.Add(new StatusEffect { Kind = StatusKind.Guarded, Amount = 1, Turns = 1, SourceUid = hero.Uid });
                Emit(new StatusEvent(t, StatusKind.Guarded, true));
                Log($"  {hero.Name} shields {t.Name} with their body.");
                if (HasWeapon(hero, "oathkeeper")) ReadyRiposte(hero);
            }
        }

        // Gravewheat Scythe: the basic attack sweeps through to a second enemy at half power.
        if (def.IsBasic && def.Target == TargetKind.SingleEnemy && def.Power > 0 && HasWeapon(hero, "reap") && hero.IsAlive
            && AliveEnemies.Where(e => !targets.Contains(e)).OrderBy(e => e.Row).ThenBy(e => e.Slot).FirstOrDefault() is { } swept)
        {
            Emit(new TextEvent(swept, "Reap!", "#d0c0a0"));
            Strike(hero, swept, Math.Max(1, AbilityPower(hero, def, up, swept, spent) / 2), def.HitElement(0), def.Accuracy, def.CritBonus,
                   def.EffectiveIgnoresArmor(up), false, 0, DefenseQuality.None);
        }

        // Vigil Lantern: damage from abilities mends the most wounded ally for 20% of it.
        if (!def.IsBasic && HasWeapon(hero, "lantern") && DamageDealt - dealtBefore > 0
            && AliveHeroes.OrderBy(h => h.Hp * 100 / Math.Max(1, h.MaxHp)).FirstOrDefault() is { } wounded)
            HealUnit(wounded, Math.Max(1, (DamageDealt - dealtBefore) * 20 / 100), fromHero: true);

        // Lifesteal (Bloodthirst, Siphon Soul, Brakka's Bloodbath)
        int steal = def.LifestealPct + (def.Power > 0 && def.TargetsEnemies && HasTrait(hero, "brakka_bloodbath") ? 15 : 0);
        int dealt = DamageDealt - dealtBefore;
        if (steal > 0 && dealt > 0 && hero.IsAlive)
        {
            int amount = Math.Max(1, dealt * steal / 100);
            if (HasTrait(hero, "sable_reach")) foreach (var ally in AliveHeroes.ToList()) HealUnit(ally, amount, fromHero: true);
            else HealUnit(hero, amount, fromHero: true);
        }

        foreach (var apply in def.EffectiveApplies(up).Where(a => a.OnSelf))
            ApplyStatus(hero, apply, hero.Uid);
        if (def.SwitchSelfRow) SwitchRow(hero);
        if (def.IsBasic && hero.ResourceDef is not null) GainResource(hero, hero.ResourceDef.OnBasic);

        FinishHeroAction(hero);
        return true;
    }

    public bool CanDefend(Combatant hero) => IsHeroTurn(hero);

    private bool DefendCore(Combatant hero)
    {
        if (!CanDefend(hero)) return false;
        Emit(new ActionEvent(hero, "Defend", Vfx.Buff, Element.Physical, new List<Combatant> { hero }));
        Log($"[color=#8fd18f]{hero.Name}[/color] takes a defensive stance.");
        ApplyStatus(hero, new StatusApply(StatusKind.Defending, 1, 1), hero.Uid);
        if (HasTrait(hero, "kael_ironwall") || HasTrait(hero, "isolde_bladeward")) AddShield(hero, 6, 1);
        GainResource(hero, 1);
        // Defending also shakes off the worst lingering affliction (never Stun or a Mark/brand).
        if (hero.Statuses.Where(s => s.Kind is StatusKind.Bleed or StatusKind.Burn or StatusKind.Poison
                                     or StatusKind.Weakened or StatusKind.Slowed)
                .OrderBy(s => s.Kind switch { StatusKind.Bleed => 0, StatusKind.Burn => 1, StatusKind.Poison => 2, StatusKind.Weakened => 3, _ => 4 })
                .FirstOrDefault() is { } affliction)
        {
            RemoveStatus(hero, affliction);
            Emit(new TextEvent(hero, $"Shakes off {affliction.Kind}", "#bfeaff"));
            Log($"  {hero.Name} shakes off {affliction.Kind}.");
        }
        if (HasWeapon(hero, "oathkeeper")) ReadyRiposte(hero);
        if (HasRelic("sentinels_oath") && AliveHeroes.Where(h => h != hero).OrderBy(h => h.HealthPct).FirstOrDefault() is { } ward)
            AddShield(ward, 6, 2);
        FinishHeroAction(hero);
        return true;
    }

    public bool CanMove(Combatant hero, out string reason)
    {
        reason = "";
        if (!IsHeroTurn(hero)) { reason = "Not this hero's turn."; return false; }
        if (AliveHeroes.Count() < 2 && hero.Row == Row.Front) { reason = "No one to cover the front row."; return false; }
        return true;
    }

    /// <summary>Switch rows. If the front row is full, swap places with a front-row ally.</summary>
    private bool MoveCore(Combatant hero)
    {
        if (!CanMove(hero, out _)) return false;
        Emit(new ActionEvent(hero, hero.Row == Row.Front ? "Fall back" : "Advance", Vfx.Buff, Element.Physical, new List<Combatant> { hero }));
        SwitchRow(hero);
        FinishHeroAction(hero);
        return true;
    }

    public bool CanUseItem(Combatant hero, int index, out string reason)
    {
        reason = "";
        if (!IsHeroTurn(hero)) { reason = "Not this hero's turn."; return false; }
        if (index < 0 || index >= Consumables.Count) { reason = "No such item."; return false; }
        var item = Database.Item(Consumables[index]);
        if (item.ReviveHpPct > 0 && !_heroes.Any(h => !h.IsAlive)) { reason = "No fallen hero to revive."; return false; }
        return true;
    }

    public IReadOnlyList<Combatant> ItemTargets(ItemDef item)
    {
        if (item.ReviveHpPct > 0) return _heroes.Where(h => !h.IsAlive).ToList();
        if (item.TargetsAlly) return AliveHeroes.ToList();
        if (item.TargetsEnemy) return AliveEnemies.ToList();
        return new List<Combatant>();
    }

    private bool UseItemCore(Combatant hero, int index, Combatant? target)
    {
        if (!CanUseItem(hero, index, out _)) return false;
        var item = Database.Item(Consumables[index]);
        var targets = ItemTargets(item);
        if (item.NeedsTarget && (target is null || !targets.Contains(target))) return false;

        if (HasRelic("alchemist_kit") && !_freeItemUsed)
        {
            _freeItemUsed = true;
            Emit(new TextEvent(hero, "Alchemist's Kit: not used up!", "#9ef0d6"));
        }
        else Consumables.RemoveAt(index);
        Log($"[color=#8fd18f]{hero.Name}[/color] uses [b]{item.Name}[/b].");

        if (item.DamageAll > 0)
        {
            var enemies = AliveEnemies.ToList();
            Emit(new ActionEvent(hero, item.Name, Vfx.Projectile, item.DamageElement, enemies));
            foreach (var e in enemies)
            {
                Strike(hero, e, item.DamageAll, item.DamageElement, 0, 0, false, false, 0, DefenseQuality.None);
                if (item.BreakAll > 0 && e.IsAlive) ChipBreak(e, item.BreakAll);
            }
        }
        else if (item.TargetsEnemy)
        {
            var e = target!;
            Emit(new ActionEvent(hero, item.Name, Vfx.Projectile, item.DamageElement, new List<Combatant> { e }));
            Strike(hero, e, 4, item.DamageElement, 0, 0, false, false, 0, DefenseQuality.None);
            if (e.IsAlive && e.IsCharging)
            {
                if (e.IsBoss)
                {
                    Emit(new TextEvent(e, "Staggered!", "#ffb85a"));
                    Log($"  {e.Name} is too mighty to interrupt, but staggers.");
                    ChipBreak(e, 2);
                }
                else
                {
                    e.ChargedMove = null;
                    e.Intent = null;
                    Emit(new TextEvent(e, "Interrupted!", "#ffa040"));
                    Log($"  {e.Name}'s attack is interrupted!");
                    Emit(new IntentsChangedEvent());
                }
            }
        }
        else if (item.ShieldAll > 0)
        {
            var allies = AliveHeroes.ToList();
            Emit(new ActionEvent(hero, item.Name, Vfx.Buff, Element.Physical, allies));
            foreach (var ally in allies) AddShield(ally, item.ShieldAll, 2);
        }
        else
        {
            var t = target!;
            Emit(new ActionEvent(hero, item.Name, Vfx.Heal, Element.Fire, new List<Combatant> { t }));
            if (item.ReviveHpPct > 0 && !t.IsAlive)
            {
                t.Hp = Math.Max(1, t.MaxHp * item.ReviveHpPct / 100);
                t.Statuses.Clear();
                Emit(new HealEvent(t, t.Hp, t.Hp));
                Emit(new TextEvent(t, "Revived!", "#6be07a"));
                Log($"  {t.Name} rises again!");
            }
            if (item.HealAmount > 0) HealUnit(t, item.HealAmount, fromHero: true);
            if (item.ResourceAmount > 0)
            {
                GainResource(t, item.ResourceAmount);
                Emit(new TextEvent(t, $"+{item.ResourceAmount} {t.ResourceDef?.Name}", t.ResourceDef?.ColorHex ?? "#ffffff"));
            }
            if (item.CleanseAll) Cleanse(t);
            if (item.RegenAmount > 0) ApplyStatus(t, new StatusApply(StatusKind.Regen, item.RegenAmount, 3), hero.Uid);
        }

        FinishHeroAction(hero);
        return true;
    }

    private void FinishHeroAction(Combatant hero)
    {
        if (CheckEnd()) return;
        // Killing Spree (Nyx): a kill earns another turn right away (once per round).
        if (_extraTurn == hero && hero.IsAlive)
        {
            _extraTurn = null;
            Emit(new TextEvent(hero, "Killing Spree!", "#ff7ab0"));
            Log($"  [color=#ff7ab0]{hero.Name} strikes again![/color]");
            RefreshIntents();
            Current = hero;
            State = BattleState.HeroTurn;
            Emit(new TurnStartEvent(hero));
            return;
        }
        _extraTurn = null;
        EndTurn(hero);
        RefreshIntents();
        Advance();
    }

    // ═════════════════════════ Enemy turn ═════════════════════════

    /// <summary>True when the current enemy is about to attack heroes (the UI may run the parry input).</summary>
    public bool EnemyAttackPending =>
        State == BattleState.EnemyTurn && Current?.Intent is { Charging: false } intent
        && intent.Move.Kind == MoveKind.Attack && intent.Move.HitsHeroes;

    /// <summary>Who the pending enemy attack will hit (for the parry prompt).</summary>
    public IReadOnlyList<Combatant> PendingAttackTargets =>
        Current?.Intent is { } intent ? IntentTargets(Current, intent) : new List<Combatant>();

    private void ExecuteEnemyTurnCore(DefenseQuality defense)
    {
        if (State != BattleState.EnemyTurn || Current is null) return;
        var enemy = Current;
        var intent = enemy.Intent;

        if (intent is null)
        {
            Emit(new TextEvent(enemy, "Waits", "#a99f8f"));
        }
        else if (intent.Charging)
        {
            enemy.ChargedMove = intent.Move;
            Emit(new ChargeEvent(enemy, intent.Move.Name));
            Log($"[color=#ff9b9b]{enemy.Name}[/color] gathers power for [b]{intent.Move.Name}[/b]!");
        }
        else
        {
            ExecuteMove(enemy, intent, defense);
        }

        enemy.Intent = null;
        if (CheckEnd()) return;
        if (enemy.IsAlive) EndTurn(enemy);
        else Current = null;
        RefreshIntents();
        Advance();
    }

    private void ExecuteMove(Combatant enemy, Intent intent, DefenseQuality defense)
    {
        var move = intent.Move;
        enemy.LastMoveId = move.Id;
        enemy.MoveUses[move.Id] = enemy.MoveUses.GetValueOrDefault(move.Id) + 1;
        var targets = IntentTargets(enemy, intent);
        bool big = move.Kind == MoveKind.Attack &&
                   (move.Charged || ScaledPower(move.Power, enemy) * move.Hits >= 16 || (enemy.IsBoss && move.Target == MoveTarget.AllHeroes));
        Emit(new ActionEvent(enemy, move.Name, move.Vfx, move.Element, targets, big));
        Log($"[color=#ff9b9b]{enemy.Name}[/color] uses [b]{move.Name}[/b]!");

        if (move.Target == MoveTarget.Branded && targets.Count == 0)
            Log("  ...but no one bears the brand. The lightning strikes empty stone.");

        switch (move.Kind)
        {
            case MoveKind.Attack:
                for (int i = 0; i < move.Hits; i++)
                {
                    foreach (var t in targets)
                    {
                        if (!t.IsAlive) continue;
                        var hit = Strike(enemy, t, ScaledPower(move.Power, enemy), move.Element, move.Accuracy, 0, false,
                                         move.Melee, 0, defense);
                        // A perfect parry also deflects the attack's afflictions.
                        if (hit is not null && hit.IsAlive && defense != DefenseQuality.Perfect)
                            foreach (var apply in move.Applies.Where(a => !a.OnSelf))
                                ApplyStatus(hit, apply, enemy.Uid);
                        if (!enemy.IsAlive) break; // killed by a riposte
                    }
                    if (!enemy.IsAlive || !AliveHeroes.Any()) break;
                }
                // Perfect parry: the defending hero counterattacks (like a riposte, but anyone can do it).
                if (defense == DefenseQuality.Perfect && enemy.IsAlive && targets.FirstOrDefault(t => t.IsHero && t.IsAlive) is { } parrier)
                {
                    int counter = (int)Math.Round((parrier.Basic?.Def.EffectivePower(parrier.Basic.Upgraded) ?? 6) * ParryCounterMult);
                    Emit(new TextEvent(parrier, "Counter!", "#8fb3ff"));
                    Log($"  [color=#8fb3ff]{parrier.Name} parries everything and counterattacks![/color]");
                    Strike(parrier, enemy, counter, Element.Physical, 0, 0, false, false, 1, DefenseQuality.None);
                }
                break;

            case MoveKind.Heal:
                foreach (var t in targets) HealUnit(t, ScaledPower(move.Power), fromHero: false);
                break;

            case MoveKind.Shield:
                foreach (var t in targets)
                {
                    if (move.Power > 0) AddShield(t, ScaledPower(move.Power), 2);
                    foreach (var apply in move.Applies.Where(a => !a.OnSelf)) ApplyStatus(t, apply, enemy.Uid);
                }
                break;

            case MoveKind.Empower:
                foreach (var t in targets)
                    foreach (var apply in move.Applies.Where(a => !a.OnSelf)) ApplyStatus(t, apply, enemy.Uid);
                break;

            case MoveKind.Defend:
                ApplyStatus(enemy, new StatusApply(StatusKind.Defending, 1, 1), enemy.Uid);
                if (move.Power > 0) AddShield(enemy, ScaledPower(move.Power), 2);
                break;

            case MoveKind.Summon:
                if (move.SummonId is not null && AliveEnemies.Count() < MaxEnemies)
                {
                    var summoned = Database.CreateEnemy(move.SummonId, Floor, summoned: true, heat: Heat);
                    summoned.Row = Row.Front;
                    summoned.Slot = AliveEnemies.Where(e => e.Row == Row.Front).Select(e => e.Slot).DefaultIfEmpty(-1).Max() + 1;
                    _enemies.Add(summoned);
                    NormalizeSlots(AliveEnemies.ToList());
                    Emit(new SummonEvent(summoned));
                    Log($"  {summoned.Name} answers the call!");
                }
                else Log("  ...but nothing answers.");
                break;

            case MoveKind.Usurp:
                // Steal the target's strongest ready ability: it goes on a long cooldown and the King grows stronger.
                foreach (var t in targets)
                {
                    var stolen = UsurpTarget(t);
                    if (stolen is null) continue;
                    stolen.CooldownLeft = 3;
                    Emit(new TextEvent(t, $"{stolen.Def.Name} stolen!", "#c070ff"));
                    Log($"  [color=#c070ff]{enemy.Name} usurps {t.Name}'s {stolen.Def.Name}![/color] (sealed for 3 turns)");
                }
                ApplyStatus(enemy, new StatusApply(StatusKind.Empowered, 0, 2), enemy.Uid);
                break;

            case MoveKind.Consume:
                var minion = intent.Target is { IsAlive: true, Summoned: true } marked && marked != enemy
                    ? marked
                    : AliveEnemies.Where(e => e != enemy && e.Summoned).OrderBy(e => e.Hp).FirstOrDefault();
                if (minion is not null)
                {
                    minion.Hp = 0;
                    OnDeath(minion, null, countKill: false);
                    HealUnit(enemy, ScaledPower(move.Power), fromHero: false);
                    Log($"  {enemy.Name} devours {minion.Name}!");
                }
                else
                {
                    AddShield(enemy, ScaledPower(move.Power) / 2, 2);
                    Log($"  With nothing to devour, {enemy.Name} hardens its shell.");
                }
                break;
        }

        if (enemy.IsAlive)
            foreach (var apply in move.Applies.Where(a => a.OnSelf)) ApplyStatus(enemy, apply, enemy.Uid);
    }

    /// <summary>True when another enemy plans to devour this servant this round (shown on its bubble).</summary>
    /// <summary>True while a boss's protector (the Lich's phylactery) still stands.</summary>
    public bool IsWarded(Combatant unit) =>
        unit.Enemy?.ProtectedBy is { } guardian && AliveEnemies.Any(e => e.DefId == guardian);

    public bool IsDoomed(Combatant unit) =>
        AliveEnemies.Any(e => e != unit && e.Intent is { Charging: false } i && i.Move.Kind == MoveKind.Consume && i.Target == unit);

    private int ScaledPower(int power, Combatant? attacker = null) =>
        (int)Math.Round(power * EnemyPowerMult * (attacker?.DamageMult ?? 1.0));

    /// <summary>Damage a move will deal before modifiers (for intent display).</summary>
    public int IntentPower(EnemyMoveDef move, Combatant? enemy = null) =>
        ScaledPower(move.Power, move.Kind == MoveKind.Attack ? enemy : null);

    // ═════════════════════════ Intents ═════════════════════════

    private void ChooseIntent(Combatant enemy)
    {
        if (enemy.Has(StatusKind.Stun))
        {
            enemy.Intent = null;
            return;
        }
        if (enemy.ChargedMove is not null)
        {
            enemy.Intent = new Intent { Move = enemy.ChargedMove, Charging = false };
            enemy.ChargedMove = null;
        }
        else
        {
            var move = ApplyTactics(enemy, EnemyAI.ChooseMove(this, enemy, _rng));
            if (move.Kind == MoveKind.Summon && AliveEnemies.Count() >= MaxEnemies)
                move = enemy.Moves.FirstOrDefault(m => m.Kind == MoveKind.Attack) ?? move;
            if (move.Kind == MoveKind.Consume && !AliveEnemies.Any(e => e != enemy && e.Summoned))
                move = enemy.Moves.FirstOrDefault(m => m.Kind == MoveKind.Attack) ?? move;
            enemy.Intent = new Intent { Move = move, Charging = move.Charged };
        }
        enemy.Intent.Target = PickTarget(enemy, enemy.Intent.Move, null);
    }

    /// <summary>Concrete target for single-target moves. Keeps the current target while it stays valid.</summary>
    private Combatant? PickTarget(Combatant enemy, EnemyMoveDef move, Combatant? current)
    {
        if (move.Target == MoveTarget.Hero)
        {
            var candidates = TargetableHeroes(move.Melee).ToList();
            if (candidates.Count == 0) return null;
            var taunter = AliveHeroes.FirstOrDefault(h => h.Has(StatusKind.Taunt));
            if (taunter is not null) return taunter;
            if (current is not null && candidates.Contains(current)) return current;
            return move.Pick switch
            {
                HeroPick.LowestHp => candidates.OrderBy(h => h.Hp).First(),
                HeroPick.HighestHp => candidates.OrderByDescending(h => h.Hp).First(),
                HeroPick.Backline => candidates.OrderByDescending(h => h.Row).ThenByDescending(h => h.Slot).First(),
                HeroPick.Frontline => candidates.OrderBy(h => h.Row).ThenBy(h => h.Slot).First(),
                HeroPick.Marked => candidates.OrderByDescending(h => h.Has(StatusKind.Marked)).ThenByDescending(h => h.Hp).First(),
                _ => Smart(enemy) ? SmartPick(enemy, move, candidates) : candidates[_rng.Next(candidates.Count)],
            };
        }
        if (move.Kind == MoveKind.Consume)
        {
            if (current is { IsAlive: true, Summoned: true }) return current;
            return AliveEnemies.Where(e => e != enemy && e.Summoned).OrderBy(e => e.Hp).FirstOrDefault();
        }
        if (move.Target == MoveTarget.Ally)
        {
            if (current is not null && current.IsAlive) return current;
            // Cover the ally closest to breaking first, then the most wounded.
            bool shielding = move.Kind is MoveKind.Shield or MoveKind.Defend;
            return AliveEnemies.OrderBy(e => shielding && !e.Broken && e.Break <= 1 && !e.Has(StatusKind.Shield) ? 0 : 1)
                .ThenBy(e => e.HealthPct).ThenBy(e => e == enemy ? 1 : 0).FirstOrDefault();
        }
        return null;
    }

    /// <summary>Exactly who an intent will affect if it happened right now.</summary>
    public IReadOnlyList<Combatant> IntentTargets(Combatant enemy, Intent intent)
    {
        var move = intent.Move;
        if (intent.Charging) return new List<Combatant>();
        var heroes = AliveHeroes.ToList();
        if (move.Kind == MoveKind.Consume)
        {
            var servant = intent.Target is { IsAlive: true } ? intent.Target : PickTarget(enemy, move, null);
            return servant is null ? new List<Combatant> { enemy } : new List<Combatant> { servant };
        }
        switch (move.Target)
        {
            case MoveTarget.Hero:
                var t = intent.Target is { IsAlive: true } ? intent.Target : PickTarget(enemy, move, null);
                return t is null ? new List<Combatant>() : new List<Combatant> { t };
            case MoveTarget.FrontRow:
                var front = heroes.Where(h => h.Row == Row.Front).ToList();
                return front.Count > 0 ? front : heroes;
            case MoveTarget.FrontOnly:
                return heroes.Where(h => h.Row == Row.Front).ToList();
            case MoveTarget.BackOnly:
                return heroes.Where(h => h.Row == Row.Back).ToList();
            case MoveTarget.BackRow:
                var back = heroes.Where(h => h.Row == Row.Back).ToList();
                return back.Count > 0 ? back : heroes;
            case MoveTarget.AllHeroes:
                return heroes;
            case MoveTarget.Branded:
                return heroes.Where(h => h.Has(StatusKind.Marked)).ToList();
            case MoveTarget.Self:
                return new List<Combatant> { enemy };
            case MoveTarget.Ally:
                var ally = intent.Target is { IsAlive: true } ? intent.Target : PickTarget(enemy, move, null);
                return ally is null ? new List<Combatant>() : new List<Combatant> { ally };
            default:
                return AliveEnemies.ToList();
        }
    }

    /// <summary>
    /// Keeps every shown intent truthful after anything changes (taunt, deaths, row changes):
    /// single-target moves re-pick only when their target became invalid, or a taunter appeared.
    /// </summary>
    private void RefreshIntents()
    {
        bool changed = false;
        foreach (var enemy in AliveEnemies)
        {
            if (enemy.Intent is not { } intent || intent.Charging) continue;
            if (intent.Move.Target is not (MoveTarget.Hero or MoveTarget.Ally) && intent.Move.Kind != MoveKind.Consume) continue;
            var picked = PickTarget(enemy, intent.Move, intent.Target);
            if (picked != intent.Target)
            {
                intent.Target = picked;
                changed = true;
            }
        }
        if (changed) Emit(new IntentsChangedEvent());
    }

    // ═════════════════════════ Helpers ═════════════════════════

    private void NormalizeSlots(List<Combatant> units)
    {
        foreach (var row in new[] { Row.Front, Row.Back })
        {
            int slot = 0;
            foreach (var u in units.Where(u => u.Row == row).OrderBy(u => u.Slot).ToList()) u.Slot = slot++;
        }
    }

    private void SwitchRow(Combatant unit)
    {
        var team = unit.IsHero ? _heroes : _enemies;
        if (unit.Row == Row.Back)
        {
            var front = team.Where(u => u.IsAlive && u.Row == Row.Front && u != unit).OrderByDescending(u => u.Slot).ToList();
            if (front.Count >= MaxFrontRow)
            {
                var swap = front[0];
                swap.Row = Row.Back;
                swap.Slot = unit.Slot;
                Emit(new MoveRowEvent(swap));
            }
            unit.Row = Row.Front;
            unit.Slot = 99;
        }
        else
        {
            unit.Row = Row.Back;
            unit.Slot = -1;
        }
        NormalizeSlots(team);
        Emit(new MoveRowEvent(unit));
        Log($"  {unit.Name} moves to the {unit.Row.ToString().ToLowerInvariant()} row.");
    }

    private void GainResource(Combatant hero, int amount)
    {
        if (amount <= 0 || !hero.IsHero) return;
        hero.Resource = Math.Min(hero.MaxResource, hero.Resource + amount);
    }

    private static string Name(Combatant c) => c.IsHero ? $"[color=#8fd18f]{c.Name}[/color]" : $"[color=#ff9b9b]{c.Name}[/color]";

    private void Emit(BattleEvent e) => Happened?.Invoke(e);
    private void Log(string text) => Logged?.Invoke(text);
}
