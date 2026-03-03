# Tank Royale — POWER-UPS + CHALLENGE MODE PM Spec & JIRA Ticket Backlog

**Owner:** Product Manager (Power-Ups & Challenge Mode)  
**Game:** Tank Royale  
**Art Source:** **Cartoon Tank Pack** (all power-up icons, tank skins, pickup VFX, crate variants, explosions should match this style)  
**Arena Baseline:** 30x30 grid, destructible crates, 1 player + AI opponents

---

## 1) Power-Up Design Specification

## 1.1 System Rules (Global)
- Power-ups drop from destroyed crates using weighted RNG.
- A dropped power-up remains on the map for **12s** before despawn.
- Pickup radius: **1.0 tile**.
- Spawn protection: no new power-up can spawn within **4 tiles** of another active power-up.
- Player can hold multiple *different* power-up effects simultaneously.
- Duplicate pickup behavior is power-up-specific (defined below).
- Every power-up has:
  - **Map cooldown** (controls how often that pickup type can spawn globally)
  - **Personal cooldown** (prevents immediate re-pickup by the same tank)

### Baseline drop weights (Normal difficulty)
- Ricochet Bullet: **40%** of power-up drops
- Armor Bubble: **35%**
- Block Breaker: **25%**

---

## 1.2 Ricochet Bullet

**Fantasy:** Trick-shot offense and corridor control.

**Visual appearance (Cartoon Tank Pack style):**
- Pickup icon: glowing shell with curved arrow ring (yellow/orange core, cyan outline).
- Active state on tank: rotating yellow ring around turret base.
- Projectile VFX: bright trail + spark burst on each bounce.

**Pickup mechanics:**
- On pickup, player gets Ricochet mode immediately.
- Bullets gain **+2 wall bounces** before despawn.
- Ricochet bullets still deal full damage to tanks.

**Duration:**
- **12s** OR **8 shots**, whichever ends first.

**Stacking rules:**
- Picking another Ricochet while active grants:
  - **+4s** duration (max **20s** total timer)
  - **+1 extra bounce** (max **3 bounces** total)
- At cap, additional pickup converts to **+50 score** (or campaign XP equivalent).

**Cooldowns:**
- Personal cooldown: **18s** (starts after effect fully ends).
- Map cooldown: **25s** before next Ricochet can spawn.

---

## 1.3 Armor Bubble

**Fantasy:** Forgiving defensive shield for clutch survivability.

**Visual appearance (Cartoon Tank Pack style):**
- Pickup icon: blue shield orb with white cartoon glint.
- Active state: transparent blue bubble around hull with pulsing edge.
- On absorb: bubble cracks with comic “pop” burst effect.

**Pickup mechanics:**
- Grants **1 shield charge** that blocks one incoming hit completely.
- Shield does not block terrain collision; only incoming damage.

**Duration:**
- Each charge lasts up to **20s** if not consumed.

**Stacking rules:**
- Can stack up to **2 charges**.
- Additional pickup at max charges converts to **+50 score**.
- Charges consume oldest-first.

**Cooldowns:**
- Personal cooldown: **22s** after final charge expires/consumes.
- Map cooldown: **30s** before next Armor Bubble spawn.

---

## 1.4 Block Breaker

**Fantasy:** Map control and anti-camp terrain demolition.

**Visual appearance (Cartoon Tank Pack style):**
- Pickup icon: red-black shell with drill/hammer emblem.
- Active state: turret glows red; projectile muzzle flash has debris sparks.
- Impact VFX: chunked crate fragments + dust cloud in cartoon style.

**Pickup mechanics:**
- Grants **6 empowered shots** that instantly destroy destructible crates.
- Empowered shots still damage tanks (normal tank damage).
- If shot hits crate first, crate is destroyed and shot dissipates.

**Duration:**
- **14s** OR until empowered shots are consumed.

**Stacking rules:**
- Additional pickup while active gives:
  - **+3 empowered shots** (cap **10 shots**)
  - **+4s** timer (cap **22s**)
