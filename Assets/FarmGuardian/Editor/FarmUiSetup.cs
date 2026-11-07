using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FarmGuardian.Editor
{
    public static class FarmUiSetup
    {
        const string Root="Assets/FarmGuardian/Resources/UI/";
        [MenuItem("Tools/Farm Guardian/Build UI Kit")]
        public static void Build()
        {
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")==null)
            {
                string package=null;
                foreach(var dir in Directory.GetDirectories("Library/PackageCache","com.unity.ugui*"))
                { string file=dir+"/Package Resources/TMP Essential Resources.unitypackage"; if(File.Exists(file)) {package=file;break;} }
                if(package==null) throw new InvalidOperationException("TMP Essential Resources package not found.");
                ImportEssentials(package); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var theme=AssetDatabase.LoadAssetAtPath<FarmUiTheme>(Root+"FarmUiTheme.asset");
            if(theme==null) {theme=ScriptableObject.CreateInstance<FarmUiTheme>(); AssetDatabase.CreateAsset(theme,Root+"FarmUiTheme.asset");}
            theme.ApplyPastelPalette();
            theme.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if(theme.font==null) throw new InvalidOperationException("TMP essential import did not produce its bundled font.");
            theme.rounded=Shape("Rounded",false,false); theme.circle=Shape("Circle",true,false); theme.gloss=Shape("Gloss",false,true);
            string[] names={"PrimaryButton","SecondaryButton","IconButton","CreamPanel","RibbonTitle","CurrencyCounter","ItemCard","SkinCard","UpgradeCard","LevelButton","ProgressBar","PopupPanel","PausePanel","ResultPanel"};
            var prefabs=new GameObject[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                bool isButton=i<3||i==9;
                var element=FarmUiFactory.BuildTemplate(theme,names[i],isButton,i==4);
                if(i==4)
                {
                    element.Label.color=theme.ink;element.Label.outlineWidth=0;
                }
                prefabs[i]=PrefabUtility.SaveAsPrefabAsset(element.gameObject,Root+names[i]+".prefab");
                UnityEngine.Object.DestroyImmediate(element.gameObject);
            }
            theme.primaryButton=prefabs[0];theme.secondaryButton=prefabs[1];theme.iconButton=prefabs[2];theme.creamPanel=prefabs[3];theme.ribbonTitle=prefabs[4];
            theme.currencyCounter=prefabs[5];theme.itemCard=prefabs[6];theme.skinCard=prefabs[7];theme.upgradeCard=prefabs[8];theme.levelButton=prefabs[9];
            theme.progressBar=prefabs[10];theme.popupPanel=prefabs[11];theme.pausePanel=prefabs[12];theme.resultPanel=prefabs[13];
            theme.icons=new Sprite[Enum.GetValues(typeof(FarmIcon)).Length];
            for(int i=0;i<theme.icons.Length;i++) theme.icons[i]=Icon((FarmIcon)i);
            EditorUtility.SetDirty(theme); AssetDatabase.SaveAssets();
            Debug.Log("FARM_GUARDIAN_UI_KIT_OK: TMP resources, fourteen shared prefabs, sprites and theme are ready.");
        }
        static void ImportEssentials(string package)
        {
            // Unity's interactive package import is queued; extract the installed package synchronously
            // so the same builder also works in batch mode. Preserve all existing project resources.
            var entries=new Dictionary<string,byte[]>();
            using(var source=File.OpenRead(package))using(var gzip=new GZipStream(source,CompressionMode.Decompress))using(var memory=new MemoryStream())
            {
                gzip.CopyTo(memory);byte[] tar=memory.ToArray();int offset=0;
                while(offset+512<=tar.Length&&tar[offset]!=0)
                {
                    string name=Encoding.UTF8.GetString(tar,offset,100).TrimEnd('\0');
                    string sizeText=Encoding.ASCII.GetString(tar,offset+124,12).Trim('\0',' ');
                    int size=string.IsNullOrEmpty(sizeText)?0:Convert.ToInt32(sizeText,8);offset+=512;
                    if(offset+size>tar.Length)throw new InvalidDataException("Incomplete TMP resource archive.");
                    var data=new byte[size];Buffer.BlockCopy(tar,offset,data,0,size);entries[name]=data;offset+=((size+511)/512)*512;
                }
            }
            foreach(var entry in entries)
            {
                if(!entry.Key.EndsWith("/pathname",StringComparison.Ordinal))continue;
                string path=Encoding.UTF8.GetString(entry.Value).Trim();
                if(!path.StartsWith("Assets/TextMesh Pro",StringComparison.Ordinal)||path.Contains(".."))throw new InvalidDataException("Unexpected TMP resource path.");
                string prefix=entry.Key.Substring(0,entry.Key.Length-8);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if(entries.TryGetValue(prefix+"asset",out var asset)){if(!File.Exists(path))File.WriteAllBytes(path,asset);}
                else Directory.CreateDirectory(path);
                if(entries.TryGetValue(prefix+"asset.meta",out var meta)&&!File.Exists(path+".meta"))File.WriteAllBytes(path+".meta",meta);
            }
        }
        static T Save<T>(T data,string path) where T:UnityEngine.Object
        { var previous=AssetDatabase.LoadAssetAtPath<T>(path); if(previous!=null) {EditorUtility.CopySerialized(data,previous);UnityEngine.Object.DestroyImmediate(data);EditorUtility.SetDirty(previous);return previous;} AssetDatabase.CreateAsset(data,path);return data; }
        static Sprite Shape(string name,bool circle,bool gloss)
        {
            const int size=96; var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[size*size]; float radius=circle?47:22;
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                var q=new Vector2(Mathf.Abs(x-47.5f),Mathf.Abs(y-47.5f))-Vector2.one*(47-radius);
                float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
                float alpha=Mathf.Clamp01(.5f-d)*(gloss?Mathf.Pow(y/95f,3):1); pixels[y*size+x]=new Color(1,1,1,alpha);
            }
            texture.SetPixels(pixels);texture.Apply();texture=Save(texture,Root+name+"Texture.asset");
            return Save(Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,circle?Vector4.zero:Vector4.one*24),Root+name+".asset");
        }
        static Sprite Icon(FarmIcon icon)
        {
            var go=new GameObject("Icon bake",typeof(RectTransform),typeof(Canvas));go.layer=31;
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(128,128);
            var r=FarmUiFactory.Rect("Glyph",go.transform,new Vector2(112,112),Vector2.zero);r.gameObject.layer=31;
            var glyph=r.gameObject.AddComponent<FarmUiIcon>();glyph.Icon=icon;glyph.color=Color.white;
            var cameraObject=new GameObject("Icon bake camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=64;
            camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            var rt=new RenderTexture(128,128,24);var old=RenderTexture.active;camera.targetTexture=rt;
            Canvas.ForceUpdateCanvases();camera.Render();camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(128,128,TextureFormat.RGBA32,false){name="Icon "+icon};texture.ReadPixels(new Rect(0,0,128,128),0,0);texture.Apply();
            RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(go);
            texture=Save(texture,Root+"Icon"+icon+"Texture.asset");return Save(Sprite.Create(texture,new Rect(0,0,128,128),Vector2.one*.5f,100),Root+"Icon"+icon+".asset");
        }
    }
}
