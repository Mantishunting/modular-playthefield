# Play the Field — Game & Code Overview

A plain-English guide to what every script in this project does and how the
systems fit together. Written for the project owner, not just programmers.

---

## 1. What the game is

You play **a bean plant**. It's a **2D physics resource-management game** with a
**World of Goo**-style buildable structure, a hint of **RTS** (managing an economy
and a growing colony) and a hint of **unfolding/idle** growth.

Core loop:

1. You **place blocks** — **Wood** (stems), **Leaf**, and **Flower**.
2. Wood forms the **stem skeleton**; blocks are joined by physics so the plant
   sways, bends and can snap under its own weight.
3. **Leaves** produce **Food** (the currency) — but only when they are in
   **sunlight**. Stems and leaves cast shadow and can block light from reaching
   other leaves, so *where* you build matters.
4. Food is spent placing more blocks and on **upkeep**; run out and blocks
   **starve and die**.
5. **Flowers** act like **bells**. **Bees** drift in and bump around the level;
   when a bee strikes a flower it counts as a **pollination visit**.
6. Reach the target number of bee visits → **you win** the level → next level.

It is also a game about **navigating space**: the sun moves across the sky (it's
really a sprite/PNG light that gets *clipped* around your objects so it looks like
real light and shadow), and you grow your plant to catch light and reach bees.

---

## 2. The big systems at a glance

| System | Job | Key scripts |
|---|---|---|
| Blocks & placement | Click to grow the plant, arc-based placement, insert/move | `HumanClick`, `ArcMath`, `BlockSpawner`, `BlockType`, `BlockTypeManager` |
| Economy | Food currency, costs, starvation, upkeep, refunds | `ResourceManager`, `BlockDailyCost`, `HumanClick` (cost rules) |
| Tree structure | Parent/child links, generation depth | `HumanClick`, `BlockGeneration`, `TreeLooker` |
| Physics | Joints, stiffness, snapping, fossilising | `PhysicsConnector`, `Jonts` (Joint), `StructuralIntegrity`, `fosilize`, `hingeJ` |
| Light & shadow | Moving sun, beam clipped around objects, leaf light checks | `Sun`, `SunbeamTracker`, `SunBeamSpriteOccluder`, `SunbeamClipContourOccluder`, `LeafProduction` |
| Leaves / production | Food generation in rhythm, lifespan | `LeafProduction`, `BlockType` |
| Bees & flowers | Bee drifting, flower bells, win condition | `Bee`, `BeeSpawner`, `BeeDirection`, `BeeWingFlapper`, `Flower`, `BeeVisitTracker`, `PollenRelease` |
| Visuals (shader brackets) | Procedural stem/leaf look that adapts to junctions | `BracketStateController`, `BracketAnimationState`, `StructureVisuals`, `BlockScaler` |
| Previews | Ghost block before placing, delete confirmation | `PreviewBlockManager`, `DeletePreviewSystem` |
| UI | Food counter, visit counter, win popup, block buttons | `UI`, `BlockSelectionUI`, FoodBar UI scripts |
| Auto-growth | Scripted growth patterns (chains) | `GrowthAgent`, `ChainPatternAgent`, `Chains/GrowthPattern`, `Chains/Chains` |
| Audio | Per-block voices, music, placement & flower sounds | `VoiceManager`, `BlockAudioPlayer`, `WoodSound`, `BlockPlacementAudio`, `MusicLooper` |
| Camera & background | Pan/zoom, intro fly-in, parallax, sky colour | `CameraController`, `CameraIntro`, `ParallaxLayer`, `SunSet` (SkyColour) |
| Tutorial | Event-driven step-by-step intro | `TutorialManager`, `TutorialStep`, `TutorialUI`, `TutorialArrowPositioner` |
| Level flow | Bounds, level loading, resets | `LevelBounds`, `NextLevelLoader`, `SceneRefresh`, `SceenReset`, `Reseter`, `levelcle` |

---

## 3. Blocks & placement

- **`BlockType`** *(ScriptableObject asset)* — defines a kind of block: name
  (`Wood`/`Leaf`/`Flower`), colour, prefab, **cost**, whether it **produces
  resources** (and how much/how often), and **lifespan** (0 = immortal). The Wood,
  Leaf and Flower assets are instances of this.
- **`BlockTypeManager`** *(singleton)* — holds the list of available block types
  and tracks which one is currently selected. Hotkeys: **W** = Wood, **L** = Leaf,
  **B** = Flower. UI buttons also set the selection.
