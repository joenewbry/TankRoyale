#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
EXTRACT_ROOT="/tmp/extract"
ASSETS_ROOT="$REPO_ROOT/Unity/TankRoyale/Assets"

PACKAGES=(
  "$REPO_ROOT/assets/packs/toontankslowpoly.unitypackage"
  "$REPO_ROOT/assets/packs/assethunts_gamedev_starter_kit_tanks_v100.unitypackage"
)

mkdir -p "$EXTRACT_ROOT" "$ASSETS_ROOT"

total_imported=0

echo "Extracting Unity packages into: $ASSETS_ROOT"

for package in "${PACKAGES[@]}"; do
  if [[ ! -f "$package" ]]; then
    echo "ERROR: Package not found: $package" >&2
    exit 1
  fi

  package_name="$(basename "$package" .unitypackage)"
  package_extract_dir="$EXTRACT_ROOT/$package_name"

  rm -rf "$package_extract_dir"
  mkdir -p "$package_extract_dir"

  echo "- Unpacking $package_name"
  tar -xzf "$package" -C "$package_extract_dir"

  imported_for_package=0

  while IFS= read -r -d '' guid_dir; do
    pathname_file="$guid_dir/pathname"
    asset_file="$guid_dir/asset"
    asset_meta_file="$guid_dir/asset.meta"

    [[ -f "$pathname_file" ]] || continue

    # Directory entries in .unitypackage do not contain an asset payload.
    [[ -f "$asset_file" ]] || continue

    unity_path="$(<"$pathname_file")"
    unity_path="${unity_path%$'\r'}"

    if [[ "$unity_path" != Assets/* ]]; then
      echo "  ! Skipping unexpected path: $unity_path" >&2
      continue
    fi

    relative_path="${unity_path#Assets/}"
    destination="$ASSETS_ROOT/$relative_path"
    destination_dir="$(dirname "$destination")"

    mkdir -p "$destination_dir"
    cp "$asset_file" "$destination"

    if [[ -f "$asset_meta_file" ]]; then
      cp "$asset_meta_file" "$destination.meta"
    fi

    imported_for_package=$((imported_for_package + 1))
  done < <(find "$package_extract_dir" -mindepth 1 -maxdepth 1 -type d -print0)

  total_imported=$((total_imported + imported_for_package))
  echo "  Imported $imported_for_package assets from $package_name"
done

echo "Done. Imported $total_imported assets total."
