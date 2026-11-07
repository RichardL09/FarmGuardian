Latest layout: **[QUADRANT_LAYOUT.md](QUADRANT_LAYOUT.md)** documents four separated pen sections, confined secured-animal wandering, relocated village buildings/windmill and actual grass blades. **[GATES_AND_ENVIRONMENT.md](GATES_AND_ENVIRONMENT.md)** covers gates, labels, carrying motion, stun recovery, exit entry and environment motion.

Character animation: **[CHARACTER_ANIMATORS.md](CHARACTER_ANIMATORS.md)** describes the Farmer/Burglar Animator Controllers, editable clips and gameplay parameters.

Current gameplay: see **[CARRY_AND_DIFFICULTY.md](CARRY_AND_DIFFICULTY.md)** for manual carry/delivery, species difficulty, closer camera, daylight shadows and the 100-level cap. Main Menu idle animation has been removed.

# Farm Guardian

Playable Unity 6000.5.9f1 / URP / Input System implementation of the supplied Farm Guardian brief.

## Run

1. Open `Assets/FarmGuardian/Scenes/FarmGuardian.unity` and press Play.
2. Choose a level, select Play, read the introduction, and select Start Level.
3. If the generated scene is missing, wait for compilation and run **Tools > Farm Guardian > Build Game**. This creates the configuration, four animal Animator Controllers, four URP materials, bootstrap prefab, and scene, and makes the scene the enabled build entry. It keeps SampleScene but disables its build entry. Rebuilding preserves existing configuration values; it resets the generated bootstrap scene and prefab.

The original scene now also contains a saved, inspectable farm environment in Edit Mode. Its authoring view disables itself in Play Mode; the existing runtime world loads the matching dense scenery for the selected theme, without duplicating actors or objectives. See [MAP_DENSITY.md](MAP_DENSITY.md) for actual source assets, navigation safeguards and **Tools > Farm Guardian > Maps > Build Dense Farm In First Scene**. The separate redesign scene/folder has been removed.

## Controls

The main menu uses the latest supplied artwork in separate background, logo, interactive button and static character/animal composition. Original pictured lettering and placement are preserved. PLAY opens the winding farm-map level selection; Skins is available in the Shop. There is no bottom-left level counter. The wallet shows the actual saved balance. Cream surfaces, pastel outlines, shared prefabs and TextMeshPro carry the same style through every screen. The Reference Edition scene and its exclusive assets/scripts have been removed.

See **[UI_REDESIGN.md](UI_REDESIGN.md)** for the current UI stages, supported settings, scope and validation evidence. The shared theme and fourteen prefabs live under `Resources/UI`; **Tools > Farm Guardian > Build UI Kit** regenerates them using the installed TMP resources. The Canvas reference is 1920×1080, with a safe-area design root that fits landscape phones/tablets. Original artwork and its provenance remain under `Art/MainMenu`; the latest Main Menu implementation and animation limits are documented in MENU_LAYERING.md.

| Action | Editor / desktop | Mobile |
|---|---|---|
| Move | WASD / arrows | Bottom-left joystick |
| Capture animal | E or Capture button | Capture button |
| Catch thief | Hold F for 2 continuous seconds in range | Hold Catch button while following with joystick |
| Plant | Hold P | Hold Plant button |
| Pause / resume | Esc or UI | Pause / Resume buttons |

The elevated camera follows automatically. Its heading is fixed so movement and compass directions stay consistent; mouse camera rotation is unnecessary. Unavailable actions are disabled. Plant is omitted in levels without planting. Sound can be toggled in the menu or pause panel. Leaving the app pauses the game and requires an explicit Resume.

## Core rules

- Capture lifts one animal into the player's arms. Carry it to its species pen and deposit it to count toward objectives. Secured animals wander within their enclosure without opening the gate.
- The thief becomes visible after the first animal is secured and waits the configured initial delay before targeting a pen. Initial activation chooses a random reachable perimeter entrance, at least 8 units from the player and 6 from pen destinations. After stun it resumes from its last stun position.
- Stealing takes 3 seconds by default. A stolen animal is attached visibly to the thief. The secured count decreases and the player can recover it.
- Holding Catch within 2.4 units and with a clear line of sight for 2 uninterrupted seconds stuns the thief for 15 seconds. Leaving range resets progress. Speed upgrades do not shorten this hold.
- A recovered animal must be carried back to its original pen. The thief must enter the marked escape zone carrying an animal to cause an escape loss. The level timer can also cause a loss.
- All animals must be secured, all assigned planting completed, no animal permanently lost, and time remaining above zero to win. A central final-state guard prevents duplicate result screens and rewards.
- Replay earns another reward for another successful run. A second result call within the same run does not award coins again.

## Files and setup

All paths are relative to this Unity project root: `C:/Users/ASUS/UTS_GameDev_Group1`.

