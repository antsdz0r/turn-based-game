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

    /// <summary>
    /// One hit. Order: protection redirect → accuracy → crit → multipliers → armor (physical only)
    /// → shield → HP. Then: resources, break meter, riposte, death, boss phase.
    /// Returns the unit actually hit, or null on a miss.
    /// </summary>
    private Combatant? Strike(Combatant attacker, Combatant target, int power, Element element, int accuracy,
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

        if (accuracy > 0)
        {
            int chance = HitChance(target, accuracy);
            if (_rng.Next(100) >= chance)
            {
                Emit(new MissEvent(target));
                Log($"  {target.Name} evades!");
                return null;
            }
        }

        int critChance = attacker.Crit + critBonus;
        if (target.Has(StatusKind.Marked))
        {
            critChance += 20;
            if (attacker.IsHero && HasRelic("hunters_lens")) critChance += 25;
        }
        bool crit = _rng.Next(100) < critChance;
        // Opening Shot (Wren) / Duelist's Coin: a hero's first attack each battle always crits.
        if (attacker.IsHero && _firstStrike.Add(attacker.Uid) && (HasTrait(attacker, "wren_opening") || HasRelic("duelists_coin")))
            crit = true;

        var affinity = target.AffinityTo(element);
        bool exposed = target.ExposedElement == element && affinity != Affinity.Immune;
        int amount = ComputeDamage(attacker, target, power, element, ignoresArmor, crit, defense);

        int absorbed = 0;
        if (target.Get(StatusKind.Shield) is { } shield && amount > 0)
        {
            absorbed = Math.Min(shield.Amount, amount);
            shield.Amount -= absorbed;
            amount -= absorbed;
            if (shield.Amount <= 0) target.Statuses.Remove(shield);
        }

        target.Hp = Math.Max(0, target.Hp - amount);
        if (attacker.IsHero) DamageDealt += amount;
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
        Log($"  {target.Name} takes {amount} {element} damage.{tags}");

        // Resources
        if (target.IsHero && target.IsAlive && target.ResourceDef is { OnHit: > 0 } targetRes)
            GainResource(target, targetRes.OnHit);
        if (crit && attacker.IsHero && attacker.ResourceDef is { OnCrit: > 0 } attackerRes)
            GainResource(attacker, attackerRes.OnCrit);

        // Break meter: hitting a weakness chips it; at zero the enemy is broken.
        // Kael's Concussive Blows chip 1 pip on any damaging hit.
        bool weakHit = affinity == Affinity.Weak || exposed;
        bool concuss = !weakHit && amount > 0 && HasTrait(attacker, "kael_concuss");
        if (!target.IsHero && target.IsAlive && (weakHit || concuss) && !target.Broken && target.BreakMax > 0)
        {
            int chip = concuss ? 1 : 1 + breakBonus + (exposed ? 1 : 0)
                + (element == Element.Lightning && HasRelic("storm_battery") ? 1 : 0)
                + (element == Element.Lightning && HasTrait(attacker, "orrin_overload") ? 1 : 0);
            target.Break = Math.Max(0, target.Break - chip);
            Emit(new BreakDamageEvent(target, target.Break));
            if (target.Break == 0) DoBreak(target);
        }

        if (!target.IsAlive)
        {
            OnDeath(target, attacker, countKill: true);
            return target;
        }

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
        }

        CheckPhase(target);
        return target;
    }

    /// <summary>
    /// Damage before shields: power → attacker bonuses → affinity → crit → marked/broken/defending → parry
    /// → armor (physical only). Shared by real hits and the targeting preview, so the preview never lies.
    /// </summary>
    public int ComputeDamage(Combatant attacker, Combatant target, int power, Element element, bool ignoresArmor,
                             bool crit, DefenseQuality defense)
    {
        double dmg = power;
        dmg *= 1 + attacker.PowerPct / 100.0;
        if (attacker.ElementPct.TryGetValue(element, out int elementPct)) dmg *= 1 + elementPct / 100.0;
        if (attacker.IsHero && element == Element.Fire && HasRelic("cinder_idol")) dmg *= 1.25;
        if (attacker.Has(StatusKind.Empowered)) dmg *= 1.25;
        if (attacker.Has(StatusKind.Weakened)) dmg *= 0.75;
        // Dread (Sable): Weakened enemies are weakened far more.
        if (!attacker.IsHero && attacker.Has(StatusKind.Weakened) && _heroes.Any(h => h.IsAlive && HasTrait(h, "sable_dread")))
            dmg *= 0.8;   // 0.75 * 0.8 = 0.6: -40% instead of -25%
        if (attacker.IsHero)
        {
            if (attacker.Hp * 100 < attacker.MaxHp * 30 && HasTrait(attacker, "brakka_deathwish")) dmg *= 1.5;
            if (element == Element.Ice && target.Has(StatusKind.Slowed) && HasTrait(attacker, "orrin_shatter")) dmg *= 1.5;
            if (target.Hp * 100 < target.MaxHp * 35 && HasTrait(attacker, "nyx_execute")) dmg *= 1.5;
            if (target.Has(StatusKind.Marked) && HasTrait(attacker, "wren_apex")) dmg *= 1.35;
            if (attacker.Hp * 2 < attacker.MaxHp && HasRelic("berserker_torc")) dmg *= 1.4;
        }

        var affinity = target.AffinityTo(element);
        dmg *= affinity switch
        {
            Affinity.Weak => WeakMult,
            Affinity.Resist => ResistMult,
            Affinity.Immune => 0,
            _ => 1,
        };
        if (crit) dmg *= CritMult;
        if (target.ExposedElement == element && affinity != Affinity.Immune) dmg *= 2;   // boss weak point
        if (IsWarded(target)) dmg *= 0.4;                                               // phylactery ward
        if (target.Has(StatusKind.Marked)) dmg *= MarkedMult;
        if (target.Broken) dmg *= BrokenMult;
        if (target.Has(StatusKind.Defending)) dmg *= DefendMult;
        dmg *= defense switch
        {
            DefenseQuality.Perfect => 0.25,
            DefenseQuality.Good => 0.6,
            _ => 1,
        };

        int amount = (int)Math.Round(dmg);
        if (element == Element.Physical && !ignoresArmor && affinity != Affinity.Immune && amount > 0)
        {
            int armor = target.Armor - (attacker.IsHero && HasTrait(attacker, "wren_piercer") ? 3 : 0);
            amount = Math.Max(1, amount - Math.Max(0, armor));
        }
        return Math.Max(0, amount);
    }

    public int HitChance(Combatant target, int accuracy) =>
        accuracy <= 0 ? 100 : Math.Clamp(accuracy - target.Dodge + (target.Has(StatusKind.Marked) ? 10 : 0), 0, 100);

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
            for (int i = 0; i < def.HitCount; i++)
            {
                var element = def.HitElement(i);
                var affinity = target.AffinityTo(element);
                damage += ComputeDamage(hero, target, power, element, def.IgnoresArmor, false, DefenseQuality.None);
                bool exposedHit = target.ExposedElement == element && affinity != Affinity.Immune;
                if (affinity == Affinity.Weak || exposedHit)
                {
                    weak = true;
                    if (exposedHit) chip += 1;
                    chip += 1 + def.BreakBonus + (element == Element.Lightning && HasRelic("storm_battery") ? 1 : 0)
                            + (element == Element.Lightning && HasTrait(hero, "orrin_overload") ? 1 : 0);
                }
                else if (affinity != Affinity.Immune && HasTrait(hero, "kael_concuss")) chip += 1;
                if (affinity == Affinity.Resist) resisted = true;
                if (affinity == Affinity.Immune) immune = true;
            }
        }
        bool breaks = !target.IsHero && !target.Broken && target.BreakMax > 0 && chip > 0 && chip >= target.Break;
        int heal = def.Heal > 0 ? (int)Math.Round(def.EffectiveHeal(up) * (HasRelic("menders_thread") ? 1.3 : 1.0)) : 0;
        int shield = def.EffectiveShield(up);
        int chance = def.Power > 0 && def.TargetsEnemies ? HitChance(target, def.Accuracy) : 100;
        return new AbilityPreview(damage, chance, weak, resisted, immune, breaks, heal, shield,
                                  damage >= target.Hp + target.Amount(StatusKind.Shield));
    }

    private void DoBreak(Combatant enemy)
    {
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
        unit.Statuses.Clear();
        unit.Intent = null;
        unit.ChargedMove = null;
        unit.Broken = false;
        Emit(new DeathEvent(unit));
        Log($"  [color=#ff6b6b]{unit.Name} falls![/color]");

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

    private void AddShield(Combatant unit, int amount, int turns)
    {
        if (!unit.IsAlive || amount <= 0) return;
        var shield = unit.Get(StatusKind.Shield);
        if (shield is null)
        {
            shield = new StatusEffect { Kind = StatusKind.Shield, Amount = 0, Turns = turns };
            unit.Statuses.Add(shield);
        }
        shield.Amount += amount;
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
    private void ApplyStatus(Combatant target, StatusApply apply, int sourceUid)
    {
        if (!target.IsAlive) return;
        if (apply.Chance < 100 && _rng.Next(100) >= apply.Chance) return;
        if (apply.Kind == StatusKind.Stun && target.IsBoss)
        {
            Emit(new TextEvent(target, "Resists stun", "#a99f8f"));
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
            if (Combatant.IsNegative(apply.Kind) && apply.Kind != StatusKind.Stun && HasTrait(caster, "sable_lasting"))
                apply = apply with { Turns = apply.Turns + 1 };
        }

        var existing = target.Get(apply.Kind);
        if (existing is null)
        {
            target.Statuses.Add(new StatusEffect { Kind = apply.Kind, Amount = apply.Amount, Turns = Math.Max(1, apply.Turns), SourceUid = sourceUid });
        }
        else if (apply.Kind == StatusKind.Poison)
        {
            existing.Amount += apply.Amount; // poison stacks
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