- Pickup at full cap converts to **+50 score**.

**Cooldowns:**
- Personal cooldown: **20s** after effect ends.
- Map cooldown: **28s** before next Block Breaker spawn.

---

## 1.5 Difficulty Balancing Rules

### Difficulty presets
- **Cadet (Easy)**
- **Commander (Normal)**
- **Legend (Hard)**

### Power-up tuning by difficulty

| Parameter | Cadet (Easy) | Commander (Normal) | Legend (Hard) |
|---|---:|---:|---:|
| Player power-up duration | x1.25 | x1.00 | x0.85 |
| Personal cooldown (player) | x0.80 | x1.00 | x1.20 |
| Map drop chance from crates | +20% | baseline | -15% |
| AI pickup efficiency | 70% effect values | 100% | 110% |
| AI priority for power-up pathing | Low | Medium | High |
| Duplicate stack caps | +1 cap on all effects | baseline | baseline |

### Anti-snowball balancing
- If player trails by 3+ eliminations, next valid power-up spawn has +15% bias toward Armor Bubble.
- If player leads by 4+, Armor Bubble bias is removed and Ricochet/Block Breaker split evenly.
- Prevent same power-up spawning 3 times in a row unless no other type is available by cooldown.

---

## 2) Challenge Mode — 10-Level Single-Player Campaign

## 2.1 Campaign Progression Model
- 10 handcrafted challenges on 30x30 arenas.
- Each challenge has:
  - Primary objective (required)
  - Optional mastery objective (for extra reward)
- Rewards grant Credits + cosmetics + progression perks (campaign-only bonuses).

### Enemy archetypes used
- **Scout AI:** fast flanker, low accuracy.
- **Bruiser AI:** slower, tankier, pushes direct lanes.
- **Sniper AI:** holds long corridors, high accuracy.
- **Demolisher AI:** prioritizes crate destruction + line opening.
- **Ace AI:** hybrid elite behavior, uses power-ups intelligently.

---

## 2.2 Challenge Definitions (1–10)

### C1 — Boot Camp Blitz
- **Level goal:** Teach fundamentals and first elimination loop.
- **Enemy composition:** 1 Scout AI.
- **Terrain layout:** Mostly open center, light crate clusters near edges.
- **Win condition:** Eliminate enemy **3 times** before player is eliminated 3 times.
- **Mastery:** Win with 2+ lives remaining.
- **Reward:** 100 Credits + “Cadet Stripe” decal.

### C2 — Bank Shot Alley
- **Level goal:** Introduce Ricochet Bullet usage.
- **Enemy composition:** 2 Scouts.
- **Terrain layout:** Long L-shaped corridors with indestructible corners and sparse crates.
- **Win condition:** Score **4 eliminations**.
- **Mastery:** Get at least **2 ricochet-assisted eliminations**.
- **Reward:** 120 Credits + Ricochet tracer cosmetic.

### C3 — Bubble Trouble
- **Level goal:** Survive focused fire using Armor Bubble timing.
- **Enemy composition:** 1 Scout + 1 Bruiser.
- **Terrain layout:** Cross-shaped center kill zone with side cover pockets.
- **Win condition:** Survive **150s** and achieve **3 eliminations**.
- **Mastery:** Absorb 2 hits with Armor Bubble without dying.
- **Reward:** 140 Credits + blue shield ring cosmetic.

### C4 — Cratebreaker Circuit
- **Level goal:** Teach Block Breaker lane creation.
- **Enemy composition:** 2 Bruisers + 1 Demolisher.
- **Terrain layout:** Dense crate maze with three choke corridors.
- **Win condition:** Destroy **30 crates** and score **4 eliminations**.
- **Mastery:** Complete in under **4:30**.
- **Reward:** 160 Credits + “Wrecker” title.

