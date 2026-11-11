# Brimful

A desktop 3D platformer in Unity, built for x-gaming#8 (github.com/imDev2023/x-gaming): a young apprentice sluice-keeper climbs a dry aqueduct city and fills it with water on the way up, made the way Sumiro was, with our own world, hero and levels.
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
- The hero is a young woman, an apprentice sluice-keeper (owner, 2026-11-10), drawn from `concept/mood/human-woman-1.jpg`, not the clay golem the video and first stills show: fitted clothes in teal so she stands off the orange walls, nothing loose (no cape, hood, scarf, sash or long hair), hands empty; see SPEC.md, "The hero becomes human".
- The look is painted-1's (painted texture on solid 3D forms) with stylised-2's camera; image edits copy the reference's composition even when told not to, so ask for a new view in words and check it.
- The network here drops connections in bursts (2026-11-10): `tools/fal.ts` saves each submitted job to `raw/pending-<name>.json`, and `node tools/fal.ts --resume <name>` collects it; never resubmit a job that was submitted, since fal bills it anyway.
  `fal-usage.ts` prices each call from its own billed units, because fal's usage API merges calls to one endpoint in the same minute.
- Lamplighter (#7) is paused: never change `../lamplighter`.
  Its tools (`tools/rig_hero.py`, `tools/simplify.sh`, `tools/cut_views.py`) and Lantern River's `tools/lowpoly_bake.py` are copied in from GitHub when needed, then kept here.

## Unity

- Unity 6.6 (6000.6.8f1) with the Universal Render Pipeline, from the URP blank template; the Unity project is this folder (`Assets/`, `Packages/`, `ProjectSettings/`).
- This Mac is Intel, and Unity's Intel macOS editor ends after Unity 6.7 LTS: move to 6.7 LTS when it ships and stay on it here.
- Installed build modules: Mac and Web; add Windows Build Support (Mono) in Unity Hub before the first Windows build.
- The LuLu firewall on this Mac (alert mode) blocks every program it has no rule for, and on 2026-11-10 it had none for the editor or its package manager: the package manager logged `ECONNRESET`, `ETIMEDOUT` and `connect EBADF` to `packages.unity.com` while `curl` and Node reached it fine.
  Only the owner can allow a program in LuLu; a new editor version is a new path and needs its own rule.
  By 2026-11-11 it had one: the package manager downloaded `com.unity.pipeline` from `packages.unity.com` in 6 s.
- Open the project headless with `/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath <this folder> -logFile <log>`, started with `run_in_background`; the package manager's own log is `Logs/upm.log`.
- Unity's agent plugin edits scenes and assets directly, so commit before every larger change.
  The owner installed it for this folder only on 2026-11-10 (`claude plugin install unity@unity-agent-plugin --scope local`, enabled in the git-ignored `.claude/settings.local.json`); it loads in a session started in this folder, and `/unity:` lists its skills.
- Characters go to Unity as FBX with Mixamo bone names, which the Humanoid importer maps by itself (Kensei): `tools/rig_unity.py` writes them into `Assets/Characters/<name>/`, `Assets/Editor/BrimfulImport.cs` sets every import option by folder, and `Assets/Editor/HeroSetup.cs` builds the material, the prefab and the clips.
  The lopsided-marker check passed on 2026-11-11 (a box skinned to the left hand imported on her left, toes along +Z), so the export keeps left as left.
- Drive Unity through the `unity` CLI and the project's Pipeline package (`com.unity.pipeline`, added 2026-11-11): start a resident headless Editor with the batch command above minus `-quit`, with `run_in_background`, wait with `unity status --until-ready --project-path <this folder>`, run C# with `unity command script eval --caller plugin --skill unity-cli`, and stop it with TaskStop when done.
  After adding or changing a script, `AssetDatabase.Refresh()` returns before the compile: run `unity recompile --project-path <this folder>` and `unity status --until-ready` before calling the new code, or the eval fails with "type does not exist" or "no Pipeline instance".
- In edit mode a skinned mesh catches up with sampled bones only at the next update, so a check render shows the previous pose unless each `SkinnedMeshRenderer` has `forceMatrixRecalculationPerRender` set (`Assets/Editor/HeroCheck.cs` does).
- Animation comes from Quaternius's Universal Animation Library (CC0), both packs' free Standard editions, retargeted by Unity's Humanoid system; neither has a ledge hang or a shimmy, so those are keyed on our rig.
  The packs come from quaternius.itch.io through "No thanks, just take me to the downloads"; `agent-browser download 'a.download_btn' <file>` saves them, the FBX files live in the gitignored `raw/ual/` and `Assets/_Imported/Quaternius/`, and only the clips in use are copied to `Assets/Animations/`.
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
