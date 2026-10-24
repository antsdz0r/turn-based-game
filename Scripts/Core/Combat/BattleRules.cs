using System.Collections.Generic;
using System;
using System.Linq;

namespace TBRPG.Core;

/// <summary>Damage, statuses, healing, death and phase rules.</summary>
public sealed partial class Battle
{
    public const double WeakMult = 1.5;
    public const double ResistMult = 0.5;
    public const double CritMult = 1.5;
    public const double MarkedMult = 1.25;
    public const double BrokenMult = 2.0;
    public const double DefendMult = 0.5;
    /// <summary>Poison stacks, but never past this.</summary>
    public const int MaxPoison = 20;
    /// <summary>How many times a boss's break meter grows after it recovers from a break.</summary>
    public const int MaxBossHardening = 2;
    /// <summary>Stormglass Focus: elemental hits needed for an arc.</summary>
    public const int StormglassCharge = 3;

    /// <summary>
    /// One hit. Order: protection redirect → accuracy → crit → multipliers → armor (physical only)
    /// → shield → HP. Then: resources, break meter, riposte, death, boss phase.
    /// Returns the unit actually hit, or null on a miss.
    /// </summary>
    internal Combatant? Strike(Combatant attacker, Combatant target, int power, Element element, int accuracy,
                              int critBonus, bool ignoresArmor, bool melee, int breakBonus, DefenseQuality defense)
    {
        // A Vanguard's Intercept: attacks on a protected hero hit the protector instead.
        if (!attacker.IsHero && target.Get(StatusKind.Guarded) is { } guard)
        {
            var protector = _heroes.FirstOrDefault(h => h.Uid == guard.SourceUid && h.IsAlive);
            if (protector is not null && protector != target)
            {
                Emit(new TextEvent(target, "Protected!", "#8fb3ff"));
                target = protector;
            }
        }

        bool wasGuarded = target.Has(StatusKind.Shield) || target.Has(StatusKind.Defending);
        if (accuracy > 0 && !HasWeapon(attacker, "piercing"))
        {
            int chance = HitChance(target, accuracy - (HasWeapon(attacker, "flail") ? 10 : 0));
            if (_rng.Next(100) >= chance)
            {
                Emit(new MissEvent(target));
                Log($"  {target.Name} evades! [color=#a99f8f]({chance}% to hit: {accuracy} accuracy - {target.Dodge} dodge)[/color]");
                return null;
            }
        }

        if (!target.IsHero && attacker.IsHero && (target.AffinityTo(element) == Affinity.Weak || target.IsExposedTo(element))) WeaknessHits++;
        int critChance = attacker.Crit + critBonus;
        if (attacker.IsHero && HasRelic("glass_heart")) critChance += 25;
        if (target.Has(StatusKind.Marked))
        {
            critChance += 20;
            if (attacker.IsHero && HasRelic("hunters_lens")) critChance += 25;
        }
        bool crit = _rng.Next(100) < critChance;
        // Opening Shot (Wren) / Duelist's Coin: a hero's first attack each battle always crits.
        if (attacker.IsHero && _firstStrike.Add(attacker.Uid) && (HasTrait(attacker, "wren_opening") || HasRelic("duelists_coin")))
            crit = true;

        // Gravemason: the hammer smashes the Shield before the damage lands.
        if (HasWeapon(attacker, "crush") && !target.IsHero && target.Get(StatusKind.Shield) is { } smashed)
        {
            target.Statuses.Remove(smashed);
            Emit(new StatusEvent(target, StatusKind.Shield, false));
            Emit(new TextEvent(target, "Shield smashed!", "#d0c0a0"));
            Log($"  {attacker.Name}'s hammer smashes {target.Name}'s shield ({smashed.Amount}).");
        }

        var affinity = target.AffinityTo(element);
        bool exposed = target.IsExposedTo(element) && affinity != Affinity.Immune;
        var combo = ComboFor(attacker, target, element);       // decided before the hit; resolved after it
        var why = new List<string>();
        int amount = ComputeDamage(attacker, target, power, element, ignoresArmor, crit, defense, why);

        int absorbed = 0;
        if (target.Get(StatusKind.Shield) is { } shield && amount > 0)
        {
            absorbed = Math.Min(shield.Amount, amount);
            shield.Amount -= absorbed;
            amount -= absorbed;
            if (shield.Amount <= 0) target.Statuses.Remove(shield);
        }

        target.Hp = Math.Max(0, target.Hp - amount);
        if (attacker.IsHero)
        {
            DamageDealt += amount;
            DamageByHero[attacker.DefId] = DamageByHero.GetValueOrDefault(attacker.DefId) + amount;
            if (amount > BestHit) { BestHit = amount; BestHitBy = attacker.DefId; }
        }
        if (!attacker.IsHero && attacker.Modifier == "vampiric" && amount > 0 && attacker.IsAlive)
            HealUnit(attacker, Math.Max(1, amount * 40 / 100), fromHero: false);

        Emit(new HitEvent(target, amount, absorbed, target.Hp, element, crit,
                          affinity == Affinity.Weak || exposed, affinity == Affinity.Resist, affinity == Affinity.Immune,
                          target.Broken, defense));
        string tags = (crit ? " [color=#ffd76b]CRIT[/color]" : "")
                      + (affinity == Affinity.Weak ? " [color=#ffa040]WEAK[/color]" : "")
                      + (affinity == Affinity.Resist ? " (resisted)" : "")
                      + (affinity == Affinity.Immune ? " (immune)" : "")
                      + (defense == DefenseQuality.Perfect ? " [color=#8fb3ff]PARRY[/color]" : defense == DefenseQuality.Good ? " (blocked)" : "")
                      + (absorbed > 0 ? $" ({absorbed} absorbed)" : "");
        Log($"  {target.Name} takes {amount} {element} damage.{tags} [color=#7d7590]({string.Join(", ", why)})[/color]");

        // Resources
        if (target.IsHero && target.IsAlive && target.ResourceDef is { OnHit: > 0 } targetRes)
            GainResource(target, targetRes.OnHit);
        if (crit && attacker.IsHero && attacker.ResourceDef is { OnCrit: > 0 } attackerRes)
            GainResource(attacker, attackerRes.OnCrit);

        // Break meter: every source of break chip is summed in BreakChip (shared with the preview).
        if (target.IsAlive)
        {
            if (combo is { BreakBonus: > 0 }) ChipBreak(target, combo.BreakBonus);
            if (wasGuarded && HasWeapon(attacker, "guardbreaker") && !target.IsHero && !target.Broken)
                Emit(new TextEvent(target, "Guard Break!", "#ffb85a"));
            int chip = BreakChip(attacker, target, element, breakBonus, wasGuarded, amount > 0, preview: false);
            if (chip > 0) ChipBreak(target, chip);
            // Penitent's Flail: critical hits chip 2 more break pips.
            if (crit && HasWeapon(attacker, "flail") && !target.IsHero && !target.Broken)
            {
                Emit(new TextEvent(target, "Flail crack!", "#ffb85a"));
                ChipBreak(target, 2);
            }
            // Rimefang Rapier: critical hits Slow the target.
            if (crit && HasWeapon(attacker, "rime_crit") && !target.IsHero)
                ApplyStatus(target, new StatusApply(StatusKind.Slowed, 0, 1), attacker.Uid);
            // The Archivist learns: the element that just hit it is halved until a different one lands.
            if (target.Enemy?.AdaptiveResist == true && amount > 0 && target.AdaptedTo != element)
            {
                target.AdaptedTo = element;
                Emit(new TextEvent(target, $"Adapts to {element}!", "#c070ff"));
                Log($"  [color=#c070ff]{target.Name} has read your {element.ToString().ToLowerInvariant()}: it now takes 40% less damage from it.[/color]");
                Emit(new IntentsChangedEvent());
            }
        }

        // A combo resolves after the hit. If its follow-up (Ignite) finishes the target, DotDamage already ran its death.
        bool diedFromHit = !target.IsAlive;
        if (combo is not null) ResolveCombo(combo, attacker, target, power, amount);

        if (!target.IsAlive)
        {
            if (diedFromHit) OnDeath(target, attacker, countKill: true);
            return target;
        }

        // Stormglass Focus: every third elemental hit arcs to another enemy for half damage (arcs don't charge it).
        if (HasWeapon(attacker, "stormglass") && element != Element.Physical && !_arcing && !target.IsHero)
        {
            int charge = _focusCharge.GetValueOrDefault(attacker.Uid) + 1;
            _focusCharge[attacker.Uid] = charge % StormglassCharge;
            if (charge >= StormglassCharge && AliveEnemies.Where(e => e != target).OrderBy(_ => _rng.Next()).FirstOrDefault() is { } arcTarget)
            {
                _arcing = true;
                Emit(new TextEvent(arcTarget, "Arc!", "#9fd8ff"));
                Log($"  [color=#9fd8ff]Stormglass arcs to {arcTarget.Name}![/color]");
                Strike(attacker, arcTarget, Math.Max(1, power / 2), element, 0, 0, ignoresArmor, false, 0, DefenseQuality.None);
                _arcing = false;
            }
        }

        // Gravemason: every hit sunders 1 armor for the rest of the battle.
        if (HasWeapon(attacker, "crush") && !target.IsHero && target.Armor > 0)
        {
            target.Armor--;
            Emit(new TextEvent(target, "Sundered -1 armor", "#d0c0a0"));
        }
        // Cinder Tome: fire hits set the target Burning.
        if (HasWeapon(attacker, "cinder") && element == Element.Fire && !target.IsHero)
            ApplyStatus(target, new StatusApply(StatusKind.Burn, 2, 2), attacker.Uid);

        // Ashwood Longbow: the first hit each battle marks the target.
        if (HasWeapon(attacker, "hunter") && !target.IsHero && _huntMarked.Add(attacker.Uid))
            ApplyStatus(target, new StatusApply(StatusKind.Marked, 0, 2), attacker.Uid);

        if (attacker.IsHero && element == Element.Ice && amount > 0 && HasRelic("frostbound_locket"))
            ApplyStatus(target, new StatusApply(StatusKind.Slowed, 0, 1), attacker.Uid);

        // Wildfire (Orrin): fire damage sets the target ablaze and the flames leap to another enemy.
        if (attacker.IsHero && element == Element.Fire && amount > 0 && HasTrait(attacker, "orrin_wildfire") && !target.IsHero)
        {
            ApplyStatus(target, new StatusApply(StatusKind.Burn, 2, 2), attacker.Uid);
            var other = AliveEnemies.Where(e => e != target).OrderBy(_ => _rng.Next()).FirstOrDefault();
            if (other is not null)
            {
                Emit(new TextEvent(other, "Wildfire!", "#ff7a3d"));
                ApplyStatus(other, new StatusApply(StatusKind.Burn, 2, 2), attacker.Uid);
            }
        }

        // Guardian Angel (Maela): the first time an ally drops low, she heals them.
        if (target.IsHero && target.IsAlive && target.Hp * 100 < target.MaxHp * 30 && !_guardianSaved.Contains(target.Uid)
            && AliveHeroes.FirstOrDefault(h => HasTrait(h, "maela_martyr")) is { } angel)
        {
            _guardianSaved.Add(target.Uid);
            Emit(new TextEvent(angel, "Guardian Angel!", "#ffe09a"));
            HealUnit(target, 8, fromHero: true);
        }

        // Thorned enemies hurt whoever hits them in melee.
        if (melee && attacker.IsHero && attacker.IsAlive && target.Modifier == "thorned")
        {
            Emit(new TextEvent(attacker, "Thorns!", "#6f9a40"));
            DotDamage(attacker, StatusKind.Bleed, 3, useShield: true);
        }

        // Riposte: melee attackers get countered (counters are not melee, so they can't chain).
        if (melee && attacker.IsAlive && target.Get(StatusKind.Riposte) is { } riposte)
        {
            Emit(new TextEvent(target, "Riposte!", "#ffd76b"));
            bool vengeful = HasTrait(target, "kael_vengeance");
            Strike(target, attacker, riposte.Amount * (vengeful ? 2 : 1), Element.Physical, 0, 0, false, false, 0, DefenseQuality.None);
            if (vengeful) HealUnit(target, 4, fromHero: true);
            // Oathkeeper: the counter also staggers the attacker.
            if (HasWeapon(target, "oathkeeper") && attacker.IsAlive) ChipBreak(attacker, 1);
        }
        else if (melee && attacker.IsAlive && !attacker.IsHero && target.IsHero && target.IsAlive && HasRelic("thorn_pact"))
        {
            Emit(new TextEvent(target, "Thorns!", "#6f9a40"));
            Strike(target, attacker, 5, Element.Physical, 0, 0, false, false, 0, DefenseQuality.None);
        }

        CheckPhase(target);
        return target;
    }

