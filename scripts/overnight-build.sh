#!/usr/bin/env bash
# overnight-build.sh — pulls latest, runs full Unity setup+build in one invocation
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity"
PROJECT="/Users/joe/dev/TankRoyale/Unity/TankRoyale"
BUILD_DIR="/Users/joe/dev/TankRoyale/Builds/WebGL"
LOG="/tmp/tankroyale-build.log"
REPO="/Users/joe/dev/TankRoyale"

echo "======================================" >> "$LOG"
echo "[$(date)] 🏗  Tank Royale overnight build started" | tee -a "$LOG"

# Kill any existing Unity instance for this project to avoid conflicts
pkill -f "Unity.*TankRoyale" 2>/dev/null && echo "[$(date)] Killed existing Unity" | tee -a "$LOG" || true
sleep 2

# Pull latest
echo "[$(date)] Pulling latest from GitHub..." | tee -a "$LOG"
cd "$REPO" && git pull origin main 2>&1 | tee -a "$LOG"

# One Unity invocation: setup scene + configure WebGL + build
echo "[$(date)] Running Setup → Configure → Build WebGL (single Unity launch)..." | tee -a "$LOG"
"$UNITY" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT" \
  -executeMethod "TankRoyale.Editor.BuildScript.SetupAndBuildWebGL" \
  -logFile "${LOG}.build" \
  2>&1
BUILD_EXIT=$?

if [ $BUILD_EXIT -eq 0 ] && [ -d "$BUILD_DIR" ]; then
  echo "[$(date)] ✅ Build succeeded!" | tee -a "$LOG"
  ls -lh "$BUILD_DIR" | tee -a "$LOG"
  touch /tmp/tankroyale-build-ok
  echo "BUILD_OK" > /tmp/tankroyale-build-status
else
  echo "[$(date)] ❌ Build FAILED (exit $BUILD_EXIT)" | tee -a "$LOG"
  tail -50 "${LOG}.build" | tee -a "$LOG"
  echo "BUILD_FAILED" > /tmp/tankroyale-build-status
  exit 1
fi
