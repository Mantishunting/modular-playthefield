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

## 8. NEXT PLAN — Seed System (procedural, evolving flowers)

> **STATUS: design record for a future session. Do NOT auto-implement.** This was written
> by an earlier Claude from the owner's spec; by the time you read it, parts may already
> exist, be partly built, or exist as a similar-but-different system. **Each phase below
> opens with a `▶ CHECKPOINT` — do it before writing any code.**

### 8.0 How to use this document (read first)

1. **Re-read §1–§7 above** — they're the living reference and may already record changes
   that supersede parts of this plan.
2. For each phase, run its **`▶ CHECKPOINT`**: grep/read for the named symbols and the
   *concept* nearby. If something already does this (or close to it), write a short
   **adapt-vs-replace** judgement before coding:
   - Does it already satisfy the intent (genome-driven, monotonic evolution, **reuse of the
     existing agent system**, per-block bells unchanged)?
   - Is it cheaper/safer to **extend** it than replace it? What would replacing break
     (callers, prefab refs, save data)?
   - Default to **adapting** existing systems; only replace with a written reason.
3. Confirm earlier phases' prerequisites still exist as described before relying on them.

### 8.1 Intent (the owner's spec, condensed)

Flowers stop being one hand-placed block and become a **seed-grown bloom**: the player
places a flower, then the **existing marching-ant agent system** (`ChainPatternAgent`)
auto-grows a small structure (a stem with parts). A **genome ("the seed")** controls shape,
parts and colours. New game rolls a fresh seed; within a level every flower is the same
type; at a pollination threshold the genome **evolves** (monotonic — grow/shift, never
shrink) and the evolved genome carries into later levels. **Reuse the agent framework —
no new agent types**; the seed supplies the rules the agents run. Everything except flowers
(and the intro landing-level flex) stays player-placed. Bells stay **per-block and modular**
(a bee hitting one block scores one pollination, not the whole bloom).

### 8.2 Genome data model

```
FlowerGenome:  int seed; int generation; int stemLength; List<PartGene> parts
PartGene (recursive): PartCategory category; int attachIndex; bool budLeft;
                      int extraBits(0..5); int colourIndex; List<PartGene> subParts
```
All genes **monotonic** under evolution (increment or hold), except `colourIndex` which
**drifts ±1** around a colour wheel.

**Mutation per evolution:** `stemLength += 0|1`; `parts` count `+= 0|1`; each
`extraBits += 0|1|2` (clamp ≤5); each part rolls to gain **1–2** `subParts` (recursion);
each `colourIndex` drifts ±1. Never decrease.

### 8.3 Phases (each gated by a checkpoint)

