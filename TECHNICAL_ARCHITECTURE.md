# Tank Royale — Technical Architecture

## 1) Architecture Goals & Constraints

### Product Goals
- Fast-paced top-down tank battles on a **30x30 destructible arena grid**.
- Cross-platform release: **WebGL, iOS, Android, Xbox**.
- MVP gameplay loop:
  - 1 player + 3 AI opponents
  - Single weapon baseline
  - 3 power-ups (Ricochet, Armor Bubble, Block Breaker)
- Support **offline mode first**, then online multiplayer as a follow-up phase.

### Hard Constraints (Driving Technical Choices)
- **WebGL + mobile performance** is the primary constraint.
- Low-end devices must still run at playable framerates.
- Memory footprint must be controlled (especially browser/mobile).
- Multiplayer stack must work across browser + mobile + console (future phase).

---

## 2) Recommended Tech Stack

- **Unity LTS (2022.3+ or 6 LTS when stable for target SDKs)**
- **URP** (single forward renderer, mobile/WebGL-tuned)
- **New Input System**
- **Addressables** for content streaming and variant loading
- **Photon Fusion** (recommended over NGO for this project)

### Why Photon Fusion (Recommendation)
For Tank Royale, Photon Fusion is a better fit than Unity Netcode (NGO) because:
1. Cross-platform room/match flow is mature and fast to ship.
2. Browser compatibility via WebSockets is proven.
3. Built-in replication patterns (networked vars/RPC/snapshots) reduce custom backend work.
4. Better fit for future online rollout with minimal custom infra.

> If team prefers Unity-native stack long-term, NGO is viable, but shipping risk is higher for rapid cross-platform WebGL/mobile rollout.

---

## 3) Unity Project Structure

```text
Unity/TankRoyale/
├─ Assets/
│  ├─ _Project/
│  │  ├─ Art/
│  │  │  ├─ Environment/
│  │  │  ├─ Tanks/
│  │  │  ├─ VFX/
│  │  │  └─ UI/
│  │  ├─ Audio/
│  │  ├─ Config/
│  │  │  ├─ ScriptableObjects/
│  │  │  ├─ Tuning/
│  │  │  └─ BuildProfiles/
│  │  ├─ Core/
│  │  │  ├─ Bootstrap/
│  │  │  ├─ Services/
│  │  │  ├─ Save/
│  │  │  └─ Utilities/
│  │  ├─ Gameplay/
│  │  │  ├─ Tanks/
│  │  │  ├─ Weapons/
│  │  │  ├─ Projectiles/
│  │  │  ├─ PowerUps/
│  │  │  ├─ Terrain/
│  │  │  └─ AI/
│  │  ├─ Networking/
│  │  │  ├─ Fusion/
│  │  │  ├─ Replication/
│  │  │  └─ Prediction/
│  │  ├─ Input/
│  │  ├─ UI/
│  │  ├─ Scenes/
│  │  │  ├─ Boot.unity
│  │  │  ├─ MainMenu.unity
│  │  │  ├─ Arena.unity
│  │  │  └─ ChallengeMode.unity
│  │  └─ Tests/
│  │     ├─ EditMode/
│  │     └─ PlayMode/
│  ├─ ThirdParty/
│  │  ├─ CartoonTankPack/
│  │  └─ PhotonFusion/
│  └─ AddressableAssetsData/
├─ Packages/
├─ ProjectSettings/
└─ Builds/
```

### Assembly Definition Strategy
Use `.asmdef` per major domain:
- `TankRoyale.Core`
- `TankRoyale.Gameplay`
- `TankRoyale.Networking`
- `TankRoyale.UI`
- `TankRoyale.Input`
- `TankRoyale.Tests`

This keeps compile times low and enforces clean dependencies.

---

## 4) Cross-Platform Build Targets

## 4.1 Build Matrix

| Platform | Backend | Target FPS | Quality Tier | Primary Input |
|---|---|---:|---|---|
| WebGL | IL2CPP (WASM) | 30–60 (adaptive) | WebLow/WebMedium | Keyboard + mouse, gamepad |
| iOS | IL2CPP ARM64 | 30 default, 60 high-end | MobileLow/MobileMedium | Touch + optional controller |
| Android | IL2CPP ARM64 | 30 default, 60 high-end | MobileLow/MobileMedium | Touch + optional controller |
| Xbox | IL2CPP / platform SDK | 60 | ConsoleHigh | Gamepad |

## 4.2 Platform-Specific Notes

