# Tank Royale — MENU SYSTEM PM Backlog (JIRA-Style)

## Scope
Complete UI/UX flow for:
- Main Menu
- Pause Menu
- Settings (Key Bindings, Volume, Graphics)
- Challenge Mode Level Select
- Player Profile
- Cross-platform input support (Mouse/Keyboard, Touch, Controller)
- Accessibility and animation polish

## Technical Architect Coordination (Feasibility Confirmed)
**Status:** GO with conditions.

**Architectural decisions locked:**
1. Prefer **uGUI + Unity Input System + EventSystem** for fastest parity across KBM/touch/controller (UI Toolkit acceptable if already standardized, with extra controller-nav QA budget).
2. Implement a centralized **Menu State Machine + Screen/Modal Stack** (`Main -> Submenu -> Modal`) with explicit Back/Cancel routing.
3. Use **View + Presenter/ViewModel** separation (no gameplay logic in UI MonoBehaviours).
4. Keep menu transitions on **unscaled time** to support pause-state UI.
5. Use **ScriptableObject-driven configs** for menu definitions, challenge metadata, accessibility defaults, and graphics option manifests.
6. Use a versioned **Save/Profile service** (atomic writes + migration support).
7. Add dedicated input services: **InputPromptService** (glyph swaps), **RebindService**, **ControlSchemeDetector**.
8. Use a capability-gated **Graphics Settings manifest** per platform (WebGL/mobile/Xbox constraints).

**Feasibility risks to track from TA review:**
- Highest risk: cross-platform input consistency + rebinding conflicts.
- Medium risk: pause behavior in multiplayer contexts, profile migration edge cases, and Xbox compliance semantics.
- Mitigations: deterministic focus graph, platform locklist for reserved buttons, QA input smoke suite, explicit TRC/TCR checklist, and suspend/resume coverage.

---

## UX Flow Map (v1)
1. **Boot/Splash** → 2. **Main Menu**
   - Play → Mode Select (Quick Match / Challenge)
   - Challenge → **Level Select** → Match Start
   - Profile → **Player Profile**
   - Settings → **Settings Hub** (Controls / Audio / Graphics / Accessibility)
2. **In Match** → Pause input → **Pause Menu**
   - Resume / Restart / Settings / Exit to Main Menu
3. Any destructive action (Exit, Restart, Reset Defaults) → Confirmation Modal

---

## Cross-Platform Input Mapping Baseline
| Intent | Mouse/Keyboard | Touch | Controller (Xbox layout) |
|---|---|---|---|
| Navigate menu | Mouse hover + click, Arrow keys | Tap, swipe for list scroll | Left Stick / D-Pad |
| Confirm / Select | Left Click, Enter, Space | Tap button | A |
| Back / Cancel | Esc, Backspace | Top-left back button | B |
| Switch tabs | Q/E, Ctrl+Tab, click tab | Tap tab header, horizontal swipe | LB/RB |
| Pause | Esc | Pause icon (top-right) | Menu/Start |
| Slider adjust | Drag, A/D or Left/Right | Drag slider | Left/Right on D-Pad or Stick |
| Rebind start | Click “Rebind” + key press | Tap “Rebind” + virtual key prompt | Select action + press target button |
| Rebind cancel | Esc | Tap Cancel | B |
| Scroll lists | Mouse wheel | Swipe/drag list | Right Stick / D-Pad |

**Input UX rules:**
- Last-used input method becomes active prompt set (dynamic button glyph swap).
- Minimum touch target = **48x48 dp**.
- Full menu must be operable without pointer (keyboard/controller focus ring + logical tab order).

---

## Epic Tickets

### MENU-EPIC-01 — Unified Menu Framework & Navigation State Machine
- **Type:** Epic
- **Priority:** Highest
- **Description:** Build reusable menu shell with screen routing, modal stack, and transition handling across all menu surfaces.
- **UI Assets Needed:**
  - Global background layers (static + parallax)
  - Shared panel frame (9-slice)
  - Primary/secondary button states (default/hover/focus/pressed/disabled)
  - Icon set for Play, Settings, Profile, Back, Confirm
