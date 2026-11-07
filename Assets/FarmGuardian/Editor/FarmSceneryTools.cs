using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FarmGuardian.Editor
{
    public static partial class FarmSceneryTools
    {
        public const string TownPath="Assets/UTS_Project/village-town-assets/source/PictureCrossTownsAssets.fbx";
        public static void InspectAssets()
        {
            var output=new StringBuilder();
            foreach(string guid in AssetDatabase.FindAssets("t:GameObject",new[]{"Assets/UTS_Project"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(path.Contains("OBJ format")||path.Contains("Previews"))continue;
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(root==null)continue;
                output.AppendLine("SOURCE "+path);
                foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
                {var r=f.GetComponent<Renderer>();output.AppendLine(f.name+" | vertices="+f.sharedMesh.vertexCount+" | bounds="+f.sharedMesh.bounds+" | world="+r.bounds+" | material="+(r.sharedMaterial!=null?r.sharedMaterial.name:"none"));}
            }
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/FarmGuardian-source-assets.txt",output.ToString());
            Debug.Log("FARM_SCENERY_INVENTORY_OK");
        }
    }
}