    /// <summary>
    /// Damage before shields: power → attacker bonuses → affinity → crit → marked/broken/defending → parry
    /// → armor (physical only). Shared by real hits and the targeting preview, so the preview never lies.
    /// </summary>
    public int ComputeDamage(Combatant attacker, Combatant target, int power, Element element, bool ignoresArmor,
                             bool crit, DefenseQuality defense, List<string>? why = null)
    {
        // Every multiplier goes through M(), which also records it for the combat log's damage breakdown.
        double dmg = power;
        why?.Add($"{power} base");
        void M(double factor, string label)
        {
            if (Math.Abs(factor - 1) < 0.001) return;
            dmg *= factor;
            why?.Add($"x{factor:0.##} {label}");
        }

        M(1 + attacker.PowerPct / 100.0, "power");
        if (attacker.IsHero && HasRelic("bloodstone_crown")) M(1.3, "Bloodstone Crown");
        if (attacker.IsHero && HasRelic("doom_clock")) M(1.4, "Doom Clock");
        if (target.IsHero && HasRelic("glass_heart")) M(1.2, "Glass Heart");
        if (HasOmen("blood_moon")) M(1.2, "Blood Moon");
        if (attacker.ElementPct.TryGetValue(element, out int elementPct)) M(1 + elementPct / 100.0, element.ToString().ToLowerInvariant());
        if (attacker.IsHero && element == Element.Fire && HasRelic("cinder_idol")) M(1.25, "Cinder Idol");
        if (attacker.Has(StatusKind.Empowered)) M(1.25, "empowered");
        if (attacker.Has(StatusKind.Weakened))
            M(!attacker.IsHero && _heroes.Any(h => h.IsAlive && HasTrait(h, "sable_dread")) ? 0.6 : 0.75, "weakened");
        if (attacker.IsHero)
        {
            if (attacker.Hp * 100 < attacker.MaxHp * 30 && HasTrait(attacker, "brakka_deathwish")) M(1.5, "Death Wish");
            if (element == Element.Ice && target.Has(StatusKind.Slowed) && HasTrait(attacker, "orrin_shatter")) M(1.5, "Shatter");
            if (target.Hp * 100 < target.MaxHp * 35 && HasTrait(attacker, "nyx_execute")) M(1.5, "Executioner");
            if (target.Has(StatusKind.Marked) && HasTrait(attacker, "wren_apex")) M(1.35, "Apex");
            if (attacker.Hp * 2 < attacker.MaxHp && HasRelic("berserker_torc")) M(1.4, "Berserker's Torc");
            if (HasWeapon(attacker, "execution") && target.Broken) M(1.5, "Oathbreaker");
            if (HasWeapon(attacker, "hunter")) M(target.Has(StatusKind.Marked) ? 1.3 : 0.9, "Longbow");
            if (HasWeapon(attacker, "piercing")) M(0.85, "Windpiercer");
            if (HasWeapon(attacker, "reach") && target.IsCharging) M(1.3, "Pike vs charge");
            if (HasWeapon(attacker, "stormglass") && element == Element.Physical) M(0.8, "Stormglass");
            if (HasWeapon(attacker, "cinder") && element != Element.Fire) M(0.85, "Cinder Tome");
            if (element != Element.Physical && HasTrait(attacker, "isolde_edge")) M(1.2, "Elemental Edge");
        }

        if (ComboFor(attacker, target, element) is { DamageMult: > 1 } combo) M(combo.DamageMult, combo.Name + " combo");
        var affinity = target.AffinityTo(element);
        M(affinity switch { Affinity.Weak => WeakMult, Affinity.Resist => ResistMult, Affinity.Immune => 0, _ => 1 },
          affinity.ToString().ToLowerInvariant());
        if (crit) M(CritMult, "crit");
        if (target.IsExposedTo(element) && affinity != Affinity.Immune) M(2, "weak point");
        if (IsWarded(target)) M(0.4, "warded");
        if (target.Enemy?.AdaptiveResist == true && target.AdaptedTo == element) M(0.6, "adapted");
        if (target.Has(StatusKind.Marked)) M(MarkedMult, "marked");
        if (target.Broken) M(BrokenMult, "broken");
        if (target.Has(StatusKind.Defending)) M(DefendMult, "defending");
        // Parry (every beat of the parry sequence perfect): no damage. Block (every beat at least good): -40%.
        M(defense switch { DefenseQuality.Perfect => 0, DefenseQuality.Good => 0.6, _ => 1 }, defense == DefenseQuality.Perfect ? "parry" : "block");

        int amount = (int)Math.Round(dmg);
        bool pierce = ignoresArmor || HasWeapon(attacker, "piercing");
        if (element == Element.Physical && !pierce && affinity != Affinity.Immune && amount > 0)
        {
            int armor = Math.Max(0, target.Armor - (attacker.IsHero && HasTrait(attacker, "wren_piercer") ? 3 : 0));
            if (armor > 0) why?.Add($"-{armor} armor");
            amount = Math.Max(1, amount - armor);
        }
        return Math.Max(0, amount);
    }