- **Interactions:** Focus management, breadcrumb/back stack, modal blocking, unsaved-change prompts.
- **Animations:** 200–300ms screen slide/fade; modal scale-in (overshoot max 1.04x).
- **Accessibility:** Reduced motion toggle support, focus ring visibility at all times.
- **Cross-Platform Input:** Centralized input action map for UI_Navigate/UI_Submit/UI_Cancel/UI_Pause.
- **Technical Architect Feasibility Notes:**
  - Use Unity Input System action maps and one menu state machine service.
  - Keep transitions unscaled (work during paused timescale).
  - Avoid nested canvases deeper than 3 levels to reduce rebuild cost.
- **Acceptance Criteria:**
  - All menu screens can be opened/closed via router.
  - Back action is deterministic on all platforms.
  - No input dead-ends (every focused element reachable).

### MENU-EPIC-02 — Main Menu Experience
- **Type:** Epic
- **Priority:** High
- **Description:** High-clarity entry point with Play, Challenge, Profile, Settings, and Quit/Exit behavior by platform.
- **UI Assets Needed:**
  - Logo lockup + title treatment
  - Hero background art with subtle motion layers
  - CTA button art variants
  - Platform-specific footer hints (e.g., A=Select, Esc=Back)
- **Interactions:** First focused control defaults to Play; dynamic contextual hints per device.
- **Animations:** Logo intro (max 1.2s), CTA stagger reveal (80ms offset).
- **Accessibility:** Option to skip intro animation; large text compatibility.
- **Cross-Platform Input:** Pointer-first on desktop, focus-first on controller.
- **Technical Architect Feasibility Notes:**
  - Detect platform via runtime capability flags; hide unsupported options.
  - Keep hero background effect GPU-light for WebGL/mobile.
- **Acceptance Criteria:**
  - Time to interactive <2.5s after load (target hardware).
  - All main actions accessible in <=2 inputs from first focus.

---

## Story / Task Tickets

### MENU-101 — Main Menu Information Architecture & Wireframe
- **Type:** Story
- **Priority:** High
- **Description:** Define final hierarchy, placement, and spacing system for all main menu controls and legal/footer region.
- **UI Assets Needed:** Low-fi wireframes, spacing token sheet, typography scale.
- **Interactions:** Hover/focus parity; safe area adaptation for mobile.
- **Animations:** None (wireframe phase).
- **Accessibility:** Contrast-compliant text styles (WCAG AA target).
- **Input Mapping:** Validate keyboard/controller traversal order.
- **Dependencies:** MENU-EPIC-02.
- **Acceptance Criteria:** Approved wireframes for desktop/mobile/controller safe zones.

### MENU-102 — Main Menu Production UI + Dynamic Prompt Glyphs
- **Type:** Story
- **Priority:** High
- **Description:** Build production main menu prefabs with dynamic glyph swap based on current input device.
- **UI Assets Needed:** Final button sprites/atlases, input glyph icon pack (KBM, touch, Xbox).
- **Interactions:** Last input source updates prompts in <150ms.
- **Animations:** Button hover pulse (subtle), focus ring animate-in.
- **Accessibility:** Focus ring thickness option (normal/high visibility).
- **Input Mapping:** KBM/touch/controller prompt switching.
- **Dependencies:** MENU-101, MENU-EPIC-01.
- **Acceptance Criteria:** Prompt glyphs always match active device; no stale hints.

### MENU-103 — Pause Menu Layout & Resume Flow
- **Type:** Story
- **Priority:** Highest
- **Description:** Implement in-match pause overlay with Resume, Restart, Settings, Exit to Main Menu.
- **UI Assets Needed:** Pause panel frame, dim overlay, iconography, confirm modal templates.
- **Interactions:** Pause toggles overlay; input locked to menu while open.
- **Animations:** Dim fade (120ms), panel slide-down (180ms).
- **Accessibility:** Maintain readable contrast over gameplay scene.
- **Input Mapping:** Esc/Menu opens; B/Esc closes if no modal active.
- **Dependencies:** MENU-EPIC-01.
- **Acceptance Criteria:** No gameplay input leakage while paused; Resume always returns control cleanly.

