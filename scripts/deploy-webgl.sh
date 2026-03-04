#!/usr/bin/env bash
# deploy-webgl.sh — deploys WebGL build to arcade.digitalsurfacelabs.com
# Requires: SSH access to the server (set SSH_HOST, SSH_USER, SSH_PATH below)
# Called after overnight-build.sh succeeds.
set -euo pipefail

BUILD_DIR="/Users/joe/dev/TankRoyale/Builds/WebGL"
LOG="/tmp/tankroyale-build.log"

# ⚠️  FILL THESE IN (or set as env vars)
SSH_HOST="${ARCADE_SSH_HOST:-arcade.digitalsurfacelabs.com}"
SSH_USER="${ARCADE_SSH_USER:-joe}"
SSH_PATH="${ARCADE_SSH_PATH:-/var/www/arcade/tank-royale}"

if [ ! -f /tmp/tankroyale-build-ok ]; then
  echo "[deploy] ❌ No build-ok signal found. Run overnight-build.sh first." | tee -a "$LOG"
  exit 1
fi

if [ ! -d "$BUILD_DIR" ]; then
  echo "[deploy] ❌ Build directory not found: $BUILD_DIR" | tee -a "$LOG"
  exit 1
fi

echo "[$(date)] 🚀 Deploying Tank Royale to ${SSH_USER}@${SSH_HOST}:${SSH_PATH}" | tee -a "$LOG"

# Create remote directory if needed
ssh "${SSH_USER}@${SSH_HOST}" "mkdir -p ${SSH_PATH}" 2>&1 | tee -a "$LOG"

# Sync build output
rsync -avz --delete \
  "$BUILD_DIR/" \
  "${SSH_USER}@${SSH_HOST}:${SSH_PATH}/" \
  2>&1 | tee -a "$LOG"

# Clear build-ok signal
rm -f /tmp/tankroyale-build-ok

echo "[$(date)] ✅ Deploy complete → https://arcade.digitalsurfacelabs.com/tank-royale/" | tee -a "$LOG"
