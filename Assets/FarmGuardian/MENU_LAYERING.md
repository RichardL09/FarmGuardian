# Current Main Menu — static reference composition

The original `Scenes/FarmGuardian.unity` remains the entry scene. The Main Menu uses the supplied `DesignSource.png` unchanged, with independent background, logo, PLAY, SHOP, SETTINGS and wallet layers created by `FarmIllustratedMenu`.

At the user's latest request, all head and animal idle/hop animations were removed. The original farmer and animals are displayed directly in the source image at their exact original locations, avoiding cutout edges. `FarmMenuArtwork.cs` and the no-longer-used `MotionPlate.png` were deleted.

The logo/button typography is sampled directly from the source. The live balance uses the existing TMP font and actual saved coins. Buttons retain the original layout and artwork and call the existing navigation methods. The pictured plus opens the existing shop. The 16:9 composition fits within the safe area; other aspect ratios preserve it without cropping, with margins as needed.

`BackgroundPlate.png`, previously reconstructed using the built-in imagegen skill, is used only under the separated interface rectangles. No generated character or head replaces any part of the now-static composition. Refresh import settings with Tools > Farm Guardian > Import Layered Reference Menu.

Latest gameplay and validation details are documented in CARRY_AND_DIFFICULTY.md. Earlier animated-menu test reports are historical and do not describe the current menu.
