# Brimful: spec

The game for [x-gaming#8](https://github.com/imDev2023/x-gaming/issues/8): a 3D platformer with climbing, ledges, parkour and light puzzles, made the way Sumiro was, with our own world, hero and levels.
On 2026-11-10 the owner chose this idea from three, Unity desktop as the engine, and a plan of about 10 USD of generation up to a playable prototype, with every paid call asked for first.
"Brimful" is a working title: the first one, "Highwater", is already a 2024 game by Demagog Studio.

## The pitch

A young apprentice sluice-keeper sets out from the lowest basin of a sun-baked terracotta city whose aqueducts have run dry.
The apprentice climbs walls, hangs from ledges and leaps between arcades on the way up to the reservoir at the top.
Each sluice opened sends water after them, and the rising water lifts crates and rafts into new ledges, so the way up is also the way the city fills again.
Chapters: the dry basin, the arcades, the water stairs and the high reservoir.
The hero began as a clay golem; on 2026-11-10 the owner asked for a proper human being instead (see "The hero becomes human" below), so the video and the first mood stills still show the golem.

## How it is made

Sumiro's method ([note](https://github.com/imDev2023/x-gaming/blob/main/sources/2026-11-04-xikhar-sumiro.md)) with Kensei's split on top ([note](https://github.com/imDev2023/x-gaming/blob/main/sources/2026-11-07-kensei-opus-blender-unity.md)), following x-gaming's [WORKFLOW.md](https://github.com/imDev2023/x-gaming/blob/main/WORKFLOW.md):

1. A generated concept video sets the look, the world and the hero (done, below).
2. Mood stills from the gameplay camera, until the owner approves the look.
3. A 4-view sheet of the hero, lifted to 3D with Tripo's multiview model, cut to budget and rigged in code with Mixamo bone names, which Unity's Humanoid importer maps by itself.
4. A playable prototype of one stretch: climb, hang and shimmy along a ledge, pull up, jump, and one sluice whose water lifts a crate to a ledge.
5. Concept art for each chapter in the approved style.
6. Each chapter built from its concept, then played, reported and fixed in a loop.

Only the few solid hero objects are generated: the hero, a sluice gate, a crate or raft.
Walls, arches, stairs and channels are built in code with CC0 textures mapped at real size, and CC0 models dress the rest.

## Concept video prompt

> Stylised 3D action-adventure game with a hand-painted gouache look and soft brush textures, bright midday sun over a dry terracotta city of aqueducts, arcades and stepped stone cisterns: a small, friendly, chunky fired-clay golem with rounded red-brown limbs and two carved eye holes climbs a sandstone wall, grabs the top ledge and pulls itself onto an empty aqueduct channel; it heaves a wooden sluice gate open and bright turquoise water gushes into the dry cistern below, lifting a floating wooden crate up toward the ledge. Tall arches, ochre plaster towers and a pale blue sky with soft clouds behind. Steady third-person camera behind and slightly above the golem. No text, no HUD, no people.

MiniMax H3 Max on fal (`minimax/h3-max/text-to-video`), 10 s, 1080p, 16:9 (a climbing platformer needs width), seed 101, prompt expansion off, one take.
The arguments are in [prompts/concept-video.json](prompts/concept-video.json).

## From the video

One take, generated on 2026-11-10 for 1.12 USD: 10.13 s, 1920 x 1080 at 24 fps, no cuts, with a sound track.
It is kept re-encoded at 960 px in [concept/video.mp4](concept/video.mp4).
[concept/frames/](concept/frames/) holds one frame every 0.5 s (`f_001` at 0.0 s, `f_020` at 9.5 s), and [concept/sheet.jpg](concept/sheet.jpg) is the contact sheet.

### What happens

- 0.0 to 2.5 s: the golem climbs the face of a plain adobe wall, and the camera rises with it (`f_001` to `f_006`).
- 2.5 to 4.5 s: it reaches the top, where two wooden boards lie across the end of a channel that already holds water (`f_006` to `f_010`).
- 5.0 to 6.5 s: the boards swing up from the middle while the golem stands on their seam with its arms raised, water foams below, and the boards end up standing as two tall gate leaves (`f_011` to `f_014`).
- From 6.75 s: a crate surfaces in the channel right behind the golem and floats there to the end (`f_015` to `f_020`).

### Camera

- In front of the golem, looking back at it: the golem faces the camera for the whole clip, the opposite of the chase camera the prompt asked for.
- It starts near the wall's foot looking at its face, rises with the climber to just above the wall's top, then pushes slowly in over the channel's near end, with no cuts and no shake.
- The golem stays centred; at 9.5 s it stands about 300 px tall in the 1080 px frame, in the lower half, and the channel runs to a vanishing point just above the frame's centre.
- A moderately wide lens: the gate leaves lean outward at the frame's edges.
  The clip carries no known distance, so the engine camera's field of view is matched to `f_020` by eye and measured there.

### World

- A dry city of adobe houses with flat roofs, beam ends, small windows and clay-tile awnings, iron lanterns by arched doorways, red-brick aqueduct arches on square piers, palm crowns, and a pale desert beyond.
- The channel is a long open trough of smooth sandstone, filled with clear turquoise water.
- The wall the golem climbs is a large smooth adobe block with a few rough bricks at its foot, and it shows no holds.

### Hero

- A blocky clay figure: a cube head with two round eye holes and no mouth, a box torso with one diagonal crease, thin cylinder limbs with ball joints, and large round mitten hands with no fingers (`f_017` to `f_020`).
- Solid and symmetrical, with no cloth or hair: a good subject for a 4-view sheet, Tripo and a code rig.
- The video never shows its back.

### Palette, measured

8 x 8 px averages of the 1080p frames at 1.75 s and 9.5 s.

| Where | Time, pixel | Colour | Luma |
| --- | --- | --- | --- |
| Sky, top | 1.75 s, 933,53 | `#5b97c7` | 142 |
| Sky, low through an arch | 1.75 s, 693,347 | `#b8dced` | 214 |
| Cloud | 1.75 s, 1240,80 | `#f1f1ee` | 241 |
| Aqueduct brick, lit | 9.5 s, 773,160 | `#d37c47` | 139 |
| Aqueduct brick, in the haze | 1.75 s, 827,147 | `#a46c4c` | 118 |
| Adobe wall, sunlit | 1.75 s, 1200,600 | `#d7945d` | 158 |
| Adobe wall, in the golem's shadow | 1.75 s, 787,827 | `#ce7641` | 133 |
| Adobe house, sunlit | 1.75 s, 333,267 | `#e3a36c` | 173 |
| Adobe house, in shade | 1.75 s, 1840,600 | `#b65e34` | 110 |
| Sand, sunlit | 1.75 s, 133,827 | `#f6daa8` | 220 |
| Sand, in shade | 1.75 s, 1733,867 | `#5e3423` | 60 |
| Stone top, sunlit | 9.5 s, 400,1013 | `#eccf95` | 209 |
| Golem's shadow on stone | 9.5 s, 827,920 | `#4f3d2f` | 64 |
| Golem, body | 9.5 s, 953,800 | `#cb5f39` | 115 |
| Golem, body in shade | 1.75 s, 953,533 | `#a54126` | 84 |
| Golem, top of the head in sun | 9.5 s, 953,647 | `#f4bb80` | 195 |
| Water, sunlit | 9.5 s, 867,373 | `#7ed6bd` | 193 |
| Water, shaded | 9.5 s, 1067,373 | `#5495a4` | 136 |
| Gate wood, sunlit | 9.5 s, 400,533 | `#875f3d` | 101 |
| Gate wood, shaded | 9.5 s, 1467,600 | `#4a2b18` | 48 |
| Crate, top | 9.5 s, 953,507 | `#bf9666` | 155 |
| Crate, side | 9.5 s, 960,567 | `#6d4e30` | 82 |
| Palm | 1.75 s, 1853,67 | `#687a3f` | 114 |

Shadows stay warm brown and never near black: the darkest ground sample, sand in shade, has a luma of 60.
A shadow on a sunlit adobe wall turns a deeper orange rather than grey (`#ce7641` against `#d7945d`).

### Light

- A high sun from the right: shadows fall to the left, short on the ground.
- A strong warm grade with soft haze in the distance.
- Measured over all 243 frames: at most 0.1% of a frame is clipped to white and at most 0.01% is near black.

### What went wrong

Checked frame by frame at 4 per second, and over all 243 frames by script (the largest change from one frame to the next is 11.6 against a median of 4.0, so nothing pops in or flickers).

1. **The camera is in front of the golem, not behind it.** The golem faces the camera throughout, so it climbs the wall with its back to it (`f_001` to `f_006`), and the clip never shows the back a chase camera would see.
2. **The golem does not open the gate.** The boards swing up on their own while it stands on the seam where they part, which is where it would fall in (`f_011` to `f_013`).
3. **The gate leaves look broken.** Their tops are ragged like snapped planks, and they hang from nothing but flat plates (`f_014` to `f_020`).
4. **The channel was never dry.** Water fills it from 3.0 s, before the gate opens (`f_007`), so opening the gate releases nothing and the water never visibly rises.
5. **The crate appears from nowhere and reads as a backpack.** It surfaces at 6.75 s directly behind the golem's head and stays there, and it never rises toward a ledge (`f_015` to `f_020`).
6. **The climb is stiff and unreadable.** At 0.0 s the golem is spread-eagled off the wall, its legs vanish behind its body from 0.25 to 1.25 s, and its fingerless mitts never show a grip (`f_001` to `f_003`).
7. **The wall shows nothing to climb.** It is a smooth block with no holds or ledges, so the clip gives no visual language for what is climbable.
8. **It is clean stylised 3D, not gouache.** Soft, smooth shading like a stylised asset pack, with no brush strokes; the bright palette and warm shadows came through, the painted texture did not.
9. **The head is close to a known character.** A cube head with two round eye holes recalls Danbo, the cardboard robot from Yotsuba&!; the hero sheet must change the head enough to be clearly ours.

### What we keep, and what the game does instead

- Keep the world, the palette, the warm shadows and the hero's build: the adobe city, the red-brick arches, the turquoise water, a solid clay figure with ball-jointed limbs and mitten hands.
- Keep the two-leaf gate lying across a channel's end as a sluice the golem works, and the floating crate.
- The game's camera follows behind the golem; the video's front view is only a reference for the look.
- The golem climbs facing the wall, on surfaces that read as climbable: protruding bricks, beam ends, cracks and ledge lips in a lighter, worn tone.
- The golem works each sluice with a wheel or lever, the channel starts dry, and the water's rise visibly carries the crate up to a ledge.
- The hero sheet changes the head (for example a rounded, kiln-fired head with a carved brow) so it no longer recalls Danbo, and draws the back from the front.

## Mood stills

Two calls on 2026-11-10, 2 images each at 2K 16:9, for 0.60 USD in all (`fal-ai/nano-banana-pro/edit`, 0.15 per image), edited from the video's frame at 1.75 s for the world and a close-up of the hero at 9.5 s, told to take only their style: [prompts/mood-stylised.json](prompts/mood-stylised.json) asks for the video's clean stylised 3D, [prompts/mood-painted.json](prompts/mood-painted.json) for a hand-painted gouache look.
Both ask for the camera behind and slightly above the golem as it climbs a wall with a readable route up to a dry channel and a closed sluice gate.
Small copies are in [concept/mood/](concept/mood/), side by side in [concept/mood-sheet.jpg](concept/mood-sheet.jpg); the 2K originals stay in `raw/`.

All four fixed the video's main fault: the golem is seen from behind, chest to the wall, hands on a hold and feet on another, on walls whose holds (protruding bricks and pale stones, beam ends, a coping lip) read as climbable.

Measured as 16 x 16 px averages of the 2K images:

| Still | Hero, torso | Hero, back of the head | Wall beside it |
| --- | --- | --- | --- |
| stylised-1 | `#a44024`, luma 83 | `#b15b3d`, luma 107 | bricks `#ab5b38`, luma 105 |
| stylised-2 | `#b35332`, luma 101 | `#e79966`, luma 166 | `#e5a769`, luma 176 |
| painted-1 | `#b35631`, luma 103 | `#bd5d38`, luma 111 | `#d18753`, luma 147 |
| painted-2 | `#ac603a`, luma 109 | `#aa5934`, luma 104 | `#d68759`, luma 148 |

### What is wrong in each

- **stylised-1:** the camera is high and to one side, looking down along the wall rather than up it from behind; the channel on top is full of water although the prompt asked for a dry one, and the gate is a door in a tower above the channel rather than across it; the back of the golem's head is the colour of the bricks beside it (`#b15b3d` against `#ab5b38`); the coping stones are as big as its torso; the wall's foot bends at the bottom left (a wide lens).
- **stylised-2:** the clearest gameplay view (camera behind and above, the golem centred under a coping lip, beam ends to either side, holds below, a dry paved channel up to a closed gate), but in the sun the golem's head disappears into the wall (luma 166 against 176); a blank block fills the left third of the frame; the gate is a courtyard door at the end of a walkway, not a sluice.
- **painted-1:** the best mood: painted texture on solid 3D forms, a hero that stands off the wall (luma 103 to 111 against 147), a clear route of pale stones and red bricks up to the lip, and a brick arch over turquoise water; but the back of the head still reads as a rounded cube, two big wooden posts are cut off at the bottom-left edge, water already runs in the channel below the dry city, the gate is again a walkway gate, and a palm grows through the arch's opening.
- **painted-2:** a round ball head that no longer recalls Danbo, and a gate under a lifting frame that reads as a real sluice; but it looks like a 2D illustration, with outlines and brush strokes over the forms that a real-time 3D scene would not match, a strong fisheye bulges the near wall, holds are sprinkled over the wall like a climbing gym with no single route, the camera is beside the golem rather than behind it, and a carved panel on its back reads almost like the letters "IO".

### Recommendation

Take painted-1's look (hand-painted texture on solid 3D forms, which Unity can render with painted albedo textures, soft light and a painted sky) with stylised-2's camera, and carry painted-2's lifting-frame sluice gate into the props.

## The hero becomes human

On 2026-11-10, after the mood stills, the owner asked for the hero to be a proper human being, and chose to see a young woman and a young man side by side.
The design, the same for both:

- About twenty, an apprentice sluice-keeper, with stylised but natural human proportions and sun-browned skin.
- A close-fitting sleeveless tunic in deep teal, belted and tucked in: teal stands off the orange walls, where the golem's sunlit head vanished into them.
- Fitted dark-brown trousers bound at the shins, soft leather climbing shoes, leather wrist wraps and a small satchel strapped tight.
- Nothing loose: no cape, hood, scarf or long hair, since generators and code rigs handle fitted clothes well and loose cloth badly (Lamplighter's tunic hem pushed through the trousers); no sash or sword either, which keeps the hero clear of well-known desert-parkour heroes.
- Hands empty, so nothing is welded into the mesh.
- The woman wears her dark hair in a tight low bun; the man's is short-cropped.

The stills are edited from painted-1 for the look, with the camera behind the hero and a little to the left so the face shows in profile ([prompts/mood-human-woman.json](prompts/mood-human-woman.json), [prompts/mood-human-man.json](prompts/mood-human-man.json)).
Two calls on 2026-11-10, 2 images each, 0.60 USD in all; small copies are `concept/mood/human-*.jpg`, side by side in [concept/human-sheet.jpg](concept/human-sheet.jpg).

All four show a readable human climber in painted-1's world: chest to the wall, hands gripping a hold, feet on others, face in profile, and a sluice gate under a lifting frame at the top of the wall.
The teal tunic stands well off the wall (luma 51 to 80 against the wall's 148, 14 x 14 px averages); sunlit skin comes close to the wall's tone (man-1's upper arm `#e69655`, luma 162, against `#d58551`, 146), but the hair and tunic break the outline.

### What is wrong in the hero stills

- **In all four:** the edit kept painted-1's composition (the same wall, holds, arch, posts and crates) although the prompt said not to; water still runs in the channel below the dry city, and the wooden posts are still cut off at the bottom left; the satchel moved from the back to the hip; the tunic flares over the hips instead of staying fitted, which is loose cloth for a rig.
- **woman-1:** the closest to the design: natural proportions, the low bun as asked, a believable grip on the brick overhead with one knee raised; she looks sideways rather than up at the next hold.
- **woman-2:** the bun sits on top of the head, not low as asked; the trousers balloon at the thighs, and her proportions read squat (short legs, wide hips).
- **man-1:** the best climbing pose, reaching up to the next stone with a foot on a lower one, and natural proportions; his wrist wraps are white cloth, not leather, and two dark smudges at the top left are shadows cast by beam ends that are not in the frame.
- **man-2:** his head is large for his body, so he reads as a young teenager, his feet look bare, and the pose is static, hugging the wall; the gate's frame has no lifting gear.

### Recommendation

woman-1 or man-1, whichever the owner prefers: both have natural proportions and a pose that reads as climbing.
The hero sheet fixes the drift from the design: a fitted tunic that ends at the hip without a flare, leather wrist wraps, and the satchel either strapped to the back or kept as a hip pouch, both solid enough to generate and rig.
