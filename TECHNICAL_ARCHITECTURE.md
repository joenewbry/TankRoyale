# Tank Royale — Technical Architecture (Unity 3D v2)

## 1) Core Technical Direction

Tank Royale is a **Unity 3D** top-down tank game using real 3D prefabs from:
- `toontankslowpoly.unitypackage`
- `assethunts_gamedev_starter_kit_tanks_v100.unitypackage`

This document replaces prior 2D placeholder architecture. The project is now explicitly:
- **3D world + 3D prefabs**
- **Top-down orthographic camera**
- **Grid-based gameplay on a 30x30 arena**
- **WebGL + iOS + Android targets**

---

## 2) Engine, Render Pipeline, and Platform Targets

## 2.1 Engine
- Unity LTS (2022.3+ recommended)
- Scripting backend: IL2CPP for shipping builds

## 2.2 Render Pipeline
- **URP (Universal Render Pipeline)**
- Forward renderer
- Mobile-friendly shader variants only
- Prefer single-directional light + baked/cheap lighting setup

## 2.3 Platforms
- **WebGL** (primary browser target)
- **iOS**
- **Android**

## 2.4 Performance Goal
- **60 FPS target in WebGL gameplay scene** on reasonable desktop hardware
- Graceful quality scaling on mobile while preserving gameplay parity

---

## 3) Scene and World Architecture

## 3.1 Camera Model (Top-Down Orthographic)
- Main gameplay camera:
  - Projection: **Orthographic**
  - Orientation: top-down, pitched to view arena cleanly
  - Follows player tank with smoothing
- Camera script keeps gameplay centered while clamping to arena bounds

Recommended transform baseline (tunable):
- Position: `(15, 28, 15)` for 30x30 map center
- Rotation: `(90, 0, 0)` pure top-down OR slight tilt if desired
- Orthographic size tuned to show tactical area around player

## 3.2 Arena Grid
- Arena size: **30 x 30 cells**
- Floor generated from:
  - `Assets/AssetHunts!/GameDev Starter Kit - Tanks/.../3D_Tile_Ground_01.prefab`
- One tile prefab instance per cell (or chunked instancing strategy)
- Grid origin at world `(0,0,0)` and deterministic index mapping:
  - `index = x + y * width`

Data model:
```csharp
public struct GridCoord { public int x, y; }
public const int GridWidth = 30;
public const int GridHeight = 30;
```

---

## 4) Tank Entity Architecture

## 4.1 Required Hierarchy
Each tank prefab is normalized to:

```text
TankRoot
├── TankBody   (mesh/collider, movement reference)
└── Turret     (child transform, rotates independently)
```

`TankRoot` owns gameplay components:
- `TankMotor` (movement)
- `TankHealth`
- `TankWeaponController`
- `TankInputAdapter` (player or AI)

## 4.2 Movement
- Movement is body-driven on XZ plane
- Rotation for hull is independent from turret aim
- Rigidbody-based or kinematic controller allowed; must avoid jitter in WebGL

## 4.3 Turret Aiming (Independent)
- Turret rotates independently of hull
- Mouse world-point projection via raycast onto ground plane
- Apply **Y-axis rotation only** for top-down behavior

Pseudo-flow:
1. Screen mouse position -> camera ray
2. Intersect with XZ ground plane
3. Compute direction from turret pivot to hit point
4. Rotate turret around Y only toward direction

---

## 5) Combat and Projectile System

## 5.1 Projectile Base Prefab
Default shot uses:
- `Weapon_Tank_Shell_01.prefab`

Runtime wrapper component:
- `ShellProjectile`
  - speed
  - lifetime
  - damage
  - owner team/id
  - collision mask

## 5.2 Projectile Lifecycle
- Spawn from muzzle transform on turret
- Move forward in world space (XZ plane)
- Hit resolution against tanks, crates, and world blockers
- Return object to pool on hit/timeout

## 5.3 Pooling (Mandatory)
- No runtime Instantiate/Destroy in combat loop
- `ProjectilePool` prewarms shell instances at match start
- Pool size configurable by max fire rate × alive time × active tanks

---

## 6) Destructible Environment System

## 6.1 Destructible Block Prefabs
Use these as destructibles:
- `Prop_Crate_01.prefab` ... `Prop_Crate_07.prefab`

## 6.2 Health Model
Each destructible crate has:
- `maxHitPoints`
- `currentHitPoints`
- optional damage state visuals (intact/damaged/destroyed)

On projectile impact:
1. Apply damage
2. If HP <= 0: mark cell walkable, disable/destroy crate visual via pool/state system
3. Emit destruction SFX event

## 6.3 Grid Integration
- Crates occupy grid cells
- Destroyed crates update occupancy in:
  - collision lookup
  - AI pathfinding walkability map

---

## 7) AI Navigation and Pathfinding

