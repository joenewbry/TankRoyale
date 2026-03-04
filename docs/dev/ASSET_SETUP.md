# Asset Setup

This project imports two Unity asset packs from `.unitypackage` archives into the Unity project’s `Assets/` folder.

## One-time extraction

From repo root:

```bash
./scripts/extract_packages.sh
```

The script will:
- Extract both packages into `/tmp/extract/`
- Read each GUID folder’s `pathname`
- Copy `asset` to `Unity/TankRoyale/Assets/<pathname after Assets/>`
- Copy `asset.meta` to `<filename>.meta`
- Skip package entries that do not include an `asset` file (directory-only entries)

Packages imported:
- `assets/packs/toontankslowpoly.unitypackage`
- `assets/packs/assethunts_gamedev_starter_kit_tanks_v100.unitypackage`

## Prefabs to use in gameplay

Use these extracted prefabs as defaults:

- **Player tank:**
  - `Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Player_Tank _GO-07 v01.prefab`
  - Alternate option: `Assets/PolygonalStudios/ToonTanksLowpoly/Prefabs/Tank1.prefab`
- **Enemy tanks:**
  - `Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Enemy_Tank_01.prefab`
- **Projectile:**
  - `Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Weapon/Weapon_Tank_Shell_01.prefab`
- **Ground tiles:**
  - `Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/3D Tile/3D_Tile_Ground_01.prefab`
- **Crates / destructible blocks:**
  - `Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Prop/Prop_Crate_01.prefab`

## Full asset map

For the complete list of imported models/prefabs and recommended usage, see:

- [`ASSET_INVENTORY.md`](../../ASSET_INVENTORY.md)
