# Life, score and Windows build

Latest fullscreen/menu update: see `MAIN_MENU_FULLSCREEN.md`. The build tool now
uses borderless native-resolution fullscreen instead of the windowed defaults
described below. The carry/keyboard update is recorded in `CARRY_SHORTCUTS.md`.
The previous carry-update ZIP contained
`FarmGuardian-Windows-Updated`, with 29/29 regression checks and a new successful
Windows build/startup test. The verification section below records the earlier
Life/Score build, not the current archive's byte count or output folder.

The gameplay HUD explicitly displays LIFE and SCORE in its Cozy mission panel.
Life equals original animal count minus permanent theft losses; temporary theft
does not remove a life. The existing timeout/all-animals-lost defeat rules remain.

Score is local to the current run: 100 points per animal currently safe in its
pen and 150 per planted plot. A stolen animal temporarily removes its 100 points;
recovery and delivery restore those points, without accumulating extra score.
Repeated planting is also idempotent. Score is independent of the saved coin
balance and coin upgrades, pauses with the run, appears in the result screen, and
resets when a new level/retry is introduced. Life also resets to that level's
original animal count. Neither currency rewards nor star thresholds changed.

## Rebuilding the Windows application

In Unity, choose **Tools > Farm Guardian > Build Windows EXE**. Windows Build
Support must be installed. `FarmWindowsBuild` builds the FarmGuardian scene for
Windows x64 with the Mono backend and Direct3D 11, in a resizable 1280x720 window.
Output is `Builds/FarmGuardian-Windows` beside the Unity project directory.
The command uses Unity's supported [custom build pipeline](https://docs.unity.com/en-us/engine/6000.5/manual/building-and-publishing/build-customize-build-pipeline/build-script-build).

Distribute the entire output folder (or a ZIP of it), not only FarmGuardian.exe.
The executable needs its generated data directory, UnityPlayer.dll and the Mono
runtime folder. ZIP recipients must extract the whole archive before launching.
No Google Drive upload is performed by the build tool.

## Verification of the original Life/Score build

The full gameplay fixture plus five UI/skin regression checks passed 28/28 in
`Logs/FarmGuardian-life-score-tests.xml`. The Life/Score test checks permanent vs
temporary theft, re-delivery without point farming, repeated planting, unchanged
wallet on loss, pause, result values and retry reset. HUD previews at 1280x720,
1920x1080, 1440x1080 and 2340x1080 and the updated result screen were captured;
the 1280x720 HUD and 1920x1080 result were visually inspected.

`Logs/FarmGuardian-windows-build.log` reports a successful Windows x64 build.
The actual EXE was launched with graphics enabled in batch mode: Direct3D 11,
NVIDIA RTX 2050, process responding, and no errors/exceptions in the startup log
`Logs/FarmGuardian-player-smoke.log`. This is a startup smoke test, not a full
human playthrough of the standalone build. The isolated test process was stopped
after checking it; local user progress was not reset.

Non-shipping Burst debug symbols were retained separately in `Builds/Diagnostics`.
The playable output includes an English README with controls and scoring rules.
`Builds/FarmGuardian-Windows.zip` is 79,280,722 bytes and was checked against all
189 shipped files for entry presence and byte length. SHA-256 hashes of the
archived EXE and FarmGuardian.dll match the unpacked output. Debug-only symbols
are not included. Runtime and build-tool source hashes match the tested copy.
