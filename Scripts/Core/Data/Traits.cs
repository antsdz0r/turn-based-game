using System.Collections.Generic;
using System.Linq;

namespace TBRPG.Core;

/// <summary>
/// A level-up trait: on each level-up a hero picks 1 of 3. Stat traits are applied in HeroState.ComputeStats;
/// the rest are rules checked by id in the battle code (Battle.HasTrait).
/// </summary>
public sealed record TraitDef(string Id, string HeroId, string Name, string Description,
                              int MaxHp = 0, int Armor = 0, int Speed = 0, int Crit = 0, int Dodge = 0,
                              int PowerPct = 0, int StartResource = 0);

public static partial class Database
{
    public static readonly IReadOnlyList<TraitDef> AllTraits = new List<TraitDef>
    {
        // ── Kael, the Vanguard: tank, protector, and now a breaker ──
        new("kael_ironwall", "vanguard", "Iron Wall", "Defending also grants Kael a 6 Shield."),
        new("kael_vengeance", "vanguard", "Vengeful Riposte", "Riposte counters deal double damage and heal Kael 4 HP."),
        new("kael_juggernaut", "vanguard", "Juggernaut", "+2 armor and +12 max HP.", MaxHp: 12, Armor: 2),
        new("kael_rally", "vanguard", "Rallying Cry", "At the start of every battle, all heroes gain a 5 Shield."),
        new("kael_concuss", "vanguard", "Concussive Blows", "Kael's hits chip 1 break pip even when they don't hit a weakness."),
        new("kael_resolve", "vanguard", "Unbreakable Will", "Start every battle with +3 Resolve.", StartResource: 3),

        // ── Orrin, the Arcanist: elemental engine ──
        new("orrin_wildfire", "arcanist", "Wildfire", "Orrin's fire damage applies 2 Burn, and the flames leap to a second enemy."),
        new("orrin_shatter", "arcanist", "Shatter", "Ice damage against Slowed enemies is increased by 50%."),
        new("orrin_overload", "arcanist", "Overload", "Orrin's lightning hits on a weakness chip 1 extra break pip."),
        new("orrin_focus", "arcanist", "Deep Focus", "+1 Mana every turn."),
        new("orrin_echo", "arcanist", "Spell Echo", "Abilities costing 3+ Mana have a 30% chance to refund their cost."),
        new("orrin_glass", "arcanist", "Glass Cannon", "+25% damage, but -8 max HP.", MaxHp: -8, PowerPct: 25),

        // ── Maela, the Warden: healer who protects and burns ──
        new("maela_overheal", "warden", "Hearthlight", "Excess healing on heroes becomes Shield."),
        new("maela_martyr", "warden", "Guardian Angel", "The first time each ally drops below 30% HP in a battle, Maela heals them 8."),
        new("maela_holyfire", "warden", "Holy Fire", "Maela's attacks apply 2 Burn."),
        new("maela_blessing", "warden", "Blessing", "At the start of every battle, all heroes gain Regen 2 for 3 turns."),
        new("maela_devotion", "warden", "Devotion", "+1 Faith every turn."),
        new("maela_ward", "warden", "Warding Light", "Shields Maela grants are 50% stronger."),

        // ── Nyx, the Shadowblade: chains kills ──
        new("nyx_spree", "shadowblade", "Killing Spree", "Once per round, when Nyx kills an enemy she immediately takes another turn."),
        new("nyx_venom", "shadowblade", "Toxic Blades", "Nyx's attacks apply 2 Poison."),
        new("nyx_execute", "shadowblade", "Executioner", "+50% damage to enemies below 35% HP."),
        new("nyx_assassin", "shadowblade", "Assassin", "+15% crit chance.", Crit: 15),
        new("nyx_shadow", "shadowblade", "Shadowstep", "+15% dodge and +1 speed.", Dodge: 15, Speed: 1),
        new("nyx_flow", "shadowblade", "Flow State", "Start every battle with +2 Combo.", StartResource: 2),

        // ── Wren, the Ranger: precision and volleys ──
        new("wren_apex", "ranger", "Apex Predator", "+35% damage against Marked enemies."),
        new("wren_barrage", "ranger", "Barrage", "Wren's abilities that hit all enemies deal +40% damage."),
        new("wren_opening", "ranger", "Opening Shot", "Wren's first attack in every battle is always a critical hit."),
        new("wren_eagle", "ranger", "Eagle Eye", "+12% crit chance.", Crit: 12),
        new("wren_piercer", "ranger", "Armor Piercer", "Wren's attacks ignore 3 armor."),
        new("wren_swift", "ranger", "Quick Draw", "+2 speed: act earlier in the round.", Speed: 2),

        // ── Brakka, the Berserker: blood for power ──
        new("brakka_bloodbath", "berserker", "Bloodbath", "All of Brakka's attacks heal her for 15% of the damage dealt."),
        new("brakka_thick", "berserker", "Thick-Skinned", "+15 max HP.", MaxHp: 15),
        new("brakka_rampage", "berserker", "Rampage", "Every kill restores 2 Rage."),
        new("brakka_unstoppable", "berserker", "Unstoppable", "Brakka can't be Slowed, Weakened or Stunned."),
        new("brakka_deathwish", "berserker", "Death Wish", "Below 30% HP, Brakka deals +50% damage."),
        new("brakka_warcry", "berserker", "War Cry", "Brakka starts every battle Empowered for 2 turns."),

        // ── Sable, the Hexer: afflictions ──
        new("sable_spread", "hexer", "Contagion", "When an afflicted enemy dies, its afflictions spread to another enemy."),
        new("sable_agony", "hexer", "Agony", "Poison Sable applies is 2 stronger."),
        new("sable_lasting", "hexer", "Lingering Curse", "Afflictions Sable applies last 1 turn longer."),
        new("sable_dread", "hexer", "Dread", "Weakened enemies deal 40% less damage instead of 25%."),
        new("sable_harvest", "hexer", "Soul Harvest", "+1 Hex every turn."),
        new("sable_reach", "hexer", "Shared Siphon", "Sable's lifesteal heals every hero, not just her."),
    }.Concat(TraitsPack2).ToList();

    public static bool HasTraitDef(string id) => AllTraits.Any(t => t.Id == id);
    public static TraitDef Trait(string id) => AllTraits.First(t => t.Id == id);
    public static IEnumerable<TraitDef> TraitsFor(string heroId) => AllTraits.Where(t => t.HeroId == heroId);
}
