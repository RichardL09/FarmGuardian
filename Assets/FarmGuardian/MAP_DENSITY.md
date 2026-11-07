# Farm Guardian — dense farm using the supplied assets

The first scene is again `Assets/FarmGuardian/Scenes/FarmGuardian.unity` and is the enabled build scene. The separate FarmGuardianRedesign folder, scene, assets, test assembly and presentation hooks were removed. Original player/burglar rigs, animal animations, UI, level configuration, save data, interaction timing, progression and thief state machine are retained.

## Asset inspection and actual sources

No downloads were used. Imported source models were inspected in Unity, including mesh bounds, vertex counts and materials. The farm ZIP was inspected without importing its contents; the existing town collection already supplies the needed environment pieces. The archive's generic OBJ object names were not guessed or renamed into fictional assets.

The environment source is **`Assets/UTS_Project/village-town-assets/source/PictureCrossTownsAssets.fbx`**. The names below are verified nodes within that FBX, not separate prefab files:

| Purpose | Supplied nodes used |
|---|---|
| Main landmarks | `Barn`, `Windmill`, `Well` |
| Farmhouses and village groups | `House_Blue`, `House_Purple`, `House_Red`, `House_2Story_Purple` |
| Vegetation | `Tree_Square`, `Tree_Tall`, `Tree_Pine`, `Daisy`, `Potted_Bush` |
| Fields | `Farm_Cabbage`, `Farm_Carrot`, `Farm_Wheat` |
| Farm storage | `Hay_Bale`, `Hay_Cart`, `Barrell`, `Crate` |
| Ground/yard detail | `Boulder`, `StonePath`, `Fence` |

`Barrell` is the asset author's actual spelling. A real `Well` model exists, so no replacement well was invented. The shared environment material remains `Assets/FarmGuardian/Materials/Town.mat`, using `Assets/UTS_Project/village-town-assets/textures/Gradients.png` through URP/Lit. Supplied mesh UVs and texture colors are retained. The existing farmer, burglar and cube-pet assets remain assigned to the original gameplay configuration.

There is no verified standalone grass-blade model in the imported town set. Low ground cover therefore uses the supplied Daisy foliage and small Tree_Square saplings over fresh green terrain; Tree_Pine and Tree_Tall add variation in the larger groves and boundary planting. Potted bushes remain near houses rather than being presented as a fictional wild-bush asset. No external grass pack or replacement character model was introduced.

## Composition

- The original authored main/alternative routes, player/animal/thief spawn points, response hub, escape zone and mission plot positions remain authoritative in FarmMapLayout.
- Compact farmhouses occupy safe interior pockets. Additional groups of houses, kitchen gardens, storage and trees form a village around the original fenced play area. Houses beyond the perimeter are background scenery, not new playable objectives or enterable interiors.
- A roofed well remains beside the main route, framed with supplied stones and flowers. StonePath branches link it and interior houses to nearby paths.
- All four original pen locations are visible from early levels. Empty pens are scenery for future animal types; they do not spawn extra animals or change level requirements. The south entrances remain open.
- Idle planting locations show ornamental crop beds. When a location becomes a real planting mission, that ornamental bed is hidden and the existing PlantingPlot owns its empty/planted state. Decoration never contributes to planted counts or rewards.
- Roadside flowers and low foliage follow path shoulders and garden islands. Trees have varied heights/shapes; village lots and landmark groves avoid a single repeated grid. Open roads, courtyards and interaction areas intentionally remain usable.
- Boundary collision stays at its original position/height. Its opaque cube renderers are replaced by the supplied wooden Fence mesh. Pen fence visuals also use that mesh while retaining the original continuous colliders.

## Saved scene and runtime

The scene contains **Farm Environment - saved Edit Mode view**, with actual meshes and scenery visible before Play. FarmScenePreview disables this authoring view at runtime; FarmWorld builds the selected theme using the same baked scenery assets. This avoids double-rendering or duplicate colliders/actors. The original FarmSession remains the only game manager.

