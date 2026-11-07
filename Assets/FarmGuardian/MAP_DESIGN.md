# Farm Guardian — strategic map plans

These plans are authored before assembling the new 3D layouts. Coordinates are metres `(x,z)`; north is `+z`, the playable fence is at x ±21 / z ±17, and the ground is 44 × 36 m. All four layouts use the supplied town FBX and its Gradients atlas. No bridge model was found: the mountain crossings are explicitly built from simple timber gameplay geometry.

The authoritative placement plan is `Scripts/FarmMapLayout.cs`. `Docs/Maps/*.svg` are scaled top-down diagrams exported from those same definitions, including footprints, route widths, pen entrances, all eight potential animal spawns, three potential planting plots, response hubs and thief entrances. Slots are activated according to the mission; these are four intentionally authored maps, not random prop scattering. Randomness selects only reachable thief entrances and local animal wandering.

## 1. Countryside Farm — levels 1–15

```text
N  Barn       A5 ===== northern return loop ===== A6    Windmill
   west lane          [COW][PIG][CHICK][BUNNY]
   A3 / hay cart -------- wide pen approach -------- well
   |             hay       H ===== main lane ======= |
   |                 F1      F2                     A4
   A7 ---- A1 -------- P -------- A2 ---- shortcut --- E
S
```

- Player `(0,-11)`; escape `(17,-13)`; response hub `(0,-2)`.
- Pen centres `(-6,6),(-2,6),(2,6),(6,6)`, added as species unlock. All mouths face south, with 3.45 m clear entrances. Early levels share the central rescue hub.
- Animals: `(-5,-9),(7,-10),(-13,-1),(13,-5),(-9,13),(7,13),(-16,-12),(15,1)`. Near animals teach capture; northern animals require going around the pens.
- Plots: `(-4,-5),(5,-5),(-12,1)`. The first two are beside the main loop; the third rewards a combined west rescue/plant trip.
- Main routes are 3.6 m wide: south spawn to the response hub, east to the exit approach, and a broad south-facing pen apron. A northern return loop and southwest lane provide alternatives. The southeast diagonal lets the player cut toward an escaping thief.
- Barn `(-15,8)` and windmill `(15,10)` are readable landmarks. Well, hay bale, cart and a short west fence cause small, visible detours without turning the tutorial into a maze. Sparse perimeter trees retain an open middle.
- Challenge: prioritize near/far rescues and recognize the exit; interception happens at the east lane or southeast shortcut.

## 2. Woodland Ranch — levels 16–30

```text
N  A5 ===== north loop ==== A6 ===== A8
   [COW]    [PIG]                 [BUNNY]
   |              well       [CHICK]       A4
   F3 ---- west return ----- H ----- east return
   Barn     PINE GROVE    /     PINE GROVE
   |           fence    /          |
   A7 == west bypass == P == A1 === A2 ==== E
S
```

- Player `(0,-12)`; escape `(17,-13)`; hub `(1,-3)`.
- Pens `(-12,7),(-6,11),(6,6),(13,10)` are separated rather than sharing one apron. Mouths remain south-facing.
- Animals: `(-3,-10),(12,-10),(-12,-2),(17,6),(-16,13),(2,12),(-16,-12),(9,14)`.
- Plots: `(-3,-2),(11,-8),(-16,1)` link west planting to the barn rescue, while the eastern plot tempts the player away from western pens.
- Two dense pine/undergrowth islands at `(-7,-7)` and `(8,-4)`, plus a short fence at `(-1,-6)`, turn the main route into a winding S. The undergrowth is visible solid geometry, not an invisible forest wall. The west perimeter and east outer loop bypass these islands.
- Main width 3.2 m; alternatives 2.8 m or wider. The barn is southwest, the windmill east, and the well north of the central hub. Tree silhouettes distinguish this map from the open first map.
- Challenge: choose an inner S route or an outer bypass, group nearby species/plots, and return through the central clearing to intercept toward the southeast exit.

## 3. Mountain Farm — levels 31–45

```text
N  A5 ===== [COW] [PIG]  ###   A6 [CHICK] [BUNNY]
   west upper approach === NORTH PASS === east apron
   Barn                  ###                 A8
   A3       well         ###     boulder    Windmill
   |        boulder      ###
   F1 ===== west junction == SOUTH CAUSEWAY == F2
   P == A1 ===== A2       ###    A4 ===== A7 ==== E
S
```