    public int HitChance(Combatant target, int accuracy) =>
        accuracy <= 0 ? 100 : Math.Clamp(accuracy - target.Dodge + (target.Has(StatusKind.Marked) ? 10 : 0) - (HasOmen("thick_fog") ? 15 : 0), 0, 100);

    /// <summary>What an ability would do to a target right now (no crit, before shields).</summary>
    public AbilityPreview Preview(Combatant hero, AbilityInstance ability, Combatant target)
    {
        var def = ability.Def;
        bool up = ability.Upgraded;
        int damage = 0, chip = 0;
        bool weak = false, resisted = false, immune = false;
        if (def.Power > 0 && def.TargetsEnemies)
        {
            int power = AbilityPower(hero, def, up, target, def.PowerPerResource > 0 ? hero.Resource : 0);
            for (int i = 0; i < HitsFor(hero, def); i++)
            {
                var element = def.HitElement(i);
                var affinity = target.AffinityTo(element);
                damage += ComputeDamage(hero, target, power, element, def.EffectiveIgnoresArmor(up), false, DefenseQuality.None);
                bool guarded = target.Has(StatusKind.Shield) || target.Has(StatusKind.Defending);
                chip += BreakChip(hero, target, element, def.EffectiveBreakBonus(up), guarded, true, preview: true);
                chip += ComboFor(hero, target, element)?.BreakBonus ?? 0;
                if (affinity == Affinity.Weak || (target.IsExposedTo(element) && affinity != Affinity.Immune)) weak = true;
                if (affinity == Affinity.Resist) resisted = true;
                if (affinity == Affinity.Immune) immune = true;
            }
        }
        bool breaks = !target.IsHero && !target.Broken && target.BreakMax > 0 && chip > 0 && chip >= target.Break;
        int heal = def.Heal > 0 ? (int)Math.Round(def.EffectiveHeal(up) * (HasRelic("menders_thread") ? 1.3 : 1.0)) : 0;
        int shield = def.EffectiveShield(up);
        int chance = def.Power > 0 && def.TargetsEnemies && !HasWeapon(hero, "piercing") ? HitChance(target, def.Accuracy) : 100;
        return new AbilityPreview(damage, chance, weak, resisted, immune, breaks, heal, shield,
                                  damage >= target.Hp + target.Amount(StatusKind.Shield));
    }

