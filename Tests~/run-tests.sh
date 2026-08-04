#!/bin/bash
# Runs the rules-engine tests outside Unity using the editor's bundled Roslyn.
# Pure-logic scripts only (PlayingCard, CaptureChecker) - no scene needed.
# Usage: bash Tests~/run-tests.sh   (from the repo root or anywhere)
set -e

REPO="$(cd "$(dirname "$0")/.." && pwd)"
EDITOR_DIR="${UNITY_EDITOR:-$(ls -d /Applications/Unity/Hub/Editor/*/Unity.app/Contents 2>/dev/null | sort -V | tail -1)}"
[ -z "$EDITOR_DIR" ] && { echo "No Unity editor found (set UNITY_EDITOR)"; exit 1; }

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

NS=$(ls "$EDITOR_DIR"/NetStandard/ref/*/netstandard.dll | head -1)
REFS=(-r:"$NS")
for d in "$EDITOR_DIR"/NetStandard/compat/*/shims/netstandard/*.dll; do REFS+=(-r:"$d"); done
REFS+=(-r:"$EDITOR_DIR/Managed/UnityEngine/UnityEngine.dll")
REFS+=(-r:"$EDITOR_DIR/Managed/UnityEngine/UnityEngine.CoreModule.dll")

FAIL=0
for TEST in "$REPO/Tests~"/*Tests.cs; do
  NAME="$(basename "$TEST" .cs)"
  "$EDITOR_DIR/Tools/netcorerun/netcorerun" "$EDITOR_DIR/DotNetSdkRoslyn/csc.dll" \
    -nologo -t:exe -nowarn:0219 -out:"$WORK/$NAME.exe" "${REFS[@]}" \
    "$REPO/Assets/Scripts/PlayingCard.cs" "$REPO/Assets/Scripts/CaptureChecker.cs" \
    "$REPO/Assets/Scripts/Build.cs" "$REPO/Assets/Scripts/GamePlayer.cs" "$TEST"
  cp "$EDITOR_DIR/DotNetSdkRoslyn/csc.runtimeconfig.json" "$WORK/$NAME.runtimeconfig.json"
  echo "===== $NAME ====="
  "$EDITOR_DIR/Tools/netcorerun/netcorerun" "$WORK/$NAME.exe" || FAIL=1
done
exit $FAIL
