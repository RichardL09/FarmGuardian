using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmGuardian.Editor
{
    public static class FarmSkinAssetBuilder
    {
        [MenuItem("Tools/Farm Guardian/Import New Skin Collection")]
        public static void Build()
        {
            const string root="Assets/FarmGuardian";
            Directory.CreateDirectory(root+"/Resources/Skins");Directory.CreateDirectory(root+"/Materials/Skins");AssetDatabase.Refresh();
            string[] names={"Tourist","Queen","Ninja","Samurai","GoldenSensei"};
            string[] models={"Assets/tourist/source/Player_SkinTourist.obj","Assets/queen/source/Player_SkinQueen.obj","Assets/ninja/source/Player_SkinNinja.obj",root+"/ImportedSkins/Samurai/samurai.obj",root+"/ImportedSkins/GoldenSensei/dynamitron.obj"};
            string[] textures={"Assets/tourist/textures/Design_sem_nome_-_2026-02-17T073353.108.png","Assets/queen/textures/Design_sem_nome_-_2026-03-11T023055.208.png","Assets/ninja/textures/Design_sem_nome_-_2026-02-23T051750.832.png",root+"/ImportedSkins/Samurai/Playerskinsamuraivariant10011_diff.png",null};
            for(int i=0;i<names.Length;i++)
            {
                AssetDatabase.ImportAsset(models[i],ImportAssetOptions.ForceSynchronousImport);
                var importer=(ModelImporter)AssetImporter.GetAtPath(models[i]);importer.isReadable=true;importer.SaveAndReimport();
                string path=root+"/Materials/Skins/"+names[i]+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
                material.SetFloat("_Smoothness",.2f);
                if(textures[i]!=null)
                {
                    AssetDatabase.ImportAsset(textures[i],ImportAssetOptions.ForceSynchronousImport);
                    material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(textures[i]);material.color=Color.white;
                }
                else {material.mainTexture=null;material.color=new Color(1,.68f,.12f);material.SetFloat("_Metallic",.55f);}
                EditorUtility.SetDirty(material);
                FarmCharacterRigBuilder.Create(names[i],models[i],material,root+"/Resources/Skins",i>=3?180:0);
            }
            const string coin=root+"/Resources/CozyUI/CloverCoin.jpg";
            AssetDatabase.ImportAsset(coin,ImportAssetOptions.ForceSynchronousImport);
            var coinImporter=(TextureImporter)AssetImporter.GetAtPath(coin);coinImporter.textureType=TextureImporterType.Default;
            coinImporter.textureShape=TextureImporterShape.Texture2D;
            coinImporter.maxTextureSize=512;coinImporter.mipmapEnabled=false;coinImporter.textureCompression=TextureImporterCompression.Uncompressed;
            coinImporter.wrapMode=TextureWrapMode.Clamp;coinImporter.SaveAndReimport();
            AssetDatabase.SaveAssets();Debug.Log("FARM_SKIN_COLLECTION_READY: five walking rigs and clover coin imported.");
        }
    }
}
