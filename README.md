# Ashenbound: The Last Ember

A turn-based roguelite RPG built with **Godot 4.7 (.NET)** and **C#**. Pick three heroes, descend through five
regions of a cursed vault, and rekindle the Last Ember by defeating the Hollow King.

![Heroes and monsters](docs/characters.png)

## Features

**Combat**
- Initiative-based turn order, front and back rows (melee can only reach the front row).
- Four elements (Physical, Fire, Ice, Lightning) with weaknesses, resistances and immunities.
- Break system: hitting weaknesses depletes an enemy's break meter; a broken enemy loses its turn and takes double damage.
- Honest enemy intents: every enemy shows exactly what it will do and to whom, including charged attacks.
- 14 status effects, hero resources (Resolve, Mana, Faith, Combo, Focus, Rage, Hex) and cooldowns.
- Optional timing-based parry.

**Content**
- 7 heroes (2 unlockable), each with 5 abilities and 6 level-up traits.
- 5 regions, each with its own enemies, elites, music, ambience and a boss with a unique mechanic:
  - *The Bone Colossus* exposes a different weak point every round.
  - *The Drowned Matriarch* floods the front row, so heroes have to fall back.
  - *Vaelith the Frost Lich* is shielded by her phylactery until it is destroyed.
  - *Kharos the Storm Tyrant* brands a hero and then strikes them with lightning.
  - *The Hollow King* steals heroes' abilities and devours his own minions.
- 37 enemy types (including 10 elites and 5 bosses), plus random elite modifiers (Frenzied, Armored, Splitting, Vampiric, Thorned).
- Branching map with battles, elites, events, treasure, shops, rest sites, toll gates and golden rooms.
- Story cutscenes, in-battle dialogue and event chains that span the whole run.

**Progression**
- Gold, XP, equipment, relics, consumables and ability upgrades within a run.
- Ember Shards carry over between runs and buy permanent upgrades and hero unlocks.
- Daily challenge (same seed, party and modifier for everyone) and 5 Heat difficulty levels.

![The five regions](docs/regions.png)

## Running the game

1. Install [Godot 4.7 .NET](https://godotengine.org/download) and the [.NET 8 SDK](https://dotnet.microsoft.com/download).
2. Clone this repository and open `project.godot` in Godot.
3. Press **F5**. The first launch imports the assets and builds the C# project.

**Controls:** mouse for everything. In battle, **1-9** select actions, **Tab / arrows + Enter** pick targets,
**Esc** cancels or pauses, **Space** parries. **F11** or **Alt+Enter** toggles fullscreen.

## Architecture

```
Scripts/
  Core/                 Pure C# game rules, no Godot dependencies
    Data/               Content as data: heroes, abilities, enemies, items, relics, traits, regions
    Combat/             Battle state machine, damage and status rules, enemy AI, battle events
    Run/                Run state, map generation, events, story, meta-progression, save/load
  Game/                 Scene flow, saving, audio, global hotkeys
  UI/
    Components/         Reusable UI: unit views, pickers, panels, pause menu
    Scenes/             One script per screen (menu, map, battle, rewards, shop, cutscenes...)
Scenes/                 One .tscn per screen; all UI is built in code
Art/, Audio/            Generated assets and the Python scripts that generate them
```

The battle rules are a deterministic state machine that resolves instantly and emits a stream of events
(hits, statuses, deaths, phase changes). The UI replays those events with animations. Keeping the rules
separate from presentation makes them testable and prevents soft-locks caused by interrupted animations.

Runs, the profile and settings are saved as JSON. Seeded randomness means reloading a run gives the same results.

## Assets

All art and audio are original and generated procedurally:
- `Art/style_kit.py`, `units_v2.py`, `backgrounds_v2.py`, `ui_v2.py`: pixel art with a fixed 38-color palette,
  shaded color ramps, ordered dithering, outlines and baked lighting (Python + Pillow + NumPy).
- `Audio/music_v2.py`: orchestral music synthesized from scratch (strings, brass, choir, harp, drums,
  convolution reverb) and rendered to seamless loops (NumPy + SciPy + ffmpeg).

## Author

Luka Antonić, [@antsdz0r](https://github.com/antsdz0r)