    /// <summary>
    /// Break pips one hit removes. Weakness (+1, +1 more on an exposed weak point, + ability/relic/trait bonuses),
    /// Concussive Blows on any damaging hit, Embercleaver vs guarded targets, Pike vs charging targets,
    /// Shatterglass on Slowed targets, Static Sigil on the first lightning hit per enemy.
    /// </summary>
    private int BreakChip(Combatant attacker, Combatant target, Element element, int breakBonus, bool guarded, bool damaging, bool preview)
    {
        if (target.IsHero || target.Broken || target.BreakMax <= 0) return 0;
        var affinity = target.AffinityTo(element);
        if (affinity == Affinity.Immune) return 0;
        bool exposed = target.IsExposedTo(element);
        int chip = 0;
        if (affinity == Affinity.Weak || exposed)
            chip += 1 + breakBonus + (exposed ? 1 : 0)
                    + (element == Element.Lightning && HasRelic("storm_battery") ? 1 : 0)
                    + (element == Element.Lightning && HasTrait(attacker, "orrin_overload") ? 1 : 0);
        else if (damaging && HasTrait(attacker, "kael_concuss")) chip += 1;
        if (!attacker.IsHero) return chip;
        if (guarded && HasWeapon(attacker, "guardbreaker")) chip += 2;
        if (target.IsCharging && HasWeapon(attacker, "reach")) chip += 1;
        if (element == Element.Fire && target.Has(StatusKind.Burn) && HasWeapon(attacker, "cinder")) chip += 1;
        if (element == Element.Physical && target.Has(StatusKind.Slowed) && HasRelic("shatterglass")) chip += 1;
        if (element == Element.Lightning && HasRelic("static_sigil") && !_staticHit.Contains(target.Uid))
        {
            chip += 1;
            if (!preview) _staticHit.Add(target.Uid);
        }
        return chip;
    }

