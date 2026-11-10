# Credits

Every asset the repo holds or the game ships, with its source and terms.

## Shipped in the game

Nothing yet: the game has no build.

## Reference only, not shipped

| Asset | Source | Terms |
| --- | --- | --- |
| `concept/video.mp4`, `concept/frames/` and `concept/sheet.jpg` | Generated on fal with `minimax/h3-max/text-to-video` on 2026-11-10 (request `01a1c436-b6ea-7053-914f-045bb440d20a`, 1.12 USD), re-encoded at 960 px; the prompt is in [SPEC.md](SPEC.md) | fal's and MiniMax's output terms; our part is CC0 1.0 |
| `prompts/refs/` | Frames of the concept video above (1.75 s, and a crop at 9.5 s), the references for the mood stills | as the concept video |
| `concept/mood/` and `concept/mood-sheet.jpg` | Generated on fal with `fal-ai/nano-banana-pro/edit` on 2026-11-10 (requests `01a1c447-bded-7142-bafe-b590aa66b777` and `01a1c448-6fcf-7753-95db-c0ac8228c19c`, 0.30 USD each), scaled to 1376 px; the prompts are in `prompts/mood-*.json` | fal's and Google's output terms; our part is CC0 1.0 |

Raw generator outputs are kept out of git in `raw/` on the build machine; [fal-log.json](fal-log.json) lists every call with its arguments and billing headers.