**Phase 1 — Genome → growth, reusing agents.**
`▶ CHECKPOINT:` search for `FlowerGenome`, `FlowerGrowthBuilder`, any runtime
`GrowthPattern` generation, or genome/seed types. Check `ChainPatternAgent`/`Chains.cs`/
`GrowthPattern.cs` still work as in §2. Adapt-vs-replace if a generator already exists.
- New `FlowerGenome` (plain serializable C#) + `FlowerGrowthBuilder` (translator). The
  builder creates runtime `GrowthPattern`s (`ScriptableObject.CreateInstance`): `stemLength`×
  North `Move` steps with `Spawn` steps at each part's `attachIndex`; each spawn's
  `spawnPattern` is the part's category shape sized by `extraBits`; `subParts` → nested
  spawns; `budLeft` sets East/West (handedness auto-mirrors via `IsLeftHanded`).
- Drive it through the existing path: `ChainPatternAgent.StartWithPattern(stemPattern, flowerBlockType)`.
- **Don't** edit `Chains.cs` structs for colour/category — carry those on the spawned
  **agent** (builder configures each), not the `GrowthStep`.

**Phase 2 — Placement + one-genome-per-level.**
`▶ CHECKPOINT:` search for `GenomeManager`/any seed singleton; check how a Flower is placed
today (`HumanClick` flower path, `BlockSpawner`, the flower's `ChainPatternAgent` running
`ZigZagEN`). Decide whether to adapt that bootstrap.
- New `GenomeManager` singleton (`DontDestroyOnLoad`) holds `Current` genome + seeded RNG.
- Placing a Flower reads `GenomeManager.Current` and runs `FlowerGrowthBuilder` (replacing
  the flower's authored `ZigZagEN`, which becomes a fallback). Intro landing-level pattern
  untouched.

**Phase 3 — Evolution + persistence across levels.**
`▶ CHECKPOINT:` find the pollination/win code (`BeeVisitTracker`, `visitsToWin`, the win
event) and any existing level-flow reset (`NextLevelLoader`, scene-reset scripts). Check
nothing already evolves/persists state.
- Add editable **`evolutionThreshold`** beside `visitsToWin` (default = `visitsToWin`; lower
  = evolve sooner). On reaching it, **evolve once** for the level (apply §8.2 mutations,
  bump `generation`). Evolved genome carries to next level; **only newly-placed** flowers
  use it (never re-grow existing blooms). New game → fresh random `seed`, reset to gen 0.

**Phase 4 — Colour wheel + per-part colour + drift.**
`▶ CHECKPOINT:` search for any palette/`ColourWheel`/`_BracketColor` setters and how the
flower material colour is set today (material is *Pink Bracket Flower*, `_BracketColor`).
- New `ColourWheel` (ordered palette/HSV ring). Builder applies each part's
  `colourIndex` colour to its blocks via a small `_BracketColor` hook on
  `BracketStateController` (or a tiny applier the agent calls). Evolution drifts indices ±1.

**Phase 5 — Flower-group "visited" flag (poem-ready hook; poem itself is future).**
`▶ CHECKPOINT:` search for `FlowerGroup` or any per-bloom grouping. Confirm `Flower.cs` bell
logic is still per-block and unchanged.
- New light `FlowerGroup` on the seed block; member blocks reference it; exposes
  `AnyVisited` (set true the first time any member's `Flower` registers a visit). **No score
  change** — pollination stays per-visited-block. The future "poem" feature (each block
  reveals a line) reads this flag; do **not** build the poem here.

### 8.4 Part categories (proposed — confirm with owner)

Categorise by placement behaviour, each a small parametric `GrowthPattern` shape scaled by
`extraBits`: **Spike** (straight run out), **Cluster** (compact pom-pom), later **Fan** /
**Ring/Crown** (around the tip). Start with Spike + Cluster.

### 8.5 Files (expected; verify each still applies)

- New: `FlowerGenome.cs`, `FlowerGrowthBuilder.cs`, `GenomeManager.cs`, `ColourWheel.cs`,
  `FlowerGroup.cs`.
- Edit: `BeeVisitTracker` (`evolutionThreshold` + evolve event), `BracketStateController.cs`
  (`_BracketColor` hook), the flower-placement bootstrap, and a new-game reset hook in the
  level-flow scripts.
- Reuse unchanged: `ChainPatternAgent.cs` (incl. auto-handedness), `Chains.cs`, `Flower.cs`.

### 8.6 Verification

Same genome for all flowers in a level; one evolution at the threshold (new flowers bigger /
more parts / colour drifted one step, existing blooms unchanged); evolved genome persists to
next level; new game resets to a different flower; a bee hitting one bloom block scores one
pollination and flips that bloom's `FlowerGroup.AnyVisited`; west-budded parts auto-mirror.

### 8.7 Open choices to confirm with the owner

- Part categories (§8.4) and how parts distribute along the stem (proposed: upper stem,
  alternating sides for symmetry).
- Whether evolution can fire more than once per level if `evolutionThreshold < visitsToWin`
  (proposed: once per level).
- Where the new-game reset is triggered (depends on the menu/level-flow entry point).