    /// <summary>Removes break pips; at zero the enemy is broken.</summary>
    internal void ChipBreak(Combatant target, int chip)
    {
        if (target.IsHero || !target.IsAlive || target.Broken || target.BreakMax <= 0 || chip <= 0) return;
        target.Break = Math.Max(0, target.Break - chip);
        Emit(new BreakDamageEvent(target, target.Break));
        if (target.Break == 0) DoBreak(target);
    }

    /// <summary>Oathkeeper: a 6-damage Riposte until the hero's next turn.</summary>
    private void ReadyRiposte(Combatant hero)
    {
        if (hero.Amount(StatusKind.Riposte) >= 6) return;   // keep a stronger riposte from an ability
        hero.Remove(StatusKind.Riposte);
        hero.Statuses.Add(new StatusEffect { Kind = StatusKind.Riposte, Amount = 6, Turns = 1, SourceUid = hero.Uid });
        Emit(new StatusEvent(hero, StatusKind.Riposte, true));
        Log($"  {hero.Name} raises Oathkeeper, ready to riposte.");
    }

    private void DoBreak(Combatant enemy)
    {
        Breaks++;
        bool interrupted = enemy.Intent is not null || enemy.ChargedMove is not null;
        enemy.Broken = true;
        enemy.Intent = null;
        enemy.ChargedMove = null;
        if (!enemy.Has(StatusKind.Stun))
            enemy.Statuses.Add(new StatusEffect { Kind = StatusKind.Stun, Amount = 1, Turns = 1 });
        Emit(new BreakEvent(enemy, interrupted));
        Log($"  [color=#ffa040][b]{enemy.Name} is BROKEN![/b] Stunned, takes double damage.[/color]");

        if (HasRelic("shattered_aegis"))
            foreach (var hero in AliveHeroes) AddShield(hero, 5, 2);
        if (HasRelic("headsman_seal"))
        {
            // Never the killing blow here (the hit that broke it resolves its own death).
            int cut = Math.Min(enemy.MaxHp * 15 / 100, enemy.Hp - 1);
            if (cut > 0)
            {
                Emit(new TextEvent(enemy, "Headsman's Seal!", "#ff9b9b"));
                DotDamage(enemy, StatusKind.Bleed, cut, useShield: false);
            }
        }
    }