### WebGL (Highest Risk Target)
- Use **WebSocket networking only**.
- Avoid heavy runtime memory spikes (browser tab kills on OOM).
- Keep initial download small (Addressables + compressed textures/audio).
- Avoid features with poor WebGL support (thread-heavy systems, excessive post-processing).
- Prefer simple shaders and minimal overdraw.

### iOS/Android
- Default to **30 FPS** on low/mid devices; unlock 60 for capable hardware.
- Use aggressive texture compression and atlas strategy.
- Use low-poly collision meshes and pooled projectiles.
- Thermal management: lower quality dynamically when sustained frame time rises.

### Xbox
- Enable enhanced visuals (higher texture budget, particles, and shadows) while preserving gameplay parity.
- Maintain identical gameplay simulation to avoid cross-platform desync.

## 4.3 Build Profiles & Defines
- `USE_PHOTON_FUSION`
- `PLATFORM_WEBGL`, `PLATFORM_MOBILE`, `PLATFORM_XBOX`
- `OFFLINE_MODE` for AI-only challenge mode

---

## 5) Networking Model (Photon Fusion)

## 5.1 Authority Model
- **Server-authoritative simulation** (host or dedicated authority in Fusion terms).
- Clients submit input; authority validates movement/shots/power-up pickups.
- Prevents client-side cheating and keeps destructible terrain consistent.

## 5.2 Simulation & Tick
- Network tick: **20–30 Hz** (start at 20 Hz for WebGL/mobile safety).
- Rendering interpolation on clients.
- Keep physics deterministic-enough via fixed timestep and simplified collision logic.

## 5.3 Replicated Entities
- Tanks: position/rotation/HP/active effects
- Projectiles: spawn seed, transform, bounce count
- Terrain cells: state changes only (delta replication, not full grid each tick)
- Power-up spawners: spawn state, cooldown, pickup ownership

## 5.4 Bandwidth Strategy
- Delta-compress only changed values.
- Send terrain updates as compact `{cellIndex, newState}` events.
- Avoid per-frame RPC spam for VFX/SFX; derive cosmetic effects locally from authoritative events.

## 5.5 Offline/Online Shared Code
Use same gameplay services in both modes:
- `IMatchSimulation`
- `ITerrainService`
- `IPowerUpService`

Offline mode runs local simulation + AI; online mode swaps in Fusion replication adapters.

---

## 6) Asset Pipeline (Cartoon Tank Pack)

## 6.1 Ingestion
1. Import raw pack into `Assets/ThirdParty/CartoonTankPack` (read-only).
2. Create project-owned prefabs/material variants under `Assets/_Project/Art/...`.
3. Never modify third-party source files directly.

## 6.2 Optimization Pass
- Reduce oversized textures; generate platform-specific import settings.
- Build atlases for UI and repeating environment props.
- Ensure meshes are marked Read/Write disabled unless needed.
- Enable GPU instancing where materials permit.
- Strip unused animations/material variants.

## 6.3 Addressables Layout
- `label:core` (always loaded: tanks, UI, base arena)
- `label:arena_theme_x` (optional map skins)
- `label:vfx_high` (console/high-end only)

This keeps WebGL/mobile first-load small and enables quality-tier content.

---

## 7) Input Handling Per Platform

Use **Unity Input System** with action maps:
- `Gameplay`: Move, Aim, Fire, UsePower
- `UI`: Navigate, Submit, Back, Pause

## 7.1 Desktop/WebGL
- Keyboard: WASD move
- Mouse: aim turret
- Mouse/Space: fire
- Gamepad support enabled

## 7.2 Mobile (iOS/Android)
- Left virtual joystick: movement
- Right side drag zone or stick: aiming
- Fire button + power-up button
- Optional auto-fire assist when aim is stable

## 7.3 Xbox
- Left stick move, right stick aim
- RT fire, LB/RB power-up
- Full menu/controller navigation

## 7.4 Input Abstraction
Gameplay reads from `IPlayerInputSource`, not directly from device APIs. This allows:
- same controller logic for AI/human/network ghost players
- easy test automation in PlayMode tests

---

## 8) Performance Strategy (WebGL + Mobile First)

## 8.1 Budgets
- **WebGL memory target:** <= 220 MB runtime
- **Mobile memory target:** <= 300 MB low-end, <= 450 MB mid/high
- **CPU frame budget (30 FPS):** 33.3 ms total
- **CPU frame budget (60 FPS):** 16.6 ms total
- Keep draw calls and overdraw tightly controlled

