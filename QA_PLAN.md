# Tank Royale QA Plan

**Owner:** QA Engineer  
**Project:** Tank Royale  
**Last Updated:** 2026-03-03  
**Reference:** `PROJECT_BRIEF.md` (until GDD v1 is published)

---

## 1) QA Objectives & Quality Gates

### Objectives
- Ensure Tank Royale is stable, fair, and responsive across target platforms (Web, iOS, Android, Xbox).
- Prevent gameplay regressions in core loop: movement, aiming, shooting, AI combat, power-ups, terrain destruction.
- Enforce minimum performance standards, especially on low-end mobile devices.
- Integrate QA into development so quality checks happen **before merge**, not only before release.

### Release Quality Gates (must pass)
1. **No open P0/P1 defects** in release branch.
2. **All required regression tests pass** (see Section 3).
3. **Performance benchmarks meet threshold** on low-end mobile + one console + one desktop browser.
4. **Crash-free sessions >= 99.5%** during pre-release test window.
5. **Accessibility checklist complete** for all changed user-facing features.
6. **GDD validation checklist complete** for changed mechanics/content.

---

## 2) Testing Matrix

## 2.1 Platform/Device Coverage

| Tier | Platform | Device / Environment | OS / Runtime | Build Type | Frequency |
|---|---|---|---|---|---|
| P0 | Web | Chrome desktop (Windows/Mac) | Latest stable | WebGL | Per PR smoke + nightly |
| P0 | Android (low-end) | Moto G Power (or equivalent) | Android 12+ | Native/IL2CPP | Per PR smoke (critical areas) + nightly |
| P0 | iOS (low-end) | iPhone SE (2nd gen) or equivalent | iOS 16+ | Native | Nightly + pre-release |
| P0 | Xbox | Xbox Series S dev kit/test kit | Latest | Console build | Nightly + pre-release |
| P1 | Web | Safari (macOS/iOS), Firefox (desktop) | Latest stable | WebGL | Nightly |
| P1 | Android (mid/high) | Pixel 7 / Samsung A-series | Android 13+ | Native | Nightly |
| P1 | iOS (mid/high) | iPhone 13+ | iOS 17+ | Native | Nightly |

## 2.2 Input Method Coverage

| Input Method | Platforms | Required Test Areas |
|---|---|---|
| Keyboard + Mouse | Web/Desktop | Movement, aim precision, rebinding, pause/menu navigation |
| Touchscreen | iOS/Android | Virtual stick responsiveness, dead zones, thumb reach, accidental tap rejection |
| Gamepad (generic) | Desktop/Web where supported | Dual-stick aim/move, trigger fire rate, menu focus order |
| Xbox Controller | Xbox | Full gameplay loop + menu/settings navigation |

## 2.3 Match Configuration Coverage

| Scenario | Players/AI | Arena | Purpose |
|---|---|---|---|
| Standard Match | 1 player + 3 AI | 30x30 random crates | Core loop validation |
| Destruction Stress | 1 + 3 AI | High crate density | Physics/load and terrain destruction correctness |
| Power-Up Stress | 1 + 3 AI | Boosted spawn rates (test config) | Power-up stacking/expiry edge cases |
| Challenge Mode | 1 player | Levels 1-10 | Progression and balance regression |

---

## 3) Regression Test Suite (Required)

> IDs are stable; automation should map to these IDs where possible.

### 3.1 Core Gameplay
- **TR-RG-001 Spawn & Controls:** player spawns correctly, can move/aim/fire within 2 seconds.
- **TR-RG-002 Projectile Collision:** bullets damage tanks; no ghost hits through solid walls.
- **TR-RG-003 Player Death/Respawn/End:** correct elimination flow and end-of-match state.
- **TR-RG-004 Camera Bounds:** camera follows player and remains inside arena limits.

### 3.2 AI Behavior
- **TR-RG-010 AI Navigation:** AI avoids blocked tiles and does not stall >3s without pathing reason.
- **TR-RG-011 AI Combat:** AI aims/fires within expected behavior profile.
- **TR-RG-012 AI Difficulty Consistency:** challenge level scaling changes behavior/accuracy as designed.

### 3.3 Terrain & Arena
- **TR-RG-020 Arena Generation:** valid 30x30 grid, spawn-safe tiles, reachable lanes.
- **TR-RG-021 Crate Destruction:** destructible crates break correctly and update collision map.
- **TR-RG-022 Block Breaker Interaction:** only intended tiles destructible; no chain corruption.

### 3.4 Power-Ups
- **TR-RG-030 Ricochet Bullets:** expected bounce count/angles; no infinite loop.
- **TR-RG-031 Armor Bubble:** absorbs exactly one extra hit; visual/audio state synced.
- **TR-RG-032 Power-Up Timing:** duration starts/ends accurately and clears status effects.
- **TR-RG-033 Multiple Pickups:** pickup arbitration is deterministic and matches design rules.

