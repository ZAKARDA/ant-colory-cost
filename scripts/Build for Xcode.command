#!/bin/zsh
# Exports the RELEASE iOS Xcode project (live ads, device SDK) and opens it in Xcode.
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
mkdir -p logs
LOG="$ROOT/logs/xcode-export.log"
UNITY_EDITOR_PATH="${UNITY_EDITOR_PATH:-/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity}"
if [ ! -x "$UNITY_EDITOR_PATH" ]; then echo "FAILED: Unity 6000.6.7f1 not found."; exit 1; fi
if pgrep -x Unity >/dev/null; then
  echo ">>> Unity is open. Quit it (Unity > Quit Unity), then press Enter here."
  read -r _
  pgrep -x Unity >/dev/null && { echo "FAILED: Unity is still running."; exit 1; }
fi
echo ">>> Exporting Xcode project from Unity (a few minutes)..."
rm -rf "$ROOT/Builds/AntColonyColors-iOS"
"$UNITY_EDITOR_PATH" -batchmode -nographics -quit -buildTarget iOS -projectPath "$ROOT/AntchemyUnity" \
  -executeMethod Antchemy.Editor.SceneSetup.BuildIOS -logFile "$LOG"
if ! grep -q "COLONY_IOS_EXPORT_OK" "$LOG"; then echo "FAILED: Unity export. Last lines:"; tail -30 "$LOG"; exit 1; fi
cd "$ROOT/Builds/AntColonyColors-iOS"
if [ -f Podfile ]; then pod install --repo-update || { echo "FAILED: pod install"; exit 1; }; fi
echo ">>> Done. Opening Xcode..."
open Unity-iPhone.xcworkspace 2>/dev/null || open Unity-iPhone.xcodeproj