## 8.2 Rendering
- URP with minimal renderer features
- Baked lighting where possible; limit real-time lights
- Disable soft shadows on low tiers
- Avoid full-screen post-processing on WebGL/mobile low tiers
- Use LOD groups for complex props (if any)

## 8.3 Gameplay Runtime
- Object pool for projectiles, explosions, floating UI text
- No per-frame allocations in hot paths (`Update`, `FixedUpdate`)
- Burst/Jobs only where proven beneficial and platform-safe
- Prefer simple colliders and fast hit checks (ray/sphere casts)

## 8.4 Terrain System Optimization
- Store 30x30 grid in compact arrays (`byte`/`ushort` states)
- Batch mesh updates by chunk (e.g., 10x10) instead of rebuilding whole arena
- Update nav/AI blockers only when cells change
- Network only changed cells

## 8.5 Adaptive Quality Controller
Runtime scaler monitors frame time and adjusts:
- shadow quality
- particle count
- render scale
- target frame rate (60 -> 30 fallback)

Especially important for thermal throttling on mobile.

---

## 9) Power-Ups Implementation (Code Architecture)

## 9.1 Data-Driven Definitions
Use ScriptableObjects:

- `PowerUpDefinition`
  - `id`
  - `durationSeconds`
  - `icon`
  - `stackPolicy`
  - `effectType`

## 9.2 Runtime Contracts

```csharp
public interface IPowerUpEffect
{
    void OnApply(TankContext tank);
    void OnRemove(TankContext tank);
    void OnBeforeShot(ref ProjectileSpec spec);
    void OnDamageTaken(ref DamageContext damage);
}
```

`TankPowerUpController` manages active effects with authoritative timers.

## 9.3 Specific Effects
- **Ricochet Bullets**: sets `ProjectileSpec.MaxBounces = N`.
- **Armor Bubble**: grants `shieldCharges = 1`; first valid hit consumes charge.
- **Block Breaker**: projectile gets `canBreakDestructible = true`; applies terrain damage on collision.

## 9.4 Spawn/Pickup System
- `PowerUpSpawner` with weighted random table and cooldown windows.
- Spawn locations validated against blocked cells and tank proximity.
- On pickup, authority assigns effect and replicates event.

---

## 10) Destructible Terrain Implementation (Code Architecture)

## 10.1 Grid Data Model

```csharp
public enum CellType : byte { Empty, Solid, Destructible }

public struct TerrainCell
{
    public CellType Type;
    public byte HitPoints; // 0..N
}
```

- Arena stored as `TerrainCell[30,30]`.
- Optional flattened index for network packets: `index = x + y * width`.

## 10.2 Damage Flow
1. Authoritative projectile impact resolves collision.
2. If target cell is destructible, decrement HP.
3. If HP <= 0: set cell to Empty and emit `CellDestroyed(index)`.
4. Clients receive event and update visuals/collision for that cell/chunk.

## 10.3 Visual/Collision Update
- Keep static base floor mesh.
- Destructible blocks rendered as pooled instances keyed by cell index.
- On destruction: disable instance + collider (no full-scene rebuild).

## 10.4 AI Integration
- AI pathing grid references same terrain state.
- Recompute only affected local regions after cell destruction.

---

## 11) Testing & Validation

- **Unit tests**: power-up timers, projectile modifiers, terrain damage rules.
- **PlayMode tests**: offline match loop, AI + terrain interactions.
- **Network tests**: late join, packet loss simulation, reconnect behavior.
- **Performance gates**:
  - WebGL: test on low-spec laptop/browser
  - Android: test on low-end device class
  - iOS: older supported iPhone model

Exit criteria for MVP:
- Stable 30 FPS on low-end mobile/web target scene.
- No major memory spikes during 10-minute match session.
- Terrain + power-up state remains consistent between authority and clients.

---

## 12) Delivery Phasing

### Phase A (MVP Offline First)
- Core tank movement/combat
- AI opponents
- Terrain destruction
- 3 power-ups
- WebGL + mobile builds

### Phase B (Online Multiplayer)
- Photon room flow + matchmaking
- Authoritative replication
- Reconnect/host migration strategy
- Cross-platform QA hardening

---

## Final Architectural Notes
- Build for **WebGL and low-end mobile first**, then scale visual fidelity upward for Xbox/high-end.
- Keep gameplay systems deterministic and data-driven so offline and online share the same core logic.
- Treat destructible terrain and power-up replication as critical-path systems for both performance and net consistency.
