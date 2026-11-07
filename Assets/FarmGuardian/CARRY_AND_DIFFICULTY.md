# Carrying, species difficulty and 100-level campaign

Use the original scene: `Assets/FarmGuardian/Scenes/FarmGuardian.unity`. The Main Menu is static and retains the supplied design.

## Rescue flow

Press the existing capture action (mobile button or E) near an animal on the ground. Stay within interaction range while lifting; moving away cancels the attempt. Only one capture or carried animal is allowed at a time. The animal attaches in front of the farmer with its NavMeshAgent disabled, and the farmer uses the existing carrying arm pose. Walking and turning move the carried animal with the player; it never walks to a pen itself.

Bring it near its own assigned `Home` inside the matching pen, within 2.1 metres with unobstructed line of sight, then press MASUKKAN KANDANG/E. Only successful delivery sets Secured and counts toward victory. Planting is unavailable while carrying. Pause freezes the attempt and walking; retry/menu clears the carry reference and actors. Animals recovered from the thief are carried if the player's hands are free; otherwise they remain nearby and must be picked up. They are never automatically secured by recovery.

## Animal differences

- Cow: 3x base lift duration (1.95 seconds before upgrades), slow wandering, carrying reduces player movement to 70%.
- Pig: 1.6x lift duration, short local flee movements at 1.8 m/s when approached, carrying movement 90%.
- Chick: 1.25x lift duration; occasionally hops/flutters up to 0.8 m for 0.9 seconds when approached. Cannot be captured while airborne.
- Bunny: 1.4x lift duration and occasional smaller 0.35 m hop, also unavailable while airborne.

Capture upgrades still reduce lifting time. Movement upgrades still apply before the carrying weight multiplier. Animal navigation remains within the original 2.4 m wander area and uses complete local NavMesh routes; hops offset only the visual model and do not detach navigation from the ground. All remaining timers, thief stun/escape logic, rewards and planting objectives retain their prior rules.

## Camera and lighting

Gameplay follow offset changes from (0,14.5,-12) to (0,11.5,-9.5); FOV is 48 rather than 52. Collision avoidance remains active. The existing directional sun is reused, with warm daylight, 72% shadow strength, controlled bias and balanced sky/equator/ground ambient light. Existing ground/materials receive shadows; tall scenery also casts shadows. No per-object lights or post-processing stack is added. Existing URP profile shadow support remains authoritative (mobile uses its existing shadow quality).

## Progression

`FarmConfig.MaxLevel` is 100. Configuration, selection, introductions, page controls, save loading/storing and unlock rewards use that cap. Older saves beyond 100 are clamped to 100 while preserving coins, upgrades and outfit ownership. Finishing level 100 offers level selection rather than level 101. Map themes continue the existing 15-level rotation.

## Verification

Play Mode tests are updated for player delivery rather than autonomous return. Tests cover static menu, airborne capture restriction, cow lift duration/weight differences, single-animal carrying, pause, delivery-only scoring, all four map routes, thief recovery, old-save migration and the last progression page. Executed results and gameplay captures are added after validation; no physical mobile performance test is implied.

Executed result: 23/23 Play Mode tests passed in 84.57 seconds. See Logs/FarmGuardian-carry-tests.xml. Runtime sources in the main project exactly match those in the tested clone. Visual inspection covered the static menu, closer gameplay camera, directional shadows and actual cow-carry HUD. Final images include Logs/FarmGuardian-carry-gameplay.png and landscape menu/gameplay captures. Physical-device profiling and a complete human playthrough of all 100 levels have not been performed.

The later gate/stun/exit/environment update adds supporting carry animations, matching-pen gate readiness, retained stun position, and actual exit entry detection. See GATES_AND_ENVIRONMENT.md for the current behavior; the old theft deadline no longer causes a loss outside the exit.
