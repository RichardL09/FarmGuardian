# Farm Guardian

A Unity 3D farming game: rescue animals, protect them from thieves, plant seeds,
and complete timed levels. Includes level progression, earned stars, upgrades,
character skins, Cozy UI, and local progress saving.

## Open the project

1. Clone this repository.
2. In Unity Hub, add the repository folder containing `Assets`, `Packages`, and
   `ProjectSettings`.
3. Open it with **Unity 6000.5.9f1** and let Unity restore packages/import assets.
4. Open `Assets/FarmGuardian/Scenes/FarmGuardian.unity` and enter Play Mode.

## Controls

| Action | Input |
| --- | --- |
| Move | WASD, arrow keys, or on-screen joystick |
| Pick up / deliver animal | E |
| Catch thief | Hold F |
| Plant seed | Hold P |
| Pause / resume | Esc |
| Exit | EXIT on the main menu (standalone build) |

## Windows build

Install Windows Build Support for the Unity version above, then choose
**Tools > Farm Guardian > Build Windows EXE**. The build tool writes the complete
game to `Builds/FarmGuardian-Windows` beside the project folder. Distribute that
entire output folder, not only its EXE.

## Tests and repository contents

Open **Window > General > Test Runner** and run the PlayMode tests in
`Assets/FarmGuardian/Tests/PlayMode`.

This repository contains the editable Unity project, including asset `.meta`
files and package lockfile. Unity caches, temporary files, local editor settings,
generated IDE projects, and compiled game builds are intentionally excluded.
Third-party artwork, fonts, and models remain subject to their original licenses.
