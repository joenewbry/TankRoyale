# Task Stack

Use this as the live task stack. I will keep it updated and commit incrementally.

## Active
- [in_progress] Tank driving feel + camera/combat debug pass
- [in_progress] Per-iteration PlayTest scene snapshots with each commit

## Queued
- [queued] Verify tread rotation is visible on all player/enemy tank prefabs
- [queued] Tie shot trajectory to camera angle and validate across all 3 camera modes
- [queued] Ensure ramps are climbable with momentum + sliding/friction behavior
- [queued] Add/finish 3-view camera cycle (top-down, shoulder, front)
- [queued] Add tank-themed HUD polish for in-cockpit feel
- [queued] Expand debug menu options (hitboxes, rendering modes, shader/ray/arc toggles)

## Done (recent)
- [done] Switched treads to animator-driven params (no manual forward/back tread mesh spin) + `PlayTest7`
- [done] Tank controls switched to throttle+turn (W/S move, A/D rotate) + trajectory flip fix + `PlayTest6`
- [done] 3-view Tab cycle finalized (TopDown/Shoulder/Cockpit) + cockpit target overlay + `PlayTest5`
- [done] Paintball-style projectile splatter on contact + `PlayTest4` snapshot
- [done] Added `PlayTest3` snapshot for current tread-direction iteration
- [done] Mouse look drives camera instead of directly twisting turret mesh
- [done] Added runtime fill light for side-face readability
- [done] Added on-screen debug menu with hitbox/ray/arc/wireframe toggles
- [done] Switched movement input to 8-way digital pattern (WASD + diagonals)
