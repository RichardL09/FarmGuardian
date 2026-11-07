# Skin collection and clover coin

| Skin | Rarity | Coins |
| --- | --- | --- |
| Classic Farmer | Common | Free |
| Tourist | Uncommon | 200 |
| Queen | Rare | 400 |
| Ninja | Epic | 700 |
| Samurai | Legendary | 1100 |
| Golden Sensei | Mythic | 1600 |
| Golden Farmer (legacy outfit) | Rare | 300 |

All outfits are cosmetic. The original character collider, movement speed,
interactions and carry behavior are retained. New models have normalized 1.8m
walking rigs using the existing editable Farmer Animator Controller.
Samurai and Golden Sensei meshes are rotated 180 degrees during rig generation to
match the other characters' +Z forward axis, rather than rotating their UI only.

Tourist, Queen and Ninja use the supplied OBJ/texture pairs in their respective
asset folders. Samurai is extracted from `Assets/samurai/source/samurai.zip` into
`ImportedSkins/Samurai`. Golden Sensei's outer ZIP contains another ZIP; its
`dynamitron.obj` / `Ninja1` model has no supplied texture and a blue MTL. The model
from that folder is used with a new gold material, not represented as a textured
sensei model. Archives and original source files are unchanged.

Generated prefabs are in `Resources/Skins`, meshes in `Meshes`, and materials in
`Materials/Skins`. Rebuild with Tools > Farm Guardian > Import New Skin Collection
after extracting the supplied archives. The three-card carousel shows rarity,
actual 3D preview, price/ownership and Buy/Equip state.

The v1 save key is retained. New `ownedSkins` and `equippedSkin` fields coexist
with the old Golden Farmer flags. Older saves retain ownership, coins, stars,
unlocked levels and upgrades. Buying auto-equips; already owned skins never charge
again. Invalid IDs and insufficient funds are rejected. Reset clears the collection.

Every live coin icon is routed through `FarmUiFactory.Symbol` to the supplied
clover artwork (`golden-coin-with-clover-icon/JEMA GER 1640-11.jpg`). The unmodified
JPG is copied into `Resources/CozyUI/CloverCoin.jpg`, explicitly imported as a
Texture2D at 512px (not an automatically detected Cubemap). Circular
sprite geometry clips its white corners without editing the bitmap. Its colors
are preserved; no brown theme tint is applied.

Sprite geometry is supplied in pixel-based Sprite Rect space; Unity applies the
pivot and unit conversion. See [Unity Sprite.OverrideGeometry](https://docs.unity.cn/6000.0/Documentation/ScriptReference/Sprite.OverrideGeometry.html).

## Validation

`Logs/FarmGuardian-skin-collection-tests.xml` records 27/27 passed PlayMode tests:
the complete gameplay fixture and five UI/collection regression checks. The new
collection check buys/equips all five skins, verifies saved ownership and equipped
IDs, rejects insufficient funds/invalid IDs, prevents repeated charges, checks
legacy Golden Farmer migration, and confirms reset clears the collection. Coin
art is checked for its texture, white tint and circular mesh.

All three catalogue pages were rendered at 1920x1080, 1440x1080 and 2340x1080.
Desktop pages, tablet layout and equipped Queen/Golden Sensei gameplay were
visually inspected. Generated prefab/material/mesh hashes match the tested copy.
Previews are `Logs/FarmGuardian-skin-collection-page{page}-{width}.png` and
`Logs/FarmGuardian-equipped-{skin-id}.png`.