### C5 — Triangle Ambush
- **Level goal:** Multi-angle threat management.
- **Enemy composition:** 1 Scout + 1 Sniper + 1 Bruiser.
- **Terrain layout:** Tri-lane triangle arena with contested center crate block.
- **Win condition:** Reach **7 eliminations** before enemies reach 7.
- **Mastery:** Win with max 2 deaths.
- **Reward:** 180 Credits + turret fin cosmetic unlock.

### C6 — Mirror Match
- **Level goal:** Symmetry map tactics and power-up denial.
- **Enemy composition:** 2 Aces.
- **Terrain layout:** Symmetrical north/south spawn zones, mirrored crate walls.
- **Win condition:** First to **9 eliminations**.
- **Mastery:** Deny enemies 4+ power-up pickups.
- **Reward:** 200 Credits + campaign perk: +5% pickup radius.

### C7 — Fortress Break
- **Level goal:** Break entrenched defenders with mixed power-up timing.
- **Enemy composition:** 1 Sniper + 2 Bruisers + 1 Demolisher.
- **Terrain layout:** Central fortress crate ring + two side breach lanes.
- **Win condition:** Eliminate all enemies **8 total times** within **6:00**.
- **Mastery:** Use Block Breaker to destroy 15+ crates.
- **Reward:** 220 Credits + hull camo “Siege Green”.

### C8 — Crossfire Canyon
- **Level goal:** High-pressure survival in exposed sightlines.
- **Enemy composition:** 2 Snipers + 1 Scout + 1 Demolisher.
- **Terrain layout:** Open midlane canyon with thin crate cover strips.
- **Win condition:** Survive **5:00** and secure **8 eliminations**.
- **Mastery:** Finish with 3+ Armor Bubble absorbs.
- **Reward:** 250 Credits + campaign perk: +1s Armor Bubble duration.

### C9 — Ace Squadron
- **Level goal:** Elite AI coordination challenge.
- **Enemy composition:** 3 Aces.
- **Terrain layout:** Hybrid map (open corners + dense central crate lattice).
- **Win condition:** Reach **10 eliminations** before enemies.
- **Mastery:** Win while conceding no more than 6 deaths.
- **Reward:** 300 Credits + animated victory banner.

### C10 — Crown of Steel (Finale)
- **Level goal:** Endgame mastery of all three power-ups.
- **Enemy composition:** 2 Aces + 1 Sniper + 1 Demolisher (staggered respawn pressure).
- **Terrain layout:** Four-quadrant arena, rotating safe lanes created via destructible crate clusters.
- **Win condition:** Reach **15 eliminations** within **8:00**.
- **Mastery:** Complete all three power-up mastery conditions in one run:
  - 3 ricochet-assisted hits
  - 3 blocked hits via Armor Bubble
  - 20 crates destroyed with Block Breaker
- **Reward:** 500 Credits + “Tank Royale Champion” skin + Challenge Mode completion badge.

---

## 2.3 Reward Progression Summary
- Total base Credits (C1–C10): **2,170**
- Mastery rewards: cosmetics, titles, and campaign-only micro-perks.
- Completion unlock: Champion skin + replay modifier toggle (“Hard Remix”).

---

## 3) JIRA-Style Ticket List

> Format: **Key — Title**  
> **Type:** Epic/Story/Task | **Priority:** P0/P1/P2 | **Estimate:** Story points

### TR-PU-100 — Power-Up System v1 (Epic)
- **Type:** Epic | **Priority:** P0 | **Estimate:** 34
- **Description:** Implement core power-up framework, effects, stacking, cooldowns, and HUD feedback.
- **Acceptance Criteria:**
  - All 3 power-ups functional with spec values.
  - Cooldowns and stacking enforced server-authoritatively.
  - Visual/audio feedback consistent with Cartoon Tank Pack style.

### TR-PU-101 — Ricochet Bullet Implementation
- **Type:** Story | **Priority:** P0 | **Estimate:** 8
- **Description:** Add ricochet projectile behavior, bounce counting, and timer/shot consumption logic.
- **Acceptance Criteria:**
  - Bullets bounce 2x baseline, stack to max 3x.
  - Effect ends on timer or shot cap.
  - Personal and map cooldowns respected.