### MENU-104 — Pause Menu Confirmation Modals (Restart/Exit)
- **Type:** Story
- **Priority:** High
- **Description:** Add destructive action confirmations with clear primary/secondary actions.
- **UI Assets Needed:** Warning icon, modal button variants, backdrop blur (optional).
- **Interactions:** Default focus on non-destructive action.
- **Animations:** Modal pop + backdrop fade.
- **Accessibility:** Explicit copy (“Progress in current match will be lost”).
- **Input Mapping:** A/Enter confirm, B/Esc cancel.
- **Dependencies:** MENU-103.
- **Acceptance Criteria:** Restart/Exit cannot be triggered without explicit confirmation.

### MENU-105 — Settings Hub Shell (Tabs + Save/Apply Model)
- **Type:** Story
- **Priority:** Highest
- **Description:** Create unified settings shell with tabs: Controls, Audio, Graphics, Accessibility.
- **UI Assets Needed:** Tab headers, section dividers, apply/revert/reset buttons.
- **Interactions:** Dirty-state tracking; Apply/Revert availability.
- **Animations:** Tab content crossfade (150ms).
- **Accessibility:** Clear labels and helper text under each setting.
- **Input Mapping:** LB/RB or Q/E to switch tabs.
- **Dependencies:** MENU-EPIC-01.
- **Acceptance Criteria:** Unsaved changes prompt appears before leaving settings.

### MENU-106 — Audio Settings (Master/Music/SFX)
- **Type:** Story
- **Priority:** Medium
- **Description:** Implement volume sliders with live preview and mute toggles.
- **UI Assets Needed:** Slider rails/thumbs, mute icons, numeric value labels.
- **Interactions:** Real-time feedback tone at adjusted category.
- **Animations:** Slider value tooltip fade-in.
- **Accessibility:** Non-audio confirmation (value text) for hearing-impaired players.
- **Input Mapping:** Drag/tap/left-right step increments.
- **Dependencies:** MENU-105.
- **Acceptance Criteria:** Settings persist between sessions; no clipping at max values.

### MENU-107 — Graphics Settings with Platform Gating
- **Type:** Story
- **Priority:** High
- **Description:** Implement graphics options with conditional visibility by platform capability.
- **UI Assets Needed:** Dropdowns/toggles for quality preset, shadows, VFX intensity, FPS cap.
- **Interactions:** Show unsupported options as hidden or disabled with tooltip rationale.
- **Animations:** None required.
- **Accessibility:** Tooltips in plain language.
- **Input Mapping:** Standard tab/arrow/controller cycling.
- **Dependencies:** MENU-105.
- **Technical Architect Feasibility Notes:**
  - WebGL: expose only safe subset (quality + VFX + FPS cap); avoid unsupported APIs.
  - Mobile: default medium preset and thermal-safe frame cap.
- **Acceptance Criteria:** No option causes crash or major hitch on target platforms.

### MENU-108 — Key Binding Rebind Flow (Keyboard/Mouse)
- **Type:** Story
- **Priority:** Highest
- **Description:** Build rebind UI for movement, fire, pause, and menu actions; include conflict detection and reset defaults.
- **UI Assets Needed:** Action list rows, “Listening…” state chips, conflict warning icon.
- **Interactions:** Capture next key/button; reject reserved/system keys with message.
- **Animations:** Listening state pulse.
- **Accessibility:** Show both icon and readable key name (e.g., “Left Mouse Button”).
- **Input Mapping:** Enter starts rebind; Esc cancels; click row to start on pointer.
- **Dependencies:** MENU-105.
- **Technical Architect Feasibility Notes:**
  - Use Input System rebinding APIs; serialize overrides to persistent data.
  - Provide one-click reset map by control scheme.
- **Acceptance Criteria:** Rebind persists after restart; conflicts blocked or resolved with user confirmation.

### MENU-109 — Controller Mapping & Deadzone Options
- **Type:** Story
- **Priority:** High
- **Description:** Add controller settings: look/move deadzone sliders, vibration toggle, invert axis options.
- **UI Assets Needed:** Controller diagram, slider/toggle components, test vibration prompt.
- **Interactions:** Live test area for deadzone feedback.
- **Animations:** Controller highlight flash on detected input.
- **Accessibility:** Explanatory helper text for deadzone/invert settings.
- **Input Mapping:** Full operability from controller only.
- **Dependencies:** MENU-105.
- **Acceptance Criteria:** Works on Xbox controller and standard XInput devices.