Four scenery prefabs live in `Assets/FarmGuardian/Prefabs/Environment/`. Their combined meshes are stored as subassets in one `Assets/FarmGuardian/Resources/FarmScenery.asset`, rather than creating hundreds of individual mesh files. Runtime theme rotation remains the existing 15-level progression.

Regenerate with **Tools > Farm Guardian > Maps > Build Dense Farm In First Scene**, outside Play Mode. Save unrelated scene work first. This updates the original scene and scenery; it does not create another gameplay scene. The older Build Game command rebuilds the bootstrap; if used, run the dense-map command afterwards to restore its saved Edit Mode environment.

## Collision and mobile considerations

Large interior props and trees are placed only after checking authored route clearances, pen entrances, every potential planting location, all animal wandering origins, player spawn, escape and thief entrances. New interior solids use simple box/trunk colliders on the existing solid layer and NotWalkable modifiers. Flowers, small foliage, field detail and outside-village scenery have no extra gameplay collision. The existing NavMesh is built after scenery is instantiated, and required destinations are validated.

Small meshes are combined by material, category and approximately 9m spatial cell, preserving useful camera culling. Empty placement wrappers are removed. Tiny ground detail does not cast shadows; trees and buildings use the single daylight source. One shared town material/atlas is used for environment props. Source mesh Read/Write is enabled only during baking and restored afterwards. Daylight uses a warm Directional Light, ambient lighting and a procedural engine skybox. The Mobile pipeline retains its existing 1024 main-light shadows and hard-shadow support; PC retains its existing soft-shadow support.

This is an Editor-validated mobile-oriented layout, not a measured device performance guarantee. The four baked theme meshes still consume memory, and target-device frame time, peak memory and thermal behavior require profiling. No Android/iOS performance figures are claimed.

## Validation evidence

See project-root `Logs/FarmGuardian-scenery-built.txt` for final source-instance/renderer/house/tree counts and exact node names, and `Logs/FarmGuardian-scenery-tests.xml` for executed test results. Source placements are counts before batching, not claims of that many live renderers.

The existing gameplay/map/UI regression suite and two scenery cases check original asset use, original scene selection, preview isolation, four-map reachability, pen access, animal return, random thief entrances, physical chase/interception, actual escape, catch/stun/planting, pause, rewards, retries, purchasing and UI navigation. Saved progress in the user's project is not used for the run: validation uses a separate company namespace in a temporary copy.

Runtime captures include five gameplay-camera positions plus an overview for each theme, named `Logs/FarmGuardian-dense-[0–3]-view[0–4].png` and `...-overview.png`. View 0 includes the existing HUD; the other inspection views hide it to expose scenery. Final executed results and any remaining limitations are reported after checking those files.

### Executed final checks — 16 October 2026

Unity PlayMode completed **19/19 tests passed, 0 failed**, in 83.6124594 seconds (report end: 08:34:15 UTC). All four layouts were rendered and 24 captures retained in the main project's Logs folder. Gameplay-camera and overview inspections show the well, farm buildings, pens, vegetation and surrounding village; roads and pen entrances remain intentionally open.

| Theme | Source placements before batching | Scenery renderer chunks | Houses, including background village | Interior trees |
|---|---:|---:|---:|---:|
| Countryside | 1043 | 143 | 22 | 8 |
| Woodland | 976 | 136 | 19 | 4 |
| Mountain | 955 | 138 | 18 | 11 |
| Advanced | 904 | 134 | 18 | 8 |

Renderer counts exclude the existing actors, UI, core landmarks and fences. Interior tree counts exclude boundary and background trees. Advanced Farm keeps its validated interior tree topology: adding extra large trees altered the baked chase corridor, so those additions were removed before the final passing run. No movement speed, chase rule or navigation test tolerance was changed to obtain the result.

Implementation sources, the saved original scene, scenery prefabs and resource data were compared against the tested copy before removing that temporary copy. The supplied source models remain in place. No physical Android/iOS build, device frame-rate profiling or thermal test was performed. The houses beyond the original perimeter remain decorative, and the original simple path surfaces and gameplay markers remain visible; this work does not claim a replacement terrain system or enterable village.
