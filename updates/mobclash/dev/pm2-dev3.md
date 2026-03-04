# PM2-DEV3 — Encounter Loop (Auto-Battle)

## Summary
Implemented a simple MobClash board encounter loop where two squads spawn, advance across the board, engage enemies in range, resolve attack/damage, and stop when one side is defeated.

## What Was Added
- `Unity/TankRoyale/Assets/Scripts/MobClash/MobClashEncounterLoop.cs`
  - Spawns **friendly + enemy squads** with configurable counts and lanes.
  - Auto-creates an encounter loop object in `Arena` scene at runtime (if missing) for quick playtest.
  - Tracks alive units per side.
  - Handles **end condition** when either side reaches 0 creatures.

- `Unity/TankRoyale/Assets/Scripts/MobClash/MobClashCreature.cs`
  - Creature auto-behavior:
    - advance across board when no enemy target,
    - acquire nearest enemy,
    - move toward enemy,
    - attack in range on cooldown,
    - receive damage and die.
  - Implements **basic attack/damage resolution**.

- Unity metadata files for new MobClash script folder/files.

## Gameplay Loop Implemented
1. Encounter starts and both squads spawn.
2. Units march toward opposing side.
3. Units detect nearest enemy and enter combat when in range.
4. Damage is exchanged until units die.
5. Encounter ends when one team has no living creatures.

## Branch
- `mobclash/pm2-dev3-encounter-loop`

## PR
- (to be filled after PR creation)