### MENU-110 — Touch-Optimized Settings & Navigation Behavior
- **Type:** Story
- **Priority:** High
- **Description:** Tune menu density and gesture behavior for mobile touch ergonomics.
- **UI Assets Needed:** Larger touch button variants, mobile-safe headers, gesture hints.
- **Interactions:** Scroll inertia, tap debouncing, edge-safe back button.
- **Animations:** Subtle tap feedback scale.
- **Accessibility:** Minimum 48x48 dp targets and 8dp spacing.
- **Input Mapping:** Gesture + tap-only flow without hardware buttons.
- **Dependencies:** MENU-EPIC-01, MENU-105.
- **Acceptance Criteria:** No mis-taps in QA scripted flow on common phone resolutions.

### MENU-111 — Challenge Mode Level Select Grid
- **Type:** Story
- **Priority:** Highest
- **Description:** Build 10-level challenge select with lock/progress indicators and best score/time summary.
- **UI Assets Needed:** Level tiles (locked/unlocked/completed), medal icons, progress bar.
- **Interactions:** Select tile -> preview panel update -> confirm start.
- **Animations:** Tile focus zoom (1.03x), unlock burst effect.
- **Accessibility:** Use icon+text status (not color-only).
- **Input Mapping:** D-pad/stick grid navigation + touch tap.
- **Dependencies:** MENU-EPIC-01.
- **Acceptance Criteria:** Unlock logic reflects progression data reliably.

### MENU-112 — Level Preview Panel & Launch Confirmation
- **Type:** Story
- **Priority:** Medium
- **Description:** Right-side panel with level objective, enemy mix, and expected difficulty.
- **UI Assets Needed:** Mini-map thumbnails, objective icons, difficulty badges.
- **Interactions:** Start level from panel; blocked if locked.
- **Animations:** Preview image fade/slide on tile change.
- **Accessibility:** Readable text at large font mode.
- **Input Mapping:** A/Enter to launch, B/Esc to back.
- **Dependencies:** MENU-111.
- **Acceptance Criteria:** Correct data binding for all 10 challenge levels.

### MENU-113 — Player Profile Overview Screen
- **Type:** Story
- **Priority:** High
- **Description:** Create profile screen showing player name, avatar, match stats, challenge completion, and preferred tank skin.
- **UI Assets Needed:** Avatar frames, stat cards, editable name field, skin thumbnail rail.
- **Interactions:** Edit name, choose avatar/skin, save profile.
- **Animations:** Card reveal stagger, avatar focus ring.
- **Accessibility:** Input validation messages in plain language.
- **Input Mapping:** On-screen keyboard fallback for consoles/touch.
- **Dependencies:** MENU-EPIC-01.
- **Acceptance Criteria:** Profile edits persist and reflect immediately in menu HUD.

### MENU-114 — Profile Persistence Schema & Future Online-Ready IDs
- **Type:** Task
- **Priority:** Medium
- **Description:** Define local profile schema with stable player ID, display name, cosmetics, and progress fields for future online sync.
- **UI Assets Needed:** N/A (data task).
- **Interactions:** Save/load profile seamlessly at startup.
- **Animations:** N/A.
- **Accessibility:** N/A.
- **Input Mapping:** N/A.
- **Dependencies:** MENU-113.
- **Technical Architect Feasibility Notes:**
  - Store JSON in persistent data path with versioned schema.
  - Include migration handler for future fields.
- **Acceptance Criteria:** Backward-compatible load for schema v1→v2 migration test.

### MENU-115 — Accessibility Panel (Visual, Motion, Input)
- **Type:** Story
- **Priority:** Highest
- **Description:** Deliver dedicated accessibility options: text scale, high contrast UI, reduced motion, colorblind-safe indicators, hold-to-confirm toggle.
- **UI Assets Needed:** High-contrast theme set, icon variants, alt color palette tokens.
- **Interactions:** Live preview of each accessibility change.
- **Animations:** Reduced motion mode disables non-essential tweening.
- **Accessibility:** Core feature ticket.
- **Input Mapping:** All accessibility options reachable via controller only.
- **Dependencies:** MENU-105.
- **Acceptance Criteria:** Accessibility presets apply globally and persist.

