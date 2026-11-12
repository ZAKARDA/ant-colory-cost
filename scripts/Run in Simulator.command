#!/bin/zsh
# Builds Ant Colony Colors for the iOS Simulator (Google TEST ads), installs it and launches it.
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
mkdir -p logs
LOG="$ROOT/logs/simulator-run.log"
exec > >(tee "$LOG") 2>&1
echo "=== Ant Colony Colors -> iOS Simulator  $(date) ==="
BUNDLE_ID="com.antcolonycolors.game"
CONFIG="$ROOT/AntchemyUnity/Assets/Resources/AdMobConfiguration.json"
UNITY_EDITOR_PATH="${UNITY_EDITOR_PATH:-/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity}"

if [ ! -x "$UNITY_EDITOR_PATH" ]; then echo "FAILED: Unity 6000.6.7f1 not found at $UNITY_EDITOR_PATH"; exit 1; fi
if ! xcodebuild -version >/dev/null 2>&1; then echo "FAILED: Xcode is not installed or not selected (xcode-select)."; exit 1; fi

if pgrep -x Unity >/dev/null; then
  echo ">>> Unity is open. Quit it (Unity > Quit Unity), then press Enter here."
  read -r _
  if pgrep -x Unity >/dev/null; then echo "FAILED: Unity is still running."; exit 1; fi
fi

# The simulator build must use Google test ads. Turn them on temporarily and always restore the file.
cp "$CONFIG" "$CONFIG.bak"
restore() { [ -f "$CONFIG.bak" ] && mv -f "$CONFIG.bak" "$CONFIG"; }
trap restore EXIT
sed -i '' 's/"useTestAds": false/"useTestAds": true/' "$CONFIG"

echo ">>> 1/4 Exporting Xcode project from Unity (a few minutes)..."
rm -rf "$ROOT/Builds/AntColonyColors-iOS"
mkdir -p "$ROOT/Builds"
"$UNITY_EDITOR_PATH" -batchmode -nographics -quit -buildTarget iOS -projectPath "$ROOT/AntchemyUnity" \
  -executeMethod Antchemy.Editor.SceneSetup.BuildIOSTestAdsSimulator -logFile "$ROOT/logs/unity-ios-sim.log"
if ! grep -q "COLONY_IOS_EXPORT_OK" "$ROOT/logs/unity-ios-sim.log"; then
  echo "FAILED: Unity export. Last lines of logs/unity-ios-sim.log:"; tail -30 "$ROOT/logs/unity-ios-sim.log"; exit 1
fi
restore; trap - EXIT

EXPORT="$ROOT/Builds/AntColonyColors-iOS"
cd "$EXPORT"
if [ -f Podfile ] && [ ! -d Unity-iPhone.xcworkspace ]; then
  echo ">>> Installing CocoaPods..."
  pod install || { echo "FAILED: pod install (install CocoaPods: brew install cocoapods)"; exit 1; }
fi

echo ">>> 2/4 Picking a simulator..."
DEVICE_ID=$(xcrun simctl list devices booted | grep -E "iPhone" | head -1 | grep -oE '[0-9A-F]{8}-([0-9A-F]{4}-){3}[0-9A-F]{12}')
if [ -z "$DEVICE_ID" ]; then
  DEVICE_ID=$(xcrun simctl list devices available | grep -E "iPhone" | grep -vE "SE|iPad" | tail -1 | grep -oE '[0-9A-F]{8}-([0-9A-F]{4}-){3}[0-9A-F]{12}')
fi
if [ -z "$DEVICE_ID" ]; then echo "FAILED: no iPhone simulator found. Open Xcode > Settings > Components and install an iOS Simulator runtime."; exit 1; fi
echo "Using simulator $DEVICE_ID"
xcrun simctl boot "$DEVICE_ID" 2>/dev/null
open "$(xcode-select -p)/Applications/Simulator.app"

echo ">>> 3/4 Building for the simulator..."
if [ -d Unity-iPhone.xcworkspace ]; then TARGET=(-workspace Unity-iPhone.xcworkspace); else TARGET=(-project Unity-iPhone.xcodeproj); fi
xcodebuild "${TARGET[@]}" -scheme Unity-iPhone -configuration Debug -sdk iphonesimulator \
  -destination "platform=iOS Simulator,id=$DEVICE_ID" -derivedDataPath "$ROOT/Builds/DerivedData-iOS" CODE_SIGNING_ALLOWED=NO build > "$ROOT/logs/xcodebuild-sim.log" 2>&1
if ! grep -q "BUILD SUCCEEDED" "$ROOT/logs/xcodebuild-sim.log"; then
  echo "FAILED: xcodebuild. Errors:"; grep -E "error:|Undefined symbols|ld: " "$ROOT/logs/xcodebuild-sim.log" | head -20; echo "Full log: logs/xcodebuild-sim.log"; exit 1
fi
APP=$(find "$ROOT/Builds/DerivedData-iOS/Build/Products" -maxdepth 2 -name "*.app" | head -1)
if [ -z "$APP" ]; then echo "FAILED: no .app produced."; exit 1; fi

echo ">>> 4/4 Installing and launching..."
xcrun simctl uninstall "$DEVICE_ID" "$BUNDLE_ID" 2>/dev/null
if ! xcrun simctl install "$DEVICE_ID" "$APP"; then echo "RESULT: FAILED (install)"; exit 1; fi
if ! xcrun simctl launch "$DEVICE_ID" "$BUNDLE_ID"; then echo "RESULT: FAILED (launch)"; exit 1; fi
echo "RESULT: SUCCESS - Ant Colony Colors is running in the Simulator."
echo "Done. Tell Claude 'sala'. You can close this window."
