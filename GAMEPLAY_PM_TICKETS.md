# Tank Royale — Core Gameplay PM Tickets (JIRA Style)

## Scope & Design Baseline
- Arena uses a **30x30 grid** (900 tiles total).
- Core mode baseline: **1 Player + 3 AI tanks** in top-down arena combat.
- Art/audio implementation must use **Cartoon Tank Pack** assets for tank body/turret variants, projectile visuals, explosions, hit sparks, and movement/fire SFX where available.
- Gameplay systems covered in this backlog: movement physics, shooting/recoil, collision, AI behavior, death/respawn, match end conditions, and player feedback cues.

## AI Difficulty Matrix (Design Target)
| Parameter | Easy | Medium | Hard |
|---|---|---|---|
| Reaction delay | 450–700 ms | 250–400 ms | 120–220 ms |
| Aim inaccuracy | ±12° | ±6° | ±2.5° + lead prediction |
| Dodge trigger chance | 25% when threatened | 55% when threatened | 80% when threatened |
| Pathing sophistication | Shortest path only | Path + light cover preference | Path + strong cover/flank preference |
| Target switching | Every 3.0s | Every 1.8s | Every 1.0s + threat-aware |
| Aggression | Low (breaks chase early) | Balanced | High (sustained chase + pressure) |
| Burst discipline | Random fire windows | Controlled bursts | Cooldown-perfect firing windows |

---

## EPIC: GP-EPIC-01 Movement, Shooting, and Collision Foundation

### TR-GP-001 — Define 30x30 Grid Coordinate, Physics, and Tuning Constants
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 3 SP  
- **Status:** TODO
- **Description:** Create centralized gameplay constants for 30x30 tile coordinates, tank physics, projectile tuning, and collision layers.
- **Acceptance Criteria:**
  - A single config source defines tile size, arena bounds (0–29 on X/Y), and world-to-grid conversion helpers.
  - Default tuning values are documented and loaded at runtime (movement, fire cooldown, recoil, HP, respawn delay).
  - Collision layers/masks are defined for Tanks, Projectiles, Terrain, and Pickups.

### TR-GP-002 — Implement Tank Movement Physics (Acceleration, Turn, Drift, Reverse)
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 8 SP  
- **Status:** TODO
- **Description:** Build responsive top-down movement with arcade feel and predictable control.
- **Design Targets:**
  - Max forward speed: **5.0 tiles/s**
  - Max reverse speed: **3.0 tiles/s**
  - Acceleration: **10 tiles/s²**
  - Brake/deceleration: **14 tiles/s²**
  - Turn speed: **160°/s at low speed**, scales down to **100°/s** near max speed
- **Acceptance Criteria:**
  - Tank velocity changes smoothly (no snap movement).
  - Reverse movement and turn scaling feel distinct from forward.
  - Tank cannot leave 30x30 map bounds.
  - Movement remains stable at 60 FPS and 30 FPS simulation checks.

### TR-GP-003 — Implement Turret Aiming + Shooting Cadence
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Add independent turret aiming and a single base weapon with cooldown-limited fire.
- **Design Targets:**
  - Base fire cooldown: **0.65s** (1.54 shots/sec)
  - Projectile spawn offset from barrel muzzle to prevent self-collision
- **Acceptance Criteria:**
  - Turret rotates toward aim direction independent of hull.
  - Fire input during cooldown is ignored with clear player feedback.
  - Fire timing is deterministic (no double-fire on rapid input).

### TR-GP-004 — Implement Projectile Ballistics + Recoil Response
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Implement projectile travel, lifetime, impact handling, and firing recoil.
- **Design Targets:**
  - Projectile speed: **14 tiles/s**
  - Projectile lifetime: **2.5s**
  - Recoil impulse: **0.35 tiles** opposite fire vector
- **Acceptance Criteria:**
  - Projectiles move consistently and despawn on lifetime expiry.
  - Shooter receives recoil impulse without clipping through walls.
  - Recoil can slightly affect positioning and high-skill peeking.

### TR-GP-005 — Collision Detection: Tank ↔ Terrain, Tank ↔ Tank, Projectile ↔ World
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 8 SP  
- **Status:** TODO
- **Description:** Build robust collision detection/resolution for terrain blocks, tanks, and projectiles.
- **Acceptance Criteria:**
  - Tanks do not tunnel through terrain or each other.
  - Projectile collisions use continuous checks to avoid pass-through at high speed.
  - Tank-vs-tank contact resolves with push/separation (no overlap lock).
  - Terrain collision respects destructible vs indestructible block types.

### TR-GP-006 — Destructible Terrain Interaction Rules
- **Type:** Story  
- **Priority:** P1  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Define and implement damage interaction with crates/blocks on the 30x30 grid.
- **Acceptance Criteria:**
  - Standard bullets damage destructible crates per ruleset.
  - Indestructible terrain blocks projectile movement.
  - Terrain destruction updates navigation occupancy for AI in real time.
  - Block destruction produces Cartoon Tank Pack-compatible VFX/SFX cues.

---

## EPIC: GP-EPIC-02 AI Combat Behavior

### TR-GP-007 — AI State Machine (Patrol, Chase, Dodge, Attack, Reacquire)
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 8 SP  
- **Status:** TODO
- **Description:** Implement core AI finite state machine with explicit transitions.
- **State Requirements:**
  - **Patrol:** Move between waypoint cells on navigable 30x30 grid.
  - **Chase:** Pursue last known enemy position on detection.
  - **Dodge:** Perform lateral/retreat move if incoming projectile intersects predicted path.
  - **Attack:** Stop/strafe and fire when line-of-sight and range are valid.
  - **Reacquire:** Search nearby cells after losing LOS.