### 3.5 UI/UX & Settings
- **TR-RG-040 Main Menu Navigation:** start game, challenge mode, settings, back navigation.
- **TR-RG-041 Key Binding Save/Load:** remaps persist after restart; conflict handling clear.
- **TR-RG-042 Pause/Resume:** pause freezes gameplay simulation appropriately.
- **TR-RG-043 HUD Accuracy:** health, armor, active power-up indicators match actual state.

### 3.6 Modes & Progression
- **TR-RG-050 Challenge Mode Level Unlock:** levels unlock/track as designed.
- **TR-RG-051 Offline AI Mode:** playable without network dependency.
- **TR-RG-052 Session Recovery:** app resume/relaunch safely returns to menu or recoverable state.

### 3.7 Platform Reliability
- **TR-RG-060 Startup/Shutdown:** cold start < target time, no crash on exit.
- **TR-RG-061 Background/Foreground (mobile):** state survives interruptions.
- **TR-RG-062 Controller Disconnect/Reconnect:** graceful prompt and recovery.

---

## 4) Performance Benchmark Plan

## 4.1 Performance Targets

### Low-End Mobile (primary gate)
- **Target framerate:** 60 FPS preferred
- **Minimum acceptable average FPS:** **>= 45 FPS** during heavy combat
- **1% low FPS:** **>= 30 FPS**
- **Frame-time spikes:** < 50ms for 99th percentile during normal gameplay
- **RAM budget:** no sustained growth trend > 10% over 20-minute run (leak check)
- **Thermal behavior:** no severe thermal throttling before 15-minute mark in stress test

### Other Platforms
- Web desktop (Chrome): avg >= 60 FPS in standard match
- Xbox Series S: avg >= 60 FPS, 1% low >= 45 FPS

## 4.2 Benchmark Scenarios
1. **Perf-01 Idle Arena:** 60 seconds post-spawn, no firing.
2. **Perf-02 Standard Combat:** 5 minutes, 1 player + 3 AI.
3. **Perf-03 Stress Combat:** maximum simultaneous projectiles + crate destruction.
4. **Perf-04 Long Session:** 20 minutes continuous challenge gameplay.

## 4.3 Measurement Method
- Unity Profiler (CPU/GPU/memory), frame timing logs, and in-game telemetry markers.
- Capture per-build benchmark report JSON + summary markdown.
- Compare against previous successful build; flag >10% regressions automatically.

---

## 5) Crash Reporting Protocol

## 5.1 Tooling
- Integrate crash SDK (Unity Cloud Diagnostics and/or Crashlytics/Sentry).
- Collect both **fatal crashes** and **non-fatal exceptions**.

## 5.2 Required Crash Payload
- Build version + git commit SHA
- Platform/device/OS/runtime
- Stack trace + breadcrumbs (last 20 gameplay events)
- Scene/mode context (menu/match/challenge level)
- Active power-ups and entity counts at time of crash

## 5.3 Severity & SLA
- **P0 (release blocker):** crash on startup, during first match, or reproducible crash in common flow.  
  - Triage: immediate, fix start within 2 hours.
- **P1:** frequent crash in non-critical flow or specific device class.  
  - Triage same day, hotfix candidate.
- **P2/P3:** rare/edge-case crashes.  
  - Queue for sprint planning with workaround notes.

## 5.4 Operational Protocol
1. Auto-create tracker issue from crash alert (if threshold met).
2. QA reproduces on same build/device class.
3. Add minimal repro steps + logs + video if needed.
4. Assign owner and target milestone.
5. Verify fix on original repro device + one additional device.
6. Close only after confirmation in next build and no recurrence spike.

---

## 6) PR Code Review Checklist (QA Gate)

> **Every PR must pass this checklist before merge.**

### Functional
- [ ] Change matches ticket scope and acceptance criteria.
- [ ] No broken core loop behavior (move/aim/fire/hit/die).
- [ ] Edge cases handled (null refs, empty states, invalid inputs).

### Testing
- [ ] Unit/PlayMode tests added or updated for changed logic.
- [ ] Relevant regression IDs listed in PR and executed.
- [ ] QA notes include test environment/device.

### Performance
- [ ] No obvious per-frame allocations in hot paths.
- [ ] Object pooling used for frequent spawned objects (projectiles/effects).
- [ ] Profiling evidence attached for systems affecting combat/rendering.

### Platform/Input
- [ ] Works for all impacted input methods.
- [ ] No platform-specific compile/runtime errors.
- [ ] Mobile background/resume and controller reconnect considered where relevant.

### Accessibility
- [ ] UI text legible and contrast-compliant.
- [ ] Feature supports remapping/toggle where required.
- [ ] Color is not sole indicator for critical gameplay state.

