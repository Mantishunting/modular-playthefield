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

## 8. FLOWER ENGINE — design goals (not yet built)

> **STATUS: design goals only. Nothing in this section is implemented.** An earlier attempt
> at a full seed/genome system was built and **rolled back**. The current branch keeps only
> the left/right handedness (§4) and the agent machinery (§2); there is **no** `FlowerGenome`,
> `GenomeManager`, builder, colour wheel, or `FlowerGroup` in the code. We are rebuilding
> **slowly and incrementally** — keeping the design goals below, discarding the previous
> flawed plan. The owner authors the part shapes; Claude wires the engine and the budgets.
> **Before writing any code, run the checkpoints in §8.8.**

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
