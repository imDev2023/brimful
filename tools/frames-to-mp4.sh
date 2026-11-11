#!/usr/bin/env bash
# Join FrameRecorder's frames into an H.264 clip and a contact sheet.
#
#   tools/frames-to-mp4.sh <frames dir> <out.mp4> [fps=30] [sheet every n frames=30]
#
# Writes <out>.mp4 (yuv420p, plays everywhere) and <out>-sheet.jpg, a grid of every n-th
# frame with its time in seconds, to look at the run frame by frame. Free: ffmpeg runs locally.
set -euo pipefail
dir="$1"; out="$2"; fps="${3:-30}"; every="${4:-30}"
count=$(find "$dir" -name '*.png' | wc -l | tr -d ' ')
[ "$count" -gt 0 ] || { echo "no frames in $dir" >&2; exit 1; }
ffmpeg -v error -y -framerate "$fps" -i "$dir/%05d.png" -c:v libx264 -preset slow -crf 20 -pix_fmt yuv420p -movflags +faststart "$out"
sheet="${out%.mp4}-sheet.jpg"
cols=6
rows=$(( (count / every + cols - 1) / cols ))
ffmpeg -v error -y -framerate "$fps" -i "$dir/%05d.png" \
  -vf "select='not(mod(n\,$every))',scale=480:-1,drawtext=text='%{eif\:t\:d}.%{eif\:mod(t*10\,10)\:d} s':x=8:y=8:fontsize=22:fontcolor=white:box=1:boxcolor=black@0.5,tile=${cols}x${rows}" \
  -frames:v 1 -q:v 3 "$sheet"
echo "$count frames -> $out ($(du -h "$out" | cut -f1)), sheet $sheet"