- **`HumanClick`** — **the heart of the game**, lives on every block. It:
  - Detects mouse clicks/holds near the block and decides where a new block goes.
  - Uses **arc placement** (see `ArcMath`): the side of the block you click
    (forward / left / right) decides the direction the new block grows.
  - Supports **insert** (clicking an existing child shoves it outward and drops a
    new block in between) and **continuous placement** while holding the mouse.
  - Maintains **directional links**: each block knows its `north/south/east/west`
    parent and children — this is the plant's tree structure.
  - Enforces **placement rules** (e.g. you can't put Wood on a Leaf; Leaves need
    Wood nearby to survive).
  - Handles **right-click delete** (with a confirmation preview) and **`Die()`**,
    which recursively destroys a block and everything growing off it.
  - Fires static events `OnBlockPlaced` / `OnBlockDestroyed` that audio, tutorial
    and other systems listen to.
- **`ArcMath`** *(static helper)* — the maths behind placement. Given where you
  clicked relative to the block's "forward", it returns whether to **Add** (and in
  which arc: Forward/Left/Right/Bottom) or **Insert**, and rejects clicks that are
  **occluded** by neighbouring blocks.
- **`BlockSpawner`** — simple factory that instantiates a block prefab at a
  position and tags it with its `BlockType`.
- **`BlockInitializer`** — on spawn, clears a block's parent/child links so a
  fresh block starts disconnected.

## 4. Economy (Food)

- **`ResourceManager`** *(singleton)* — tracks **Food**. Adds/subtracts/spends
  food, checks affordability. Runs **starvation** (when food is very low it kills
  childless "tip" blocks and partially refunds them), a **stagnation failsafe**
  (if food sits unchanged too long it forces a kill so the game can't deadlock),
  and **death refunds** (blocks dying for any reason refund a fraction of cost).
  Debug keys: **F** = +1 food, **0** = +2000 food (cheat).
  - Note: Wood and Leaf have **dynamic costs** that rise as the plant grows
    (computed in `HumanClick.GetDynamicCost`: Wood = 5 + total blocks; Leaf scales
    more gently). Bigger plant = pricier expansion.
- **`BlockDailyCost`** — periodic **upkeep**: every N seconds it charges food per
  block. If you can't pay, an optional penalty (none / stop production /
  kill random blocks) kicks in. Also has its own stagnation failsafe.

## 5. Tree structure & generations

- **`BlockGeneration`** — each block's **depth from the root** (root = 0). Updates
  automatically when connections change and tracks the **global maximum
  generation** (how tall/deep the plant has grown). Fires `OnTreeGrew`. Used by
  scaling, fossilising and joint-stiffness systems.
- **`TreeLooker`** — utility to find all blocks within a radius of a point (used
  e.g. to check "is there Wood near this Leaf?"). Also a debug proximity readout.

## 6. Physics (sway, bend, snap)

- **`PhysicsConnector`** — puts a **FixedJoint2D** between a block and its parent.
  The **root** is kinematic (the immovable anchor); children are dynamic so they
  swing. Joint **stiffness** is computed from **depth** and **load** (mass hanging
  below): rigid near the trunk, floppier toward the tips. Recomputed only when the
  structure changes, not every frame.
- **`Jonts`** (class `Joint`) — an **alternative** generation-based stiffness
  scheme: the first few generations are perfectly rigid, then stiffness decays
  toward the tips. It defers to the global `JointStiffness` controller if one is
  present (so the two don't fight).
- **`StructuralIntegrity`** — a **kill switch**: if a block gets stretched too far
  from its parent (e.g. physics blew it apart), the block and its branch die.
- **`fosilize`** (class `Fossilize`) — performance + feel: blocks far from the
  growing tip turn **kinematic ("stone")** so old growth stops simulating; only the
  newest ~8 generations at the tip keep moving.
- **`hingeJ`** — hinge-joint variant/helper for block jointing (rotational joint
  behaviour).
- **`JointConnectorVisualizer`** *(debug tool)* — drop on any GameObject to
  visualise the plant's hidden structure. Four independent overlays, each with its
  own toggle + hotkey:
  - **Joint connectors** (**J**) — line from every block to its parent.
  - **Clickable nodes** (**N**) — circles around each block (inner = body/delete
    radius, outer = the larger place-a-block range).
  - **Open build spots** (**K**) — a dot at every empty cell where you could click
    to place the *currently selected* block type (respects real placement rules).
  - **Cursor snap dot** (**M**) — a dot that follows the mouse and snaps to where
    the next block would land.

  Shows in both the **Game view** (via the render pipeline, URP-safe) and the
  editor **Scene view** (gizmos); Play mode only. Backed by helper methods on
  `HumanClick`: `GetBodyRadius()`, `GetClickRadius()`, `GetOpenPlacementPositions()`,
  `TryGetPlacementPosition()`.

## 7. Light & shadow

- **`Sun`** — moves a light source along an **arc** across the sky (start angle →
  stop angle over a set duration, then loops after a delay). Exposes the **light
  direction**, orbit radius and current angle for other systems. The sun is the
  game's clock.
- **`SunbeamTracker`** — keeps the sunbeam sprite glued to the sun's position and
  pointed **outward** (away from centre).
- **`SunBeamSpriteOccluder`** (class `SunbeamSpriteOccluder`) — resizes the beam
  PNG so it **stops at the first object it hits** (raycasts both edges, takes the
  shorter) — a cheap "beam blocked by the plant" look.
- **`SunbeamClipContourOccluder`** — the fancier version: casts many rays across
  the beam's width and writes a **1×N cutoff texture** to the beam's shader, so the
  beam's bottom edge **hugs the silhouette** of whatever blocks it — giving real
  per-column shadows around the plant.
- **`LeafProduction`** — on each Leaf: periodically **box-casts toward the sun** to
  check it isn't shaded. When lit, it produces Food on a **musical rhythm** (delays
  quantised to a BPM) and plays the block's voice. Also handles block **lifespan**.
  This is where "leaves only feed you in the light" actually happens.
