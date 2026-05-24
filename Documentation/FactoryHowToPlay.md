# Factory Scene Guide

For the full-game run structure, scene flow, story, and reward loop, use these as the primary game-level docs:

- [Game Design Document](GameDesignDocument.md)
- [Game Flow And Scene Guide](GameFlowAndSceneGuide.md)
- [Workshop Preparation Design](WorkshopPreparationDesign.md)

## What this scene is

This scene is the **workshop half** of Arcane Atelier.

The idea is simple:

1. Place spirit and factory nodes on the grid.
2. Let them produce elements and spells over time.
3. Route outputs into the correct next machine.
4. Build up a stock of finished spell cards.
5. Commit that stock as the deck payload for the future battle scene.

This slice does **not** implement the boss fight itself.
It only implements the factory scene and the handoff into battle.

For teammates: the generated workshop scene and generated database are the current source of truth for this guide.

---

## How this matches the game rules

The original game loop says:

1. Enter the factory scene.
2. Place spirit nodes to produce resources.
3. Convert resources into spell cards through factories.
4. Enter battle and use those spell cards.
5. Win rewards.
6. Return to the factory and optimize again.

This workshop slice implements the **factory-side part** of that loop:

- spirit generation
- element fusion
- spell shaping
- spell fusion
- reward-style unlock hooks
- battle deck payload export

This workshop slice does **not** yet implement:

- the battle scene
- bosses
- drag-and-drop casting
- auto-cast in battle
- health restore / level-up / passive-card choice after battle

So the correct way to describe the current implementation is:

> "The factory scene is playable now. The combat scene is not part of this slice yet."

---

## The resource system in plain English

There are two layers of elements:

- **Basic elements**
  - Fire
  - Water
  - Wind
  - Earth

- **Secondary elements**
  - Ice = Wind + Water
  - Thunder = Wind + Fire
  - Light = Earth + Fire
  - Dark = Earth + Water

In gameplay terms:

- spirit nodes create elements automatically
- factory nodes consume those elements
- the output becomes stronger spell cards

---

## The node types

### 1. Spirit nodes

Spirit nodes are the starting point of the line.

They continuously generate elemental resources.

Current default spirits:

- Fire Spirit
- Water Spirit
- Wind Spirit
- Earth Spirit

Reward-unlocked spirits also exist for:

- Ice
- Thunder
- Light
- Dark

## What you see at the start

When the generated scene first opens, the starter layout is already producing spells.

The opening layout demonstrates two working lines:

- **Top lane**
  - `Fire Spirit -> Arcane Conduit -> Element Shaper -> Spell Fusion I -> Spell Conduit -> Battle Deck Collector`
  - `Fire Spirit -> vertical Arcane Conduit -> rotated Element Shaper -> Spell Fusion I`
  - result: `Inferno Brand`

- **Lower lane**
  - `Water Spirit -> Arcane Conduits -> Element Fusion`
  - `Wind Spirit -> Element Fusion`
  - `Element Fusion -> Element Shaper`
  - result: `Frost Pin`

You also begin with these nodes unlocked in the palette:

- Fire Spirit
- Water Spirit
- Wind Spirit
- Earth Spirit
- Arcane Conduit
- Turning Conduit
- Spell Conduit
- Turning Spell Conduit
- Battle Deck Collector
- Element Shaper
- Element Fusion
- Spell Fusion I

You do **not** begin with every factory unlocked.

These still start locked:

- `Spell Fusion II`
- `Spell Fusion III`

To try the default fusion features:

1. open `WorkshopScene`
2. press `Advance 1 Prep Tick` several times, or let time run
3. select `Spell Conduit` to see cards moving through the lane
4. `Inferno Brand` proves Spell Fusion I is working
5. `Frost Pin` proves Element Fusion plus Element Shaper is working
6. watch the right-side `Battle Deck`; it only includes cards that reached `Battle Deck Collector`

To see the higher fusion tiers:

1. open the boon drawer
2. unlock the factory you want to test
3. place it on the grid
4. route the correct inputs into it

---

### 2. Arcane Conduit

This is the element relay node.

Use it to carry element resources forward through the line. It does not accept spell cards.

### 2a. Turning Conduit

This is the L-shaped element relay node.

Its default facing accepts an element from the east side and outputs north. Rotate it to make the other corner directions. It follows the same resource-only rules as `Arcane Conduit`.
Right click its palette card to arm the mirrored west-to-north version without cluttering the palette with a second card.

### 2b. Spell Conduit

This is the spell-card relay node.