| File under `Assets/FarmGuardian/` | Responsibility |
|---|---|
| `Scripts/FarmConfig.cs` | Inspector configuration and deterministic per-level objectives |
| `Scripts/FarmSession.cs` | Session state, timer, interactions, objectives, final results, rewards |
| `Scripts/FarmPlayer.cs` | CharacterController movement, gravity, normalized input and Animator state input |
| `Scripts/FarmCharacterAnimation.cs` | Sends Speed, WalkRate, Carrying and Stunned parameters to character Animator Controllers |
| `Scripts/FarmCamera.cs` | Stable elevated follow with obstacle sphere cast |
| `Scripts/FarmAnimal.cs` | Escape/wander/capture/return/secure/theft/recovery states |
| `Scripts/FarmThief.cs` | Activation, target selection, theft, escape, stun, reactivation |
| `Scripts/PlantingPlot.cs` | One-shot planting and procedural plant growth |
| `Scripts/FarmWorld.cs` | Designed map layouts, verified models, simple colliders, runtime NavMesh |
| `Scripts/FarmHud.cs` | Main menu, introduction, HUD, pause, results, shop, safe areas |
| `Scripts/FarmJoystick.cs` | Pointer-owned joystick supporting simultaneous action-button touches |
| `Scripts/FarmHoldButton.cs` | Pointer-held interaction with release/disable cleanup |
| `Scripts/FarmUiSkin.cs`, `FarmUiIcon.cs`, `FarmUiMotion.cs` | Rounded panels, vector icons, circular joystick, fades and responsive button motion |
| `Scripts/FarmSave.cs` | Local validated save and purchase rules |
| `Editor/FarmGuardianSetup.cs` | Reproducible asset/scene setup and configuration validation |
| `Editor/FarmCharacterRigBuilder.cs` | Generate skinned character prefabs and mesh assets from the inspected OBJs |
| `Editor/FarmMenuArtSetup.cs` | Import illustrated menu textures, trim transparent sprite margins and assign persistent artwork references |
| `Tests/PlayMode/FarmGameplayTests.cs` | Integrated navigation, rescue, recovery, stun, timer, reward, and save checks |

Required existing packages: AI Navigation 2.0.14 (`Unity.AI.Navigation`), Input System 1.20.0, Unity UI 2.5.0, URP 17.5.0, Test Framework 1.7.0. No additional package installation is required. The UI creates an InputSystemUIInputModule with default UI actions; desktop movement reads the new Input System. The existing project-wide action asset is preserved.

### Inspector configuration

Select `Assets/FarmGuardian/Data/FarmGuardianConfig.asset`:

- Player speed 5.8; interaction range 2.4; animal capture 0.65s.
- Thief catch 2s; stun 15s; theft 3s; start delay 4s; escape deadline 22s.
- Thief base speed 2.2, increase 0.2 per level, maximum 3.3.
- Levels per theme 15; timer bands 90, 80, 70, 60 seconds, with a 60s floor.
- Maximum 8 animals, 4 species, and 3 planting plots. These caps keep later levels bounded.
- First five levels use 3/4/4/5/5 animals and 0/1/0/2/0 plots. Later planting is seeded by level number, so retries preserve their mission.
- Rewards: 30 base + 10 per animal + 15 per plot + 1 per five whole seconds remaining, then coin upgrade multiplier.
- Four upgrades have four tiers by default: +8% base player speed, +15% coin reward, +20% farming rate, +20% animal-capture rate per tier. The first upgrade costs 60, then 120/180/240. The golden farmer tint costs 150 coins and cannot be purchased twice.

Configured speeds, times, and objectives can be changed without editing code. Animal source ordering is Cow, Pig, Chick, Bunny and must match the names/controllers arrays.

### Scene and runtime hierarchy

```text
Farm Guardian                  [prefab: FarmSession, config assigned]
Main Camera                    [Camera, AudioListener, URP camera data]
Farm Sun                       [directional light]

At runtime:
Farm Guardian - <theme>
  Environment
    Ground / Boundaries / Paths / Decorations
  FarmBuildings
  AnimalPens
  PlantingPlots
  PlayerSpawn
  AnimalSpawnPoints
  ThiefEscapeZone
  InteractiveObjects
```

Runtime NavMeshSurface collects the simple colliders on layer 8 and excludes actors, path paint and purely decorative geometry. Four authored layouts rotate every 15 levels: Countryside (1–15), Woodland (16–30), Mountain (31–45), Advanced (46–60), then repeat at 61. Each changes pen/spawn/plot placement, obstacles and route connections. Advanced moves the exit west; Mountain requires one of two timber crossings through a rock divider. Obstacles are Not Walkable and all objective slots and route segments are validated after baking.

No manual object references are required after Build Game. To change the camera, edit `FarmCamera.Offset`. Author placements in `FarmMapLayout.cs`; `FarmWorld` assembles the plan. See **[MAP_DESIGN.md](MAP_DESIGN.md)** for all four top-down plans, location coordinates, obstacle intent, hierarchy, collider/navigation settings and Unity build instructions. **Tools > Farm Guardian > Maps** exports scaled SVG diagrams and opens unsaved additive Edit Mode previews without unlocking or saving progression.