    private void CheckPhase(Combatant unit)
    {
        var def = unit.Enemy;
        if (def?.Phase2Moves is null || unit.Phase != 1 || !unit.IsAlive || unit.Hp * 2 > unit.MaxHp) return;

        unit.Phase = 2;
        if (def.Phase2Name is not null) unit.Name = def.Phase2Name;
        if (def.Phase2Affinities is not null) unit.Affinities = def.Phase2Affinities;
        unit.BreakMax = def.Phase2BreakMax > 0 ? def.Phase2BreakMax : unit.BreakMax;
        if (def.Phase2Armor is int armor) unit.Armor = armor;
        unit.Break = unit.BreakMax;
        unit.Broken = false;
        unit.Moves = def.Phase2Moves;
        unit.ChargedMove = null;
        unit.Statuses.RemoveAll(s => Combatant.IsNegative(s.Kind));

        Emit(new PhaseChangeEvent(unit, unit.Name));
        Log($"[color=#ffa040][b]{unit.Name} enters a new phase![/b] Its weaknesses have changed.[/color]");

        // Only pick a new intent if it still acts this round; otherwise the shown intent would be fake.
        if (_queue.Contains(unit)) ChooseIntent(unit);
        else unit.Intent = null;
        Emit(new IntentsChangedEvent());
    }

    private void OnDeath(Combatant unit, Combatant? killer, bool countKill)
    {
        // Phoenix Feather: the first hero to fall in a battle rises again.
        if (unit.IsHero && HasRelic("phoenix_feather") && !_phoenixUsed)
        {
            _phoenixUsed = true;
            unit.Hp = Math.Max(1, unit.MaxHp * 30 / 100);
            unit.Statuses.RemoveAll(s => Combatant.IsNegative(s.Kind));
            Emit(new TextEvent(unit, "Phoenix Feather!", "#ffb85a"));
            Emit(new HealEvent(unit, unit.Hp, unit.Hp));
            Log($"  [color=#ffb85a]{unit.Name} rises from the ashes![/color]");
            return;
        }
        if (!unit.IsHero && killer is { IsHero: true, IsAlive: true } && HasTrait(killer, "nyx_spree") && _spreeRound != Round)
        {
            _spreeRound = Round;
            _extraTurn = killer;
        }
        if (!unit.IsHero && killer is { IsHero: true, IsAlive: true } && HasWeapon(killer, "blood_price")) GainResource(killer, 1);
        if (!unit.IsHero && killer is { IsHero: true, IsAlive: true } && unit.Has(StatusKind.Marked) && HasRelic("hunters_tally"))
        {
            GainResource(killer, 2);
            HealUnit(killer, 4, fromHero: true);
            Emit(new TextEvent(killer, "Hunter's Tally!", "#7fd4a0"));
        }
        if (!unit.IsHero && killer is { IsHero: true, IsAlive: true } && HasTrait(killer, "brakka_rampage"))
        {
            GainResource(killer, 2);
            Emit(new TextEvent(killer, "Rampage! +2 Rage", "#ff6a4a"));
        }
        // Contagion (Sable): a dying enemy's afflictions spread to another enemy.
        if (!unit.IsHero && _heroes.Any(h => h.IsAlive && HasTrait(h, "sable_spread")))
        {
            var afflictions = unit.Statuses.Where(s => Combatant.IsNegative(s.Kind) && s.Kind != StatusKind.Stun).ToList();
            var next = AliveEnemies.Where(e => e != unit).OrderBy(_ => _rng.Next()).FirstOrDefault();
            if (afflictions.Count > 0 && next is not null)
            {
                Emit(new TextEvent(next, "Contagion!", "#a8406a"));
                foreach (var s in afflictions) ApplyStatus(next, new StatusApply(s.Kind, s.Amount, Math.Max(1, s.Turns)), s.SourceUid);
            }
        }
        // Splitting enemies break into two smaller copies.
        if (!unit.IsHero && unit.Modifier == "splitting" && !unit.Summoned && unit.Enemy is { } splitDef)
        {
            Emit(new TextEvent(unit, "Splits apart!", "#b3d05a"));
            for (int i = 0; i < 2 && AliveEnemies.Count() < MaxEnemies; i++)
            {
                var half = Database.CreateEnemy(splitDef.Id, Floor, summoned: true, heat: Heat);
                half.Name = $"{splitDef.Name} Shard";
                half.MaxHp = half.Hp = Math.Max(4, unit.MaxHp * 30 / 100);
                half.Row = unit.Row;
                half.Slot = AliveEnemies.Where(e => e.Row == half.Row).Select(e => e.Slot).DefaultIfEmpty(-1).Max() + 1;
                _enemies.Add(half);
                Emit(new SummonEvent(half));
            }
            Log($"  {unit.Name} splits apart!");
        }
        if (unit.IsHero) HeroFalls++;
        unit.Statuses.Clear();
        unit.Intent = null;
        unit.ChargedMove = null;
        unit.Broken = false;
        Emit(new DeathEvent(unit));
        Log($"  [color=#ff6b6b]{unit.Name} falls![/color]");

        // Volatile enemies explode when they die, scorching the heroes' front row.
        if (!unit.IsHero && unit.Modifier == "volatile")
        {
            Log($"  [color=#ff8a2c]{unit.Name} explodes![/color]");
            foreach (var hero in AliveHeroes.Where(h => h.Row == Row.Front).ToList())
            {
                Emit(new TextEvent(hero, "Explosion!", "#ff8a2c"));
                DotDamage(hero, StatusKind.Burn, 6, useShield: true);
            }
        }

        // Spore Puffer: bursts on death and poisons the front row.
        if (!unit.IsHero && unit.DefId == "spore_puffer")
        {
            Log($"  [color=#b3d05a]{unit.Name} bursts into a cloud of spores![/color]");
            foreach (var hero in AliveHeroes.Where(h => h.Row == Row.Front).ToList())
            {
                Emit(new TextEvent(hero, "Spores!", "#b3d05a"));
                ApplyStatus(hero, new StatusApply(StatusKind.Poison, 3, 3), unit.Uid);
            }
        }

        // Destroying a boss's protector (the Lich's phylactery) is rewarded: it tears at the boss's soul.
        foreach (var master in AliveEnemies.Where(e => e.Enemy?.ProtectedBy == unit.DefId).ToList())
        {
            int tear = Math.Min(master.MaxHp * 10 / 100, master.Hp - 1);
            Emit(new TextEvent(master, "Soul torn!", "#9fd8ff"));
            Log($"  [color=#9fd8ff]The {unit.Name} shatters and tears at {master.Name}'s soul![/color]");
            if (tear > 0) DotDamage(master, StatusKind.Bleed, tear, useShield: false);
            ChipBreak(master, 2);
        }

        if (!unit.IsHero && countKill)
        {
            EnemiesSlain++;
            if (killer is { IsHero: true, IsAlive: true } && HasRelic("bloodpact_ring")) HealUnit(killer, 4, fromHero: true);
            if (killer is { IsHero: true, IsAlive: true } && HasRelic("hungry_blade"))
            {
                GainResource(killer, 2);
                Emit(new TextEvent(killer, $"+2 {killer.ResourceDef?.Name}", killer.ResourceDef?.ColorHex ?? "#ffffff"));
            }
        }
    }

