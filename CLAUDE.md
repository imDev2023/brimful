# Brimful

A desktop 3D platformer in Unity, built for x-gaming#8 (github.com/imDev2023/x-gaming): a clay golem climbs a dry aqueduct city and fills it with water on the way up, made the way Sumiro was, with our own world, hero and levels.
On 2026-11-10 the owner chose this idea, Unity desktop, and about 10 USD of generation up to a playable prototype.
Read [SPEC.md](SPEC.md) for the game and what the concept video got wrong, and x-gaming's `WORKFLOW.md` (`../x-gaming/WORKFLOW.md`) for the asset pipeline, before changing anything.

## Rules

- Show the owner samples early, and before showing anything look at every frame and list what looks wrong: Lamplighter's sample 5 went out with faults visible in its own contact sheet.
  Measure rather than eyeball: brightness, floating props, triangle counts, frame rate.
- Generation calls (fal and the like) are paid and need the owner's approval each time, with the cost.
  Run them through `node tools/fal.ts`, read the exact charge with `node tools/fal-usage.ts`, and log that in `~/.config/spend/ledger.csv` and [CREDITS.md](CREDITS.md).
  MiniMax H3 Max video costs 0.112 USD per second at 1080p until 2026-11-15 and 0.16 after; Nano Banana Pro images 0.15 each; Tripo H3.1 multiview 0.01 per credit, 60 credits for detailed geometry and texture.
- The repo is public: no keys, no raw paid outputs (they live in the gitignored `raw/`, the only copy, never delete it), and no copied text or screenshots of other people's work.
- Licence, as for Dockstack and Lantern River: code MIT No Attribution, our assets CC0.
- Generate only the solid hero objects, from 3 or 4 views each; build walls, arches, stairs and channels in code with CC0 textures at real size; dress with CC0 models (Poly Haven, Poly Pizza).
- The hero's head must not recall Danbo (a cube head with two round eye holes, as the concept video drew it): change it on the hero sheet.
- Lamplighter (#7) is paused: never change `../lamplighter`.
  Its tools (`tools/rig_hero.py`, `tools/simplify.sh`, `tools/cut_views.py`) and Lantern River's `tools/lowpoly_bake.py` are copied in from GitHub when needed, then kept here.

## Unity

- Unity 6.6 (6000.6.8f1) with the Universal Render Pipeline, from the URP blank template; the Unity project is this folder (`Assets/`, `Packages/`, `ProjectSettings/`).
- This Mac is Intel, and Unity's Intel macOS editor ends after Unity 6.7 LTS: move to 6.7 LTS when it ships and stay on it here.
- Installed build modules: Mac and Web; add Windows Build Support (Mono) in Unity Hub before the first Windows build.
- The LuLu firewall on this Mac (alert mode) blocks every program it has no rule for, and on 2026-11-10 it had none for the editor or its package manager: the package manager logged `ECONNRESET`, `ETIMEDOUT` and `connect EBADF` to `packages.unity.com` while `curl` and Node reached it fine.
  Only the owner can allow a program in LuLu; a new editor version is a new path and needs its own rule.
- Open the project headless with `/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath <this folder> -logFile <log>`, started with `run_in_background`; the package manager's own log is `Logs/upm.log`.
- Unity's agent plugin edits scenes and assets directly, so commit before every larger change.
  The owner installs it for this folder only with `claude plugin marketplace add Unity-Technologies/unity-agent-plugin` and `claude plugin install unity@unity-agent-plugin --scope local`; it loads in a session started here.
- Characters go to Unity as FBX with Mixamo bone names, which the Humanoid importer maps by itself (Kensei); check the export once with a deliberately lopsided marker before trusting it.
- Steam bans games that issue or trade crypto or NFTs (its 2021 rule, still in place on 2026-11-10), so a blockchain build ships from our own site or Epic.
  Keep chain code out of the game's rules; nothing touches a chain until the game is built.

## Blender

- MCP for Blender is configured per project (local scope): for a session started here, the owner adds it with `claude mcp add blender -s local -e DISABLE_TELEMETRY=true -e BLENDER_MCP_SAFE_MODE=1 -- uvx mcp-for-blender`.
- Headless Blender scripts need no bridge: `/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python-exit-code 1 -P <script>`.
- Start Blender's GUI for the bridge with `run_in_background` and stop it with TaskStop.

## Issues

Issues are tracked on github.com/imDev2023/x-gaming: #8 is this game.

## Multica dashboard

This project is mirrored on the owner's Multica dashboard (workspace Pandora) under the x-gaming project `5e65d424-8653-4e40-8aca-449c8e90891c`.
GitHub Issues on `imDev2023/x-gaming` stay the source of truth; Multica only mirrors them, and each Multica ticket title starts with its issue number.
At the end of every session, after the issues are updated, run the `multica-sync` skill for that project: mirror ticket statuses, add any new tickets, and refresh the project's "Current step" section.
Every status and assignment write passes `--no-start` (`issue create` rejects the flag), and no ticket is ever assigned to an agent, so the sync never starts an agent run.
If the `multica` CLI is not signed in, say so in the session summary and stop anyway; the sync never blocks the work.