### MENU-116 — Focus Order, Screen Reader Labels, and Semantic Naming
- **Type:** Task
- **Priority:** High
- **Description:** Enforce deterministic focus traversal and semantic labels for UI automation/accessibility layers.
- **UI Assets Needed:** N/A.
- **Interactions:** Focus never lost when dynamic elements hide/show.
- **Animations:** None.
- **Accessibility:** Descriptive labels for all actionable controls.
- **Input Mapping:** Keyboard/controller parity.
- **Dependencies:** MENU-EPIC-01, MENU-115.
- **Acceptance Criteria:** Automated traversal test passes on all menu screens.

### MENU-117 — UI Asset Production & Integration Pack
- **Type:** Task
- **Priority:** High
- **Description:** Produce and integrate full menu asset bundle with naming conventions and atlas packing.
- **UI Assets Needed:**
  - Buttons, tabs, panels, modals, icons
  - Input glyph sets (KBM/Touch/Xbox)
  - Challenge tile states
  - Profile frames, badges
  - Accessibility theme variants
- **Interactions:** N/A.
- **Animations:** N/A.
- **Accessibility:** Ensure high-contrast variants for critical elements.
- **Input Mapping:** Include prompt glyph mapping sheet.
- **Dependencies:** MENU-101, MENU-111, MENU-113, MENU-115.
- **Acceptance Criteria:** Asset manifest complete; no missing references in prefab audit.

### MENU-118 — Motion Design Implementation & Performance Budget
- **Type:** Task
- **Priority:** High
- **Description:** Implement animation tokens and enforce menu performance budgets across target platforms.
- **UI Assets Needed:** Motion spec sheet (duration/easing/intensity tiers).
- **Interactions:** Interruptible transitions (no lockups on rapid back/forward input).
- **Animations:** Standardized curves; reduced-motion fallback path.
- **Accessibility:** Reduced motion reduces distance, duration, and particle usage.
- **Input Mapping:** Inputs remain responsive during transitions.
- **Dependencies:** MENU-EPIC-01.
- **Technical Architect Feasibility Notes:**
  - Prefer lightweight tweening; avoid expensive full-canvas alpha animations.
  - WebGL/mobile target: menu frame cost <=1.5ms CPU on reference devices.
- **Acceptance Criteria:** Meets frame budget in profiling captures.

### MENU-119 — Cross-Platform QA Matrix & Input Compliance
- **Type:** Task
- **Priority:** Highest
- **Description:** Build QA checklist covering every menu path and input method combination.
- **UI Assets Needed:** Test matrix template and bug taxonomy sheet.
- **Interactions:** End-to-end scripted flows for each platform.
- **Animations:** Validate reduced-motion and standard modes.
- **Accessibility:** Verify contrast, text scale clipping, colorblind clarity.
- **Input Mapping:** Full matrix: KBM / touch / controller.
- **Dependencies:** All functional stories.
- **Acceptance Criteria:** Zero blocker issues in menu certification pass.

### MENU-120 — Technical Architect Sign-off Gate (Feasibility & Risk)
- **Type:** Task
- **Priority:** Highest
- **Description:** Formal architecture review checkpoint before implementation sprint lock.
- **UI Assets Needed:** Final wireframes, flow diagram, settings schema, input mapping matrix.
- **Interactions:** Review meeting + issue log triage.
- **Animations:** Review motion spec risk areas.
- **Accessibility:** Validate feasibility of all accessibility commitments.
- **Input Mapping:** Validate platform-specific behavior for WebGL/iOS/Android/Xbox.
- **Dependencies:** MENU-EPIC-01 through MENU-119.
- **Acceptance Criteria:** Written TA approval with risk register and mitigation actions.

---

## Technical Architect Coordination Notes (Detailed Feasibility Sign-off)

### 0) Foundation (applies to all menu tickets)
- **Feasibility:** High, with medium risk concentrated in cross-platform input + rebinding + Xbox compliance.
- **Architecture confirmed:**
  - `MenuFlowController` state machine for Main/Pause/Settings/LevelSelect/Profile.
  - `MenuContext` services: Input, Save/Profile, Audio, Graphics, PlatformCapabilities.
  - Modular UI panels via prefabs/views; async panel loading for heavier screens.
- **Cross-platform constraints:**
  - WebGL: reserved browser keys, tab focus loss, storage constraints.
  - iOS/Android: safe area, thermal/memory limits, touch-first ergonomics.
  - Xbox: controller-first navigation + TRC/TCR certification requirements.
