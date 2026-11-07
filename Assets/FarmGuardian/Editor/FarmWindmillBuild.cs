using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FarmGuardian.Editor
{
    public static class FarmWindmillBuild
    {
        [MenuItem("Tools/Farm Guardian/Build Animated Windmill From Existing Model")]
        public static void Build()
        {
            string path="Assets/UTS_Project/village-town-assets/source/PictureCrossTownsAssets.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);bool readable=importer.isReadable;
            GameObject root=null;
            try
            {
                importer.isReadable=true;importer.SaveAndReimport();
                var config=AssetDatabase.LoadAssetAtPath<FarmConfig>("Assets/FarmGuardian/Data/FarmGuardianConfig.asset");
                root=new GameObject("Source windmill extraction");
                var model=FarmUiModels.Model(FarmUiModels.Prop(config,"Windmill"),root.transform,Vector3.zero,1,config.environmentMaterial);
                var filter=model.GetComponentInChildren<MeshFilter>();var mesh=filter.sharedMesh;
                var positions=mesh.vertices;var triangles=mesh.triangles;var normals=mesh.normals;
                var parents=new int[positions.Length];var weld=new Dictionary<string,int>();
                int Find(int a){while(parents[a]!=a){parents[a]=parents[parents[a]];a=parents[a];}return a;}
                void Union(int a,int b){parents[Find(a)]=Find(b);}
                for(int i=0;i<positions.Length;i++)
                {
                    parents[i]=i;positions[i]=filter.transform.TransformPoint(positions[i]);normals[i]=filter.transform.TransformDirection(normals[i]).normalized;
                    var v=positions[i];string key=$"{Mathf.RoundToInt(v.x*100000)},{Mathf.RoundToInt(v.y*100000)},{Mathf.RoundToInt(v.z*100000)}";
                    if(weld.TryGetValue(key,out int j))Union(i,j);else weld[key]=i;
                }
                for(int i=0;i<triangles.Length;i+=3){Union(triangles[i],triangles[i+1]);Union(triangles[i],triangles[i+2]);}
                var bounds=new Dictionary<int,Bounds>();
                for(int i=0;i<positions.Length;i++)
                {int id=Find(i);if(!bounds.ContainsKey(id))bounds[id]=new Bounds(positions[i],Vector3.zero);var b=bounds[id];b.Encapsulate(positions[i]);bounds[id]=b;}
                var rotating=new HashSet<int>();
                // Verified normalized source: blade beams and lattice lie in this thin front plane.
                // Windows, tower, roof and the spindle have different depth bounds and stay static.
                foreach(var pair in bounds)if(pair.Value.min.z>.18f&&pair.Value.max.z<.21f)rotating.Add(pair.Key);
                var bodyIndices=new List<int>();var rotorIndices=new List<int>();
                for(int i=0;i<triangles.Length;i+=3)
                {var list=rotating.Contains(Find(triangles[i]))?rotorIndices:bodyIndices;list.Add(triangles[i]);list.Add(triangles[i+1]);list.Add(triangles[i+2]);}
                if(rotorIndices.Count<400||bodyIndices.Count<1000)throw new System.InvalidOperationException("Source mesh no longer matches verified windmill blade bounds. Inspect before rebuilding.");
                const string assetPath="Assets/FarmGuardian/Resources/FarmWindmill.asset";
                var art=AssetDatabase.LoadAssetAtPath<FarmWindmillAsset>(assetPath);
                if(art==null){art=ScriptableObject.CreateInstance<FarmWindmillAsset>();AssetDatabase.CreateAsset(art,assetPath);}
                else foreach(var sub in AssetDatabase.LoadAllAssetsAtPath(assetPath))if(sub!=art)Object.DestroyImmediate(sub,true);
                art.Pivot=new Vector3(.000415f,.59109f,.1933f);art.Axis=Vector3.forward;
                Mesh Make(string name,List<int> indices,Vector3 pivot)
                {
                    var verts=(Vector3[])positions.Clone();for(int i=0;i<verts.Length;i++)verts[i]-=pivot;
                    var result=new Mesh{name=name};result.vertices=verts;result.normals=normals;result.uv=mesh.uv;result.triangles=indices.ToArray();result.RecalculateBounds();AssetDatabase.AddObjectToAsset(result,art);return result;
                }
                art.Body=Make("Existing windmill tower and spindle",bodyIndices,Vector3.zero);
                art.Rotor=Make("Existing four blade beams and lattice",rotorIndices,art.Pivot);
                EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
                Debug.Log($"FARM_WINDMILL_SPLIT_OK: body triangles={bodyIndices.Count/3}, rotor triangles={rotorIndices.Count/3}, rotating components={rotating.Count}");
            }
            finally{if(root!=null)Object.DestroyImmediate(root);importer.isReadable=readable;importer.SaveAndReimport();}
        }
    }
}