    private void TickStatuses(Combatant unit)
    {
        foreach (var status in unit.Statuses.ToList())
        {
            if (!unit.IsAlive) return;
            switch (status.Kind)
            {
                case StatusKind.Poison:
                    DotDamage(unit, StatusKind.Poison, status.Amount * DotMult(unit), useShield: true);
                    status.Amount--;
                    if (status.Amount <= 0) RemoveStatus(unit, status);
                    break;
                case StatusKind.Burn:
                    DotDamage(unit, StatusKind.Burn, status.Amount * DotMult(unit), useShield: true);
                    status.Turns--;
                    if (status.Turns <= 0) RemoveStatus(unit, status);
                    break;
                case StatusKind.Bleed:
                    DotDamage(unit, StatusKind.Bleed, status.Amount * DotMult(unit), useShield: false);
                    status.Turns--;
                    if (status.Turns <= 0) RemoveStatus(unit, status);
                    break;
                case StatusKind.Regen:
                    HealUnit(unit, status.Amount, fromHero: unit.IsHero);
                    status.Turns--;
                    if (status.Turns <= 0) RemoveStatus(unit, status);
                    break;
            }
        }
    }

    private void RemoveStatus(Combatant unit, StatusEffect status)
    {
        unit.Statuses.Remove(status);
        Emit(new StatusEvent(unit, status.Kind, false));
    }

    private void DotDamage(Combatant unit, StatusKind kind, int amount, bool useShield)
    {
        if (amount <= 0) return;
        if (useShield && unit.Get(StatusKind.Shield) is { } shield)
        {
            int absorbed = Math.Min(shield.Amount, amount);
            shield.Amount -= absorbed;
            amount -= absorbed;
            if (shield.Amount <= 0) unit.Statuses.Remove(shield);
        }
        unit.Hp = Math.Max(0, unit.Hp - amount);
        Emit(new TickEvent(unit, kind, amount, unit.Hp));
        Log($"  {unit.Name} suffers {amount} from {kind}.");
        if (!unit.IsAlive) OnDeath(unit, null, countKill: true);
        else CheckPhase(unit);
    }

