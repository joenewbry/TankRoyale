# Demo Scene 02 — Layout Notes

## Coordinate System
- Scene uses real 3D coordinates, NOT a 0-30 grid origin
- Objects range roughly from X: -21 to +3, Z: -28 to 0
- Estimated scene center: (-10, 0, -14)
- Scene has existing skybox, ambient lighting, and lightmap settings

## Camera Setup for Top-Down View
- Suggested position: (-10, 25, -14)
- Rotation: (90, 0, 0) — looking straight down
- Orthographic size: 20 (slightly larger to cover full scene)

## A* Grid Setup
- Origin: (-22, 0, -30) — cover full scene with margin
- Grid size: 30x30 cells, cellSize = 1.5f (to match scene scale)
- Uses Physics.CheckBox per cell (no manual block map needed)
- ObstacleLayer: everything except player/enemy/collectibles

## Tank Spawn Points (estimated safe areas)
- Player: (-20, 0.5, -26)
- Enemy 1: (-2, 0.5, -2)
- Enemy 2: (-2, 0.5, -26)
- Enemy 3: (-20, 0.5, -2)

## Notes
- Scene has prefab references to AssetHunts! pack assets (via GUIDs)
- Demo scene has ambient lighting set up — don't override it
- Skybox is Unity's default — keep it
- Tags Player/Enemy/Block need to be added (not in demo scene by default)
