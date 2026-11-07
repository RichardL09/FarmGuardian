using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmGuardian.Editor
{
    public static class FarmMenuArtSetup
    {
        const string ArtPath = "Assets/FarmGuardian/Art/MainMenu/";

        [MenuItem("Tools/Farm Guardian/Import Main Menu Artwork")]
        public static void Import()
        {
            var config = AssetDatabase.LoadAssetAtPath<FarmConfig>("Assets/FarmGuardian/Data/FarmGuardianConfig.asset");
            if (config == null) throw new System.InvalidOperationException("Farm Guardian configuration is missing.");
            Apply(config); AssetDatabase.SaveAssets();
            Debug.Log("FARM_GUARDIAN_MENU_ART_OK: Background, transparent logo and blank wooden button imported.");
        }

        public static void Apply(FarmConfig config)
        {
            if (!File.Exists(ArtPath + "MenuBackground.png")) return;
            config.menuBackground = SpriteAsset("MenuBackground", false);
            config.menuLogo = SpriteAsset("MenuLogo", true);
            config.menuButton = SpriteAsset("MenuButton", true);
            EditorUtility.SetDirty(config);
        }

        static Sprite SpriteAsset(string name, bool trimAlpha)
        {
            string texturePath = ArtPath + name + ".png";
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = trimAlpha;
            importer.mipmapEnabled = false; importer.isReadable = trimAlpha;
            importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Rect crop = new Rect(0, 0, texture.width, texture.height);
            if (trimAlpha)
            {
                var pixels = texture.GetPixels32();
                int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (pixels[y * texture.width + x].a > 8)
                        { minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }
                if (maxX <= minX || maxY <= minY) throw new System.InvalidOperationException("Empty transparent menu image: " + texturePath);
                crop = Rect.MinMaxRect(Mathf.Max(0, minX - 2), Mathf.Max(0, minY - 2), Mathf.Min(texture.width, maxX + 3), Mathf.Min(texture.height, maxY + 3));
                importer.isReadable = false; importer.SaveAndReimport();
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }
            var sprite = Sprite.Create(texture, crop, Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            string spritePath = ArtPath + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing == null) AssetDatabase.CreateAsset(sprite, spritePath);
            else { EditorUtility.CopySerialized(sprite, existing); Object.DestroyImmediate(sprite); sprite = existing; }
            return sprite;
        }
    }
}