Use it after `Element Shaper` or `Spell Fusion` when you want cards to visibly travel through a card lane toward a `Battle Deck Collector`. It does not accept element resources.

### 2c. Turning Spell Conduit

This is the L-shaped spell-card relay node.

Its default facing accepts a spell card from the east side and outputs north. Rotate it to make the other corner directions. It follows the same card-only rules as `Spell Conduit`.
Right click its palette card to arm the mirrored west-to-north version.

### 2d. Battle Deck Collector

This is the spell-card collection point.

Only cards routed into this block are counted by the battle deck. Cards sitting in a spell fusion machine or spell conduit buffer are visible for debugging, but they are not deployed until they reach a collector.

### 3. Element Fusion

This factory combines two different, non-opposing **basic** elements into one **secondary** element.

Examples:

- Wind + Water -> Ice
- Wind + Fire -> Thunder
- Earth + Fire -> Light
- Earth + Water -> Dark

### 4. Element Shaper

This factory turns **one element** into **one basic spell card**.

That means elements are the raw material, and the shaper turns them into usable spell cards.

### 5. Spell Fusion I

This is the first spell fusion tier.

It combines:

- two basic spells of the same element

Example:

- Fire spell + Fire spell -> stronger Fire spell

### 6. Spell Fusion II

This is the second spell fusion tier.

It combines:

- two compatible intermediate spells produced by `Spell Fusion I`

Example:

- Inferno Brand + Tide Chorus -> Steam Requiem

This factory now sits after `Spell Fusion I`. Feed it intermediate spell cards produced by `Spell Fusion I`.

### 7. Spell Fusion III

This is the final spell fusion tier.

It combines:

- two matching advanced spells produced by `Spell Fusion II`

Example:

- Steam Requiem + Steam Requiem -> final steam spell

---

## Step-by-step placement examples

These examples describe the intended way to place machines in the current workshop slice.

### Example A: Make a basic Fire spell

Goal:

- turn Fire into a basic spell card

Placement idea:

`Fire Spirit -> Arcane Conduit -> Element Shaper`

What to do:

1. place a `Fire Spirit`
2. place an `Arcane Conduit` directly to its right
3. place an `Element Shaper` to the right of the conduit
4. make sure all three face along the same line

What happens:

- `Fire Spirit` produces `Fire`
- the conduit passes `Fire` forward
- `Element Shaper` consumes `Fire`
- the shaper produces a basic Fire spell card

### Example B: Make Ice from Wind + Water

Goal:

- combine two basic elements into one secondary element

Placement idea:

```
Wind Spirit  -> [Element Fusion]
Water Spirit -> [Element Fusion]
```

Important:

- the fusion factory needs two valid inputs
- those inputs must enter from sides the machine accepts
- **direction has to match**

What to do:

1. place `Wind Spirit`
2. place `Water Spirit`
3. place `Element Fusion` so it can receive both lines
4. rotate the machine until both feeds enter valid input sides
5. place a conduit or shaper on its output side if you want to keep using the result

What happens:

- `Wind + Water -> Ice`

### Example C: Make an Ice spell

Goal:

- turn a fused secondary element into a spell card

Placement idea:

`Wind Spirit + Water Spirit -> Element Fusion -> Element Shaper`

What to do:

1. build Example B first
2. place `Element Shaper` on the output side of `Element Fusion`
3. make sure the shaper faces the incoming line

What happens:

- `Element Fusion` creates `Ice`
- `Element Shaper` consumes `Ice`
- the shaper produces a basic Ice spell card

### Example D: Make a stronger same-element spell

Goal:

- fuse two basic spells of the same element

Placement idea:

`Element Shaper -> Spell Fusion I`

What to do:

1. unlock `Spell Fusion I`
2. produce a steady flow of one basic spell type
3. route those basic cards into `Spell Fusion I`
4. keep the input line stable so the machine can collect pairs

What happens:

- two matching basic spells become one intermediate spell

### Example E: Make a mixed compatible spell

Goal:

- combine two different but non-opposing basic spells

Example pair:

- `Inferno Brand + Tide Chorus`

What to do:

1. unlock `Spell Fusion II`
2. produce both source intermediate spells through `Spell Fusion I`
3. route both lines into the same `Spell Fusion II`
4. keep both lines supplied

What happens:

- the factory consumes the compatible pair
- it outputs an advanced spell

### Example F: Make a final spell

Goal:

- combine matching advanced spells

Example pair:

- `Steam Requiem + Steam Requiem`

What to do:

1. unlock `Spell Fusion III`
2. first build the earlier lines that produce the advanced spell through `Spell Fusion II`
3. route the advanced output into `Spell Fusion III`

What happens:

- the machine consumes two matching advanced cards
- it produces one final advanced spell card

---

## What the player is actually trying to do

The workshop is not just about placing random machines.

The player goal is to build a **production line**.

That line should:

1. generate elements fast enough
2. deliver the right elements to the right machines
3. avoid blocking itself
4. keep producing finished spell cards over time

The more efficient the layout is, the more battle cards the player can prepare.

---

## How outputs move

Every node has a direction.

That direction matters because items only move when:

1. the current node outputs toward the next cell
2. the next node accepts input from that side
3. the next node accepts that item type
4. the next node still has room

This means placement and rotation both matter.

If the line is facing the wrong direction, nothing useful will move.

Simple rule:

- the arrow of the previous machine must point into the next machine
- the next machine must have an input on that side

Example:

- `Fire Spirit -> Arcane Conduit -> Element Shaper` works
- `Fire Spirit` facing away from the conduit does **not** work

---

## What happens to finished spell cards

Spell cards do not always go straight to the player immediately.

Current rule:

- if a spell card still has a valid downstream machine, it can continue through the line
- if it reaches a normal machine or conduit with no valid output, it stays in that block's buffer
- only cards delivered into a `Battle Deck Collector` are counted in the workshop's prepared battle deck

That is how the factory scene feeds the battle scene later.

---

## What the current spell cards contain

Every produced spell card now carries gameplay metadata for later battle use:

- spell name
- element
- tier
- role
  - Attack
  - Healing
  - Defense
- rarity band
- primary effect value
- hit count
- secondary effect value
- effect keyword

This is how the current code maps to the card rules you defined.

### Important note

`Card artwork` is **not implemented yet**.

The cards use placeholder colors and metadata only.

---

## Current Card List

The battle side consumes the spell metadata exported from the workshop payload.
The table below reflects the current generated content in `WorkshopProjectBootstrap.cs`, so names and numbers may still change during balance passes.

`Produced From` lists the main production path for each card, not every alternate recipe branch.
Battle now consumes `PrimaryValue`、`HitCount`、`SecondaryValue` and `EffectKeyword` through generated per-card definitions and runtime status handling.
The exact keyword semantics are still current-implementation rules rather than final long-term balance design, so timing, scaling, and wording may continue to evolve.

