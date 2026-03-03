# Tank Royale — Game Design Document (GDD)

## 1) High Concept
**Tank Royale** is a fast, top-down arcade combat game where **1 player tank battles 3 AI tanks** in a compact, tactical battlefield built on a **30x30 grid**. Every match is a readable, high-energy duel of positioning, line-of-sight, and clutch shots through destructible terrain.

The design goal is to combine:
- **Classic tank combat clarity** (easy to learn, high skill ceiling)
- **Reactive arena tactics** (destructible blocks, chokepoints, flank routes)
- **Arcade excitement** (quick matches, power-up swings, explosive feedback)

Visual identity uses the **Cartoon Tank Pack** for stylized tanks, environment tiles, pickups, and effects.

---

## 2) Design Pillars
1. **Readable Combat**  
   Player always understands where danger is: clean projectile trails, obvious hit feedback, strong silhouette contrast.

2. **Meaningful Terrain**  
   Arena geometry creates micro-decisions every second: peek, push, rotate, or break through.

3. **Short, Replayable Matches**  
   2–4 minute rounds with procedural map variety and escalating AI pressure.

4. **Power-Up Moments**  
   Three power-ups create dramatic momentum shifts without overwhelming complexity.

---

## 3) Core Match Structure
- Arena: **30 columns x 30 rows**
- Participants: **1 Player + 3 AI tanks**
- Mode: Single-player tactical survival/elimination
- Match length target: **~3 minutes** average
- Session format:
  - Challenge Mode (10 levels)
  - Quick Skirmish (single match)

### Core Loop
1. Spawn in randomized arena with safe start pockets.
2. Scout lanes, break/hold terrain, trade shots.
3. Collect power-ups (ricochet, armor, block-breaker).
4. Eliminate all AI tanks before being destroyed.
5. Advance (Challenge) or restart/rematch (Skirmish).

---

## 4) Arena & Grid Rules (30x30)
### Tile Types
- **Open Ground**: passable, projectile pass-through.
- **Hard Wall**: impassable, indestructible, blocks shots.
- **Crate Block**: impassable, **destructible** by normal shots (2 hits) or instantly by block-breaker shots.
- **Spawn Buffer Tiles**: reserved open tiles around each spawn to prevent unfair opening traps.

### Spatial Rules
- Grid coordinates: `(0..29, 0..29)`
- Outer border ring contains hard wall to prevent escape/exploit.
- Tanks move continuously but resolve collision against tile bounds.
- Projectiles simulate in world space, ray-checked against grid occupancy.

---

## 5) Player Controls
Controls are tuned for responsiveness first, realism second.

### Keyboard + Mouse (default)
- **W / S**: Move forward / reverse
- **A / D**: Rotate hull left / right
- **Mouse Aim**: Turret rotates toward cursor
- **Left Click**: Fire shell
- **Space**: Quick brake / handbrake pivot (high turn traction)
- **Shift (hold)**: Precision move (reduced speed, tighter control)
- **Esc**: Pause menu

### Gamepad
- **Left Stick Y**: Forward/reverse
- **Left Stick X**: Hull rotation
- **Right Stick**: Turret aim
- **RT / R2**: Fire
- **A / Cross**: Brake pivot
- **Start**: Pause

### Mobile (future-ready spec)
- Left virtual stick: movement + hull turn
- Right drag pad: turret aim
- Fire button: bottom-right
- Brake button: bottom-left

---

## 6) Movement Model
### Tank Motion
- Base forward speed: **4.2 tiles/sec**
- Reverse speed: **2.8 tiles/sec**
- Hull turn speed: **180°/sec**
- Acceleration/deceleration for weighty but snappy feel
- Collision response: slide along walls when glancing impacts occur

### Handling States
- **Normal Drive**: balanced speed and turn
- **Brake Pivot**: low linear speed, high rotational control for corner fights
- **Precision Mode**: 70% speed, improved micro-positioning for narrow lanes

### Feel Targets
- Input-to-motion latency under 80ms
- Player can reliably dodge long-range shots with good read timing
- Tanks should feel heavy yet agile enough for arcade pace

---

## 7) Shooting & Combat Rules
### Base Weapon
- Single cannon type for all tanks
- Fire rate: **1 shot every 0.65s**
- Shell speed: **11 tiles/sec**
- Direct hit damage: **1 integrity point**
- Max active shells per tank: **2**

### Tank Durability
- Base integrity: **3 HP**
- On HP 0: tank destroyed, explosion VFX, removed from match

### Projectile Interactions
- Hits tank: deal damage, short hit-stun visual shake (no control lock)
- Hits hard wall: shell destroyed (unless ricochet active)
- Hits crate: crate takes damage (or is instantly broken with block-breaker)
- Friendly fire between AIs: enabled (creates emergent chaos)

