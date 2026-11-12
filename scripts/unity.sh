#!/bin/zsh
set -eu
TASK_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_EDITOR_PATH="${UNITY_EDITOR_PATH:-/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity}"
mkdir -p "$TASK_ROOT/logs"
case "${1:-help}" in
  test)
    exec "$UNITY_EDITOR_PATH" -batchmode -nographics -projectPath "$TASK_ROOT/AntchemyUnity" -executeMethod Antchemy.Editor.GameplayValidation.RunEditMode -logFile "$TASK_ROOT/logs/unity-tests.log"
    ;;
  setup|mac|android|ios|ios-test)
    case "$1" in
      setup) TASK_UNITY_METHOD=BuildMainScene ;;
      mac) TASK_UNITY_METHOD=BuildMac ;;
      android) TASK_UNITY_METHOD=BuildAndroid ;;
      ios) TASK_UNITY_METHOD=BuildIOS ;;
      ios-test) TASK_UNITY_METHOD=BuildIOSTestAdsSimulator ;;
    esac
    TASK_UNITY_PLATFORM=()
    if [[ "$1" == ios || "$1" == ios-test ]]; then TASK_UNITY_PLATFORM=(-buildTarget iOS); fi
    if [[ "$1" == android ]]; then TASK_UNITY_PLATFORM=(-buildTarget Android); fi
    exec "$UNITY_EDITOR_PATH" -batchmode -nographics -quit "${TASK_UNITY_PLATFORM[@]}" -projectPath "$TASK_ROOT/AntchemyUnity" -executeMethod "Antchemy.Editor.SceneSetup.$TASK_UNITY_METHOD" -logFile "$TASK_ROOT/logs/unity-$1.log"
    ;;
  *)
    echo 'Usage: scripts/unity.sh {setup|test|mac|android|ios|ios-test}'
    echo 'Override UNITY_EDITOR_PATH if Unity is installed elsewhere.'
    ;;
esac