| Card | Tier | Element | Role | Effect | Keyword | Produced From |
| --- | --- | --- | --- | --- | --- | --- |
| `Cinder Dart` | Basic | Fire | Attack | Deal 8 damage. SecondaryValue: 1. | `Burn` | `Element Shaper` from `Fire` |
| `Tidal Mend` | Basic | Water | Healing | Restore 6 HP. SecondaryValue: 8. | `Regen` | `Element Shaper` from `Water` |
| `Zephyr Cut` | Basic | Wind | Attack | Deal 5 damage x2. SecondaryValue: 10. | `Expose` | `Element Shaper` from `Wind` |
| `Stoneguard Sigil` | Basic | Earth | Defense | Gain 7 shield. SecondaryValue: 18. | `Bulwark` | `Element Shaper` from `Earth` |
| `Frost Pin` | Basic | Ice | Attack | Deal 4 damage x2. SecondaryValue: 20. | `Slow` | `Element Shaper` from `Ice` |
| `Volt Javelin` | Basic | Thunder | Attack | Deal 7 damage. SecondaryValue: 15. | `Shock` | `Element Shaper` from `Thunder` |
| `Lumen Prayer` | Basic | Light | Healing | Restore 5 HP x2. SecondaryValue: 12. | `Bless` | `Element Shaper` from `Light` |
| `Gloam Ward` | Basic | Dark | Defense | Gain 6 shield. SecondaryValue: 20. | `Veil` | `Element Shaper` from `Dark` |
| `Inferno Brand` | Intermediate | Fire | Attack | Deal 14 damage x2. SecondaryValue: 2. | `Burn` | `Spell Fusion I` from `Cinder Dart + Cinder Dart` |
| `Tide Chorus` | Intermediate | Water | Healing | Restore 11 HP x2. SecondaryValue: 14. | `Regen` | `Spell Fusion I` from `Tidal Mend + Tidal Mend` |
| `Razor Monsoon` | Intermediate | Wind | Attack | Deal 8 damage x3. SecondaryValue: 12. | `Expose` | `Spell Fusion I` from `Zephyr Cut + Zephyr Cut` |
| `Bastion Pulse` | Intermediate | Earth | Defense | Gain 12 shield. SecondaryValue: 28. | `Ward` | `Spell Fusion I` from `Stoneguard Sigil + Stoneguard Sigil` |
| `Glacier Bind` | Intermediate | Ice | Attack | Deal 9 damage x2. SecondaryValue: 24. | `Freeze` | `Spell Fusion I` from `Frost Pin + Frost Pin` |
| `Stormbreaker` | Intermediate | Thunder | Attack | Deal 16 damage. SecondaryValue: 18. | `Stun` | `Spell Fusion I` from `Volt Javelin + Volt Javelin` |
| `Dawn Benediction` | Intermediate | Light | Healing | Restore 9 HP x3. SecondaryValue: 18. | `Radiance` | `Spell Fusion I` from `Lumen Prayer + Lumen Prayer` |
| `Umbral Bastion` | Intermediate | Dark | Defense | Gain 10 shield x2. SecondaryValue: 24. | `Shade` | `Spell Fusion I` from `Gloam Ward + Gloam Ward` |
| `Steam Requiem` | Advanced | Fire | Attack | Deal 20 damage x2. SecondaryValue: 28. | `Scald` | `Spell Fusion II` from `Inferno Brand + Tide Chorus` |
| `Worldsplit Tempest` | Advanced | Wind | Attack | Deal 12 damage x3. SecondaryValue: 20. | `Rend` | `Spell Fusion II` from `Razor Monsoon + Bastion Pulse` |
| `Eclipse Covenant` | Advanced | Light | Healing | Restore 14 HP x3. SecondaryValue: 24. | `Radiance` | `Spell Fusion II` from `Dawn Benediction + Umbral Bastion` |
| `Absolute Zero Surge` | Advanced | Ice | Defense | Gain 16 shield x2. SecondaryValue: 35. | `Static Shell` | `Spell Fusion II` from `Glacier Bind + Stormbreaker` |
| `Boiling Star Requiem` | Advanced | Fire | Attack | Deal 30 damage x2. SecondaryValue: 36. | `Scald` | `Spell Fusion III` from `Steam Requiem + Steam Requiem` |
| `Heavenbreaker Tempest` | Advanced | Wind | Attack | Deal 18 damage x4. SecondaryValue: 28. | `Rend` | `Spell Fusion III` from `Worldsplit Tempest + Worldsplit Tempest` |
| `Eclipse Apotheosis` | Advanced | Light | Healing | Restore 20 HP x4. SecondaryValue: 32. | `Radiance` | `Spell Fusion III` from `Eclipse Covenant + Eclipse Covenant` |
| `Zero Point Citadel` | Advanced | Ice | Defense | Gain 24 shield x3. SecondaryValue: 42. | `Static Shell` | `Spell Fusion III` from `Absolute Zero Surge + Absolute Zero Surge` |

---

## Workshop Recipe Outputs

This table lists exactly what the workshop machines should make.
If the game only produces `Cinder Dart` and `Tidal Mend`, the workshop content database is stale or the machine inputs are not routed into the correct ports.

### Element Fusion

| Machine | Inputs | Output |
| --- | --- | --- |
| `Element Fusion` | `Wind + Water` | `Ice` |
| `Element Fusion` | `Wind + Fire` | `Thunder` |
| `Element Fusion` | `Earth + Fire` | `Light` |
| `Element Fusion` | `Earth + Water` | `Dark` |

Invalid pairs stay in the `Element Fusion` buffer and do not output anything.
For example, `Wind + Earth` is not a current Element Fusion recipe, so it should not produce a secondary element and it should not leak `Wind` or `Earth` into a downstream conduit.

If more than two valid elements are buffered, `Element Fusion` resolves every possible recipe in recipe-list order as cycle time becomes available. For example, a buffer with `Wind x2`, `Water x1`, and `Fire x1` produces `Ice` first, then `Thunder`.

### Element Shaper

| Machine | Input | Output Spell |
| --- | --- | --- |
| `Element Shaper` | `Fire` | `Cinder Dart` |
| `Element Shaper` | `Water` | `Tidal Mend` |
| `Element Shaper` | `Wind` | `Zephyr Cut` |
| `Element Shaper` | `Earth` | `Stoneguard Sigil` |
| `Element Shaper` | `Ice` | `Frost Pin` |
| `Element Shaper` | `Thunder` | `Volt Javelin` |
| `Element Shaper` | `Light` | `Lumen Prayer` |
| `Element Shaper` | `Dark` | `Gloam Ward` |