### Combat Readability
- Muzzle flash for every shot
- Distinct projectile color per owner team
- Flash + spark + hit marker on impact
- Low camera shake on nearby explosions

---

## 8) Power-Up System (3 Total)
Power-ups spawn as floating pickups with bright cartoon icons, soft bob animation, and glow ring.

- Spawn cadence: every **18–24 seconds** (randomized)
- Max active pickups on field: **2**
- Pickup rule: contact-based collection
- If holding active timed power-up, new pickup replaces old one (no stacking except armor as state)

### 8.1 Ricochet Rounds
- Effect: shells bounce off hard walls/crates before despawning
- Bounce count: **up to 2 bounces per shell**
- Duration: **16 seconds** or until **6 enhanced shots** fired
- Tactical use: bank shots into cover, corner pressure, denial fire

### 8.2 Armor Bubble
- Effect: grants **1 bonus hit** shield layer
- Behavior: first incoming hit is absorbed; armor pops with clear effect
- Duration: until consumed (or match end)
- Visual: translucent bubble + shield shimmer around tank

### 8.3 Block-Breaker Shells
- Effect: shots instantly destroy crate blocks and continue with reduced speed
- Penetration: each shell can break up to **2 crate blocks**
- Post-break speed: reduced to 75%
- Duration: **14 seconds** or **5 enhanced shots**
- Tactical use: carve new lanes, force flanks, collapse enemy cover

---

## 9) AI Behavior (3 Opponents)
AI must feel distinct, tactical, and fair. Each bot uses shared perception + role-specific priorities.

### AI Roles
1. **Vanguard (Aggressor)**
   - Prioritizes direct pressure and short routes
   - Pushes when player is reloading or exposed

2. **Flanker (Positioning)**
   - Seeks side lanes, crossfire angles
   - Avoids mirrored duels; prefers off-axis shots

3. **Breaker (Terrain Control)**
   - Focuses on opening map geometry, denying safe cover
   - Prioritizes block-breaker pickup when available

### Perception
- Line-of-sight checks through grid tiles
- Last known player position memory (2.5s)
- Threat map from recent projectile paths
- Pickup desirability scoring (distance + danger + role preference)

### Behavior State Machine
- **Patrol**: move between tactical anchors when no target
- **Engage**: fire when line-of-sight and aim confidence threshold met
- **Reposition**: strafe/rotate to avoid predictable peeks
- **Seek Cover**: retreat to nearest protected tile cluster on low HP
- **Pickup Hunt**: temporarily detour for high-value power-up

### Skill Scaling (Challenge Mode)
Across levels 1–10:
- Faster reaction windows
- Higher aim confidence (less random spread)
- Better flank frequency
- Smarter retreat/push decisions

Important: AI difficulty should improve decision quality, not cheating (no wallhacks, no perfect prediction).

---

## 10) Level Generation
Procedural arenas are generated per match with fairness constraints.

### Generation Pipeline
1. **Seed Setup** (level seed + random salt)
2. **Frame Layout** (outer hard wall, optional internal hard-wall skeleton)
3. **Spawn Reservation**
   - Four spawn zones in broad quadrants
   - Minimum Manhattan distance between spawns
   - Guaranteed 3x3 open area around each spawn center
4. **Crate Distribution**
   - Fill target density (22%–34% based on level)
   - Noise + cluster pass to create readable lanes and pockets
5. **Connectivity Check**
   - Ensure all spawns are reachable via at least 2 unique path regions
6. **Cover Validation**
   - Prevent one spawn from having dominant immediate line-of-sight advantage
7. **Power-Up Nodes**
   - Candidate tiles for fair, contestable pickup spawns

### Replayability Rules
- No repeated seed in adjacent runs (unless user retries same challenge level)
- Three environment themes (using Cartoon Tank Pack tiles):
  - Grassfield Outpost
  - Desert Depot
  - Snowyard Stronghold

Theme changes are visual; collision and gameplay rules remain identical for fairness.

---

## 11) Win / Lose Conditions
### Win
- Destroy all **3 AI tanks** before player HP reaches 0.

### Lose
- Player HP reaches 0.

### Optional Timeout Rule (Challenge only)
- Soft target time: 4:00
- No hard fail on timeout by default; instead:
  - Bonus score ends at 4:00
  - AI aggression increases gradually after 4:00 to force resolution

### Scoring (for stars/leaderboards)
- +100 per AI destroyed
- +25 per power-up collected
- +10 per crate destroyed
- Time bonus: up to +200 (decays over first 4 min)
- Damage penalty: -30 per HP lost

### Challenge Grade
- **3 Stars**: clear with 2+ HP and under target time
- **2 Stars**: clear with 1+ HP
- **1 Star**: clear with 0 bonus criteria

---

