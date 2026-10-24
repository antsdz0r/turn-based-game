using System.Collections.Generic;
using System;
using System.Linq;

namespace TBRPG.Core;

/// <summary>
/// Per-enemy decision rules. Runs once per round when intents are chosen; the chosen move is
/// shown to the player and then executed exactly (see Battle.RefreshIntents for retargeting).
/// </summary>
public static class EnemyAI
{
    public static EnemyMoveDef ChooseMove(Battle battle, Combatant e, Random rng)
    {
        EnemyMoveDef? Find(string id) => e.Moves.FirstOrDefault(m => m.Id == id);
        EnemyMoveDef Pick(string id) => Find(id) ?? e.Moves[0];
        bool Roll(int percent) => rng.Next(100) < percent;
        bool Last(string id) => e.LastMoveId == id;
        int Uses(string id) => e.MoveUses.GetValueOrDefault(id);

        var allies = battle.AliveEnemies.ToList();
        var heroes = battle.AliveHeroes.ToList();

        switch (e.DefId)
        {
            case "bones":
                return e.HealthPct < 50 && !Last("rattle") && Roll(40) ? Pick("rattle") : Pick("club");

            case "archer":
                bool crowdedBack = heroes.Count(h => h.Row == Row.Back) >= 2;
                return !Last("pin") && crowdedBack && Roll(40) ? Pick("pin") : Pick("arrow");

            case "ghoul":
                return !Last("gorge") && Roll(35) ? Pick("gorge") : Pick("claws");

            case "acolyte":
                if (allies.Any(a => a.HealthPct < 60) && !Last("dark_mend")) return Pick("dark_mend");
                if (allies.Count > 1 && !allies.Any(a => a.Has(StatusKind.Empowered)) && !Last("blessing") && Roll(50))
                    return Pick("blessing");
                return Pick("cinder_bolt");

            case "wraith":
                return !Last("wail") && Roll(40) ? Pick("wail") : Pick("chill");

            case "bulwark":
                if (allies.Count > 1 && !Last("shield_wall") && allies.Any(a => a != e && !a.Has(StatusKind.Shield)) && Roll(60))
                    return Pick("shield_wall");
                if (e.HealthPct < 40 && !Last("brace")) return Pick("brace");
                return Pick("slam");

            case "blight":
                int poisoned = heroes.Count(h => h.Has(StatusKind.Poison));
                return poisoned < 2 && !Last("miasma") && Roll(45) ? Pick("miasma") : Pick("spit");

            case "caller":
                if (allies.Count < 3 && !Last("raise") && Uses("raise") < 3) return Pick("raise");
                if (!heroes.Any(h => h.Has(StatusKind.Marked)) && !Last("death_mark") && Roll(40)) return Pick("death_mark");
                return Pick("soul_bolt");

            case "gravemaw":
                if (e.Phase == 1)
                {
                    if (!Last("gorge_big") && Roll(30)) return Pick("gorge_big");
                    if (!Last("howl") && Roll(30)) return Pick("howl");
                    return Pick("maul");
                }
                if (e.HealthPct < 30 && !Last("bloodlust")) return Pick("bloodlust");
                if (!Last("devour") && Roll(35)) return Pick("devour");
                return Pick("frenzy_claws");

            case "templar":
                if (!Last("judgment") && Roll(35)) return Pick("judgment");
                if (!e.Has(StatusKind.Shield) && e.HealthPct < 60 && !Last("oath")) return Pick("oath");
                return Pick("cleave");

            case "hollow_king":
                if (e.Phase == 1)
                {
                    if (allies.Count < 2 && !Last("call_dead") && Uses("call_dead") < 3) return Pick("call_dead");
                    // Usurp: steals the strongest ability of the sturdiest hero, every few rounds.
                    if (battle.Round % 4 == 2 && !Last("usurp")) return Pick("usurp");
                    // The crown is telegraphed on a fixed rhythm: it starts charging every third round.
                    if (battle.Round % 3 == 0 && !Last("crown")) return Pick("crown");
                    if (!heroes.Any(h => h.Has(StatusKind.Marked)) && !Last("decree") && Roll(35)) return Pick("decree");
                    return Pick("grave_blade");
                }
                // Devours at most 3 servants and calls the dead at most 4 times in phase 2: no endless heal loops.
                if (allies.Any(a => a.Summoned) && e.HealthPct < 60 && !Last("consume") && Uses("consume") < 3) return Pick("consume");
                if (allies.Count < 2 && !Last("call_dead2") && Uses("call_dead2") < 4) return Pick("call_dead2");
                if (battle.Round % 3 == 1 && !Last("usurp2")) return Pick("usurp2");
                if (!Last("ashfall") && Roll(40)) return Pick("ashfall");
                return Pick("rend");

            case "mimic":
                if (e.HealthPct < 50 && !e.Has(StatusKind.Shield) && !Last("snap_shut")) return Pick("snap_shut");
                return !Last("tongue") && Roll(35) ? Pick("tongue") : Pick("chomp");

            case "wight":
                return e.HealthPct < 45 && !e.Has(StatusKind.Shield) && !Last("frost_guard") ? Pick("frost_guard") : Pick("rime_blade");

            case "wisp":
                return !heroes.Any(h => h.Has(StatusKind.Slowed)) && !Last("chill_aura") && Roll(50) ? Pick("chill_aura") : Pick("frostbolt");

            case "hound":
                return allies.Count > 1 && !allies.Any(a => a.Has(StatusKind.Empowered)) && !Last("hound_howl") && Roll(25)
                    ? Pick("hound_howl") : Pick("hound_rend");

            case "revenant":
                if (battle.Round % 3 == 2 && !Last("permafrost")) return Pick("permafrost");
                if (e.HealthPct < 50 && !e.Has(StatusKind.Shield) && !Last("ice_wall")) return Pick("ice_wall");
                return Pick("glacial_cleave");

            case "alpha_hound":
                if (allies.Count < 3 && !Last("call_pack") && Uses("call_pack") < 3) return Pick("call_pack");
                return Pick("savage");

            case "phylactery":
                return Pick("soul_tether");

            case "lich":
                if (e.Phase == 1)
                {
                    if (battle.Round % 3 == 2 && !Last("glacial_prison")) return Pick("glacial_prison");
                    if (!Last("frost_nova") && Roll(40)) return Pick("frost_nova");
                    return Pick("ice_spear");
                }
                if (!allies.Any(a => a.DefId == "phylactery") && !Last("rebind") && Uses("rebind") < 1) return Pick("rebind");
                if (battle.Round % 3 == 0 && !Last("blizzard")) return Pick("blizzard");
                if (battle.Round % 3 == 1 && !Last("glacial_prison2")) return Pick("glacial_prison2");
                return Pick("ice_spear2");

            case "harpy":
                return !Last("shriek") && Roll(30) ? Pick("shriek") : Pick("talon_dive");

            case "construct":
                if (battle.Round % 3 == 2 && !Last("overcharge")) return Pick("overcharge");
                if (e.HealthPct < 50 && !e.Has(StatusKind.Shield) && !Last("static_field")) return Pick("static_field");
                return Pick("piston");

            case "stormcaller":
                if (allies.Count > 1 && !allies.Any(a => a.Has(StatusKind.Empowered)) && !Last("tailwind") && Roll(35)) return Pick("tailwind");
                return !Last("chain_bolt") && Roll(50) ? Pick("chain_bolt") : Pick("thunderbolt");

            case "thunderbird":
                if (battle.Round % 3 == 2 && !Last("gale")) return Pick("gale");
                return !Last("storm_wings") && Roll(35) ? Pick("storm_wings") : Pick("thunder_dive");

            case "arc_golem":
                return battle.Round % 3 == 2 && !Last("overload") ? Pick("overload") : Pick("arc_slam");

            case "tyrant":
            {
                // Brand a hero, then charge the Thunderstrike at the branded hero the next round.
                bool p1 = e.Phase == 1;
                string brand = p1 ? "storm_brand" : "storm_brand2", strike = p1 ? "thunderstrike" : "thunderstrike2";
                bool someoneBranded = heroes.Any(h => h.Has(StatusKind.Marked));
                if (Last(brand) && someoneBranded) return Pick(strike);
                if (!p1 && allies.Count < 2 && !Last("call_harpies") && Uses("call_harpies") < 3) return Pick("call_harpies");
                if (battle.Round % 3 == 1 && !Last(strike)) return Pick(brand);
                return p1 ? Pick("tempest") : Pick("cyclone");
            }

            case "rats":
                return e.HealthPct < 50 && !Last("scurry") && Roll(35) ? Pick("scurry") : Pick("gnaw");

            case "moth":
                return !heroes.Any(h => h.Has(StatusKind.Weakened)) && !Last("dust") && Roll(55) ? Pick("dust") : Pick("sip");

            case "thrall":
                if (e.HealthPct < 45 && !Last("brace_t") && Roll(40)) return Pick("brace_t");
                return !Last("grasp") && Roll(35) ? Pick("grasp") : Pick("fist");

            case "siren":
                if (allies.Any(a => a.HealthPct < 55) && !Last("hymn")) return Pick("hymn");
                if (!Last("dirge") && Roll(30)) return Pick("dirge");
                return Pick("lament");

            case "leech":
                return e.HealthPct < 55 && !Last("engorge") && Roll(60) ? Pick("engorge") : Pick("latch");

            case "siren_queen":
                if (allies.Count < 2 && !Last("call_thrall") && Uses("call_thrall") < 3) return Pick("call_thrall");
                if (battle.Round % 3 == 2 && !Last("dirge_q")) return Pick("dirge_q");
                return Pick("lament_q");

            case "champion":
                if (!Last("undertow") && Roll(30)) return Pick("undertow");
                if (e.HealthPct < 50 && !e.Has(StatusKind.Shield) && !Last("brace_c")) return Pick("brace_c");
                return Pick("anchor");

            case "imp":
                return allies.Count > 1 && !allies.Any(a => a.Has(StatusKind.Empowered)) && !Last("cackle") && Roll(40)
                    ? Pick("cackle") : Pick("firebolt");

            case "golem":
                if (!Last("eruption") && battle.Round % 3 == 2) return Pick("eruption");
                if (e.HealthPct < 50 && !e.Has(StatusKind.Shield) && !Last("harden")) return Pick("harden");
                return Pick("molten_slam");

            case "behemoth":
                if (e.Phase == 1)
                    return !Last("eruption_b") && battle.Round % 3 == 2 ? Pick("eruption_b") : Pick("molten_b");
                return !Last("meltdown") && Roll(35) ? Pick("meltdown") : Pick("rampage");

            case "colossus":
                if (e.Phase == 1)
                {
                    if (battle.Round % 3 == 0 && !Last("quake")) return Pick("quake");
                    if (e.HealthPct < 70 && !Last("reassemble") && Roll(25)) return Pick("reassemble");
                    if (!Last("barrage") && Roll(40)) return Pick("barrage");
                    return Pick("bone_slam");
                }
                if (allies.Count < 2 && !Last("call_bones") && Uses("call_bones") < 3) return Pick("call_bones");
                if (battle.Round % 3 == 0 && !Last("quake2")) return Pick("quake2");
                return Pick("frenzy_slam");

            case "matriarch":
                if (e.Phase == 1)
                {
                    if (allies.Count < 2 && !Last("call_deep") && Uses("call_deep") < 3) return Pick("call_deep");
                    // The tide is charged a round ahead: pull the front row back before it lands.
                    if (battle.Round % 4 == 2 && !Last("flood")) return Pick("flood");
                    if (battle.Round % 3 == 0 && !Last("drown")) return Pick("drown");
                    if (!Last("wave") && Roll(40)) return Pick("wave");
                    return Pick("riptide");
                }
                if (e.HealthPct < 30 && !Last("tide_mend") && Roll(50)) return Pick("tide_mend");
                if (battle.Round % 3 == 2 && !Last("flood2")) return Pick("flood2");
                if (battle.Round % 3 == 0 && !Last("drown2")) return Pick("drown2");
                if (!Last("abyss_wail") && Roll(40)) return Pick("abyss_wail");
                return Pick("depths");

            default:
                return e.Moves[rng.Next(e.Moves.Count)];
        }
    }
}
