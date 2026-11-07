using UnityEditor;
using UnityEngine;

namespace FarmGuardian.Editor
{
    public static class FarmReferenceMenuImport
    {
        [MenuItem("Tools/Farm Guardian/Import Layered Reference Menu")]
        public static void Import()
        {
            foreach(string name in new[]{"DesignSource","BackgroundPlate"})
            {
                string path="Assets/FarmGuardian/Resources/ReferenceMenu/"+name+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null)throw new System.InvalidOperationException("Menu artwork missing: "+path);
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;importer.isReadable=false;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();Debug.Log("FARM_LAYERED_MENU_IMPORT_OK");
        }
    }
}

