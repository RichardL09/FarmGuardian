using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FarmGuardian.Editor
{
    public static partial class FarmSceneryTools
    {
        static void GrassTufts()
        {
            var group=Group("Grass blade meadows - no miniature trees",root);
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Fresh blade grass",color=new Color(.25f,.53f,.10f)};
            material.SetFloat("_Smoothness",0);AssetDatabase.AddObjectToAsset(material,set);
            var vertices=new Dictionary<Vector2Int,List<Vector3>>();var indices=new Dictionary<Vector2Int,List<int>>();int tufts=0;
            for(int row=0;row<53;row++)for(int col=0;col<66;col++)
            {
                var p=P(-19.5f+col*.6f+(Hash(col,row,8)-.5f)*.34f,-15.5f+row*.6f+(Hash(row,col,9)-.5f)*.34f);
                if(!Clear(p,.12f,false)||Hash(col,row,10)<.12f)continue;
                var key=new Vector2Int(Mathf.FloorToInt(p.x/9),Mathf.FloorToInt(p.z/9));
                if(!vertices.ContainsKey(key)){vertices[key]=new List<Vector3>();indices[key]=new List<int>();}
                var verts=vertices[key];var tris=indices[key];tufts++;
                for(int blade=0;blade<7;blade++)
                {
                    float angle=Hash(col+blade,row,11)*Mathf.PI*2;
                    var side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    var origin=p+new Vector3((Hash(col,row,blade)-.5f)*.28f,.035f,(Hash(row,col,blade+4)-.5f)*.28f);
                    float height=.16f+Hash(col+blade,row,12)*.21f,width=.025f+Hash(row+blade,col,13)*.028f;
                    var a=origin-side*width;var b=origin+side*width;
                    var tip=origin+Vector3.up*height+side*(Hash(col,blade,14)-.5f)*.12f;
                    int start=verts.Count;verts.Add(a);verts.Add(b);verts.Add(tip);verts.Add(a);verts.Add(tip);verts.Add(b);
                    for(int t=0;t<6;t++)tris.Add(start+t);
                }
            }
            foreach(var pair in vertices)
            {
                var mesh=new Mesh{name=map.Name+" real grass blades "+pair.Key,indexFormat=IndexFormat.UInt32};
                mesh.SetVertices(pair.Value);mesh.SetTriangles(indices[pair.Key],0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.AddObjectToAsset(mesh,set);
                var chunk=Group("Grass patch "+pair.Key,group);chunk.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=chunk.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.receiveShadows=true;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            Debug.Log($"FARM_GRASS_BUILT: {map.Name}, tufts={tufts}, render chunks={vertices.Count}");
        }
    }
}
