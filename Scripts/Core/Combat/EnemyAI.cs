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

            case "bellringer":
                // The toll comes on a fixed rhythm (rounds 1, 4, 7...), so the player can plan to silence it.
                if (battle.Round % 3 == 1 && !Last("toll")) return Pick("toll");
                if (!Last("dread_toll") && Roll(40)) return Pick("dread_toll");
                return Pick("bell_strike");

            case "shellback":
                if (e.HealthPct < 60 && !Last("shell_up") && !e.Has(StatusKind.Shield) && Roll(50)) return Pick("shell_up");
                if (battle.Round % 3 == 2 && !Last("crush")) return Pick("crush");
                return Pick("pinch");

            case "rime_sentinel":
                if (allies.Any(a => a != e && a.HealthPct < 80 && !a.Has(StatusKind.Shield)) && !Last("rime_ward")) return Pick("rime_ward");
                return Pick("chill");

            case "ballista":
                if (battle.Round % 3 == 1 && !Last("ballista_bolt")) return Pick("ballista_bolt");
                if (!Last("reload") && Roll(25)) return Pick("reload");
                return Pick("rattle_shot");

            case "ash_priest":
                if (allies.Any(a => a.HealthPct < 60) && !Last("ash_mend")) return Pick("ash_mend");
                if (!Last("censer") && Roll(40)) return Pick("censer");
                return Pick("sear");

            case "tyrant":
            {
                // Brand a hero, then charge the Thunderstrike at the branded hero the next round.
                bool p1 = e.Phase == 1;
                // Phase 2: the brand forks over the back row when two heroes stand there.
                bool fork = !p1 && heroes.Count(h => h.Row == Row.Back) >= 2;
                string brand = p1 ? "storm_brand" : fork ? "forked_brand" : "storm_brand2", strike = p1 ? "thunderstrike" : "thunderstrike2";
                if (!p1 && (Last("forked_brand") || Last("storm_brand2")) && heroes.Any(h => h.Has(StatusKind.Marked))) return Pick(strike);
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
                // Phase 2: the tide alternates rows, front then back, so the party has to keep repositioning.
                if (battle.Round % 3 == 2 && !Last("flood2") && !Last("undertow"))
                    return Uses("flood2") <= Uses("undertow") ? Pick("flood2") : Pick("undertow");
                if (battle.Round % 3 == 0 && !Last("drown2")) return Pick("drown2");
                if (!Last("abyss_wail") && Roll(40)) return Pick("abyss_wail");
                return Pick("depths");

            // ── The Rotting Garden ──
            case "thornling":
                return !Last("bristle") && e.HealthPct < 70 && Roll(35) ? Pick("bristle") : Pick("thorn_lash");

            case "spore_puffer":
                return heroes.Count(h => h.Has(StatusKind.Poison)) < 2 && !Last("spore_cloud") ? Pick("spore_cloud") : Pick("puff");

            case "strangler":
                if (battle.Round % 3 == 2 && !Last("crushing_coil")) return Pick("crushing_coil");
                return Pick("root_grasp");

            case "stalker":
                return !e.Has(StatusKind.Empowered) && !Last("stalk") && Roll(30) ? Pick("stalk") : Pick("pounce");

            case "briar_witch":
                if (allies.Count < 3 && !Last("grow") && Uses("grow") < 2) return Pick("grow");
                if (allies.Any(a => a != e && a.HealthPct < 60 && !a.Has(StatusKind.Shield)) && !Last("bark_ward")) return Pick("bark_ward");
                if (!Last("bramble_curse") && Roll(40)) return Pick("bramble_curse");
                return Pick("wither");

            case "seed_pod":
                return Pick("sap_flow");

            case "mother_bloom":
                if (e.Phase == 1)
                {
                    if (allies.Count(a => a.DefId == "seed_pod") < 2 && !Last("sow") && Uses("sow") < 4) return Pick("sow");
                    if (battle.Round % 3 == 2 && !Last("bloom_burst")) return Pick("bloom_burst");
                    if (!Last("petal_storm") && Roll(40)) return Pick("petal_storm");
                    return Pick("vine_whip");
                }
                if (e.HealthPct < 35 && allies.Any(a => a.DefId == "seed_pod") && !Last("devour_pod") && Uses("devour_pod") < 2) return Pick("devour_pod");
                if (allies.Count(a => a.DefId == "seed_pod") < 1 && !Last("sow2") && Uses("sow2") < 3) return Pick("sow2");
                if (battle.Round % 3 == 0 && !Last("bloom_burst2")) return Pick("bloom_burst2");
                return Pick("strangling_roots");

            case "ember_wyrm":
                if (e.Phase == 1)
                {
                    if (battle.Round % 3 == 0 && !Last("flame_breath")) return Pick("flame_breath");
                    if (!Last("wing_gust") && Roll(20)) return Pick("wing_gust");
                    if (!Last("tail_sweep") && Roll(35)) return Pick("tail_sweep");
                    return Pick("wyrm_claw");
                }
                if (battle.Round % 3 == 1 && !Last("inferno")) return Pick("inferno");
                if (!e.Has(StatusKind.Empowered) && !Last("molten_roar") && Roll(30)) return Pick("molten_roar");
                if (!Last("tail_sweep2") && Roll(40)) return Pick("tail_sweep2");
                return Pick("wyrm_claw2");

            // ── The Silent Archive ──
            case "tome":
                return allies.Count > 1 && !allies.Any(a => a.Has(StatusKind.Empowered)) && !Last("read_aloud") && Roll(30)
                    ? Pick("read_aloud") : Pick("page_cut");

            case "ink_wraith":
                return !Last("drown_ink") && Roll(35) ? Pick("drown_ink") : Pick("ink_blot");

            case "gargoyle":
                if (battle.Round % 3 == 0 && !Last("dive")) return Pick("dive");
                if (e.HealthPct < 50 && !e.Has(StatusKind.Shield) && !Last("petrify")) return Pick("petrify");
                return Pick("stone_claw");

            case "quill_swarm":
                return Pick("quill_stab");

            case "index_golem":
                // Catalogue a hero, then Reshelve them next round (the charge gives the party one round to react).
                if (heroes.Any(h => h.Has(StatusKind.Marked)) && !Last("reshelve") && Last("catalogue")) return Pick("reshelve");
                if (!Last("catalogue") && battle.Round % 3 == 1) return Pick("catalogue");
                if (e.HealthPct < 40 && !e.Has(StatusKind.Shield) && !Last("bind_pages")) return Pick("bind_pages");
                return Pick("brass_fist");

            case "archivist":
                if (e.Phase == 1)
                {
                    if (allies.Count < 2 && !Last("summon_tomes") && Uses("summon_tomes") < 3) return Pick("summon_tomes");
                    if (battle.Round % 3 == 2 && !Last("forbidden_word")) return Pick("forbidden_word");
                    if (!Last("redact") && Roll(30)) return Pick("redact");
                    return Pick("index_bolt");
                }
                if (battle.Round % 3 == 1 && !Last("erase")) return Pick("erase");
                if (allies.Count < 2 && !Last("summon_tomes2") && Uses("summon_tomes2") < 2) return Pick("summon_tomes2");
                if (!Last("page_storm") && Roll(40)) return Pick("page_storm");
                return Pick("index_bolt2");

            default:
                return e.Moves[rng.Next(e.Moves.Count)];
        }
    }
}
