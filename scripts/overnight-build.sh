#!/usr/bin/env bash
# overnight-build.sh — runs nightly to build Tank Royale WebGL
# Called by cron jobs. Logs to /tmp/tankroyale-build.log
set -euo pipefail

UNITY="/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity"
PROJECT="/Users/joe/dev/TankRoyale/Unity/TankRoyale"
BUILD_DIR="/Users/joe/dev/TankRoyale/Builds/WebGL"
LOG="/tmp/tankroyale-build.log"
REPO="/Users/joe/dev/TankRoyale"

echo "======================================" >> "$LOG"
echo "[$(date)] 🏗  Tank Royale overnight build started" | tee -a "$LOG"

# Pull latest
echo "[$(date)] Pulling latest from GitHub..." | tee -a "$LOG"
cd "$REPO" && git pull origin main 2>&1 | tee -a "$LOG"

# Configure WebGL settings via batch mode
echo "[$(date)] Configuring WebGL settings..." | tee -a "$LOG"
"$UNITY" \
  -batchmode -quit \
  -projectPath "$PROJECT" \
  -executeMethod "TankRoyale.Editor.BuildScript.ConfigureWebGLSettings" \
  -logFile "${LOG}.configure" \
  2>&1 && echo "[$(date)] ✅ Config done" | tee -a "$LOG" \
        || echo "[$(date)] ⚠️  Config step returned non-zero (may be OK)" | tee -a "$LOG"

# Build WebGL
echo "[$(date)] Building WebGL (this takes 5-15 min)..." | tee -a "$LOG"
"$UNITY" \
  -batchmode -quit \
  -projectPath "$PROJECT" \
  -executeMethod "TankRoyale.Editor.BuildScript.BuildWebGL" \
  -logFile "${LOG}.build" \
  2>&1
BUILD_EXIT=$?

if [ $BUILD_EXIT -eq 0 ] && [ -d "$BUILD_DIR" ]; then
  echo "[$(date)] ✅ Build succeeded: $BUILD_DIR" | tee -a "$LOG"
  ls -lh "$BUILD_DIR" | tee -a "$LOG"
  # Signal deploy step
  touch /tmp/tankroyale-build-ok
else
  echo "[$(date)] ❌ Build FAILED (exit $BUILD_EXIT). See ${LOG}.build" | tee -a "$LOG"
  # Print last 30 lines of build log for quick diagnosis
  tail -30 "${LOG}.build" | tee -a "$LOG"
  exit 1
fi
