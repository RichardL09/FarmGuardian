# Farm Guardian — UI/UX implementation

The current pastel redesign is documented in [UI_REDESIGN.md](UI_REDESIGN.md), including scope and executed validation. The interface uses real Unity Canvas/Image/Button/Slider components, a Graphic winding-map path, TextMeshPro, reusable prefabs, the existing 3D menu farm and actual character previews. No screenshot replaces interactive controls. The old bottom-left main-menu level selector is absent; PLAY opens Level Select.

## Files and staged implementation

| Stage | Created/modified | Result and verification |
|---|---|---|
| 1: inspection | Read existing scripts, packages, settings, assets and pasted brief | Existing state/save/currency/gameplay APIs retained. TMP found in UGUI; Essential Resources provide the bundled Liberation Sans font and licence. |
| 2: shared style | `FarmUiTheme`, `FarmUiFactory`, `FarmUiElement`, `Editor/FarmUiSetup`, generated `Resources/UI` assets | Cream panels, thin pastel outlines, subtle gradients/shadows, lavender pill titles, gentle button states, existing icon sprites and fourteen shared prefabs. |
| 3: menu/navigation | `FarmHud`, existing `FarmMenuDiorama` and `FarmUiModels` | Existing 3D farm under a cream veil. Green PLAY, blue SHOP, lavender SETTINGS and the actual saved balance. Skins is in Shop. No new session/navigation system. |
| 4: supporting screens | `FarmHud`, existing `FarmSkinPreview` and `FarmUiPreferences`, new visual `FarmUiMapPath` | Existing upgrade/skin purchase states, actual model previews, SFX volume, graphics quality, confirmed reset and a winding level map reading existing progression. |
| 5: HUD | `FarmHud` using existing `FarmJoystick` / `FarmHoldButton` | Objective totals, timed warning colour, actual coin balance, pause, joystick and contextual capture/catch/plant actions. Smooth mission/hold feedback. |
| 6: pause/results | `FarmHud`, settings-return context in `FarmSession` | Resume/restart/settings/menu; paused settings return to Pause with time still frozen. Win/lose summaries read the existing result/reward and never award currency from UI callbacks. |
| 7: validation | Updated gameplay/presentation checks; new `FarmUiTests` | UI navigation, locked levels, duplicate-purchase rejection, idempotent outfit equip, volume/quality, pause context, reset confirmation and landscape screenshots. Existing map/AI/rig regressions remain in the suite. |

Existing AI, rescue, planting, map layouts, level-generation rules, upgrade effects and reward calculations are unchanged. State additions only connect UI screens. Presentation settings use `FarmGuardian.UI.*` PlayerPrefs keys; progress continues to use `FarmGuardian.Save.v1` with no second coin or unlock store.

## Inspector configuration and required assets

1. Open `Scenes/FarmGuardian.unity`. The existing bootstrap prefab still owns the session and UI. Do not add a second Canvas controller or GameManager.
2. Select `Resources/UI/FarmUiTheme.asset`. Its Font references the bundled `LiberationSans SDF`; Rounded/Circle/Gloss and Icons reference generated sprite assets. The fourteen prefab references must be assigned. Colours are editable here.
3. Shared prefabs in `Resources/UI/` are PrimaryButton, SecondaryButton, IconButton, RibbonTitle, CreamPanel, CurrencyCounter, ItemCard, SkinCard, UpgradeCard, LevelButton, ProgressBar, PopupPanel, PausePanel and ResultPanel. Their `FarmUiElement` bindings expose Face, Label and Button as appropriate. Runtime screens supply size, data and callbacks; they reuse the same artwork and component hierarchy.
4. If references are missing, run **Tools > Farm Guardian > Build UI Kit**. This imports TMP Essential Resources from the installed UGUI package if necessary and builds the theme, shared prefab and sprite assets. It requires a graphics-enabled Editor for the icon bake. Rebuilding preserves asset GUIDs but refreshes generated prefab contents; save custom styling in the theme or extend the builder before regenerating.
5. `Data/FarmGuardianConfig.asset` retains all existing model/material/logo references. The menu diorama reads those real models; skin previews use the supplied generated Farmer prefab and existing golden material tint. No new skin model is assumed. Original menu illustration/wooden-board assets remain available but are no longer used for the new live background/colourful buttons.
6. Canvas Scaler uses **1920 × 1080**, Scale With Screen Size, Match 0.5. A safe-area root contains a centred design root scaled to fit the available rectangle. This keeps the entire layout visible on wide phones and 4:3 tablets without stretching text. The 3D environment fills the viewport. Inspect the generated Canvas in Play Mode; it is created by the UI controller.

TMP Essential Resources include their original font licence under `Assets/TextMesh Pro/Fonts`. UI sprites are generated from authored geometric shapes and the existing FarmUiIcon definitions; no reference-image pixels are used as button labels or hit targets.

## Navigation and data

- Main Menu → PLAY → Level Select → level introduction → gameplay.
- Winding map pages contain up to 15 levels. Earlier completed nodes get a small completion-star icon, not an invented star score; the unlocked frontier remains based on FarmSave. Locked nodes can show information but cannot start. The information panel labels base reward separately from the remaining-time bonus.
- Shop provides Upgrades and Skins. Upgrade purchases call the existing upgrade method with a short shared UI purchase guard; unaffordable/max items disable their action. Golden outfit purchase is one-time, and equipping an owned outfit is idempotent. SELECT only highlights a skin; BUY/EQUIP performs the explicit action.
- Settings exposes a real SFX slider and actual project quality presets. Reset opens a blocking confirmation; Cancel keeps progress; Reset replaces only Farm Guardian progress and returns to the menu. Device preferences are retained.
- Pause → Settings → Back returns to Pause at time scale zero. UI feedback uses unscaled time. Old pages immediately stop accepting raycasts/interaction and release held input, then fade out over 0.12 seconds while the new screen is already interactive.
- Win reads earned coins from FarmSession. Next/replay/menu do not award coins again. Lose offers retry, level selection and menu.

## How to test

Run the Play Mode Test Runner (`FarmGameplayTests`, `FarmMapTests`, `FarmPresentationTests`, `FarmUiTests`). Tests back up/restore the progress save, and the UI tests also restore changed presentation preferences. Graphics-enabled runs save screen captures under the project `Logs` folder at 1920×1080, 2340×1080 and 1440×1080. The renderer-only menu has no NavMesh, thief AI, progression updates or mission timers.

Manually verify touch dragging on the joystick and volume slider, holding catch/plant, readable text, notch safe areas, navigation while paused, Cancel/Reset distinction and outfits after restarting the game. Profile the menu's model/Animator rendering and skin RenderTextures on a physical phone. Preview cameras render once per preview and are disposed with their page; generated road/map systems remain separate.

## Deliberate limits

- No music track or BGM system exists, so there is no pretend Music Volume control. Supply a licensed music clip and its playback integration before adding it.
- No localization, vibration preference, IAP/coin purchase service or earned-star system exists. Those optional controls/counters are omitted.
- Only Classic Farmer and the existing Golden Outfit tint are shown. Additional skins require actual compatible assets.
- Desktop render and automated interaction checks do not establish physical-device performance, touch comfort or every device's safe-area behaviour. These remain device acceptance checks.
