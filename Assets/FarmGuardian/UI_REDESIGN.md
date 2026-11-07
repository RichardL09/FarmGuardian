This document records the earlier UI-only redesign. Current manual carry/delivery, animal difficulty, static menu and 100-level campaign supersede its earlier gameplay assumptions; see CARRY_AND_DIFFICULTY.md and MENU_LAYERING.md.

# Farm Guardian — pastel UI on the original scene

Use `Assets/FarmGuardian/Scenes/FarmGuardian.unity`, the first enabled build scene. The Reference Edition scene, generated art/models/configuration, editor builder, runtime presentation scripts and exclusive tests were removed. Its scene-specific carrying, camera-relative controls and recovery hooks were also removed; the original scene's existing animal-return flow is retained. No new scene, game manager, currency, inventory or progression controller is introduced.

## Inspection and boundaries

The existing implementation uses one FarmSession, one runtime FarmHud Canvas, FarmUiFactory, fourteen reusable UI prefabs, FarmUiTheme, vector icon sprites, FarmSave and FarmUiPreferences. Unity 6000.5.9f1 uses URP, Input System and TextMeshPro. The available font is the installed TMP Liberation Sans SDF with its supplied font/license; no additional font or unrelated icon pack is downloaded.

The original level population, planting rules, time limits, animal and thief behavior, interaction durations, rewards, purchase prices, upgrade limits, save key, map layout, scenery assets, characters and gameplay camera remain authoritative. Hashes of the original scene, config, bootstrap, gameplay sources, environment data, render settings and package manifest are recorded in `Logs/FarmGuardian-pastel-preserved.json` after removal of the Reference-only hooks, for comparison after the UI build.

Supported settings are sound-effects volume, the installed graphics-quality profiles and confirmed progress reset. The original scene has no music playback, vibration or localization system, so those controls are omitted. The reference-only synthesized music source was removed with that scene. The game records unlocked levels, not star ratings; a small completion-star icon on a completed map node means completion and is not an invented scored rating. Result screens use a completion/check symbol rather than claiming stars earned.

## Shared design system

`FarmUiTheme.ApplyPastelPalette()` defines lavender `#B9A4D5`, light lavender `#D7C8E8`, cream `#FFF2D8`, pink `#F28BA8`, green `#91D66F`, blue `#72C6E8`, yellow `#FFD56B`, orange `#FFAD66`, muted brown `#8C725F`, dark text `#59483F` and soft red `#E98989`.

Cream surfaces, thin pastel outlines, gentle 4px shadows and low-opacity gradients replace strong borders and raised dark edges. Text has no outline. Green is the main action color, blue is secondary and lavender supports navigation. Buttons have normal/pressed/disabled color states, a 3% press scale and a smooth return. Panel entry is a brief 0.18s fade/scale; exits take 0.12s. Coin feedback is a restrained 3.5% text pulse. There are no spinning, flashing or particle effects.

Regenerate the shared assets using **Tools > Farm Guardian > Build UI Kit** outside Play Mode. This updates only `Resources/UI` and its shared prefab bindings; it does not rebuild the farm or reset progress.

## Screens and incremental checks

1. **Main Menu:** a restrained cream veil over the existing 3D menu farm, a TMP Farm Guardian title, green PLAY, blue SHOP, lavender SETTINGS and a compact wallet. PLAY opens existing level selection. No bottom-left level counter is added. Test each navigation button and return with BACK.
2. **Level Selection:** `FarmUiMapPath` draws a smooth winding path, with 15 rounded nodes per page. It is a visual Graphic, not a navigation/progression controller. Locked nodes can be inspected but cannot start; the cream detail panel reads actual objectives, time and base reward plus the existing time-bonus explanation. Test locked previews, an unlocked level and the final page ending at level 10000.
3. **Shop / Upgrades:** four existing upgrades appear in a landscape 2×2 arrangement, with icon, explanation, actual level/current bonus, cost and an affordability-aware UPGRADE button. The four-card set fits the panel without scrolling; no new catalogue is invented. Test unaffordable, successful, rapid-repeat and maximum-level purchases.
4. **Skins:** only Classic Farmer and the already supported Golden Outfit appear. Both preview the actual farmer asset; the golden variant is the existing cosmetic tint. Ownership, affordability and equipped state drive BUY/EQUIP/EQUIPPED. Test buying once, equipping either outfit and revisiting the screen.
5. **Settings:** rounded SFX slider, actual graphics profiles and reset confirmation. CANCEL preserves progress; RESET calls the existing reset function. Opening Settings from Pause keeps the game paused. No unsupported setting is displayed.
6. **Gameplay HUD:** compact mission panel at top-left, timer near top-centre, coins/pause at top-right, existing joystick bottom-left and contextual action buttons bottom-right. Planting appears only for unfinished planting objectives; Catch appears after the thief becomes active. Enabled state still uses the existing range/line-of-sight checks. Timer warnings use muted red, and mission/hold progress uses the existing values.
7. **Pause / results:** a light lavender overlay with Resume, Restart, Settings and Main Menu. Victory shows actual objective counts, coins and existing next/replay/home actions. Defeat shows the actual reason, progress, retry/select/home actions. All screens share the same UI prefabs and typography.

The Canvas keeps a 1920×1080 reference resolution, Scale With Screen Size, device safe-area anchors and a uniformly fitted design root. Controls remain within the safe root on tested landscape ratios. Background overlays extend beyond it to avoid uncovered margins. All live UI text is TMP, all controls are real Unity components and no full-screen screenshot replaces an interactive page.

## Validation

The original gameplay/navigation regression tests are retained. UI tests click actual raycast targets, verify locks and purchase guards, check paused Settings and reset confirmation, exercise every node on a page and the last progression page, and record screens at 1920×1080, 1440×1080 and 2340×1080. See `Logs/FarmGuardian-pastel-tests.xml` and `Logs/FarmGuardian-ui-*.png` for executed evidence after the run. Final results are appended only after execution and visual inspection. Physical Android/iOS profiling is not claimed.

The pastel UI regression run completed with 20/20 passing tests (90.30 seconds; see FarmGuardian-pastel-tests.xml). The subsequent latest-reference layered Main Menu supersedes the earlier menu title/composition described above; see MENU_LAYERING.md for its exact artwork, 2.5D animation, currency typography limitation and validation.