### TR-PU-102 — Armor Bubble Implementation
- **Type:** Story | **Priority:** P0 | **Estimate:** 8
- **Description:** Implement absorb shield charge system with stacking and expiry.
- **Acceptance Criteria:**
  - 1 charge per pickup, stack max 2.
  - Each charge blocks exactly one hit.
  - Charges expire at 20s each and trigger cooldown correctly.

### TR-PU-103 — Block Breaker Implementation
- **Type:** Story | **Priority:** P0 | **Estimate:** 8
- **Description:** Implement crate-destroying empowered shots with shot count and timer caps.
- **Acceptance Criteria:**
  - 6 empowered shots base; stack max 10.
  - Crates break instantly on contact.
  - Tank damage remains normal.

### TR-PU-104 — Pickup Spawn & Weighted Drop Controller
- **Type:** Story | **Priority:** P0 | **Estimate:** 5
- **Description:** Weighted drop tables, despawn timer, anti-cluster spawning, map cooldown control.
- **Acceptance Criteria:**
  - Drop weights match PM spec (40/35/25).
  - No spawn within 4 tiles of active pickup.
  - Pickup despawns at 12s.

### TR-PU-105 — Power-Up HUD & UX Cues
- **Type:** Story | **Priority:** P1 | **Estimate:** 5
- **Description:** UI indicators for active effects, timers, stacks/charges, cooldown lockout.
- **Acceptance Criteria:**
  - Distinct icon + countdown per active effect.
  - Bubble charge count visible.
  - Cooldown lockout feedback shown on attempted re-pick.

### TR-PU-106 — Cartoon Tank Pack Art Integration for Power-Ups
- **Type:** Task | **Priority:** P1 | **Estimate:** 3
- **Description:** Map pickups/VFX to Cartoon Tank Pack assets and palette.
- **Acceptance Criteria:**
  - Icons, VFX, and pickup glows sourced or derived from Cartoon Tank Pack.
  - Style consistency pass approved by PM + art reviewer.

### TR-PU-107 — Difficulty Scaling Config (Cadet/Commander/Legend)
- **Type:** Story | **Priority:** P0 | **Estimate:** 5
- **Description:** Configurable multipliers for duration, cooldown, drop rate, AI use efficiency.
- **Acceptance Criteria:**
  - All values tunable via data table.
  - Difficulty presets load correctly in challenge mode.

### TR-PU-108 — Anti-Snowball Spawn Bias Rules
- **Type:** Story | **Priority:** P1 | **Estimate:** 3
- **Description:** Add score-state-aware spawn biasing per PM rules.
- **Acceptance Criteria:**
  - Trailing player gets Armor Bubble bias when condition met.
  - Repetition guard prevents 3 identical spawns in sequence.

---

### TR-CM-200 — Challenge Mode Campaign v1 (Epic)
- **Type:** Epic | **Priority:** P0 | **Estimate:** 55
- **Description:** Build and ship 10 handcrafted single-player challenges with progression and rewards.
- **Acceptance Criteria:**
  - All 10 challenges playable and completable.
  - Win/loss conditions, AI setup, and terrain layouts match design.
  - Reward progression persists correctly.

### TR-CM-201 — Challenge 1–3 Authoring (Onboarding Arc)
- **Type:** Story | **Priority:** P0 | **Estimate:** 8
- **Description:** Build C1-C3 maps/objectives/tutorial cues.
- **Acceptance Criteria:**
  - Objectives and mastery checks implemented.
  - Power-up onboarding pacing validated via playtest.

### TR-CM-202 — Challenge 4–6 Authoring (Mid Arc)
- **Type:** Story | **Priority:** P0 | **Estimate:** 10
- **Description:** Build C4-C6 with crate-heavy tactical progression.
- **Acceptance Criteria:**
  - Block Breaker-focused flow validated.
  - Symmetry/power-up denial scenario works as intended.

