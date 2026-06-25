# Flower Tech — Systems Reference

A plain-English guide to how flowers work in *Play the Field*: how they grow, how
they're drawn, and how left/right handedness works. Companion to `GAME_OVERVIEW.md`.

> **Key mental model:** a flower's art is **symbolic, not literal.** It's a cloud of
> ASCII brackets (`( ) { } [ ]`) scattered by `BracketShader_v2`. It does not *point*
> anywhere, so "flowers point the right way" is **not** an art/shader question — it's a
> **block-placement** question (which side of the stem the flower block lands on).

---

## 1. What a flower *is* (component stack)

The `FlowerBlock` prefab (`Assets/blocktypes/FlowerBlock.prefab`) carries:

| Component | Role for the flower |
|---|---|
| `HumanClick` | The block "brain": parent/child links, placement, rotation-aware direction, delete, `Die()`. |
| `PhysicsConnector` | `FixedJoint2D` to parent; stiffness by depth/load; stretch-kill. Makes the branch sway/flex. |
| `BracketStateController` | Instances the SpriteRenderer material, sets `_Seed`, lerps the shader toward a target `BracketAnimationState`, applies `_GlobalRotation` (+ `extraRotationDeg`). |
| `ChainPatternAgent` | Scripted auto-growth: walks a `GrowthPattern` of N/S/E/W steps, placing blocks. Has the **handedness flag** (see §4). |
| `Flower` | The "bell": registers bee visits, plays a flower chime (via `VoiceManager`), visit cooldown, fires `OnFlowerVisited`. |
| `PollenRelease` | On its own flower's visit, spawns pollen particles. |
| `DeathVisuals` | On `OnDestroy`, spawns `FlowerBlockDeath` at the flower's position+rotation. |

Note: flowers carry **no junction-visual driver** (no `StructureVisuals`). On a childless
tip flower that component only ever resolves to "Standard" and never changes the look, so
the flower simply shows its `BracketStateController.startingState`. That's intentional and
harmless — the symbolic art doesn't need to react to orientation.

Related assets: `FlowerPreview.prefab` (ghost), `FlowerBlockDeath.prefab`,
`FlowerBlockPollen.prefab`, `Flower.asset` (the `BlockType`), and the `Flower_*`
`BracketAnimationState` assets in `Assets/animation states/`.

## 2. How a flower grows

There are two placement paths, and they decide direction very differently:

**A. Hand-building** — `HumanClick.HandleClick()`:
- Computes the click arc with `ArcMath.SolvePlacement` using `GetForwardVector()` (the
  block's local axis pointing *away from its parent*, in world space — rotation-aware).
- Maps arc → `spawnDirection` (`GetDirectionFromArc`): the `Left`/`Right` arcs are ±90°
  off "forward", so they **already flip with branch flex** — handedness is consistent.
- Validates (`IsValidPlacement`: a Flower attaches to Wood; flowers are normally
  leaf-like tips), spends `BlockType.cost`, spawns via `BlockSpawner`, inherits the
  parent's rotation, and `LinkChild`s into a N/S/E/W slot bucketed by local axes.

**B. Scripted growth** — `ChainPatternAgent` → `HumanClick.TryPlaceRelative(dir, …)`:
- Walks a `GrowthPattern` (`Assets/Chains/*.asset`, e.g. `ZigZagEN`): an ordered list of
  `GrowthStep`s, each an **absolute world direction** (`North/South/East/West`), a repeat
  count, anchored at `Root` (push from base) or `Tip` (extend the head).
- `TryPlaceRelative` places in that **absolute** world direction (East = +X). It does not
  use rotation/forward — scripted plants grow in fixed compass directions.
- Spawn steps can launch sub-agents on a branch (their own sub-pattern).

## 3. The visual layer (how a flower is drawn)

- `BracketShader_v2.shader` ignores the sprite texture and scatters glyphs from
  `_BracketAtlas` on a grid. Per cell it hashes (seeded by `_Seed`) a glyph type, scale,
  jitter, wiggle and a small local rotation, then applies one global rotation
  `_GlobalRotation`. Glyph set is chosen by material toggles (`_UseRoundOpen` etc.); the
  flower material (*Pink Bracket Flower*) uses a balanced `(`/`)` mix.
- `BracketAnimationState` (ScriptableObject) is a named preset of all those numbers.
  `BracketStateController.Update()` **lerps** the live shader values toward the target
  state and adds `extraRotationDeg` on top of the state's `globalRotation`.
- `_Seed` is set once in `BracketStateController.Start()` (random per instance unless
  `seedOverride>=0`). It only controls the **glyph scatter** (a flower's cosmetic
  identity) — not its shape or facing.

Because the art is symbolic, **mirroring the art is meaningless** — a mirrored bracket
cloud reads the same. Do not reach for the shader to solve handedness.

## 4. Handedness — left/right mirroring (the real feature)

Handedness is a **placement-direction** concern, solved on the scripted-growth path, and
it is **derived automatically from the tree**, not toggled by hand.

**The rule (matches the block-slot convention):** a block sitting in a parent's **West
slot** (it has an `eastParent`) is **left**; in the **East slot** (it has a `westParent`)
is **right**. Vertical N/S links carry no handedness. So a branch's handedness = the first
horizontal link you meet climbing from its base toward the root; the root and any pure
vertical stem have none → **right-handed** by default. (`ChainPatternAgent.IsLeftHanded`.)

`ChainPatternAgent` applies it like this:
- On start, the agent computes `mirrorHorizontal = IsLeftHanded(host)` from its base block.
- A **left-handed** branch passes every step through `MirrorDir()` (swap **East↔West**,
  North/South unchanged) before placing; the same mirrored dir is used for both
  `TryPlaceRelative` and the new-block lookup, so links stay correct.
- Each **spawned sub-agent computes its own** handedness from its bud block — so a branch
  that buds West builds left-handed and curves outward, its sub-branches stay consistent,
  and the plant is symmetric with no per-branch setup. Nothing is inherited or hand-set.

Net effect: the player grows **up from the ground** (establishing right vs left), the main
stem is right-handed, branches that bud to the **West** auto-mirror, and one authored
pattern (e.g. `ZigZagEN`) serves both sides. `mirrorHorizontal` is still serialized but is
**read-only/debug** — it's overwritten at runtime.

Hand-built placement (path A) is already handedness-consistent via the rotation-aware
arcs, so it needs nothing.

## 5. Wood vs Flower use of the bracket tech

Both use `BracketStateController` + the bracket shader. Wood additionally uses
`StructureVisuals` to pick a corner/straight look by **tree-link direction** and orient it
purely by `_GlobalRotation` (its corner art is rotationally symmetric). Flowers don't use
that — their symbolic cloud needs no junction look.

## 6. Flex & fossilising (context)

A block inherits its parent's rotation at spawn, then `PhysicsConnector` lets it sway, and
`Fossilize` turns it **Kinematic** once it is far enough (>~8 generations) from the tip —
freezing old growth. This is why hand-build arcs are rotation-aware (forward flexes), and
why anything that reacts to orientation should stop once a block fossilises. It does not
affect scripted-growth handedness, which is set per agent up front.

**Heaviness** of a flower block is its `Rigidbody2D.Mass` (`FlowerBlock` default `0.4`):
higher = it swings/sags more and pulls its branch down harder. `PhysicsConnector` sums the
mass of everything hanging below a block as "load" and uses it to set joint stiffness, so a
heavy tip makes the branch above it droop. Use `GravityScale` for a harder downward pull
without changing inertia; if heaviness makes a branch sag too far or trip the stretch-kill,
tune `PhysicsConnector` (`frequency`/`dampingRatio`) or the global `JointStiffness` — that's
stiffness, not weight.

## 7. Gotchas for future work

- **Handedness is placement, not art.** If a plant builds the wrong way, look at
  `ChainPatternAgent` / `GrowthPattern` directions, not the shader.
- **`_Seed` only scatters glyphs.** A flower's shape/look comes from its
  `BracketAnimationState`; the seed just varies the cloud. Keep it stable so a flower
  doesn't visibly reshuffle.
- **Never flip `transform.localScale.x`.** `HumanClick` click/occupancy radii and
  `PhysicsConnector` stretch-kill read `localScale.x`/`lossyScale.x` as a positive
  magnitude; a negative scale corrupts them.
- **Scripted growth uses absolute world directions** (`TryPlaceRelative`); hand-building
  uses rotation-aware arcs (`HandleClick`). Don't assume one path's behaviour for the other.

## 8. FLOWER ENGINE — partly built (routine-budget slice done)

> **STATUS: Slice 1 (the routine budget) is BUILT and verified (2026-06-23) — but Phase 2 RETIRES
> it.** Talking the design through (2026-06-24), the owner reframed the model: a flower is a
> **recursive tree of "flower parts,"** and growth is bounded by **how far the tree has filled**
> (one part per pollination), not by a number. So the routine budget is being **removed** and the
> genome becomes a **part-tree**. What slice 1 actually shipped is recorded in **§8.0** (kept for
> history); the **Phase-2 plan** that supersedes it is **§8.0.1**. The owner authors the 5 part
> shapes; Claude wires the engine. **Before writing each slice, re-run the checkpoints in §8.8.**

### 8.0 What's built so far (Slice 1 — the routine budget)

The genome's first and only field today is the **routine budget**: how many growth agents a
single bloom may run. Built per §8.5 (no block cap — bounded by agent count only); proven in Play
that raising the budget grows more agents and lowering it grows fewer with the extra blocks left
in place. Files in `Assets/Flowers/`:

- **`FlowerGenome.cs`** — plain `[Serializable]` class; fields `seed`, `generation`,
  `routineBudget`. Designed to gain base-shape / parts / colours later.
- **`GenomeService.cs`** — holds the **one shared genome** in `static` state, so it **persists
  across scene loads for free** (same idiom as `BlockGeneration`). Counts its **own cumulative
  pollinations** (because `BeeVisitTracker` resets every scene) and **evolves +1 budget every 10
  pollinations** from a start of **3** (monotonic). Subscribes to `BeeVisitTracker.OnVisitRegistered`
  once via `[RuntimeInitializeOnLoadMethod]`. `SetRoutineBudget()` is a clamped debug setter.
- **`RoutineBudget.cs`** — the live **per-bloom** counter, attached to the bloom **root block** by
  the starter with capacity snapshotted from the genome. `TryClaim()` **only ever refuses**, never
  enables (the §8.5 rule). Found by climbing parents; a bloom with no `RoutineBudget` on its root
  (the BEAN title-screen chains) is **never gated**.
- **`GenomeResetOnLanding.cs`** — marker component; placed on the landing scene (`StartScene`), its
  `Awake` wipes the genome back to a fresh generation-0 seed. *(Owner must add this component to
  StartScene — it is the one manual wiring step.)*

Edits to existing scripts: **`FlowerBloomStarter.cs`** snapshots the genome budget onto a
`RoutineBudget` at placement; **`ChainPatternAgent.TryStart`** self-gates (climb to the bloom budget,
`TryClaim` a slot, or self-kill leaving the block — `FindBloomBudget` helper); **`TestOverlay.cs`**
(the **`/`** overlay) gained a "Flower genome" section with a live readout and **`-`/`+`** budget
buttons.

**Lifecycle:** the genome persists level→level **and** across level restarts (the whole play
session is one "run"); **only returning to the landing page resets it**. Adjusting the budget (via
the `/` overlay or evolution) affects flowers placed **afterward** — existing blooms keep the size
they snapshotted at placement.

**Next slice:** see **§8.0.1** — the genome becomes a part-tree and this budget is retired.

### 8.0.1 Phase 2 — the part-tree (Slice A CODED 2026-06-24, pending Play verification)

> Full plan: `C:\Users\JACK\.claude\plans\ok-ok-but-that-synchronous-penguin.md`.
>
> **Slice A is implemented.** New: `Assets/Flowers/GenomeNode.cs` (tree node + slot count + deep
> copy + breadth-first first-empty-slot), `Assets/Flowers/FlowerPieceCatalogue.cs` (the 5-part
> catalogue ScriptableObject). Reworked: `FlowerGenome` (now a tree root, no budget), `GenomeService`
> (holds the tree, `Evolve()` fills the shallowest empty slot with a random `slotPart`, `SnapshotRoot()`
> deep-copies for a placed bloom), `Chains.cs` (`StepType` → Move/SpawnPiece/GMove/RandomPiece, kept
> serialized values; new `gCoeff`), `ChainPatternAgent` (carries a `GenomeNode`, grows RandomPiece
> slots recursively, budget gate removed), `FlowerBloomStarter` (snapshots & grows the tree),
> `TestOverlay` (Evolve/Reset + tree print). Deleted: `RoutineBudget.cs`.
>
> **Owner setup before it grows anything:** (1) create `Assets/Resources/FlowerPieceCatalogue.asset`
> (Create ▸ Flowers ▸ Piece Catalogue), set `root = GBass` and `slotParts = [GCross, GCurl, GFron,
> GPettle]`; (2) author the 5 part `GrowthPattern`s — GBass needs ≥1 **RandomPiece** step (a slot)
> or evolution has nowhere to grow; (3) the flower block needs `FlowerBloomStarter` + a flower
> `BlockType`; (4) still-pending from slice 1: `GenomeResetOnLanding` on `StartScene`. Until the
> catalogue exists, `FlowerBloomStarter` falls back to its serialized `rootPattern` (old GenomTest rig
> keeps working).
>
> **Slice A.5 — LOCAL-FRAME growth (built 2026-06-25).** Flower agents now grow in a **local frame**:
> each agent treats its **starting block as local South** and grows **North away from its parent**, so
> a part orients itself wherever it attaches (petals fan out instead of all pointing world-north, and a
> bloom on a leaning stem rotates with it). **Authoring convention: draw every part bottom-to-top —
> its base is South, "up/forward" is North — once; it auto-rotates and the left/right handedness mirror
> still applies.** Implementation: `ChainPatternAgent` rotates each authored dir by its entry direction
> (`EntryDirFromParent` → `RotateToWorld`), composed after the handedness `MirrorDir`; gated by a
> `useLocalFrame` flag that `FlowerBloomStarter` sets on the root and `SpawnAgentOnBlock` propagates to
> every descendant — so the **BEAN title chains (flag off) keep growing in absolute directions.**
>
> **Slice A2 — auto-evolution (built 2026-06-25).** `GenomeService.OnPollination` now calls `Evolve()`
> every `PollinationsPerEvolution` (=10) pollinations, so the shared genome grows one slot per 10 bee
> visits. The overlay's manual **Evolve** button still works alongside it.
>
> **GMove length math (built 2026-06-25, early slice B).** A `GMove` step now places
> `round(repeats + node.g * gCoeff)` blocks (min 1; non-genome agents use G=0). The `/` overlay has
> **G- / G+** buttons (`GenomeService.AdjustAllG`) that set G on every node of the live tree — bump G,
> place a flower, and GMove segments grow. Per-part G *evolution* (vs this global test knob) is still
> slice B proper.

**The model.** A flower is a **recursive tree of flower parts**. A *part* is a `GrowthPattern`;
there are exactly **5** valid parts — `GBass, GCross, GCurl, GFron, GPettle` (empty stubs in
`Assets/blocktypes/`, owner authors their steps). A part's pattern uses **4 step types**:

- **Move** — plain move (today's behaviour).
- **GMove** — move whose length scales with G: `round(base + G*gCoeff)` (field now, math in slice B).
- **SpawnPiece** — a **structural sub-agent** that extends the SAME part (e.g. `GVsubagent`, an arm that
  builds out then ends in a slot). It carries the part's genome node forward with a slot offset, so
  RandomPiece slots nested inside it count as *this part's* slots. (Title chains pass `node=null`, so
  theirs resolve to empty — unchanged.)
- **RandomPiece** — a **slot**, filled from the 5-part catalogue by the genome; filling it starts a NEW
  child subtree. A part's **slot count** = every RandomPiece reachable through its pattern **and its
  SpawnPiece sub-agents** (recursively) — the trailing number in `GCurl[GC]3`.

The **genome** is a tree of nodes `{ part, G, C(colour hue), children[slot] }` (the `[G C]`).
**Evolution** fills **one empty slot per pollination**, breadth-first; the root is **always
`GBass`** and every later fill is a **seeded-random** part from the 5 (so all flowers in a level
match). **No numeric budget** — the finite tree bounds growth, so `RoutineBudget` is **deleted**.
Placement grows the *current* tree; later flowers are bushier. Lifecycle (cross-scene persistence,
landing reset, pollination hook) is **kept** from slice 1 — only the payload (number→tree) and
evolution (+budget→fill-slot) change.

**Slices.** **A** = all the structure (step types, `GenomeNode` tree, 5-part catalogue, tree→bloom
expansion, manual "Evolve" in the `/` overlay, delete `RoutineBudget`). **A2** = wire `Evolve()` to
pollinations. **B** = the G length math. **C** = apply colour hue to the part's
`BracketStateController` material.

**Specificity gaps to resolve while building** (directionally agreed, details open): randomization
rules (uniform? same-part-in-slot? `GBass` root-only?); how the `static` `GenomeService` loads the
catalogue asset (`Resources.Load` vs injected); snapshot must be a **deep copy** so evolving the
genome never mutates a placed bloom; `children[]` index = ordinal of the RandomPiece step; one
flower `BlockType` for all parts (confirm); GMove fields = `repeats` (base) + new `gCoeff` (rate);
`G` int / `colourHue` float 0–1.

### 8.1 The vision

A flower stops being a single hand-placed block and becomes a **seed-grown bloom**. The
player places one flower block; the **existing agent system** auto-grows a small structure
off it. A **genome ("the seed")** chooses the base shape, which parts appear and where, and
(later) colours. Within a level **every flower is the same**; when a trigger is reached
(flowers placed, or pollinations) the genome **evolves a little**, and flowers placed after
that grow the new shape. Bells stay **per-block** (one bee hit on one block = one pollination).

### 8.2 What already exists to build on (reuse, do not replace)

The growth machinery is the "flower engine" in embryo — see §2 and `Chains/ChainPatternAgent.cs`:

- **`GrowthPattern`** (ScriptableObject) = an ordered list of `GrowthStep`s. Each step is a
  **`Move`** (place a block, advance the head) or a **`Spawn`** (place a block *and give it
  its own agent* running a sub-pattern). Steps carry a direction (N/S/E/W), a repeat count,
  and an anchor (`Root` = push from base / `Tip` = extend the head).
- **`ChainPatternAgent.SpawnAgentOnBlock()`** is exactly the *"tag pre-determined blocks with
  their own agents that then grow new blocks"* mechanism — it adds a fresh `ChainPatternAgent`
  to a just-placed block and runs a sub-pattern on it. **Mixing and matching branches already
  works this way.**
- **Handedness is automatic** (`IsLeftHanded` climbs the tree; a west-slot branch builds left,
  mirroring East↔West) and every spawned sub-agent recomputes its own. **Keep this untouched.**
- **`StartWithPattern(pattern, blockType)`** is the public entry point — the engine feeds
  runtime-built patterns through here.

So the engine needs **no new agent types**. It needs (a) something that **builds patterns
from the seed** and feeds them through `StartWithPattern`, and (b) the **budgets** in §8.5 —
which `ChainPatternAgent` has nothing like today.

### 8.3 The engine in plain terms

1. Player places a flower block (generation 0 of the bloom).
2. The engine reads the seed and builds a **base shape** (~2–5 blocks) that extends out using
   the existing handedness.
3. As the base builds, **pre-chosen blocks in the sequence get tagged with their own agents**
   (the `Spawn` mechanism), each running a **part template** (§8.4).
4. Part templates are **template agents/patterns**: a small parametric shape that is **copied
   and tweaked by the seed before it runs** (size, which side it buds, how many) — not
   authored one-per-flower.
5. The **budgets** (§8.5) stop it before it can run away.

### 8.4 The flower parts (owner authors the shapes)

Five small part shapes, each a parametric `GrowthPattern` template the seed can resize.
**Keep them small** (see the budget). Proposed set:

| Part | Rough idea | Typical size |
|---|---|---|
| **Base** | the core/stem the flower grows from; sets the overall form | 2–5 blocks |
| **Petal** | a short run out to the side, mirrored by handedness | 2–4 blocks |
| **Stamen** | a thin spike from the centre/tip | 1–3 blocks |
| **Frond** | a feathered / lightly branching arm | 2–4 blocks |
| **Pom** | a compact cluster / pom-pom | 2–4 blocks |

Author them **static first**, then make them seed-modifiable (size, side, count).

### 8.5 THE BUDGET — the hard lesson from the rollback

The previous version had **no budget**: patterns spawned patterns spawned patterns, blooms
ballooned to dozens of blocks, ate all the food, and broke the game. The only existing guard
(`suppressIfParentHasAgent`) does **not** stop `Spawn` steps from recursing. The engine owns
**one budget — an agent/routine budget** — and there is **no block cap, by design**. Blocks
are never counted or capped; growth is bounded purely by how many *agents* (routines) may run.

**Routine budget — controls recursion AND fan-out with one number.** One **shared budget
object** is owned by the whole bloom (found via the bloom's root block, so sharing works no
matter how an agent started). **Every routine self-gates the moment it starts**: it claims a
slot and runs, or — if the bloom is already full — **kills itself, leaving its block in place**.
A single number therefore bounds the total routines in a bloom (both fan-out and depth), and it
is a natural **genome parameter** that evolution increments to grow the flower. The limit lives
on the genome (`FlowerGenome.routineBudget`); the live per-bloom counter is a separate
`RoutineBudget`, one fresh instance per bloom (per-flower counting).

When in doubt, **build too small**: give a small routine budget. A 6-block flower that works
beats a 20-block flower that breaks the economy — and we control that by routines, not by
counting blocks.

### 8.6 The seed / genome (define later — keep it light at first)

The genome is just the set of inputs the engine reads: a random `seed`, a `generation`
counter, the base-shape choice, which parts attach and where, each part's size/side/count,
and (later) colours. **Do not over-design this up front** — the prior attempt's elaborate
recursive genome was part of what went wrong. Start with the smallest thing that drives §8.3
and expand once the engine + budgets are proven.

### 8.7 Evolution (consistent per level, small monotonic change)

- All flowers in a level share one genome.
- A trigger — **number of flowers placed** or **number of pollinations** (owner to choose;
  pollination count via `BeeVisitTracker` is the natural hook) — evolves the genome **once**.
- Evolution is a **small, mostly monotonic** change: a part grows by one, a new part appears,
  a colour drifts — **never shrink**, never a wholesale reshape.
- **Existing blooms are left alone**; only newly placed flowers grow the evolved genome.
- The evolved genome carries to later levels. A new game rolls a fresh seed at generation 0.

### 8.8 Before you build — checkpoints & lessons

This section is design intent, **not** a green light to generate code. Whoever implements:

1. **Verify the mechanism still matches §8.2** — read `ChainPatternAgent.cs`, `Chains.cs`,
   `GrowthPattern.cs`; confirm `Spawn` / `SpawnAgentOnBlock` / `StartWithPattern` /
   `IsLeftHanded` still behave as described. Adapt to what's there; don't replace working code.
2. **Build the routine budget FIRST** (§8.5), before any genome richness. Prove a flower
   *cannot* run more agents than its routine budget — test with a deliberately greedy pattern.
   (No block cap — growth is bounded by agents, not by counting blocks.)
3. **Author one tiny part end-to-end** (e.g. Base + one Petal) before adding the rest.
4. **Keep bells per-block** and leave hand-built placement (§2 path A) alone — only the flower
   growth path changes.
5. **Go slowly.** One part / one budget / one trigger at a time, each verified in play before
   the next.