### Spell Fusion I

`Spell Fusion I` combines two copies of the same basic spell into the matching intermediate spell.
It can receive those cards through `Spell Conduit` routing or directly from a neighboring producer if that producer is outputting into the fusion machine.

| Inputs | Output Spell |
| --- | --- |
| `Cinder Dart + Cinder Dart` | `Inferno Brand` |
| `Tidal Mend + Tidal Mend` | `Tide Chorus` |
| `Zephyr Cut + Zephyr Cut` | `Razor Monsoon` |
| `Stoneguard Sigil + Stoneguard Sigil` | `Bastion Pulse` |
| `Frost Pin + Frost Pin` | `Glacier Bind` |
| `Volt Javelin + Volt Javelin` | `Stormbreaker` |
| `Lumen Prayer + Lumen Prayer` | `Dawn Benediction` |
| `Gloam Ward + Gloam Ward` | `Umbral Bastion` |

### Spell Fusion II

`Spell Fusion II` combines compatible intermediate spell pairs produced by `Spell Fusion I` into advanced spells.
It follows the same direct-adjacent handoff rule as `Spell Fusion I`, so a neighboring spell-fusion factory or spell conduit can feed it when that node outputs toward the fusion machine.

| Inputs | Output Spell |
| --- | --- |
| `Inferno Brand + Tide Chorus` | `Steam Requiem` |
| `Razor Monsoon + Bastion Pulse` | `Worldsplit Tempest` |
| `Dawn Benediction + Umbral Bastion` | `Eclipse Covenant` |
| `Glacier Bind + Stormbreaker` | `Absolute Zero Surge` |

### Spell Fusion III

`Spell Fusion III` combines matching advanced spell pairs produced by `Spell Fusion II` into final advanced cards.
It also supports direct adjacent feeds from neighboring spell-fusion producers or spell conduits.
Like the earlier spell-fusion machines, it outputs east into a card lane, so a `Spell Conduit` can visibly hold its final cards.

| Inputs | Output Spell |
| --- | --- |
| `Steam Requiem + Steam Requiem` | `Boiling Star Requiem` |
| `Worldsplit Tempest + Worldsplit Tempest` | `Heavenbreaker Tempest` |
| `Eclipse Covenant + Eclipse Covenant` | `Eclipse Apotheosis` |
| `Absolute Zero Surge + Absolute Zero Surge` | `Zero Point Citadel` |

---

## Controls

Current factory controls:

- `Left Click empty tile`: place the armed palette node
- `Left Click occupied tile`: select the placed node without replacing it
- `Left Click spell-fusion edge`: cycle that edge through input, output, and off
- `Hold Left Click + Drag`: pan the workshop map
- `Right Click`: remove a node
- `R`: rotate selected placed node
- `Q / E`: rotate placement direction
- `Hover tile + T`: show the hovered block buffer contents
- `Mouse Wheel`: zoom the workshop map in / out
- `Space`: pause / resume time
- `Tab`: open / close boon drawer
- `H`: debug-load a complete endgame factory, warm it for inspection, and reset the preparation timer to the 3500-tick demo factory budget. The hack layout demonstrates Element Fusion plus four final-card routes. Each `Spell Fusion III` receives two independent `Spell Fusion II` branches and routes final cards into `Battle Deck Collector` blocks.
- `F1`: show / hide control guide
- `Esc`: open the return-to-menu confirmation. `No` or `Esc` cancels; `Yes` abandons the current run without saving workshop progress.

---

## What teammates should assume

This workshop slice is a **playable factory prototype**, not final content.

Teammates should assume these parts may still change:

- spell names
- exact recipe numbers
- rarity weights
- effect values
- UI layout
- reward sequencing

Teammates should **not** assume these parts are unstable:

- factory scene owns production logic
- factory exports a battle payload
- battle should read the payload instead of inspecting workshop scene objects directly
- spirit -> element -> spell -> fused spell is the intended content flow

---

## Short version for non-technical teammates

If someone asks "How do I play Edward's part?", the simplest answer is:

> Place spirit nodes to make elements.  
> Route those elements into factories.  
> Use factories to turn elements into spells.  
> Use higher-tier factories to fuse spells into stronger ones.  
> Let finished spell cards collect into your deck for battle.

---

## Current status

This document describes the **current workshop implementation**.

It is meant to help teammates understand the slice today.
It is **not** a promise that names, values, or UI details are final.

If the scene content and this guide ever disagree, rebuild the generated workshop content first and treat that generated scene/database as authoritative.
