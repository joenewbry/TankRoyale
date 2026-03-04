# Tank Royale Audio Sources

This document tracks all required game audio and how we source it from OpenGameArt.

## License Requirement

All production audio used for Tank Royale must be **CC0 (Public Domain)**.

- Source site: https://opengameart.org
- Filter/search for assets explicitly marked **CC0**.
- If a pack includes mixed licenses, only import files covered by CC0.

## Required Sounds

| Sound Name | In-Game Use | Suggested OpenGameArt Search Term | License Requirement |
| --- | --- | --- | --- |
| `sfxShot` | Tank cannon fire | `cc0 cannon shot sfx` | CC0 only |
| `sfxExplosion` | Projectile/tank explosion | `cc0 explosion game sound` | CC0 only |
| `sfxBlockDestroy` | Destructible block break | `cc0 block break` | CC0 only |
| `sfxPowerupPickup` | Powerup collect feedback | `cc0 pickup chime` | CC0 only |
| `sfxArmorActivate` | Armor activation cue | `cc0 shield activate` | CC0 only |
| `sfxTankEngine` | Tank movement/engine loop | `cc0 engine loop` | CC0 only |
| `musicMain` | Main gameplay background track | `cc0 arcade battle music loop` | CC0 only |

## Download + Import Workflow

1. Search OpenGameArt using one of the terms above.
2. Open candidate asset page and verify the license is **CC0**.
3. Download the asset pack.
4. Extract only the files we need into: `Unity/TankRoyale/Assets/Audio/`.
5. Rename files to match project naming convention (example: `sfx_shot.wav`, `music_main.ogg`).
6. In Unity, assign clips to:
   - `SoundLibrary` ScriptableObject asset
   - `AudioManager` (if direct override is needed)
7. Add/update this file with final source URLs and attribution notes (if any non-CC0 asset is ever proposed, reject it).

## Temporary Placeholder Clips

To avoid missing references while production audio is being sourced, placeholder `.wav` clips were added in:

- `Unity/TankRoyale/Assets/Audio/`

Current placeholder files:

- `sfx_shot_placeholder.wav`
- `sfx_explosion_placeholder.wav`
- `sfx_block_destroy_placeholder.wav`
- `sfx_powerup_pickup_placeholder.wav`
- `sfx_armor_activate_placeholder.wav`

These are short silent placeholders and can be replaced as soon as CC0 assets are approved.
