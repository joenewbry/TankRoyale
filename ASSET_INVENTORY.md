# Tank Royale - Asset Inventory

## Cartoon Tank Pack (toontankslowpoly.unitypackage)
**Path in Unity:** `Assets/PolygonalStudios/ToonTanksLowpoly/`

- **13 Tank FBX Models:** Tank1.fbx – Tank13.fbx (3D low-poly, top-down suitable)
- **13 Prefabs:** Tank1.prefab – Tank13.prefab
- **Texture:** TankColors.png (shared palette)
- **Materials:** TankColors.mat, Base.mat
- **Demo Scene:** ToonTanksLowpoly.unity
- **Utility Script:** AutoSwitchMaterialShader.cs

**Assignment:**
- Player tank: Tank1 (player color tint)
- Enemy AI tanks: Tank2, Tank3, Tank4

---

## Game Dev Starter Kit (assethunts_gamedev_starter_kit_tanks_v100.unitypackage)
**Path in Unity:** `Assets/AssetHunts!/GameDev Starter Kit - Tanks/`

### Tanks
- `Player_Tank _GO-07 v01.prefab` / `v02.prefab` — player tank with body/turret hierarchy
- `Enemy_Tank_01/02/03.prefab` — 3 enemy variants
- `Enemy_Boss_Tank_01.prefab` — boss tank

### Weapons / Projectiles
- `Weapon_Tank_Shell_01.prefab` — base projectile ✅ use for default shot
- `Weapon_Missile_01.prefab`, `Weapon_Missile_02.prefab`
- `Weapon_Land_Mine_01.prefab`

### Obstacles (use for arena decorations + destructibles)
- `Obstacle_Wooden_Spike_01.prefab`
- `Obstacle_Czech_Hedgehog_01.prefab`
- `Obstacle_Dragons_Teeth_01.prefab`
- `Obstacle_Barricade_01.prefab`
- `Obstacle_Pusher_01.prefab`
- `Obstacle_Flamethrower_01.prefab`

### Props (use for crate/pickup stands)
- `Prop_Crate_01.prefab` – `Prop_Crate_07.prefab` ✅ use as destructible blocks
- `Prop_Sandbag_01.prefab`, `Prop_Ammo_Box_01.prefab`
- Barrels, fences, signs, tents...

### 3D Tiles (use for arena floor)
- Ground tiles: `3D_Tile_Ground_01-04.prefab` ✅ use for 30x30 grid floor
- Ground slopes, socket tiles, dungeon tiles

### Collectibles (use for powerup pickups)
- `Collectible_Lightning_01/02/03.prefab` → Ricochet powerup
- `Collectible_Heart_01.prefab` → Armor bubble powerup
- `Collectible_Bomb_01/02.prefab` → Block breaker powerup
- `Collectible_Star_01/02.prefab` → Bonus pickups

### Demo Scenes
- `Demo Scene 01.unity`, `Demo Scene 02.unity`, `Demo Scene 03.unity`
- `All Assets.unity` — full layout reference

### Animations
- `Props_Tank_Spawn_01_Spawn.anim`
- `Props_Badge_Victory_01_Victory.anim`
- `Props_Badge_Defeat_01_Defeat.anim`

---

## Audio
⚠️ Neither asset pack contains audio files. 
Sound must be sourced separately — recommend OpenGameArt.org (CC0 license):
- Tank engine loop
- Tank fire (shell shot)
- Shell impact / explosion
- Block destruction (crack)
- Powerup pickup (chime)
- Armor bubble activate
- Menu click / hover

---

## Engine Confirmed
**Unity 3D** — this is a 3D top-down game, not 2D sprite-based.
Previous placeholder work used 2D; all visuals must be rebuilt using these 3D prefabs.
