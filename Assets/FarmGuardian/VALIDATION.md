# Validation record

## Complete colourful UI/UX update

- Replaced the legacy Text-based HUD/screens with TextMeshPro, a 1920×1080 responsive safe-area layout, shared cream/brown panels, glossy coloured buttons, pink ribbon titles, icon sprites and fourteen reusable prefab assets. The original reference is visual inspiration; controls are live Unity UI components.
- Removed the bottom-left main-menu level selector. PLAY now opens a dedicated paginated grid which reads existing unlocks, previews locked missions without allowing them to start, highlights selection and displays objectives, theme, time limit and base reward.
- Added a renderer-only 3D farm menu from the verified project models and real farmer previews for the two existing outfit options. Menu visuals do not instantiate AI, NavMesh agents, missions or a second game manager.
- Shop uses existing upgrade/outfit purchases and actual currency. UI purchases have a brief duplicate-click guard; repeated equip of an owned outfit is idempotent. Skin SELECT does not purchase/equip.
- Settings connects the SFX slider to the existing feedback AudioSource and cycles actual quality presets. Paused Settings returns to Pause without advancing the timer. Reset requires a blocking confirmation and only resets Farm Guardian progress, retaining device preferences.
- Existing result/reward logic remains responsible for awards. Win/lose/pause/intro and HUD use the same shared style. Page exit releases held controls and stops raycasts immediately before an unscaled 0.12-second fade; page/button entrance and progress animations also work while paused.
- `FarmUiSetup.Build` compiled and imported the installed TMP Essential Resources, retaining the supplied font licence, then generated all prefab/sprite/theme references. Build report: `Logs/FarmGuardian-ui-build.log`.
- The graphics-enabled regression suite passed **17/17**, zero failures: `Logs/FarmGuardian-ui-tests.xml`. It includes prior map/AI/rig/save checks, actual UI pointer-raycast navigation, locked levels, upgrade double-click rejection, outfit idempotency, volume/quality changes, pause/settings return and both Cancel/Reset paths. Tests restore the progress key and changed presentation preferences.
- All main screens were rendered at 1920×1080, 2340×1080 and 1440×1080; captures are `Logs/FarmGuardian-ui-*.png`. Visual inspection found an over-stretched slider fill/handle; separate fill/handle containers fixed the overlap. Full-screen modal scrims also cover tablet letterbox areas. Final previews include menu, level grid, shop, skins, settings, intro, HUD, pause, win, lose and reset confirmation.

No music, localization, vibration setting, IAP or earned-star service exists in this project, so those unsupported controls/counters were not simulated. Physical phone safe areas, touch comfort, rendering cost and audio output still require device acceptance testing. `UI_GUIDE.md` lists phase-by-phase files, Inspector configuration, assets, regeneration instructions and remaining integration points.

## Four strategic map layouts

- Rotation is now Countryside 1–15, Woodland 16–30, Mountain 31–45, Advanced 46–60, repeating at 61. Each layout has its own objective coordinates, route graph and intentional obstacles. Advanced changes the exit to the west; Mountain separates the two farm areas with a rock divider and two timber crossings.
- `MAP_DESIGN.md` records the pre-placement plans, important locations, obstacle arrangement, gameplay intent, hierarchy, collider/NavMesh settings and Unity assembly instructions for every theme. Scaled diagrams in `Docs/Maps` export directly from the authored layout data.
- Unity 6000.5.9f1 compiled the delivered code in an isolated project copy. `FarmMapTools.ValidateAndExport` successfully built all four Edit Mode previews, validated all objective slots and checked that the eight required town source models exist. `FarmGuardianSetup.Validate` passed 300 level configurations and rotation boundaries. Report: `Logs/FarmGuardian-map-validation.log`.
- The expanded graphics-enabled Play Mode suite passed **15/15**, zero failures: `Logs/FarmGuardian-map-tests.xml`. It includes prior gameplay/menu/rig/random-spawn regression tests and these map checks:
  - All four fully populated layouts, complete objective/escape paths, clear player-sized sweeps along authored lanes, real detours at the grove/barn and both mountain crossings.
  - Analytical interception opportunities along every animal home's escape route at maximum thief speed and base player speed, with a complete catch hold plus over one second of margin from the guardian response hub.
  - Actual CharacterController collision against perimeter walls and barn footprints.
  - Six seconds of local wandering followed by all eight animals actually travelling into their own pens on each theme.
  - Actual base-speed player movement and an uninterrupted catch hold against a fleeing thief from each response hub, recovering the animal.
  - An unchallenged thief actually reaching each exit before its deadline, rather than losing only because the timer expired.
- The crossing approaches were moved away from rail ends after navigation validation found insufficient agent clearance. The north woodland return was also moved away from the pen's back corner. Lane validation uses both a player capsule sweep and a complete nearly straight agent path; it tolerates small triangulation-edge deviations rather than interpreting an isolated NavMesh ray hit as a physical wall.
- Render captures: `Logs/FarmGuardian-map-{Countryside,Woodland,Mountain,Advanced}.png` and corresponding `-top.png` files. They show actual Unity geometry and supplied models, separate from the schematic SVG plans.

