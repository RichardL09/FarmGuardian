# Farmer and Burglar Animator Controllers

Both character prefabs use Unity Animator components with root motion disabled. The supplied OBJ meshes and existing seven-bone rigs remain unchanged. Animation poses are stored in editable `.anim` clips instead of calculated on bones every runtime frame.

## Edit in Unity

Quick access: **Tools > Farm Guardian > Inspect Animator > Farmer (Player) / Burglar (Thief) / Cow**. In Edit Mode this opens the selected character's controller graph. In Play Mode, after starting a level, it selects the actual runtime Animator GameObject so its live parameters and current state appear in the Animator window. Enable **Auto Live Link** and unlock the Animator window if it remains pinned to another graph.

The `Farm Environment - saved Edit Mode view` hierarchy is scenery only. Player, burglar and animal actors are created under the runtime farm's `InteractiveObjects` when a level begins. Selecting `cow.controller` in the Project panel shows the shared controller asset, not a particular live cow. Asset graphs do not advance in Edit Mode. An inactive thief and paused game also do not advance their animation.

1. Open `Prefabs/Characters/Farmer.prefab` or `Burglar.prefab`.
2. Select the root Farmer/Burglar object and inspect its Animator component.
3. Double-click `Animation/Characters/Farmer.controller` or `Burglar.controller`. Open **Window > Animation > Animator** to see states and transitions.
4. Open **Window > Animation > Animation** on the prefab to edit Idle, Walk, CarryIdle, CarryWalk or Stunned. These clips are shared by both characters; duplicate a clip and assign it in one controller for character-specific movement.

Each controller has three states: **Locomotion**, **Carrying**, and **Stunned**. Locomotion blends Idle/Walk; Carrying blends CarryIdle/CarryWalk. Stunned overrides the other states. Transitions blend over 0.12 seconds without exit-time delays.

| Parameter | Type | Source |
|---|---|---|
| Speed | Float | Actual movement speed for idle/walk blending |
| WalkRate | Float | Movement speed converted to stride playback rate |
| Carrying | Bool | Player carries an animal or thief has stolen one |
| Stunned | Bool | Thief stun state |

`Scripts/FarmCharacterAnimation.cs` remains the gameplay-to-Animator adapter, preserving player/thief call sites. It only sends parameters and no longer writes bone positions or rotations. Pause uses scaled Animator time. CharacterController/NavMeshAgent still control movement and collisions.

**Tools > Farm Guardian > Update Character Animator Controllers** installs missing controllers/clips and assigns them to both prefabs. Existing controllers/clips are preserved to protect manual edits. **Update Character Rigs** also assigns the appropriate controller after rebuilding a rig.

## Validation

Live-inspection follow-up: 17/17 gameplay tests passed (`Logs/FarmGuardian-animator-inspect-tests.xml`), including a dedicated cow test proving `Moving=true` and the live Walk state during NavMesh movement, advancing clip time, then `Moving=false` and Idle while held stationary. No cow runtime/controller changes were needed; the new Editor menu selects the correct controller or live instance.

All 28 PlayMode tests passed after conversion, including both controllers, five clips, disabled root motion, actual skinned mesh deformation, idle recovery, carrying/stun transitions and existing gameplay regressions. See `Logs/FarmGuardian-animator-tests.xml` and `Logs/FarmGuardian-animator-build.log`. The tested prefab/controller/clip files were copied back with matching hashes. Carrying screenshot: `Logs/FarmGuardian-animator-carry-pose.png`.