- Player `(-14,-11)`; escape `(17,-13)`; hub `(-6,-3)`.
- Pens `(-13,9),(-8,9),(8,11),(14,11)` split the rescue into western and eastern terraces. The play surface stays level: the supplied assets do not include validated terrain, cliff collision or climbing animations.
- Animals: `(-11,-10),(-5,-13),(-15,-3),(6,-11),(-17,13),(5,14),(17,-5),(17,6)`.
- Plots: `(-7,-9),(7,-3),(-14,5)` force a decision about when to cross the ridge.
- A continuous rocky divider along x=0 has exactly two 4.4 m openings in the rock, centred on z=-6 and z=7. Both crossings have a 3.4 m timber deck and visible low side rails, leaving 3.82 m between rail colliders. The deck is level with the ground and has no raised lip or off-mesh link.
- Main route crosses the south causeway; the northern pass offers a shorter rescue trip between the two pen groups. Barn west, windmill east, and a distinctive distant mountain silhouette help orientation. Boulder islands influence approach routes away from the crossing mouths.
- Challenge: choose a crossing before committing to a rescue or planting trip. During a theft, the player can cut south while the thief still leaves a northern pen. Narrower passages remain wide enough for opposing actors; no one-lane bottlenecks or jumps are required.

## 4. Advanced Farm — levels 46–60

```text
N  A5 == [COW] [PIG] == north crossover == [CHICK] [BUNNY] == A6
   west service lane       | fence |          east service lane
   F3 ===== inner apron == |       | == inner apron
   A3   [WEST BARN]         H       [EAST BARN]   A4
   |       storage          |          storage      |
   | ===== west fence ===  spine  === east fence ===|
   E == A7 ==== A1 ========= P ===== A2 == F2 =======|
S
```

- Player `(0,-12)`; escape `(-17,-13)`; hub `(0,-2)`. The exit moves west, requiring the player to relearn interception rather than reuse the southeast chase.
- Pens `(-13,9),(-6,10),(6,10),(13,9)` occupy two northern yards.
- Animals: `(-2,-10),(12,-11),(-13,0),(13,0),(-17,13),(17,13),(-16,-11),(3,13)`.
- Plots: `(-3,3),(14,-9),(-15,4)` create competing inner, southeast and northwest objectives.
- Barn footprints `(-8,0),(8,0)` divide inner and outer routes. Storage fences at `(-8,-7),(8,-7)` force the player through the broad central spine or around the outer ends. A north/south divider `(0,8)` can be bypassed at z=2 or z=14. The outer ring reconnects every yard.
- Main routes are 3.2 m, alternatives at least 2.8 m. Wells, hay storage and a windmill landmark sit away from junction mouths. Barn footprints and fences never enclose an objective with a single sealed entrance.
- Challenge: combine missions across four yards and decide whether to protect the central response position or take the longer outer rescue route. The western service lane is a second way to intercept the new exit.

## Shared hierarchy, collision and navigation

Each theme builds the following named hierarchy; the names of the route, obstacle and landmark groups come directly from its plan:

```text
Farm Guardian - <theme>                    [NavMeshSurface]
  Environment
    Ground                                [one BoxCollider, layer 8]
    Boundaries                            [four BoxColliders, layer 8]
    Paths / Main routes / Alternative routes [flat visual strips, no colliders]
    Decorations                           [trees and distant scenery]
    StrategicObstacles                    [named fences, groves, rock ridges]
    Crossings                             [decks with no lip; solid side rails]
  FarmBuildings                           [source visuals + simple BoxColliders]
  AnimalPens / <species> Pen               [three fences; open south entrance]
  PlantingPlots / Planting plot N          [visual soil/crops, PlantingPlot]
  PlayerSpawn
  AnimalSpawnPoints / Animal spawn N
  ThiefEntrances / Entrance N
  ResponseHub
  ThiefEscapeZone                          [visual marker; distance-based arrival]
  InteractiveObjects / Player, Animals, Burglar
```