These tests establish connectivity and representative chase feasibility. Human completion of all 60 levels, touchscreen handling, camera visibility during every route, timer/difficulty tuning and loading/frame-time profiling on physical phones remain acceptance work. The mountain game surface is level; the rock silhouette and timber crossings do not imply climbing or unsupported terrain physics. Initial validation startup required restarting the isolated compiler; the user's original Editor was kept open.

## Reference-style scenic main menu

- Replaced the menu presentation with a full-screen farm illustration, transparent Farm Guardian logo, and wooden Play, Shop, Skins, and Settings buttons. Artwork files and exact built-in imagegen prompts are recorded in `Art/MainMenu/Generation.md`.
- The coin counter reads the saved balance. Shop opens existing upgrades; Skins validates ownership and funds before charging, with free equip toggling after purchase; Settings controls the saved sound preference. Level selection remains available.
- Unity 6000.5.9f1 imported the artwork and compiled all assemblies in an isolated project copy. The full graphics-enabled Play Mode suite passed **10/10**, with zero failures: `Logs/FarmGuardian-menu-tests.xml`.
- The new menu test exercises all four buttons, sound toggling, insufficient funds, one-time skin charging, and starting a level. Existing gameplay, rig, and random-spawn regression tests also passed.
- Rendered and visually inspected the menu at 1600x900 and 1440x1080, plus Skins and Settings. A 2340x1080 preview was also captured. Preview files are in `Logs/FarmGuardian-menu*.png`, `Logs/FarmGuardian-skins.png`, and `Logs/FarmGuardian-settings.png`.
- Imported sprite assets and metadata were copied back to the main project with the tested config references. Physical mobile device touch and performance testing remain outstanding.

Date: 2026-10-15 (Asia/Bangkok).

Unity 6000.5.9f1 executed against an isolated copy of the project because the original project was already open in another Editor. The runtime and Editor scripts in that copy matched the delivered project. Generated assets retain the same source GUIDs and were copied back with their metadata.

## Passed

- Unity compilation of runtime, Editor setup, and Play Mode test assemblies.
- Graphics-enabled presentation test: menu and gameplay previews rendered and inspected at 1600 x 900. Source texture mapping and animal/player visibility were verified. The preview capture forces synchronous Editor shader compilation to avoid recording temporary shader-loading colors.
- `FarmGuardianSetup.BuildAndValidate`: saved scene, configured bootstrap prefab, four URP materials, four Animator Controllers, and 300 level configuration checks.
- Six gameplay Play Mode tests, 6 passed / 0 failed, recorded in `Logs/FarmGuardian-tests.xml`:
  - Interaction range and two live animal Animators.
  - Win, one reward per run, timer freeze after results, and restart without duplicate actors.
  - Permanent-loss state and timer expiry without rewards.
  - Invalid save fallback, clamping, insufficient funds, purchase persistence, invalid upgrade rejection.
  - Thief activation, actual theft, temporary recovery window, broken hold reset, two-second catch, original-pen recovery, 15-second stun, pause, and reactivation.
  - Complete NavMesh routes to all pens and thief exit across levels 2, 16, and 31; unfinished planting blocks victory.

## Interpretation and limits

Tests move actors directly to specific positions and advance time to exercise states deterministically. They do not replace a human completing each level with the mobile controls. Permanent-loss testing exercises the central loss entry point; normal thief escape-zone arrival also needs manual playtesting. Real-device touch input, safe-area layouts, frame rate, build size, and end-to-end difficulty balancing remain manual acceptance items in README.md.

No mobile binary or device performance claim is made. Animal idle/walk animations come from the supplied FBXs.

## Walking, random spawn, and UI update

- Added generated seven-bone skinned farmer/burglar prefabs, retaining the original OBJ geometry and texture atlases. Walking is now procedural limb animation, with opposing arm/thigh motion, knee flex, smooth idle blending, carrying poses, and a stun pose. It does not depend on imported Humanoid clips.
- Added Play Mode checks that both rigs deform the actual mesh, swing opposing limbs, and return to idle.
- Added repeated random-spawn checks across the three themes: candidate variation, player/pen clearance, and complete routes to pen destinations.
- The full updated Play Mode suite passed **9/9**, including the previous gameplay regression checks. Report: `Logs/FarmGuardian-update-tests.xml`.
- The updated UI uses rounded cards, circular joystick controls, vector icons, illustrated menu layout, button/page transitions, and smooth mission progress. Preview captures include 1600x900, 2340x1080, and 1440x1080 layouts plus the character rigs.
- The UI preview exposed a missing CanvasRenderer on custom vector icons. The component now declares that requirement, and the presentation test checks it explicitly. The capture primes URP materials before saving a frame.
- The final presentation rerun passed after the icon fix, and the menu/gameplay images were visually inspected. Final visual report: `Logs/FarmGuardian-update-visual.xml`.

Real-device touch interaction and performance still require device testing. The character rigs are fitted to the inspected stylized farmer/burglar models; different source characters need new weights rather than assuming this rig will fit them automatically.
