# Cozy gameplay and level-selection UI

`FarmCozyUiSkin` applies the locally supplied dobo_ui **DEMO Cozy UI Pack** to
level selection, mission introduction, the gameplay HUD, pause, all settings,
shop upgrades, outfit cards and results. Main-menu buttons and the coin counter
also use Cozy artwork; its illustrated scenery and logo are retained.
Progression and saved star ratings are unchanged.

The original PNG artwork is copied without modification into `Resources/CozyUI`
so builds can load it without Editor APIs or a setup menu. The supplied developer
note is retained alongside it. Source: `Assets/DEMO_Cozy_UI_Pack_doboui/DEMO_Cozy_UI_Pack_doboui`.

| UI element | Supplied artwork |
| --- | --- |
| Level nodes, pause button, joystick thumb | RoundButton1_white |
| Joystick base | RoundButton1_wood |
| Action buttons | SquareButton2_wood |
| Catch thief / Retry / Reset progress | SquareButton2_red |
| Back / Replay / Level Select / currency / timer | RoundButton2_white |
| Mission panels, dialogs, headings | WoodenContainer1 |
| Panel interiors | NoteContainerSimple |
| Previous / next arrows | ArrowBrownOuline22, mirrored for next |

Sprites are created once from the original textures with nine-slice borders for
rectangular surfaces. Colored artwork stays white-tinted to preserve its palette;
the timer changes text color for urgency. Old prefab gloss/outlines are disabled
on scoped instances only. Existing Button, joystick and hold-event components
remain in use, and decorative images do not block clicks.

The demo lacks the mission-specific glyphs and star ratings needed by this game,
so existing icons are retained. Saved best stars remain below each level number;
stars receive a brown outline for contrast over the cream Cozy buttons.

## English copy and type hierarchy

All live player-facing copy is English, including PICK UP ANIMAL, DELIVER ANIMAL,
carry instructions, pickup status and the thief-exit warning. Heading, button,
level-number, timer and world-sign typography uses **Coiny**, a rounded bubble
display font. Regular descriptions and mission details use **Balsamiq Sans**.
Both are unmodified Google Fonts sources with their OFL license files included
under `Resources/CozyUI/Fonts`:

- https://github.com/google/fonts/tree/main/ofl/coiny
- https://github.com/google/fonts/tree/main/ofl/balsamiqsans

`FarmUiTypography` creates and caches dynamic TMP atlases at runtime, with the
existing bundled TMP font as a fallback for punctuation. Source fonts are included
in builds. Editor-time shared prefab creation keeps persistent references.

## Immediate thief movement

The visible four-second spawn delay was removed. Activation and post-escape
reappearance immediately choose a secured animal and start the NavMesh path.
Without a secured target the thief patrols the response hub and pens while
checking for a target. The existing `thiefStartDelay` field now controls only
the short no-target retry interval, capped at 0.25s. Stun duration is unchanged.

PlayMode checks and render previews are recorded in `Logs/FarmGuardian-cozy-ui-tests.xml`
and `Logs/FarmGuardian-cozy-*.png` after running the matching UI test.

## Settings and upgrades

Settings includes only a live sound-effects volume percentage, wooden slider track
and cream handle, and red reset buttons with confirmation (plus Back navigation).
Graphics selection and the controls guide are no longer shown. Set volume to zero
to mute. The compact panel uses the same layout from the menu and paused game.
Paused settings keeps the game paused. Device preferences still save automatically.
The shop uses a two-by-two upgrade layout with icon badges, descriptions, current
level/bonus, price, and purchase/max state. The active shop tab uses wood artwork;
the inactive tab uses cream artwork. No purchase costs or upgrade effects changed.
The focused regression report is `Logs/FarmGuardian-cozy-settings-upgrades-tests.xml`.
Validation: all three focused PlayMode tests passed, including navigation,
purchases, paused settings and reset confirmation. Settings and
upgrade previews were rendered at 1920x1080, 1440x1080 and 2340x1080; the desktop
renders were visually inspected for hierarchy, legibility and overlap.

## Optical alignment

Cozy captions use TMP Midline (visible glyph geometry) rather than font-metric
centering. Caption rectangles sit slightly above the sprite's full rectangle to
exclude its lower drop-shadow: wood +7, cream capsule +4, circular controls 5.5%
of height. Icon-bearing buttons share that vertical axis. Active/inactive shop
tabs refresh their caption inset along with their artwork.
Upgrade badges clear the notebook gutter and share a fixed content grid. Skin
previews and their text columns have separate, non-overlapping widths. Level
numbers and star rows have dedicated centered rectangles above the lower rim.
The three focused alignment/navigation PlayMode checks pass in
`Logs/FarmGuardian-alignment-tests.xml`. The glyph-centering assertion uses actual
character vertices, not TMP textBounds (which includes font metric whitespace).
Updated Upgrade, Skins, Settings and Level Selection renders were visually checked;
tablet upgrade and ultrawide settings previews were also inspected.

## Cozy main menu

START, UPGRADES and SETTINGS are live nine-sliced wooden buttons with Coiny
captions and the existing mission/cart/gear glyphs. Stable PLAY/SHOP object names
are retained for navigation compatibility. The cream coin capsule displays the
saved balance and its round plus button opens the existing shop. No new currency
purchase flow is introduced. Scenery/logo patches remain static.
Coin artwork and the paged rarity-based skin collection are now described in
`SKIN_COLLECTION.md`; its catalogue replaces the original two-outfit cards.
The existing clean plate is blended at the removed button/counter edges by
`FarmMenuBackdropPatch`; source PNGs are unchanged. The logo is not patched out.
Five focused PlayMode tests pass in `Logs/FarmGuardian-cozy-main-menu-tests.xml`,
covering Start/Upgrade/Settings/coin navigation, live balance, mute/volume,
pause/back behavior, reset cancellation/confirmation and the existing Cozy screens.
New previews: `FarmGuardian-cozy-main-menu-{width}.png` and
`FarmGuardian-minimal-settings-{width}.png` at widths 1920, 1440 and 2340.