### TR-CM-203 — Challenge 7–10 Authoring (Endgame Arc)
- **Type:** Story | **Priority:** P0 | **Estimate:** 13
- **Description:** Build high-intensity late campaign and finale.
- **Acceptance Criteria:**
  - Elite AI encounters tuned for each difficulty.
  - Finale mastery requirements tracked and rewarded.

### TR-CM-204 — Challenge Objective System & Mastery Tracking
- **Type:** Story | **Priority:** P0 | **Estimate:** 8
- **Description:** Objective tracker supports kill targets, timers, crate destruction, and mastery conditions.
- **Acceptance Criteria:**
  - Real-time objective UI and end-screen verdict.
  - Mastery flags saved per challenge.

### TR-CM-205 — Reward Economy & Unlock Pipeline
- **Type:** Story | **Priority:** P0 | **Estimate:** 8
- **Description:** Credits payout, cosmetics unlock, campaign-only perks, completion badge.
- **Acceptance Criteria:**
  - Reward table maps exactly to C1-C10 spec.
  - No duplicate unlock errors.
  - Completion unlock triggers Champion skin.

### TR-CM-206 — AI Composition Profiles per Challenge
- **Type:** Story | **Priority:** P1 | **Estimate:** 5
- **Description:** Configure archetype behavior packages (Scout/Bruiser/Sniper/Demolisher/Ace).
- **Acceptance Criteria:**
  - Each challenge loads intended enemy mix.
  - AI power-up pickup logic respects difficulty preset.

### TR-CM-207 — Terrain Layout Data Authoring (30x30)
- **Type:** Task | **Priority:** P1 | **Estimate:** 5
- **Description:** Define crate placement, choke points, open lanes, and spawn safety.
- **Acceptance Criteria:**
  - 10 data-authored layouts exported and versioned.
  - Spawn fairness check passes for all levels.

### TR-CM-208 — Campaign Menu Flow + Unlock Gating
- **Type:** Story | **Priority:** P1 | **Estimate:** 5
- **Description:** Level select, locked/unlocked states, completion stars/mastery indicators.
- **Acceptance Criteria:**
  - Sequential progression enforced.
  - Replay and mastery visibility clear.

---

### TR-QA-300 — Power-Up Balance Test Matrix
- **Type:** Task | **Priority:** P0 | **Estimate:** 5
- **Description:** Build test matrix by power-up x difficulty x AI profile.
- **Acceptance Criteria:**
  - Baseline fairness KPIs captured (win rate, TTK, pickup impact).
  - No dominant strategy exceeds target thresholds.

### TR-QA-301 — Campaign Difficulty Curve Validation
- **Type:** Task | **Priority:** P0 | **Estimate:** 5
- **Description:** Validate intended completion rates and frustration points.
- **Acceptance Criteria:**
  - Cadet target completion: 85%+ through C6.
  - Commander target completion: 60%+ through C8.
  - Legend target completion: 30%+ through C10.

### TR-AN-400 — Telemetry for Power-Up Usage & Challenge Outcomes
- **Type:** Task | **Priority:** P1 | **Estimate:** 3
- **Description:** Instrument pickups, activations, blocked hits, crate breaks, objective failures.
- **Acceptance Criteria:**
  - Event schema documented.
  - Dashboard-ready export for balancing iterations.

---

## 4) Release Readiness Gate (PM)
- All P0 tickets done.
- Challenge 1–10 pass functional QA and balance smoke tests on Cadet/Commander/Legend.
- Art pass signed off for Cartoon Tank Pack style consistency.
- Telemetry live for post-launch balance patching.

---

## 5) Post-Launch Balance Plan (v1.1)
- Week 1: Monitor dominant power-up pick/win correlation by difficulty.
- Week 2: Adjust drop weights and personal cooldowns (data-only patch preferred).
- Week 3: Tune C8–C10 enemy aggression and respawn intervals if fail rates exceed targets.
