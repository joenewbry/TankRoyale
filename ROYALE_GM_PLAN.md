# Royale GM Execution Plan (Unity)

## North Star
A stylized, high-energy tank battler with satisfying drift movement, ballistic shell combat, kill-streak upgrades, and clear feedback loops.

---

## Phase 1 — Feel (Core Mechanics + Physics)
**Owner:** `royale-dev-physics`

### Build
- Physics and ability to move around hit walls, go up slopes, roll and tip over, driving and acceleration
- Tune tank movement for arcadey handling (quick turns, slight drift)
- Add turret rotation and body/turret separation
- Validate movement +  on PlayTest2

### Acceptance checks
- 0→top speed feels responsive in < 1.0s
- 180° direction change controllable in < 1.2s
- Turret lag visibly tracks target without feeling sluggish
- Firing produces visible recoil + subtle shake

---

## Phase 2 — Combat Loop (Hit Detection + Feedback)
**Owner:** `royale-dev-combat`

### Build
- Keep physical shell projectile path (not pure hitscan)
- Add shell drop over distance and impact feedback
- Implement health states (normal / damaged / critical)
- Add destruction event hook for progression system

### Acceptance checks
- At least 2 distinct impact feedback types (tank vs environment)
- Destroy event reliably fires once per elimination
- Combat remains readable at close + medium range

---

## Phase 3 — Progression Layer (Killstreak Upgrades)
**Owner:** `royale-dev-progression`

### Build
- Kill-streak tracker with reset rules
- Reward ladder:
  - 1 kill: speed boost
  - 2 kills: loot magnet
  - 3 kills: double barrel or explosive rounds
- Temporary buff durations + state cleanup

### Acceptance checks
- Rewards trigger exactly once per threshold
- Effects are obvious within 0.5s of unlock
- Upgrade state survives momentary UI/state refreshes

---

## Phase 4 — Visual Identity (Assets + VFX)
**Owner:** `royale-dev-ui` + `game-artist`

### Build
- Swap greybox meshes for selected tank assets
- Implement trail effects tied to speed/upgrade status
- Build stylized arena pass (vibrant, high-contrast)
- Add toon/cell-shaded material profile where practical

### Acceptance checks
- Speed boost has distinct visual language (e.g., blue trail)
- Critical damage state has clear visual warning
- Arena readability preserved during combat chaos

---

## Phase 5 — Meta + UI
**Owner:** `royale-dev-ui`

### Build
- Minimal in-game HUD (health, streak, current upgrade)
- Killstreak pop-up treatment (big, punchy, brief)
- Garage screen for previewing tank + skins

### Acceptance checks
- Player can identify current upgrade instantly
- Pop-up is readable but non-blocking
- Garage flow supports quick skin swap + return to play

---

## Iteration Rhythm
- 20-minute loops: **code → playtest → tune**
- End every loop with:
  1. one commit
  2. one before/after note
  3. one next-tuning recommendation

## Build command (headless)
```bash
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath /Users/joe/dev/TankRoyale/Unity/TankRoyale \
  -executeMethod TankRoyale.Editor.BuildScript.SetupAndBuildWebGL \
  -logFile /tmp/tankroyale-build.log
```