## 7.1 Pathfinding Choice
- **Custom grid-based A\***
- **Do not use Unity NavMesh** (too heavy/unnecessary for this WebGL+mobile topology)

## 7.2 Grid Graph
- 30x30 nodes mapped directly to arena cells
- Node state:
  - walkable / blocked
  - movement cost
  - optional dynamic danger score

## 7.3 A* Implementation Notes
- Heuristic: Manhattan or octile (depending on movement model)
- Binary heap priority queue for open set
- Replan on interval or when path invalidated by destroyed/added obstacles
- Keep allocations out of hot path (reuse buffers/lists)

## 7.4 AI Tank Loop
- Sense player + obstacles
- Request path to tactical target cell
- Follow waypoints with steering
- Aim turret independently and fire when line-of-sight is valid

---

## 8) Input and Control Architecture

## 8.1 Player Input
- Unity Input System action map:
  - Move (WASD / stick)
  - Aim (mouse position / right stick)
  - Fire

## 8.2 Separation of Concerns
- `ITankInputSource` abstraction for:
  - human player
  - AI controller
- Movement and firing systems consume generic intent, not raw devices

---

## 9) Audio Architecture

## 9.1 Source of Audio Assets
Asset packs contain no audio; all SFX/music sourced from:
- **OpenGameArt (CC0)**

Required minimum SFX set:
- engine loop
- shell fire
- shell impact/explosion
- crate damage/destruction
- pickup and UI feedback

## 9.2 Runtime Audio System
Use a central **AudioManager singleton**:
- global mixer routing
- category volume (SFX, music, UI)
- one-shot and looping API

Use **pooled AudioSources** for one-shots:
- `AudioSourcePool` prewarms channels
- Reuse sources to avoid Instantiate/Destroy spikes
- 3D spatial audio for world SFX, 2D for UI

---

## 10) Project Structure (Recommended)

```text
Unity/TankRoyale/Assets/
├─ _Project/
│  ├─ Scenes/
│  │  ├─ Boot.unity
│  │  └─ Arena.unity
│  ├─ Prefabs/
│  │  ├─ Tanks/
│  │  ├─ Projectiles/
│  │  ├─ Environment/
│  │  └─ Audio/
│  ├─ Scripts/
│  │  ├─ Core/
│  │  ├─ Grid/
│  │  ├─ Tanks/
│  │  ├─ AI/
│  │  ├─ Combat/
│  │  ├─ Destructibles/
│  │  └─ Audio/
│  ├─ ScriptableObjects/
│  └─ Materials/
├─ ThirdParty/
│  ├─ ToonTanksLowpoly/
│  └─ GameDevStarterKitTanks/
└─ Settings/
```

Rule: third-party assets remain read-only; create project-owned prefab variants under `_Project/`.

---

## 11) Performance Rules (Non-Negotiable)

1. **Object pooling for bullets/projectiles** (and recommended for impact VFX/audio emitters)
2. **No GC allocations in `Update` / `FixedUpdate` / tight AI loops**
3. Cache component references at initialization
4. Avoid LINQ and per-frame string operations in gameplay code
5. Use preallocated collections for A* and combat queries
6. Keep physics layers/masks tight to reduce broadphase overhead
7. Mobile shader variants only; avoid expensive post-processing

Validation targets:
- Stable frame-time under combat load
- No recurring GC spikes during continuous firing
- WebGL build sustains near 60 FPS in representative arena scenario

---

## 12) Implementation Sequence

1. Set project to URP + platform quality profiles (WebGL/iOS/Android)
2. Build 30x30 floor generator using `3D_Tile_Ground_01.prefab`
3. Normalize tank prefab hierarchy (`TankRoot > TankBody + Turret`)
4. Implement mouse-to-world turret aim (Y-axis only)
5. Integrate `Weapon_Tank_Shell_01.prefab` with pooled projectile system
6. Add crate destructible system using `Prop_Crate_01-07.prefab` + HP
7. Implement grid occupancy + custom A* for AI tanks
8. Add AudioManager singleton + pooled AudioSources + CC0 SFX import
9. Profile WebGL and mobile builds; eliminate allocations in hot loops

---

## Final Architecture Decision Summary

- Tank Royale is a **Unity 3D** game, not 2D.
- Arena is a **30x30 3D tile grid** built from `3D_Tile_Ground_01.prefab`.
- Tanks use split body/turret hierarchy with **independent turret aiming**.
- AI navigation is **custom grid A\*** (no NavMesh).
- Destructibles are crate prefabs with HP and grid-state updates.
- Projectile baseline is `Weapon_Tank_Shell_01.prefab` with pooling.
- Audio uses OpenGameArt CC0 assets via AudioManager + AudioSource pools.
- Shipping targets: **WebGL, iOS, Android on URP**, optimized for mobile/WebGL performance and 60 FPS WebGL goal.