- **Global acceptance criteria:**
  - No dead-end navigation paths.
  - Full coverage for controller/KBM/touch where relevant.
  - Settings persist across relaunch.

### 1) Main Menu
- **Constraints:** No true quit in WebGL; platform-specific sign-in behavior.
- **Mitigation:** Data-driven feature flags + partial-ready startup states while services initialize.
- **Acceptance criteria:** Interactive within startup KPI, offline fallback present, first controller focus valid.

### 2) Pause Menu
- **Constraints:** Different semantics for offline vs online pause; browser focus changes in WebGL.
- **Mitigation:** `PauseService` with `HardPause` vs `SoftPause`; atomic gameplay/menu action map swapping.
- **Acceptance criteria:** Reliable resume with prior input map/focus restoration; no online simulation desync.

### 3) Settings (Key Bindings / Audio / Graphics)
- **Constraints:**
  - Rebind limits for reserved/system keys and browser-captured keys.
  - Graphics options differ significantly by platform capability.
- **Mitigation:**
  - Typed setting descriptors + capability filtering.
  - Input System rebinding API + conflict validation + per-scheme reset defaults.
  - AudioMixer group controls with live application.
- **Acceptance criteria:** Conflict-safe rebinding, live/persistent audio settings, supported-only graphics options.

### 4) Challenge Level Select
- **Constraints:** Large catalog scaling + memory pressure on mobile/web.
- **Mitigation:** Data-driven level metadata + virtualized/paged list + lazy thumbnail loading.
- **Acceptance criteria:** Responsive scrolling/selection and consistent lock/unlock state with persisted progress.

### 5) Player Profile
- **Constraints:** Identity model differences (guest/local/platform accounts), sync conflicts.
- **Mitigation:** `ProfileService` abstraction with versioned schema + migration + corruption fallback.
- **Acceptance criteria:** Safe profile load, persistent edits, clear sync conflict messaging.

### 6) Cross-Platform Input (KBM/Touch/Controller)
- **Constraints:** Xbox controller completeness, touch target reliability, runtime hot-swap.
- **Mitigation:** Control schemes + context action maps + deterministic focus graph + glyph provider.
- **Acceptance criteria:** Seamless hot-swap, no unreachable controller elements, touch target reliability at 48x48dp+.

### 7) Accessibility
- **Constraints:** Readability across TV/mobile/web and uneven screen-reader support.
- **Mitigation:** `AccessibilityManager` with global toggles (text scale, high contrast, colorblind-safe palette, reduced motion).
- **Acceptance criteria:** Global persistent accessibility settings + visible focus + non-color-only status indicators.

### 8) UI Animation Performance
- **Constraints:** Tight WebGL/mobile CPU/GPU budgets.
- **Mitigation:** Split canvases by update frequency, lightweight tween transitions, pooled repeated UI items.
- **Acceptance criteria:** No noticeable transition hitching, no recurring GC spikes during menu navigation.

### 9) Xbox / Platform Caveats (early warning list)
- Respect title-safe area, suspend/resume behavior, and storage quota/failure handling.
- Mark policy-sensitive/non-remappable buttons explicitly in rebinding flows.
- Keep system Back/Cancel semantics consistent across all menus.
- Use platform on-screen keyboard APIs for text entry.
- Run cert-style checklist passes before content lock.

---

## Risks & Mitigations
- **Risk:** Input conflicts during rebind across schemes.  
  **Mitigation:** Conflict detection + explicit resolve modal + reset defaults by scheme.
- **Risk:** WebGL/mobile performance dips from animated backgrounds.  
  **Mitigation:** LOD for menu VFX and static fallback.
- **Risk:** Accessibility treated as bolt-on.  
  **Mitigation:** Dedicated tickets (MENU-115/116) required before feature complete.
- **Risk:** Inconsistent navigation between pointer and controller.  
  **Mitigation:** Mandatory cross-input QA matrix (MENU-119).

---

## Definition of Done (Menu System)
- All screens in scope implemented and reachable.
- Full feature parity across KBM, touch, and controller (except explicitly platform-gated items).
- Accessibility options implemented and verified.
- Menu/profile/settings persistence verified across app restarts.
- Technical Architect sign-off completed.
- QA matrix pass with no open blocker defects.
