#!/usr/bin/env bash
# Play the prototype stretch end to end with the scripted pilot, in the resident headless
# Editor, and record it:
#
#   tools/playtest.sh [name=run]
#
# Needs a resident Editor for this project (see CLAUDE.md, "Unity"). Recompiles, rebuilds the
# hero and the stretch from code, turns the pilot and the frame recorder on, plays until the
# pilot finishes (FrameRecorder then stops play), and writes Screenshots/<name>.mp4, its
# contact sheet and Screenshots/pilot-log.txt. Exits 0 when the pilot reached the goal, 1 when
# it failed, 2 when the run never finished. Free: everything runs locally.
set -euo pipefail
cd "$(dirname "$0")/.."
name="${1:-run}"
project="$PWD"
u() { unity "$@" --project-path "$project" --format json 2>/dev/null; }
result() { python3 -I -c 'import json,sys; d=json.load(sys.stdin); print(d["data"].get("result") if d.get("success") else d.get("errors"))'; }

# Wait for the Editor through a script reload, which drops its connection for a few seconds.
ready() {
  for _ in $(seq 1 30); do
    u status --until-ready >/dev/null && return 0
    sleep 3
  done
  echo "the Editor never became ready" >&2
  exit 2
}
compile() {
  for _ in $(seq 1 10); do
    ready
    out=$(u recompile) || true
    if echo "$out" | python3 -I -c 'import json,sys; d=json.load(sys.stdin); sys.exit(0 if d.get("success") and d.get("data") else 1)' 2>/dev/null; then
      echo "$out" | python3 -I -c 'import json,sys; d=json.load(sys.stdin)["data"]; e=d.get("errors") or []; w=d.get("warnings") or []; print("compile:", d.get("status"), len(e), "errors", len(w), "warnings"); [print("  ", x.get("file"), x.get("line"), x.get("message")) for x in (e + w)[:10]]; sys.exit(1 if e else 0)'
      return
    fi
    sleep 3
  done
  echo "recompile never answered" >&2
  exit 2
}

ready
u command script eval --caller plugin --skill unity-cli 'UnityEditor.AssetDatabase.Refresh(); return "ok";' >/dev/null
compile
ready
u command script eval --caller plugin --skill unity-cli --timeout 900 'Brimful.EditorTools.HeroSetup.BuildAll(); return Brimful.EditorTools.StretchBuilder.Build();' | result

rm -f Screenshots/pilot-log.txt
u command script eval --caller plugin --skill unity-cli 'var p = UnityEngine.Object.FindAnyObjectByType<Brimful.StretchPilot>(UnityEngine.FindObjectsInactive.Include); p.enabled = true; var r = UnityEngine.Object.FindAnyObjectByType<Brimful.FrameRecorder>(UnityEngine.FindObjectsInactive.Include); r.enabled = true; r.outDir = "Screenshots/frames"; return "pilot on";' | result
u command editor set_auto_tick >/dev/null
u command editor.playmode start >/dev/null

for _ in $(seq 1 180); do
  [ -f Screenshots/pilot-log.txt ] && break
  sleep 5
done
if [ ! -f Screenshots/pilot-log.txt ]; then
  echo "the run never finished" >&2
  u command editor.playmode stop >/dev/null || true
  exit 2
fi
for _ in $(seq 1 30); do
  u command editor status | grep -q '"playMode": "stopped"' && break
  sleep 2
done
ready
# Put the scene back as built (pilot and recorder off).
u command script eval --caller plugin --skill unity-cli --timeout 900 'return Brimful.EditorTools.StretchBuilder.Build();' >/dev/null

tools/frames-to-mp4.sh Screenshots/frames "Screenshots/$name.mp4" 30 30
tail -3 Screenshots/pilot-log.txt
grep -q "passed" Screenshots/pilot-log.txt
