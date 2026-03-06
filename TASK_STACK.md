# Task Stack

Live queue I will run through continuously. Add new tasks any time with `push: <task>`.

## Queue Status
- Queue total: **8**
- In progress: **2**
- Queued: **6**
- Done (recent): **14**

## In Progress
- [ ] Tank driving feel + camera/combat debug pass
- [ ] Per-iteration PlayTest scene snapshots with each commit

## Queued
- [ ] Verify tread animation visibility on all player/enemy tank prefabs
- [ ] Re-validate shot trajectory against camera angle in all 3 camera modes
- [ ] Re-validate ramp climb momentum + sliding/friction behavior after latest control changes
- [ ] Polish camera mode UX (Tab indicator + current mode label)
- [ ] Add tank-themed HUD polish for in-cockpit feel
- [ ] Expand debug menu options (rendering/shader/ray/arc toggles)

## Done (Recent)
- [x] Turret aim decoupled from hull/body rotation + `PlayTest11`
- [x] Added reset hotkey (`R`) to reload world + `PlayTest10`
- [x] Added 5 camera modes (IN_TANK, STARE_DOWN_MUZZLE, TOP_OF_TANK, OVERHEAD_VIEW, WORLD_EXPLORER), arrow-key aliases, live trajectory line, and tread-animation-on-throttle
- [x] Removed idle drift/creep with stronger braking and no passive slope slide + `PlayTest9`
- [x] Added slope body-tilt physics approximation + projectile edge-hit sweep checks + `PlayTest8`
- [x] Switched treads to animator-driven params (no manual forward/back tread mesh spin) + `PlayTest7`
- [x] Tank controls switched to throttle+turn (W/S move, A/D rotate) + trajectory flip fix + `PlayTest6`
- [x] 3-view Tab cycle finalized (TopDown/Shoulder/Cockpit) + cockpit target overlay + `PlayTest5`
- [x] Paintball-style projectile splatter on contact + `PlayTest4` snapshot
- [x] Added `PlayTest3` snapshot for tread-direction iteration
- [x] Mouse look drives camera instead of directly twisting turret mesh
- [x] Added runtime fill light for side-face readability
- [x] Added on-screen debug menu with hitbox/ray/arc/wireframe toggles
- [x] Switched movement input to 8-way digital pattern (WASD + diagonals)