## 12) Progression: 10-Level Challenge Mode
Each level keeps the same core rules but shifts map density and AI competence.

- **Levels 1–3 (Boot Camp):**
  - Lower crate density, slower AI decision loop
  - Teaches aiming lanes and retreat timing

- **Levels 4–7 (Warzone):**
  - Denser terrain, more flanks, frequent power-up contests
  - AI begins coordinated pressure

- **Levels 8–10 (Royal Gauntlet):**
  - Aggressive bots, tighter maps, faster punish windows
  - Requires deliberate use of all 3 power-ups

---

## 13) Menu Flow
### Boot Flow
1. Splash logos
2. Title screen (animated tank idle + “Press Start”)
3. Main Menu

### Main Menu Structure
- **Play**
  - Challenge Mode (Level Select)
  - Quick Skirmish
- **Controls**
  - Keyboard/Gamepad layout
  - Rebind actions
- **Settings**
  - Audio, video, accessibility, gameplay assists
- **Credits**
- **Quit** (platform-dependent)

### In-Game Flow
- Match Start Countdown (3-2-1)
- Live HUD
- Pause Menu:
  - Resume
  - Restart Match
  - Controls
  - Settings
  - Exit to Main Menu

### End-of-Match Flow
- Victory/Defeat panel
- Score breakdown + stars (Challenge)
- Buttons: Retry / Next Level / Main Menu

---

## 14) UI / HUD Specification
UI style should match Cartoon Tank Pack tone: chunky, high-contrast, playful military arcade.

### In-Match HUD
- **Top-left:** Player HP (3 pips) + armor indicator bubble icon
- **Top-center:** Remaining AI count (3 → 0)
- **Top-right:** Timer + score
- **Bottom-right:** Active power-up card with duration/charges
- **Bottom-left:** Control hint (first 30s only, then fades)
- **Center:** Turret reticle + subtle reload tick

### Readability Rules
- Color-blind-safe team colors and icon redundancy
- Power-up effects always use icon + outline + sound cue
- Damage flashes never fully obscure screen center
- UI scales for desktop, mobile, and TV/Xbox distances

### Minimap (Optional Toggle)
- 30x30 compressed grid view
- Shows walls/crates and approximate AI pings when firing
- Intended as accessibility aid, off by default for purist experience

---

## 15) Cartoon Tank Pack Asset Mapping
Use the pack as the visual source of truth for all playable entities and world props.

### Required Asset Categories
- Tank hull sprites (player + 3 AI color variants)
- Turret sprites (independent rotation)
- Projectile sprites and muzzle flashes
- Explosion sprite sheets (small hit, large destruction)
- Ground tiles (grass/desert/snow themes)
- Wall/crate/block tiles (hard vs destructible readability)
- Pickup icons (ricochet, armor, block-breaker)
- UI panel pieces/buttons/icons matching cartoon style

### Visual Direction
- Bold outlines and saturated faction colors
- Exaggerated recoil/explosion timing for arcade punch
- Distinct silhouette between hull and turret for clear aiming comprehension

---

## 16) Tuning Targets (Initial)
These values are starting points for playtesting:

- Player HP: 3
- AI HP: 3
- Fire cooldown: 0.65s
- Projectile speed: 11 tiles/s
- Crate density by level: 22% → 34%
- Power-up spawn interval: 18–24s
- Match target duration: 2–4 minutes

Balance priorities:
1. Avoid stalemates in heavy cover maps
2. Ensure power-ups are impactful but not mandatory for wins
3. Keep losses feeling fair and readable

---

## 17) Onboarding & First 30 Seconds
- On first launch, player enters a guided skirmish slice:
  - Move prompt
  - Aim + shoot prompt
  - Pickup prompt
  - Win by destroying one AI dummy
- Control tips fade once player performs each action
- Retry is instant (no long transitions)

Goal: player understands movement, firing rhythm, and power-up value in under 45 seconds.

---

## 18) “Fun Check” Acceptance Criteria
A build is design-valid when:
- Player can understand combat state at a glance
- Each power-up creates obvious tactical shifts
- At least 3 distinct viable tactics exist (aggressive push, cover duel, flank carve)
- AI feels challenging through behavior, not unfair stat inflation
- 5 consecutive matches feel meaningfully different due to map generation

---

## 19) Future Extensions (Post-MVP)
- Online PvP mode (same combat rules)
- Co-op survival (2 players vs AI waves)
- More power-up variants (EMP, speed boost, mine drop)
- Ranked challenge leaderboards
- Cosmetic tank customization using additional Cartoon Tank Pack skins

---

## Final Experience Statement
**Tank Royale** should feel like a toy box warzone: bright, explosive, and strategic. Every wall can become a decision, every power-up can swing momentum, and every match should tell a different mini-story of survival against three relentless rivals.