- *(Old, unused: `nolonger used scripts/SunlightBeamRenderer*` — earlier
  line-renderer beam attempts, superseded by the sprite-clip approach.)*

## 8. Bees & flowers (the win condition)

- **`Bee`** — drifts across the screen: a base direction plus layered sine waves
  and Perlin noise for a lazy, bumbling path. Bounces off colliders, despawns when
  it leaves the level bounds.
- **`BeeSpawner`** — emits bees from the level edges, biased toward higher up, but
  **only during the middle of the day** (a window around midday driven by the Sun).
- **`BeeDirection`** (class `BeeFaceMovementDirection`) — flips the bee sprite to
  face its travel direction.
- **`BeeWingFlapper`** — animates the wings flapping.
- **`Flower`** — a flower is a **bell**: when a bee touches it (trigger or
  collision) it registers a **visit**, plays a chime (assigned by `VoiceManager`),
  and has a short cooldown so one bee can't spam it.
- **`PollenRelease`** — on a flower visit, spawns physics **pollen particles** that
  pop upward.
- **`BeeVisitTracker`** *(singleton)* — counts total flower visits; at the target
  (`visitsToWin`) it fires the **win** event the UI listens for.

## 9. Visuals — the procedural "bracket" shader look

The stems/leaves don't use plain sprites; their art is drawn by a shader that
scatters little "brackets" on a grid, and the look **adapts to the block's role**.

- **`BracketAnimationState`** *(ScriptableObject asset)* — a named **look preset**:
  grid spacing, bracket size/count, edge culling, scale range, rotation, jitter and
  wiggle. You author several of these (straight, corners, leaf, danger, etc.).
- **`BracketStateController`** — drives one block's shader material toward a target
  `BracketAnimationState`, **smoothly interpolating** every parameter. Also applies
  a per-junction rotation and a random seed.
- **`StructureVisuals`** — detects a Wood block's **junction type** from its parent
  and children (straight vertical/horizontal, the four corners, or "standard") and
  tells the `BracketStateController` which look + rotation to use. This is what
  makes stems visually "connect" into bends and corners.
- **`BlockScaler`** — makes blocks **closer to the root larger** (older growth
  thickens), never shrinking, and compensates the shader spacing so bracket density
  stays consistent as the block scales up.
- **`BracketWiggleController`**, **`BracketAnimationState`** drivers in
  `animation states/` and `oldshader/BlockShudder` — supporting/older wiggle and
  shudder effects for the same shader.

## 10. Previews

- **`PreviewBlockManager`** *(singleton)* — shows a **ghost block** where your next
  block will land (one pooled ghost per type), tinted by whether you can afford it,
  with a floating **cost label**, and **highlights the parent** block you'd attach
  to. Previews have their colliders/clickable scripts stripped so they don't
  interfere.
- **`DeletePreviewSystem`** — right-click **delete confirmation**: first click puts
  the block (and, cascading, its children) into a wobbling **"danger" state**; a
  second click within the window confirms the delete. Auto-restores after a timeout
  or on left-click.

## 11. UI

- **`UI`** — the HUD: shows current **Food** with a colour-coded **net rate/sec**,
  the **bee-visit counter** (`current / goal`), and the **win popup** + Next-Level
  button when you win.
- **`BlockSelectionUI`** — the Wood/Leaf/Flower **buttons**: recolours them by
  affordability and selection, and tells `BlockTypeManager` what to place.