    private void HealUnit(Combatant unit, int amount, bool fromHero)
    {
        if (!unit.IsAlive || amount <= 0) return;
        if (fromHero && unit.IsHero && HasRelic("menders_thread")) amount = (int)Math.Round(amount * 1.3);
        int healed = Math.Min(amount, unit.MaxHp - unit.Hp);
        unit.Hp += healed;
        Emit(new HealEvent(unit, healed, unit.Hp));
        // Hearthlight (Maela): overhealing becomes a shield.
        if (fromHero && unit.IsHero && amount > healed && _heroes.Any(h => h.IsAlive && HasTrait(h, "maela_overheal")))
            AddShield(unit, amount - healed, 2);
        if (healed > 0) Log($"  {unit.Name} recovers {healed} HP.");
    }

    internal void AddShield(Combatant unit, int amount, int turns)
    {
        if (!unit.IsAlive || amount <= 0) return;
        var shield = unit.Get(StatusKind.Shield);
        if (shield is null)
        {
            shield = new StatusEffect { Kind = StatusKind.Shield, Amount = 0, Turns = turns };
            unit.Statuses.Add(shield);
        }
        // Shields stack, but never beyond the unit's max HP (no unkillable walls).
        shield.Amount = Math.Min(unit.MaxHp, shield.Amount + amount);
        shield.Turns = Math.Max(shield.Turns, turns);
        Emit(new ShieldEvent(unit, amount));
    }

    private void Cleanse(Combatant unit)
    {
        int removed = unit.Statuses.RemoveAll(s => Combatant.IsNegative(s.Kind));
        if (removed > 0)
        {
            Emit(new TextEvent(unit, "Cleansed", "#bfeaff"));
            Log($"  {unit.Name} is cleansed.");
        }
    }

    /// <summary>Applies a status with its stacking rule. Bosses resist stuns that aren't from breaks.</summary>
    internal void ApplyStatus(Combatant target, StatusApply apply, int sourceUid)
    {
        if (!target.IsAlive) return;
        if (apply.Chance < 100 && _rng.Next(100) >= apply.Chance) return;
        if (apply.Kind == StatusKind.Stun && target.IsBoss)
        {
            Emit(new TextEvent(target, "Resists stun", "#a99f8f"));
            return;
        }
        if (apply.Kind == StatusKind.Stun && _steadfastUntil.TryGetValue(target.Uid, out int until) && until >= Round)
        {
            Emit(new TextEvent(target, "Steadfast", "#a99f8f"));
            Log($"  {target.Name} was just stunned and shrugs this one off.");
            return;
        }

        if (apply.Kind == StatusKind.Shield)
        {
            AddShield(target, apply.Amount, apply.Turns > 0 ? apply.Turns : 2);
            return;
        }

        if (apply.Kind == StatusKind.Poison && HasRelic("venom_gland") && _heroes.Any(h => h.Uid == sourceUid))
            apply = apply with { Amount = apply.Amount + 2 };
        if (target.IsHero && HasTrait(target, "brakka_unstoppable") && apply.Kind is StatusKind.Slowed or StatusKind.Weakened or StatusKind.Stun)
        {
            Emit(new TextEvent(target, "Unstoppable!", "#ff6a4a"));
            return;
        }
        if (!target.IsHero && _heroes.FirstOrDefault(h => h.Uid == sourceUid) is { } caster)
        {
            if (apply.Kind == StatusKind.Poison && HasTrait(caster, "sable_agony")) apply = apply with { Amount = apply.Amount + 2 };
            if (apply.Kind == StatusKind.Poison && HasTrait(caster, "corvin_potency")) apply = apply with { Amount = apply.Amount + 2 };
            if (Combatant.IsNegative(apply.Kind) && apply.Kind != StatusKind.Stun && HasTrait(caster, "sable_lasting"))
                apply = apply with { Turns = apply.Turns + 1 };
        }

        var existing = target.Get(apply.Kind);
        if (existing is null)
        {
            int amount = apply.Kind == StatusKind.Poison ? Math.Min(MaxPoison, apply.Amount) : apply.Amount;
            target.Statuses.Add(new StatusEffect { Kind = apply.Kind, Amount = amount, Turns = Math.Max(1, apply.Turns), SourceUid = sourceUid });
        }
        else if (apply.Kind == StatusKind.Poison)
        {
            existing.Amount = Math.Min(MaxPoison, existing.Amount + apply.Amount); // poison stacks, up to a cap
        }
        else
        {
            existing.Amount = Math.Max(existing.Amount, apply.Amount);
            existing.Turns = Math.Max(existing.Turns, apply.Turns);
        }
        Emit(new StatusEvent(target, apply.Kind, true));
        if (apply.Kind is not (StatusKind.Defending)) Log($"  {target.Name}: {apply.Kind}.");
    }
}
