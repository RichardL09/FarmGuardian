# Main menu artwork

Generated with the built-in **imagegen** tool from the user's supplied menu reference, `codex-clipboard-f0af5175-fee9-4a49-8050-6dab7cfc0e7d.png`. The original attachment is a style/composition reference, not a runtime dependency. No CLI/API fallback was used.

Project-bound outputs:

- `Assets/FarmGuardian/Art/MainMenu/MenuBackground.png`: full landscape illustration without interface graphics.
- `Assets/FarmGuardian/Art/MainMenu/MenuLogo.png`: transparent Farm Guardian logo.
- `Assets/FarmGuardian/Art/MainMenu/MenuButton.png`: transparent blank wooden board shared by four interactive buttons.

The importer creates persistent Sprite `.asset` files alongside these PNGs, trims transparent margins from the logo/button, and assigns them to FarmGuardianConfig. Text, icons, coin balance and button callbacks remain real Unity UI. Original 3D models and gameplay animations are unchanged by this menu update.

## Exact prompt set

### Background (transparent_background: false)

Use case: precise-object-edit. Asset type: production Unity mobile landscape game main-menu BACKGROUND, wide 16:9 composition, preferably 1920x1080. Edit the supplied reference image. Remove ALL interface graphics: the large FARM GUARDIAN wooden logo, all four wooden menu buttons and their text/icons, and the top-right coin counter. Fill those areas naturally with the same blue sky, distant hills, river and farm scenery. Preserve the charming seated farmer on the wooden fence at lower LEFT, cow and chicken beside him, red barn at left, windmill, straw bales, lantern, foreground flowers and lush vegetation, sunny sky and soft mountains. Keep their original relative placement and proportions, warm daylight and polished colorful stylized 3D illustration. The entire right half must be quiet open scenic negative space suitable for separately overlaid UI, without new prominent foreground objects there. This is background art only, with NO text, NO logo, NO buttons, NO currency display, NO watermark. Preserve composition and visual style of the reference.

### Logo (transparent_background: true)

Use case: background-extraction. Asset type: separate transparent PNG logo for Unity game UI. Extract/recreate ONLY the large FARM GUARDIAN wooden logo from the supplied reference, including red barn on its top, straw hat upper-right, green leaves around its edges, gold chunky FARM lettering, cream chunky GUARDIAN lettering, and the small lower wooden ribbon reading exactly PROTECT · CAPTURE · GROW. Preserve the reference's typography, warm bevels, dark brown outlines, attractive 3D painted game art and overall proportions. Center the complete logo, tightly framed with just 3 percent transparent padding. Entire surrounding background must be genuine alpha transparency. NO farm background, NO farmer character, NO menu buttons, NO coin counter. Do not crop any leaves, hat, barn or logo edges.

### Button (transparent_background: true)

Use case: precise-object-edit. Asset type: ONE reusable blank horizontal wooden menu button sprite, transparent PNG, for Unity mobile game. Match the wooden button in the supplied reference: chunky rounded horizontal board made from two warm caramel-brown wood planks, a fine cream highlight at the top edge, dark brown beveled border and soft lower edge shadow, subtle wood grain, and small green leaf sprigs peeking out at both short ends. Wide 4.5:1 button shape, filling most of a wide canvas with only 4 percent transparent padding. Keep its long central face spacious and clean for separate Unity text and icons. NO text, NO letters, NO symbols or icons on the board. ONLY ONE blank wooden board isolated on genuine alpha transparency. No logo, no background, no characters, no extra button variations or sprite sheet.