- **`Scenes/ui test/FoodBar*`** (`FoodDisplayBar`, `FoodBarRotator`, `…UI`,
  `FoodBarTester`) — an alternative/experimental food bar display and its test
  harness.

## 12. Auto-growth (scripted plants)

For pre-grown plants, demos, or scripted sequences rather than player clicks:

- **`GrowthAgent`** — simplest: repeatedly grows a block in one direction at a set
  interval until blocked.
- **`Chains/GrowthPattern`** *(asset)* + **`Chains/Chains`** (the `GrowthStep` /
  `GrowthChain` data types) — a **pattern language**: a list of steps (move or
  spawn, anchored at root or tip, a direction, a repeat count, optional sub-pattern).
- **`Chains/ChainPatternAgent`** — executes a `GrowthPattern` along a block, can
  **spawn child agents** running their own sub-patterns (branching), and avoids
  fan-out by suppressing itself if its parent already has an agent.

## 13. Audio

- **`VoiceManager`** *(singleton)* — gives each block a **persistent voice + pitch**
  from a pool that **grows with the number of blocks** (more plant = richer chord).
  Separate pools for **flower chimes** and **wood** sounds, per-voice volumes, a
  master volume, and **ducking** (quietens leaf voices while music plays).
- **`BlockAudioPlayer`** — on a Leaf: plays its assigned voice each time it
  produces food.
- **`WoodSound`** — on a Wood block: small random chance to play a wood sound when
  placed, with a global lock so only one wood sound plays at a time.
- **`BlockPlacementAudio`** (class `BlockPlacementSound`) — plays a random
  placement "click" with pitch variation whenever any block is placed.
- **`MusicLooper`** — plays a music track on a loop with configurable silence
  before/after, and tells `VoiceManager` to duck the plant voices while it plays.

## 14. Camera & background

- **`CameraController`** — right-drag **pan**, scroll **zoom** (clamped), optional
  edge-panning, plus helpers to focus/reset.
- **`CameraIntro`** — a one-time **fly-in** at level start (zoom/pan from a wide
  shot to the play position, skippable), then hands control back to
  `CameraController`.
- **`ParallaxLayer`** — moves/scales background layers relative to the camera and
  zoom for depth.
- **`SunSet`** (class `SkyColour`) — tints the background through **dawn → midday →
  dusk** colours based on the Sun's arc progress.

## 15. Tutorial

- **`TutorialManager`** — drives an ordered list of steps, **advancing by listening
  to real game events** (block placed/destroyed, block type selected, bee visit, or
  reaching a target block count). Can enable components/objects as steps unlock.
- **`TutorialStep`** *(data)* — one step: its message, trigger type, target counts,
  and what to enable/activate.
- **`TutorialUI`**, **`TutorialArrowPositioner`** — display the step text and point
  an arrow at the relevant thing.
- **`TutorialMassages`** (class `TutorialMessages`, legacy) — older message system,
  auto-disabled when the new `TutorialManager` runs.
- **`InstructionDisappear`**, **`SimpleFader`**, **`TimedDestroyer`** — small
  helpers to fade/hide instruction text and clean up temporary objects.

## 16. Level flow & misc helpers

- **`LevelBounds`** *(singleton)* — the rectangular play area; used to spawn and
  despawn bees and to keep things on-screen.
- **`NextLevelLoader`** — loads the next scene / restarts the current one, resetting
  `HumanClick`'s static counters so the new level doesn't start expensive.
- **`SceneRefresh`, `SceenReset`, `Reseter`, `levelcle`** — various scene
  reset/cleanup utilities.
- **`DirectionFlip`** — flips a sprite based on movement/orientation (generic
  facing helper).
- **`TreeLooker`**, **`ArcDebugDrawer`**, **`ArcMath`** — `ArcDebugDrawer` draws the
  placement arcs in the editor for debugging the click logic.

---

## 17. Notes & loose ends

- `Assets/nolonger used scripts/` and `Assets/oldshader/` hold **superseded**
  approaches (old beam renderers, old shudder) kept for reference.
- There are two stiffness systems (`PhysicsConnector` + `JointStiffness` global, and
  the generation-based `Jonts`); `Jonts` steps aside when the global controller
  exists. (Note: `JointStiffness` is referenced by `PhysicsConnector`/`Jonts` as the
  authoritative tuning singleton.)
- The "never let starvation/upkeep destroy Wood" policy
  (`allowLowResourceDestroyWood`) is now wired up: `ResourceManager.CheckStarvation`
  and `BlockDailyCost.KillRandomBlocks` both skip Wood when the flag is off.
  (Previously the property returned the wrong field and nothing read it, so the
  setting did nothing.)

---

*This file is documentation only — it contains no code and is safe to edit or
delete. Update it as systems change.*