## Actual asset use

- Player: `Assets/UTS_Project/farmer/source/Player_SkinFarmer.obj` and its supplied PNG, converted into the generated `Assets/FarmGuardian/Prefabs/Characters/Farmer.prefab`.
- Thief: `Assets/UTS_Project/burglar/source/Player_SkinBurglar.obj` and its supplied PNG, converted into the generated `Assets/FarmGuardian/Prefabs/Characters/Burglar.prefab`.
- Animals: `animal-cow.fbx`, `animal-pig.fbx`, `animal-chick.fbx`, `animal-bunny.fbx` under the Kenney FBX folder, using supplied `colormap.png`. Existing idle/walk takes drive generated Animator Controllers, with looping enabled in those four importers.
- Environment: selected Barn, Windmill, Well, Hay_Bale, Crate, Hay_Cart, Tree_Square, Tree_Pine, and Boulder objects from `PictureCrossTownsAssets.fbx`, using the supplied `Gradients.png` explicitly to resolve the old absolute texture path.
- Primitive ground, paths, pen fencing, planting soil, seedlings, identity/exit markers, colliders, and distant mountain shapes are newly constructed gameplay geometry.
- Farmer/thief source OBJs have no rig or clips. Generated prefabs now have seven-bone skinned meshes with fitted weights: the actual legs, knees and arms swing in opposing walking cycles, driven by movement speed, with idle blending and carrying/stun poses. This is a procedural rig rather than imported Humanoid motion capture. The original OBJs and textures remain unchanged. The golden outfit is a material tint of the same farmer, not a separate supplied skin model.
- Brief synthesized sound feedback is generated at runtime; no recorded audio assets are assumed.
- The farm ZIP and alternative animal formats remain untouched. Source geometry and textures are not rewritten.

## Validation and manual QA

Run **Tools > Farm Guardian > Validate Configuration** to check 300 generated missions and theme boundaries. Run the Play Mode tests from **Window > General > Test Runner**. Tests temporarily replace and restore the Farm Guardian PlayerPrefs save key.

Manual checks still required before calling this production-ready:

- [ ] Complete level 1 with default upgrades through normal controls.
- [ ] Capture animations play on at least two animals; verify texture mapping and model facing.
- [ ] Let the thief steal, chase it, break a catch hold by leaving range, catch it, and confirm recovery.
- [ ] Observe the full 15-second stun and a subsequent theft attempt.
- [ ] Lose by actual thief arrival at the exit; retry and verify exactly one set of actors.
- [ ] Lose by timeout; pause/resume; return to menu during recovery.
- [ ] Complete planting on levels 2 and 4; confirm repeated planting cannot increase counters.
- [ ] Play theme boundaries 15/16, 30/31, and 45/46 with default upgrades and adjust difficulty if necessary.
- [ ] Buy an upgrade and cosmetic, reject insufficient funds, restart the application, and verify persistence.
- [ ] Test 16:9 and wide landscape safe areas, simultaneous joystick/hold input, and app backgrounding on a real phone.
- [ ] Profile runtime navigation construction, frame rate, memory, and build size on the minimum target device.

Automated reachability/configuration checks do not prove every generated mission is human-completable within its time limit. Device performance and end-to-end difficulty balancing require measured playtests. No Android/iOS binary, signing configuration, or store-ready package is supplied by scene setup.

## Common problems

- **Missing scene/configuration:** wait for compilation, then run Build Game. Read the first Console error if it fails.
- **Pink models:** confirm URP is active and the four generated materials use Universal Render Pipeline/Lit; rerun setup if source texture references changed.
- **Animals do not animate:** check the generated controllers and original FBX idle/walk clips. Preserve the imported mesh hierarchy.
- **Farmer/burglar do not walk:** confirm the configuration references `Prefabs/Characters/Farmer.prefab` and `Burglar.prefab`, rather than the raw OBJ files. Run **Tools > Farm Guardian > Update Character Rigs** to regenerate the rigs without replacing the scene or resetting gameplay values. Walking responds to actual movement; pushing into a wall does not produce a full walk cycle.
- **No movement in Game view:** click Game view and confirm Playing state; input is intentionally disabled in introduction, pause, and result screens.
- **Catch progress resets:** remain in range and keep line of sight; hold continuously while moving with joystick/WASD.
- **Missing model or package:** setup reports the exact verified path instead of silently inventing a replacement asset.
- **Unity batch mode exits immediately:** do not launch a second Editor against an already-open project. Close it safely or validate in a separate project copy.

## Remaining polish

Authored Humanoid animation clips, recorded audio/music, richer crop-stage assets, an accessible settings panel, edit-time map previews, and real-device balancing remain future refinements. The UI uses generated rounded shapes and vector illustrations, smooth mission progress, warning accents, and button/page transitions; its corners and joystick remain anchored to the safe area. The shop uses local in-game coins only; there are no external services or real-money purchases.