- **Acceptance Criteria:**
  - AI visibly switches states under expected triggers.
  - No state deadlocks or rapid oscillation loops.
  - Behavior remains functional with dynamic terrain destruction.

### TR-GP-008 — AI Target Priority and Threat Evaluation
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Implement weighted target scoring for smarter enemy focus.
- **Target Priority Weights (initial):**
  - Distance (closer target preferred)
  - Line-of-sight quality
  - Recent damage received from target (retaliation bias)
  - Target HP (finish low-HP targets)
  - Threat level (target aiming at AI)
- **Acceptance Criteria:**
  - AI target selection is explainable by score breakdown logs.
  - AI can switch targets when current target is no longer optimal.
  - Hard difficulty can coordinate soft focus-fire behavior (non-scripted but convergent).

### TR-GP-009 — Difficulty Profiles: Easy / Medium / Hard
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Parameterize AI behavior by difficulty using matrix above.
- **Acceptance Criteria:**
  - Easy AI misses more, reacts slower, and chases less aggressively.
  - Medium AI offers balanced challenge for average players.
  - Hard AI demonstrates faster reactions, better dodging, and cover/flank behavior.
  - Difficulty can be switched via config without code changes.

### TR-GP-010 — AI Navigation on 30x30 Grid with Dynamic Obstacles
- **Type:** Story  
- **Priority:** P1  
- **Estimate:** 8 SP  
- **Status:** TODO
- **Description:** Implement pathfinding on 30x30 occupancy grid and re-plan on terrain changes.
- **Acceptance Criteria:**
  - AI can route around static/destructible obstacles.
  - Path is recalculated when a crate is destroyed or a path becomes blocked.
  - Hard AI applies cover preference by avoiding exposed open lanes when possible.

---

## EPIC: GP-EPIC-03 Death, Respawn, Match Flow, and Feedback

### TR-GP-011 — Damage, Death, and Elimination Pipeline
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Define HP, damage intake, kill credit, and elimination flow.
- **Design Targets:**
  - Base tank HP: **3 hits**
  - Armor Bubble power-up adds **+1 temporary hit** (handled by power-up system)
- **Acceptance Criteria:**
  - Damage is applied once per valid projectile impact.
  - At 0 HP, tank enters death flow (input off, explosion, score event).
  - Kill attribution logged and shown in HUD feed.

### TR-GP-012 — Respawn System with Spawn Safety Rules
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Respawn tanks at safe cells to reduce spawn camping.
- **Design Targets:**
  - Respawn delay: **3.0s**
  - Spawn protection: **1.5s** or until first shot fired
  - Safe spawn check: min **6-tile** distance from nearest enemy + avoid direct LOS when possible
- **Acceptance Criteria:**
  - Respawns occur only on valid unoccupied grid cells.
  - Spawn protection visuals are clear and end correctly.
  - Spawn kill incidents are significantly reduced in playtests.

### TR-GP-013 — Match End Conditions and Win/Loss Rules
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 5 SP  
- **Status:** TODO
- **Description:** Define and implement end-of-match logic for standard skirmish and challenge progression.
- **Rules:**
  - **Skirmish:** Match ends at **6:00** timer OR first tank to **10 eliminations**.
  - **Winner:** Highest eliminations; tiebreakers = fewer deaths, then earliest last elimination.
  - **Challenge mode:** Level complete when all AI are defeated per level objective; fail when player life pool condition is exhausted (configured by mode owner).
- **Acceptance Criteria:**
  - Match ends deterministically from either condition.
  - End screen shows winner, kills, deaths, accuracy, and difficulty.
  - Rematch/reset returns arena to valid initial state.

### TR-GP-014 — Player Feedback System (Visual + Audio Combat Clarity)
- **Type:** Story  
- **Priority:** P0  
- **Estimate:** 8 SP  
- **Status:** TODO
- **Description:** Add readable feedback for movement, combat, damage, and state changes using Cartoon Tank Pack assets.
- **Required Feedback Cues:**
  - **Fire:** muzzle flash, shot SFX, recoil animation bump
  - **Hit Confirm:** impact spark + hit ping
  - **Taking Damage:** tank flash, directional hit indicator, damage SFX
  - **Low HP:** pulsing hull indicator + warning beep cadence
  - **Reload/Cooldown Locked:** subtle click/dry-fire cue
  - **Respawn Invulnerability:** shield tint + hum loop
  - **Elimination:** explosion VFX/SFX + short camera shake
- **Acceptance Criteria:**
  - Players can identify hit/miss/damage states without reading text.
  - Feedback remains readable in 4-player chaos scenarios.
  - Audio/visual cues map consistently to gameplay events.

### TR-GP-015 — Gameplay Telemetry and Balance Validation Pass
- **Type:** Task  
- **Priority:** P1  
- **Estimate:** 3 SP  
- **Status:** TODO
- **Description:** Instrument gameplay metrics for tuning and difficulty validation.
- **Acceptance Criteria:**
  - Capture: shots fired, hits, accuracy, time-to-kill, deaths, respawn kills, AI state dwell times.
  - Export per-match summary to debug console/file for balancing.
  - Verify Easy/Medium/Hard produce clearly different challenge outcomes in test runs.

---

## Release Gate (Core Gameplay)
Core gameplay is PM-complete for handoff when:
1. TR-GP-001 through TR-GP-014 are done and accepted.
2. Difficulty tiers show measurable separation in internal playtests.
3. Collision/respawn edge cases are stable (no out-of-bounds, no stuck tanks, no projectile tunneling).
4. Cartoon Tank Pack visuals/audio are correctly wired for all combat-critical cues.