### Observability/Debuggability
- [ ] Logs are actionable (no spam; include context).
- [ ] New errors are surfaced with meaningful messages.
- [ ] Crash/telemetry events updated if behavior changed.

### Merge Rule
- [ ] CI checks green
- [ ] 1 dev reviewer approval
- [ ] **QA checklist sign-off required**
- [ ] No unresolved P0/P1 linked to this PR

---

## 7) Accessibility Validation Checklist

- **Controls**
  - [ ] Full remapping support for keyboard/controller actions.
  - [ ] Touch controls support left/right-handed layout options (if implemented).
- **Visual Clarity**
  - [ ] Minimum text size readable on 6" mobile screen.
  - [ ] UI contrast ratio target >= 4.5:1 for critical text/icons.
  - [ ] Power-up states not communicated by color alone (shape/icon + text/hud cue).
- **Cognitive Load**
  - [ ] Tutorial/first-time prompts concise and skippable/replayable.
  - [ ] Clear feedback for damage, pickups, cooldowns.
- **Audio/Haptics**
  - [ ] Distinct SFX categories with independent volume controls (master/SFX/music).
  - [ ] Haptics toggle and intensity (mobile/controller) if supported.
- **Motion/Comfort**
  - [ ] Optional reduced screen shake and flash intensity.

Accessibility issues are tagged `accessibility` and cannot be deferred past release without product sign-off.

---

## 8) Bug Logging & Triage Process

## 8.1 Required Bug Fields
- Title (clear symptom + location)
- Build number + commit SHA
- Platform/device/OS/input method
- Repro steps (numbered)
- Expected vs actual result
- Attachments (video/screenshot/log/crash ID)
- Frequency (always/often/intermittent/once)
- Severity (P0-P3), Priority (High/Med/Low)
- Suspected area/component label

## 8.2 Severity Definitions
- **P0:** crash/blocker/data loss/unplayable core loop
- **P1:** major feature broken; strong player impact
- **P2:** functional issue with workaround
- **P3:** cosmetic/minor usability issue

## 8.3 Triage Cadence
- **Daily 15-min triage:** QA + PM + tech lead (new bugs, priority updates).
- **Pre-release triage:** review all open P1/P2, set explicit ship/no-ship decisions.

## 8.4 Workflow States
`New -> Triaged -> In Progress -> Fixed -> In QA Verify -> Closed`  
Optional: `Blocked`, `Won’t Fix` (requires PM + QA note)

---

## 9) Integration with Dev Workflow

## 9.1 Definition of Done (per ticket)
A ticket is Done only when:
- Implementation complete
- Tests updated/passing
- Relevant regression suite executed
- QA checklist signed off
- Accessibility impact reviewed
- Telemetry/crash instrumentation updated if needed

## 9.2 CI/CD QA Gates
- On every PR:
  - Static checks + unit tests
  - Smoke PlayMode tests
  - Build validation for impacted platforms
- Nightly:
  - Expanded automated regression run
  - Performance benchmark pack
  - Crash trend summary
- Pre-release:
  - Full manual matrix pass (Section 2)
  - Full required regression suite (Section 3)

## 9.3 Merge Policy
- Branch protection enabled on main/release branches.
- PR cannot merge without completed QA checklist and passing checks.
- Any failed P0/P1 test auto-blocks merge.

---

## 10) Validating Game Design Against the GDD

## 10.1 GDD Traceability Matrix
Create and maintain `GDD_TRACEABILITY.md` with:
- `GDD-ID`
- Requirement statement
- Test case IDs (manual/automated)
- Telemetry metric used to validate
- Status (Pass/Fail/At Risk)
- Notes/owner

## 10.2 Validation Workflow
1. **Parse GDD into testable requirements** (mechanics, UX, progression, balance).
2. Map each requirement to one or more test cases.
3. Validate both **functional correctness** and **player experience intent**.
4. Use playtest + telemetry to confirm balance and fun targets.
5. Report variance from GDD as defects or design-change requests.

## 10.3 Design Validation Metrics (examples)
- Average match duration within target range defined by GDD.
- Power-up pickup/use rates per match (no dominant outlier unless intended).
- Win rate distribution across challenge levels aligns with intended difficulty curve.
- Early-exit rate and retry rate by level identify frustration spikes.

## 10.4 Sign-Off Rule
- Any feature marked “Must Have” in GDD must be **implemented, tested, and verified** before release.
- If GDD changes after implementation, impacted tests are updated in the same sprint.

> **Current note:** Until a full GDD is available, QA uses `PROJECT_BRIEF.md` as temporary source-of-truth and backfills traceability once GDD v1 is published.

---

## 11) Reporting

- **Daily QA Update:** test progress, new blockers, top regressions.
- **Weekly Quality Report:** pass/fail trends, open defect burndown, crash/perf trends, release risk.
- **Release Readiness Summary:** explicit recommendation: `GO` / `NO-GO` with rationale.
