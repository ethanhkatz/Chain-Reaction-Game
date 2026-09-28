#!/bin/bash
# Headless Unity helper. Run from the repo (worktree) root. The project must NOT be open in the Unity editor.
#   Tools/jam.sh dump     Assets/Scenes/Level1.unity
#   Tools/jam.sh validate [Assets/Scenes/Level1.unity,...]
#   Tools/jam.sh capture  Assets/Scenes/Level1.unity [outDir]
#   Tools/jam.sh play     Assets/Scenes/Level1.unity "<input>" "<shots>" <seconds> [clear|gameover|any] [outDir]
#     (set JAM_FPS=30 to also record every frame as 1080p JPG into <outDir>/frames for trailer footage)
#   Tools/jam.sh build    webgl|mac [outDir]
#   Tools/jam.sh run      <Class.Method> [extra unity args...]      (e.g. a level builder)
# Prints the JAM: lines plus any compile errors; exit code is Unity's (0 = ok).
set -u
UNITY=/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
LOG="$ROOT/Logs/jam-$1-$$.log"
mkdir -p "$ROOT/Logs" "$ROOT/JamCaptures"
cmd=$1; shift
case $cmd in
  dump)     ARGS=(-executeMethod JamCli.DumpScene -jamScene "$1") ;;
  validate) ARGS=(-executeMethod JamCli.ValidateScenes ${1:+-jamScenes "$1"}) ;;
  capture)  ARGS=(-executeMethod JamCli.CaptureScene -jamScene "$1" -jamOut "${2:-JamCaptures}") ;;
  play)     ARGS=(-executeMethod JamPlaytest.Run -jamScene "$1" -jamInput "$2" -jamShots "$3" -jamSeconds "$4" -jamExpect "${5:-any}" -jamOut "${6:-JamCaptures}" -jamFps "${JAM_FPS:-0}") ;;
  build)    ARGS=(-executeMethod JamCli.Build -jamTarget "$1" -jamOut "${2:-Builds/$1}") ;;
  run)      m=$1; shift; ARGS=(-executeMethod "$m" "$@") ;;
  *) echo "unknown command $cmd"; exit 2 ;;
esac
# Watchdog: batch Unity can hang if something goes wrong in Play Mode; kill it after JAM_TIMEOUT seconds (default 900).
timeout "${JAM_TIMEOUT:-900}" "$UNITY" -batchmode -projectPath "$ROOT" "${ARGS[@]}" -logFile "$LOG"
code=$?
grep -E "JAM:|error CS|Scripts have compiler errors|Exception:" "$LOG" | grep -v "^UnityEngine\." | sed 's/^.*JAM: /JAM: /'
echo "exit=$code log=$LOG"
exit $code
