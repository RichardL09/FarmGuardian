# Responsive main menu and Windows fullscreen

Windows builds now start in borderless fullscreen at the main display's native
size. FarmSession overrides the old saved windowed resolution once at startup
(standalone Windows only, never in the Editor). Both setup and build tools use
FullScreenWindow and native-resolution defaults.

The main menu has a canvas-sized page, independent of the other screens' 1920x1080
design frame. Artwork and interactive controls fit uniformly inside the safe area.
A full-bleed clean scenery layer fills any remaining aspect-ratio space, and the
fitted illustration blends into it only along the extended axes. Buttons, logo,
and coin counter are not cropped or stretched. Main-menu entry does not scale
the entire background inward during a panel entrance animation. Gameplay and
other pages retain their existing layout and transitions.

## Tests

33/33 UI/gameplay checks passed in `Logs/FarmGuardian-fullscreen-menu-tests.xml`.
The UI suite was rerun after background adjustments (9/9), and the final menu
layout/navigation checks passed 2/2 in
`Logs/FarmGuardian-fullscreen-menu-final-tests.xml`.

The new regression check captures 1280x720, 1366x768, 1920x1080, 2560x1440,
1440x1080, 2340x1080 and 3200x1080. It asserts the scenery fills the viewport,
Start/Upgrades/Settings/coins/logo bounds stay inside it, the art scale is uniform,
and navigation works after returning from Settings. Captures are in
`Logs/FarmGuardian-fullscreen-menu-*.png`. The final 1366x768 preview and earlier
4:3/ultrawide compositions were visually inspected. Source hashes match the
isolated tested project.

The previous ZIP is retained in
`Builds/Previous/FarmGuardian-Windows-before-fullscreen-menu.zip`.

## Windows delivery before the Exit button update

`Logs/FarmGuardian-windows-build-fullscreen.log` reports a successful Windows
build in `Builds/FarmGuardian-Windows-Fullscreen`. The new EXE responded normally
and had no startup errors/exceptions in
`Logs/FarmGuardian-player-fullscreen-smoke.log`. Only the isolated startup-test
process was stopped. This is not a complete manual standalone playthrough.

`Builds/FarmGuardian-Windows.zip` now contains the complete Fullscreen folder:
79,285,994 bytes, 189 shipped files checked for presence and length, and matching
SHA-256 hashes of the archived EXE and FarmGuardian.dll. Extract the complete ZIP
and use this new EXE, not either previous output directory. Debug-only symbols
are retained separately in `Builds/Diagnostics/FarmGuardian-BurstDebugInformation-fullscreen`.

## Cozy Exit button

The main menu now has a centered, secondary EXIT button beneath Settings. It uses
the existing unmodified Cozy `SquareButton2_red` sprite and Coiny font, with a
280x70 design size and spacing from both Settings and the bottom safe edge.
`FarmSession.QuitGame` is restricted to the main menu and saves progress before
calling `Application.Quit`. Unity ignores this call in the Editor; it closes the
standalone game, not the Unity editor.

All 10 UI tests passed in `Logs/FarmGuardian-exit-tests.xml`. The new test uses
the actual button raycast/click callback, checks the Cozy sprite and font, and
verifies the saved coin balance. The viewport regression now also checks EXIT
bounds at all seven resolutions. The 1366x768 preview was visually inspected and
is saved as `Logs/FarmGuardian-cozy-exit-button.png`. Changed source hashes match
the tested copy. Standalone closing itself is not exercised by the Editor tests.

The latest playable build is `Builds/FarmGuardian-Windows-Exit`, confirmed in
`Logs/FarmGuardian-windows-build-exit.log`. Its startup smoke test responded
normally with no errors/exceptions in `Logs/FarmGuardian-player-exit-smoke.log`;
only that isolated test process was stopped. The previous Fullscreen folder was
preserved. `Builds/FarmGuardian-Windows.zip` contains the complete Exit folder:
79,283,868 bytes, 189 shipped files verified for presence/length, with matching
SHA-256 hashes for the archived EXE and FarmGuardian.dll. Debug-only symbols are
separate in `Builds/Diagnostics/FarmGuardian-BurstDebugInformation-exit`.
