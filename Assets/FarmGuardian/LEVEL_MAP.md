# Live level-selection farm map

`FarmHud.LevelSelect` builds a full-screen `FarmLevelMapView` behind the real UI
buttons. There is no containing panel or side information card. Open levels go
directly to the existing mission introduction; locked levels cannot start.
Three stars below each level number show the saved best rating (see
`STAR_OUTCOMES.md`). Unplayed levels show dim stars. The existing 15-level pages
and 100-level campaign remain.

The backdrop is a live 3D scene assembled from the project's own village FBX,
environment material and extracted windmill meshes. It includes the original
Barn, StonePath, houses, fences, crops, pond, well, hay and tree models. A sand
strip connects repeated StonePath tiles into a continuous winding road.

The UI and world use `DesignNodes()` as their common layout. The map camera
projects those design coordinates onto the ground before building the road.
Its texture has a 2560x1440 design footprint with overscan around the safe UI
area for tablet and ultrawide screens. The remote scene and camera use layer 30
and are released when the outgoing UI page is destroyed.

Tree sway, flower movement and the windmill rotor use unscaled time. These
animations are confined to the selection-map copies of the original assets.
The main menu and gameplay scenery are not modified.

PlayMode verification is in `FarmUiTests.AssetMapAlignsRoadAnimatesTreesAndReleasesItsCamera`.
It checks source assets, projection, unscaled tree motion and camera cleanup,
and records normal, tablet and ultrawide previews in `Logs` when graphics are available.

Validation on 21 October 2026: all three selected PlayMode tests passed
(`FarmGuardian-asset-level-map-tests.xml`), covering the animated map, level
locks/final campaign page, and navigation/shop/settings regression. The tested
FarmHud and FarmLevelMapView sources matched the main project by SHA-256.
The 1920x1080, 1440x1080 and 2340x1080 previews were inspected in `Logs`.
