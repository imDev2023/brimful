# Credits

Every asset the repo holds or the game ships, with its source and terms.

## Shipped in the game

| Asset | Source | Terms |
| --- | --- | --- |
| `Assets/Characters/Hero/` (the hero's FBX, its three textures, material and prefab) | Generated on fal with `tripo3d/h3.1/multiview-to-3d` on 2026-11-11 from the views in `concept/hero-views.jpg` (request `01a1c901-9071-7ce1-81b6-810e8ffb1269`, 0.60 USD), simplified by `tools/simplify.sh` and rigged by `tools/rig_unity.py`; the arguments are in `prompts/hero3d-woman.json` | Tripo's and fal's output terms; our part is CC0 1.0 |
| `Assets/Animations/Keyed/`, `Assets/Environment/` (meshes, painted textures, materials, sky and grade) and `Assets/Scenes/Stretch.unity` | Made in code by `Assets/Editor/HeroClips.cs` and `Assets/Editor/StretchBuilder.cs`; the colours are measured from the concept video | CC0 1.0 (ours) |
| `Assets/Animations/Quaternius/` | Clips from Quaternius's [Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html) and [Universal Animation Library 2](https://quaternius.com/packs/universalanimationlibrary2.html), Standard editions (free from quaternius.itch.io, fetched 2026-11-11), copied out of the Unity FBX files by `Assets/Editor/HeroSetup.cs`; the source FBX files stay in the gitignored `raw/ual/` and `Assets/_Imported/` | CC0 1.0 (Quaternius) |

## Reference only, not shipped

| Asset | Source | Terms |
| --- | --- | --- |
| `samples/stretch-run.mp4` and `samples/stretch-run-sheet.jpg` | Recorded from the prototype stretch by `tools/playtest.sh` on 2026-11-11 | CC0 1.0 (ours), with the hero's terms above |
| `concept/video.mp4`, `concept/frames/` and `concept/sheet.jpg` | Generated on fal with `minimax/h3-max/text-to-video` on 2026-11-10 (request `01a1c436-b6ea-7053-914f-045bb440d20a`, 1.12 USD), re-encoded at 960 px; the prompt is in [SPEC.md](SPEC.md) | fal's and MiniMax's output terms; our part is CC0 1.0 |
| `prompts/refs/` | Frames of the concept video above (1.75 s, and a crop at 9.5 s), the references for the mood stills | as the concept video |
| `concept/mood/` (stylised and painted) and `concept/mood-sheet.jpg` | Generated on fal with `fal-ai/nano-banana-pro/edit` on 2026-11-10 (requests `01a1c447-bded-7142-bafe-b590aa66b777` and `01a1c448-6fcf-7753-95db-c0ac8228c19c`, 0.30 USD each), scaled to 1376 px; the prompts are in `prompts/mood-stylised.json` and `prompts/mood-painted.json` | fal's and Google's output terms; our part is CC0 1.0 |
| `concept/mood/human-*.jpg`, `concept/human-sheet.jpg` and `prompts/refs/look-painted-1.jpg` | Generated on fal with `fal-ai/nano-banana-pro/edit` on 2026-11-10 from the painted-1 still (requests `01a1c461-07e9-7a00-ad3a-54dfd36a23ed` and `01a1c461-abae-7de2-9d62-9590f53a5ef9`, 0.30 USD each), scaled to 1376 px; the prompts are in `prompts/mood-human-*.json` | fal's and Google's output terms; our part is CC0 1.0 |
| `prompts/refs/hero-woman-1.png` | A crop of the 2K woman-1 still above, the reference for the hero's sheet | as the woman-1 still |
| `concept/hero-sheets.jpg` and `concept/hero-views.jpg` | Generated on fal with `fal-ai/nano-banana-pro/edit` on 2026-11-11 from `prompts/refs/hero-woman-1.png` (request `01a1c855-0dab-7093-9612-2f55034d4dae`, 0.30 USD), scaled to 1376 px; the views are cut from sheet 1 with `tools/cut_views.py`; the prompt is in `prompts/sheet-woman.json` | fal's and Google's output terms; our part is CC0 1.0 |

Raw generator outputs are kept out of git in `raw/` on the build machine; [fal-log.json](fal-log.json) lists every call with its arguments and billing headers.
