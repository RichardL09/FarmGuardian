# Matching carry animation and gameplay keyboard hints

Farmer and Burglar already share the CarryIdle/CarryWalk clips. Their animal
attachment now also shares `FarmAnimal.AttachToCarrier`: front-facing hold at
(0, 0.75, 0.75), 90-degree animal yaw, a 0.3-second smooth lift and subtle walking
bob driven by the actual carrier's speed. The held animal's own walk Animator is
stopped for both carriers. Farmer remains Carried; Burglar remains Stolen, so
theft, recovery, life, score and escape rules are unchanged. Releasing or losing
an animal clears the carrier reference. Catch clears Burglar's carrying pose
immediately and applies the normal stun animation.

Only the three gameplay actions receive non-interactive Cozy keyboard badges:

| Action | Badge |
| --- | --- |
| Pick Up Animal / Deliver Animal | E |
| Catch Thief | HOLD F |
| Plant Seed | HOLD P |

Action buttons keep their existing object names, pointer/hold handlers and
availability rules. Wider matching buttons reserve separate space for the icon,
caption and key badge. All text stays English; captions and key badges use Coiny.

## Verification

The full gameplay fixture plus five UI/skin checks passed 29/29 in
`Logs/FarmGuardian-carry-shortcuts-tests.xml`. The new carry test compares the
two carriers' arm rotations, held-animal position and rotation, pause behavior,
and immediate release of the carrying pose when the thief is caught. UI checks
cover the three shortcut badges, single-line labels without text overflow, and
the Pick Up to Deliver label change while preserving E.

The matching-carry and gameplay HUD captures were visually inspected, including
the 1440x1080 HUD. Previews are saved in `Logs/FarmGuardian-matching-carry.png`,
`Logs/FarmGuardian-action-shortcuts.png` and the `FarmGuardian-cozy-gameplay*`
images. Runtime and build-helper source hashes match the isolated tested copy.

## Updated Windows package

The running old EXE locked its output DLLs, so the first rebuild could not finish.
It was not stopped. The successful new build is in
`Builds/FarmGuardian-Windows-Updated`; use this folder rather than the old output.
The build helper accepts `-farmBuildFolder FarmGuardian-Windows-Updated` to build
into a separate directory under Builds without changing the menu's default path.
Only FarmGuardian-Windows-prefixed directory names are accepted.

`Logs/FarmGuardian-windows-build-carry-updated.log` reports build success.
The updated EXE was launched separately with Direct3D 11, responded normally,
and had no startup errors/exceptions in `Logs/FarmGuardian-player-carry-smoke.log`.
Only that test process was stopped. This is startup verification, not a complete
manual standalone playthrough.

`Builds/FarmGuardian-Windows.zip` now contains the entire Updated folder:
79,284,176 bytes, 189 shipped files verified for entry presence and byte length,
with matching SHA-256 hashes for FarmGuardian.exe and FarmGuardian.dll. Extract
the complete archive before launching. Debug-only symbols are kept outside it
in `Builds/Diagnostics/FarmGuardian-BurstDebugInformation-carry-shortcuts`.
The previous ZIP is preserved in
`Builds/Previous/FarmGuardian-Windows-before-carry-shortcuts.zip`.