- Layer 8 (`FarmObstacles`) is the only NavMesh bake input. Ground, boundaries, obstacle footprints and pen fences participate. Decorations with no collision, path paint, labels, actors and crops are excluded. All raised obstacle tops are marked Not Walkable through a NavMeshModifier; no animals spawn on barns or boulders.
- NavMeshSurface: Humanoid type 0, Children, PhysicsColliders, voxel 0.12 m, tile 128. Project's bake radius 0.5 m/height 2 m is more conservative than actor radius 0.28 m and player radius 0.32 m. Keep at least 2.8 m authored route widths, 3.45 m pen mouths and 4.4 m mountain openings.
- Player uses CharacterController (radius 0.32, height 1.8, step 0.25). Thief/animals use NavMeshAgent radius 0.28, height 1.6. Paths are not colliders. No moving doors, dynamic carved obstacles or off-mesh links are required.
- Each animal wanders only within 2.4 m of its authored spawn, accepting complete same-region paths. Captured animals navigate to their original pen; thief candidates must have complete routes to all homes and the exit, remain ≥8 m from the player and ≥6 m from homes.
- Every build validates exact spawn/objective placement against the NavMesh and verifies complete routes for player-to-objectives, spawn-to-home, home-to-exit and all authored route segments. Invalid placements fail clearly rather than silently moving an animal several metres through a wall.
- Escape routes must give at least a catch hold plus reaction time at configured maximum thief speed. Tests evaluate points along every home-to-exit route against base-speed player travel from the response hub, including a full catch hold and at least one second of remaining margin. Live interception tests also move the actual CharacterController after the thief takes an animal. This checks a fair response position, not a guarantee of winning from any location after ignoring the thief.

## Build and edit in Unity

1. Open `Assets/FarmGuardian/Scenes/FarmGuardian.unity`. Allow script import to finish.
2. Use **Tools > Farm Guardian > Maps > Export Layout Diagrams** to regenerate the four SVG plans from `FarmMapLayout` before reviewing placements.
3. Press Play and select levels 1/16/31/46 (unlock through normal progression, or change the existing test save deliberately). For editor inspection without changing the save, use **Tools > Farm Guardian > Maps > Preview > <theme>** in Edit Mode. This creates an unsaved separate preview scene with all species/plots, not a change to the gameplay scene. Generated unsaved previews close automatically before entering Play Mode or opening another preview, keeping their NavMesh and actors out of gameplay. Make permanent layout changes in the plan source rather than editing this disposable preview.
4. Edit coordinates/routes/footprints in `FarmMapLayout.cs`; keep all objective slots inside the boundaries. Rerun export and tests. Runtime layout assembly happens once at level start; no prop placement runs every frame.
5. Run **Tools > Farm Guardian > Validate Configuration**, then Play Mode tests. The map tests check all four rotations, reachability, actual animal travel, actual thief escape/interception and controller boundary collision. Inspect top-down and normal camera previews in `Logs`.
6. Play every transition pair 15/16, 30/31, 45/46 and 60/61 with zero upgrades on a physical landscape phone. Check visibility, two-way traffic, chase reaction time, timer balance and camera occlusion. Adjust coordinates after these human tests; automated travel is not a substitute for difficulty testing.

With the existing endless progression, the four-layout rotation repeats at level 61 (and every 60 levels by default). No additional fifth theme or unsupported terrain asset is assumed.

## Mobile cost and references

Maps share the supplied atlas/materials and source meshes; obstacles use primitive colliders, actor counts remain at eight, and ambient trees are placed in small fixed groups. Rounded road junctions are combined into one small generated mesh per named route, rather than one renderer/collider per strip. Fences have visible posts and two open rails backed by one continuous BoxCollider per section. Generated road meshes/materials are released on map clear. No real-time shadows are added. Collision and bake happen at level construction, not during a chase. Source model visuals are fitted inside building/rock footprints; tree canopy may overhang above the collidable undergrowth. Measure loading time, draw calls and frame time on target phones before assigning a performance target.

- [Unity AI Navigation 2.0 — NavMeshSurface](https://docs.unity.cn/Packages/com.unity.ai.navigation%402.0/manual/NavMeshSurface.html): collider-only collection and bake accuracy settings.
- [Unity 6000.5 — tile size](https://docs.unity.com/en-us/engine/6000.5/script-reference/unityengine/ai/navmeshbuildsettings/tilesize): runtime bake tile-size tradeoff.
- [Unity mobile graphics guidance](https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets): shared geometry/materials and measurement on target hardware.
