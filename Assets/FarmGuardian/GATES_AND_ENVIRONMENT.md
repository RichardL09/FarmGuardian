# Pen gates, thief exit and environment motion

This update continues to use the first scene `Scenes/FarmGuardian.unity`. Its runtime FarmWorld assembles the changes without a new scene or gameplay manager. The Main Menu artwork remains static.

## Thief

Random placement remains only for the initial activation. Catch records `StunPosition`, clears the navigation path and velocity, and stops the thief. Stun expiry transitions at that same location, with the existing brief recovery delay before searching again; it no longer calls RandomizeThiefSpawn.

The visible exit is the existing 3 x 3 metre square. `FarmThiefExitZone.Contains` requires the thief's root within an inset 1.9 x 1.9 metre area, leaving room for the body inside the visible square. Theft loss requires an escaping thief carrying an animal to enter this area. Expiration of the former escape countdown no longer causes loss outside the zone, and the HUD no longer displays that misleading countdown. The overall level time limit still applies independently.

## Pens

The existing side and rear fences are retained. Every objective pen receives a south-facing wooden double gate, a doorway collider, and a wooden name board with two posts. Names come from the existing configured animal names: COWS, PIGS, CHICKS and BUNNIES. The blue Guardian crossroads cylinder and the blue disc under the player were removed; the invisible response-hub navigation marker remains.

`FarmPenGate` opens its two leaves on approach with the correct carried animal, for an actor crossing, for a thief approaching the doorway, or to let a player inside leave safely. It holds open briefly after delivery and closes when the doorway is clear. A physical threshold barrier blocks the player until the leaves are sufficiently open. Depositing also checks that the matching gate is open. Idle secured animals do not continuously trigger the doorway sensor.

The open doorway remains in the baked NavMesh. Runtime gates are excluded from NavMesh collection; proximity requests open the gate ahead of navigating agents. This avoids rebaking the level or making pens unreachable behind a closed baked obstacle. There is no continuous dynamic carving or physics collider on each moving rail. Pause freezes the gate animation.

## Carry animation

The existing generated farmer rig now blends both arms into a stronger supporting pose, reduces arm swing while walking with an animal, and adds a slight supporting torso lean. Pickup lifts the animal over 0.3 seconds to chest height; a small bounded movement follows walking without changing the player's position. The carrying pose is requested immediately on pickup, including while stationary. Existing weight penalties and delivery-only objective counting are retained.

## Trees and windmill

`FarmFoliageWind.shader` reuses the existing town texture and palette on tree render batches. Green leaf vertices sway gently in world space while brown trunk vertices stay still. Matching deformation is used by the shadow pass. One `FarmEnvironmentMotion` clock drives all foliage and pauses with gameplay; there is no per-leaf Update component.

The supplied town FBX contains the windmill as one mesh. Connected-component inspection identified four blade beams and their lattice in a narrow front plane. `Editor/FarmWindmillBuild.cs` extracts those exact triangles and UVs to the shared `Resources/FarmWindmill.asset`: 4,502 static body triangles and 638 rotor triangles. The normalized rotor pivot is (0.000415, 0.59109, 0.1933), with local Z as its axis. The original FBX is not edited. Only `FarmWindmillRotor` turns the blade assembly, at 12 degrees per second; the tower and spindle remain static. Rebuild with Tools > Farm Guardian > Build Animated Windmill From Existing Model if required. The builder validates the inspected source-plane bounds and refuses a mismatched source.

## Validation

Play Mode regressions include all four map layouts, delivery through the opening gates, closing after delivery, physical doorway barriers, wooden labels, exact stun recovery location, no theft loss outside the exit after the former deadline, exit entry loss, supporting arm poses and pausing foliage/rotor motion. Executed results and actual camera captures are appended after testing. Physical mobile profiling is not implied.


Executed validation: 27/27 Play Mode tests passed in 104.49 seconds. Logs/FarmGuardian-gates-tests.xml records the result. Visual inspection confirmed the supporting carry pose, wooden COWS sign, fenced doorways, original windmill geometry and rotating blade assembly. Shader compilation reported no shader errors. Runtime source and derived windmill asset hashes match the tested copy. Actual captures are saved in Logs/FarmGuardian-carry-pose.png, FarmGuardian-windmill-front.png, FarmGuardian-windmill-turn.png and the landscape gameplay/UI previews. Physical mobile profiling is not performed.
