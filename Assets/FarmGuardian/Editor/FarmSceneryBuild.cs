using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FarmGuardian.Editor
{
    public static partial class FarmSceneryTools
    {
        const string SetPath="Assets/FarmGuardian/Resources/FarmScenery.asset";
        const string PrefabFolder="Assets/FarmGuardian/Prefabs/Environment";
        static FarmScenerySet set;static FarmConfig config;static Material town,meadow;
        static readonly Dictionary<string,GameObject> sources=new Dictionary<string,GameObject>();
        static readonly List<Rect> occupied=new List<Rect>();
        static readonly List<GameObject> instances=new List<GameObject>();
        static readonly HashSet<string> used=new HashSet<string>();
        static FarmMapLayout map;static Transform root,cover,trees,houses,details;static int count,houseCount,treeCount;
        static readonly string[] HouseNames={"House_Blue","House_Purple","House_Red","House_2Story_Purple"};
        static readonly string[] TreeNames={"Tree_Square","Tree_Tall","Tree_Pine"};

        static Transform Group(string name,Transform parent)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Vector3 P(float x,float z)=>new Vector3(x,0,z);
        static float Hash(int a,int b,int salt=0)=>Mathf.Repeat(Mathf.Sin(a*127.1f+b*311.7f+salt*74.7f)*43758.54f,1);
        static float Distance(Vector3 p,Vector3 a,Vector3 b)
        {Vector3 d=b-a;return Vector3.Distance(p,a+d*Mathf.Clamp01(Vector3.Dot(p-a,d)/Mathf.Max(.001f,d.sqrMagnitude)));}
        static float RectDistance(Vector3 p,Rect r)
        {return new Vector2(Mathf.Max(r.xMin-p.x,0,p.x-r.xMax),Mathf.Max(r.yMin-p.z,0,p.z-r.yMax)).magnitude;}
        static Rect Footprint(Vector3 p,float w,float d)=>new Rect(p.x-w*.5f,p.z-d*.5f,w,d);
        static bool Clear(Vector3 p,float radius,bool tall)
        {
            if(Mathf.Abs(p.x)>20-radius||Mathf.Abs(p.z)>16-radius)return false;
            foreach(var route in map.Routes)for(int j=1;j<route.Nodes.Length;j++)
                if(Distance(p,route.Nodes[j-1],route.Nodes[j])<route.Width*.5f+radius+.2f)return false;
            foreach(var pen in map.Pens)if(RectDistance(p,Footprint(pen+Vector3.back*.5f,4.2f,5.8f))<radius+.2f)return false;
            foreach(var plot in map.Plots)if(RectDistance(p,Footprint(plot,3,2.6f))<radius+.15f)return false;
            foreach(var obstacle in map.Obstacles)if(RectDistance(p,Footprint(obstacle.Position,obstacle.Size.x,obstacle.Size.z))<radius+.15f)return false;
            foreach(var r in occupied)if(RectDistance(p,r)<radius+.15f)return false;
            if(tall)
            {
                foreach(var animal in map.AnimalSpawns)if(Vector3.Distance(p,animal)<radius+FarmAnimal.WanderRadius+.35f)return false;
                foreach(var entrance in map.ThiefEntrances)if(Vector3.Distance(p,entrance)<radius+1.2f)return false;
                if(Vector3.Distance(p,map.PlayerSpawn)<radius+2||Vector3.Distance(p,map.Escape)<radius+2||Vector3.Distance(p,map.ResponseHub)<radius+2)return false;
            }
            else if(Vector3.Distance(p,map.Escape)<1.4f||Vector3.Distance(p,map.PlayerSpawn)<1.0f)return false;
            return true;
        }
        static Transform Place(string name,Vector3 p,float height,Transform parent,float yaw=0,bool solid=false)
        {
            if(!sources.TryGetValue(name,out var source))throw new InvalidOperationException("Unverified scenery source: "+name);
            var wrapper=Group(name,parent);wrapper.position=p;
            var go=UnityEngine.Object.Instantiate(source,wrapper,false);go.name=name+" source geometry";go.transform.localPosition=Vector3.zero;
            foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=town;
            var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            go.transform.localScale*=height/Mathf.Max(.00001f,bounds.size.y);
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            go.transform.position+=new Vector3(p.x-bounds.center.x,p.y-bounds.min.y,p.z-bounds.center.z);
            wrapper.localRotation=Quaternion.Euler(0,yaw,0);
            bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            if(solid)
            {
                var collider=new GameObject("Solid "+name);collider.layer=FarmWorld.SolidLayer;collider.transform.SetParent(wrapper,false);
                // Collision follows the grounded building footprint; foliage uses a small trunk box.
                var box=collider.AddComponent<BoxCollider>();box.center=new Vector3(0,bounds.size.y*.5f,0);
                box.size=name.StartsWith("Tree")?new Vector3(.30f,bounds.size.y,.30f):new Vector3(bounds.size.x,bounds.size.y,bounds.size.z);
                var modifier=collider.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
                occupied.Add(Footprint(p,bounds.size.x,bounds.size.z));
            }
            used.Add(name);count++;instances.Add(go);return wrapper;
        }
        static void House(Vector3 p,int variant,float height,float yaw,bool interior)
        {
            // Compact cottages sit only in validated pockets; larger groups are beyond the
            // original solid perimeter, so the player retains every existing chase route.
            if(interior&&!Clear(p,height*(variant%4==3?.40f:.55f),true))return;
            Place(HouseNames[variant%4],p,height,houses,yaw,interior);houseCount++;
            for(int i=0;i<4;i++)
            {
                var at=p+new Vector3((i-1.5f)*.85f,0,-2.1f);
                if(!interior||Clear(at,.25f,false))Place(i%2==0?"Potted_Bush":"Daisy",at,i%2==0?.55f:.3f,details,20*i);
            }
            if(interior)StoneBranch(p);
            if(!interior)
            {
                Place("Barrell",p+new Vector3(1.8f,0,1.3f),.7f,details);
                Place("Fence",p+new Vector3(-2,0,1.6f),.7f,details,90);
                Place("Tree_Tall",p+new Vector3(2.5f,0,1.8f),3.8f,trees);
                for(int tile=0;tile<5;tile++)Place("StonePath",p+new Vector3(0,.025f,-1.4f-tile*.65f),.035f,details,0);
                // Existing crop models are decorative here and never count as planting missions.
                Place(variant%2==0?"Farm_Carrot":"Farm_Cabbage",p+new Vector3(-1.6f,0,2.7f),.35f,details);
            }
        }
        static void Villages()
        {
            var north=new[]{P(-23,23),P(-18,25),P(-12,23.5f),P(10,24),P(17,25),P(23,23)};
            for(int i=0;i<north.Length;i++)House(north[i],(i+map.Theme)%4,3.2f+Hash(i,2)*.8f,171+Hash(i,3)*18,false);
            for(int i=0;i<4;i++)
            {House(P(-25.5f-(i%2)*1.8f,-12+i*8.2f),i+map.Theme,3.0f+Hash(i,4)*.7f,81+Hash(i,5)*18,false);House(P(25.5f+Hash(i,6)*2,-12+i*8.3f),i+map.Theme+1,3.2f+Hash(i,7)*.7f,-99+Hash(i,8)*18,false);}
            for(int i=0;i<4;i++)House(P(-17+i*10.5f,-22.5f-Hash(i,9)*2),i+2,3.0f+Hash(i,10)*.6f,-9+Hash(i,11)*18,false);
            // Small named village pockets between the four pens, clear of their aprons.
            var anchors=new[]{P(-5,9),P(-5,11),P(5,-5.5f),P(5,-7),P(4.8f,6.4f),P(-15,-13),P(15,12)};
            for(int i=0;i<anchors.Length;i++)House(anchors[i],i+map.Theme,i==1?2.1f:2.4f,i<2?180:i<4?0:90,true);
        }        static void BoundaryTrees()
        {
            for(int i=0;i<20;i++)foreach(int side in new[]{-1,1})Place("Fence",P(-20+i*2.1f,side*17),.74f,details);
            for(int i=0;i<16;i++)foreach(int side in new[]{-1,1})Place("Fence",P(side*21,-16+i*2.1f),.74f,details,90);
            for(int i=0;i<23;i++)
            {
                float x=-34+i*3.1f;
                Place(TreeNames[(i+map.Theme)%3],P(x,28+Mathf.Sin(i*1.7f)*1.7f),3.7f+Hash(i,0)*2.3f,trees,Hash(i,1)*360);
                Place(TreeNames[(i+1)%3],P(x,-29+Mathf.Sin(i)*1.3f),3.4f+Hash(i,3)*1.8f,trees,Hash(i,4)*360);
            }
            for(int i=0;i<16;i++)foreach(int side in new[]{-1,1})
                Place(TreeNames[(i+1)%3],P(side*(31+Hash(i,side)*2),-24+i*3.3f),3.8f+Hash(i,side+4)*2,trees,Hash(i,side+6)*360);
            // Irregular low/high pairs along the original map boundary frame the farm without
            // placing a new collider across a perimeter entrance.
            for(int i=0;i<18;i++)foreach(int side in new[]{-1,1})
            {float x=-23+i*2.8f;Place(TreeNames[i%3],P(x,side*(18.8f+Hash(i,side)*1.6f)),1.6f+Hash(i,6)*1.8f,trees,i*41);}
        }
        static void LandmarkGroves()
        {
            // Preserve Advanced Farm's validated collider topology: extra corner trees
            // changed the NavMesh polygon corridor even though the physical lane stayed clear.
            if(map.Theme==3)return;
            foreach(var landmark in map.Obstacles)for(int corner=0;corner<4;corner++)
            {
                var p=landmark.Position+new Vector3((corner%2==0?-1:1)*(landmark.Size.x*.5f+1.1f),0,(corner<2?-1:1)*(landmark.Size.z*.5f+1.1f));
                if(!Clear(p,.85f,true))continue;
                Place(corner%3==0?"Tree_Pine":"Tree_Tall",p,2.5f+Hash(corner,map.Theme)*.7f,trees,corner*53,true);treeCount++;
            }
        }
        static void InteriorPlanting()
        {
            int placed=0;
            for(int z=-14;z<=14;z+=4)for(int x=-18;x<=18;x+=4)
            {
                Vector3 p=P(x+(Hash(x,z)-.5f)*1.5f,z+(Hash(z,x)-.5f)*1.4f);
                if(!Clear(p,1.05f,true))continue;
                float height=map.Theme==1?3.3f:2.5f;
                Place(TreeNames[(placed+map.Theme)%3],p,height+Hash(x,z,2)*.6f,trees,Hash(x,z,3)*360,true);placed++;treeCount++;
            }
            // Low vegetation follows road shoulders and garden islands. Paths, pen gates and
            // interaction circles stay clear; all samples are deterministic, not gameplay RNG.
            for(int iz=0;iz<22;iz++)for(int ix=0;ix<27;ix++)
            {
                Vector3 p=P(-19.5f+ix*1.5f+(Hash(ix,iz)-.5f)*.65f,-15+iz*1.42f+(Hash(iz,ix)-.5f)*.6f);
                if(!Clear(p,.25f,false))continue;
                for(int j=0;j<((ix+iz)%5<2?1:2);j++)
                {var q=p+new Vector3(Mathf.Sin(ix*2.1f+j*2.4f)*.38f,.025f,Mathf.Cos(iz*1.7f+j*2.4f)*.38f);if(Clear(q,.12f,false))Place("Daisy",q,.22f+Hash(ix,iz,j)*.18f,cover,Hash(iz,ix,j)*360);}
                // Low fill is real blade grass, built separately; never miniature trees.
                if((ix+iz)%17==0)Place("Boulder",p,.14f+Hash(ix,iz)*.12f,cover,ix*29);
            }
            foreach(var route in map.Routes)for(int segment=1;segment<route.Nodes.Length;segment++)
            {
                var a=route.Nodes[segment-1];var b=route.Nodes[segment];var side=Vector3.Cross(Vector3.up,(b-a).normalized);int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/1.3f);
                for(int i=0;i<=steps;i++)foreach(int bank in new[]{-1,1})
                {var p=Vector3.Lerp(a,b,i/(float)steps)+side*bank*(route.Width*.5f+.55f+Hash(i,segment)*.3f);if(Clear(p,.14f,false))Place("Daisy",p,.23f+Hash(i,segment,bank)*.16f,cover,i*67);}
            }
        }
        static void StoneBranch(Vector3 start)
        {
            Vector3 nearest=Vector3.zero;float best=float.PositiveInfinity;
            foreach(var lane in map.Routes)for(int j=1;j<lane.Nodes.Length;j++)
            {var a=lane.Nodes[j-1];var d=lane.Nodes[j]-a;var point=a+d*Mathf.Clamp01(Vector3.Dot(start-a,d)/d.sqrMagnitude);float distance=Vector3.Distance(point,start);if(distance<best){best=distance;nearest=point;}}
            var delta=nearest-start;for(int i=2;i<Mathf.CeilToInt(delta.magnitude/.7f);i++)Place("StonePath",start+delta.normalized*(i*.7f)+Vector3.up*.025f,.035f,details,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg);
        }
        static void FarmDetails()
        {
            var well=map.Obstacles.First(o=>o.Asset=="Well");
            for(int i=0;i<16;i++)
            {float a=i*Mathf.PI*2/16;var p=well.Position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.30f;Place(i%3==0?"Boulder":"Daisy",p,i%3==0?.15f:.30f,details,i*29);}
            var bench=well.Position+new Vector3(-1.9f,0,1.6f);if(Clear(bench,.65f,true))Place("Bench",bench,.65f,details,130,true);
            StoneBranch(well.Position);
            foreach(var obstacle in map.Obstacles)
            {
                if(obstacle.Asset!="Barn")continue;
                // Inside the existing barn's reserved footprint: no new navigation obstruction.
                var p=obstacle.Position+new Vector3(obstacle.Size.x*.36f,0,-obstacle.Size.z*.32f);
                Place("Barrell",p,.75f,details);Place("Crate",p+Vector3.left*.7f,.60f,details,15);
            }
            for(int i=0;i<map.Pens.Length;i++)
            {
                var at=map.Pens[i]+new Vector3(2.45f,0,1.8f);
                if(Clear(at,.35f,false))Place("Hay_Pile",at,.4f,details);
            }
            // Additional display enclosures and crop gardens belong to village homes, not to
            // the objective system. Their fences never close an actual rescue pen entrance.
            for(int garden=0;garden<3;garden++)
            {
                var p=P(-14+garden*13,19.9f);
                for(int row=0;row<2;row++)for(int column=0;column<3;column++)Place((row+column)%2==0?"Farm_Cabbage":"Farm_Carrot",p+new Vector3(column*1.35f,0,row*1.1f),.35f,details);
                if(garden==1)Place("Farm_Wheat",p+Vector3.right*2.8f,.5f,details);
                Place("Fence",p+new Vector3(-1.2f,0,0),.55f,details,90);
                Place("Fence",p+new Vector3(1,0,1.2f),.55f,details);
                Place("Hay_Bale",p+new Vector3(3.7f,0,.5f),.7f,details,20);
            }
        }
        static void Combine(Transform group,bool shadows)
        {
            var chunks=new Dictionary<Vector2Int,List<CombineInstance>>();
            foreach(var f in group.GetComponentsInChildren<MeshFilter>())
            {
                var p=f.GetComponent<Renderer>().bounds.center;var key=new Vector2Int(Mathf.FloorToInt(p.x/9),Mathf.FloorToInt(p.z/9));
                if(!chunks.ContainsKey(key))chunks[key]=new List<CombineInstance>();
                chunks[key].Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});
            }
            foreach(var pair in chunks)
            {
                var mesh=new Mesh{name=map.Name+" / "+group.name+" / "+pair.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());mesh.RecalculateBounds();MeshUtility.SetMeshCompression(mesh,ModelImporterMeshCompression.Medium);AssetDatabase.AddObjectToAsset(mesh,set);
                var go=new GameObject("Batch "+pair.Key);go.transform.SetParent(group,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=town;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;r.receiveShadows=true;
            }
        }
        static GameObject BuildTheme(int theme)
        {
            map=FarmMapLayout.ForTheme(theme);occupied.Clear();instances.Clear();count=houseCount=treeCount=0;
            root=Group("Authored scenery - "+map.Name,null);cover=Group("Flower banks and low ground cover",root);trees=Group("Boundary woods and pocket groves",root);houses=Group("Farm village clusters",root);details=Group("Courtyards well gardens and farm storage",root);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Extended green meadow - visual background";floor.transform.SetParent(root,false);floor.transform.localPosition=new Vector3(0,-.15f,0);floor.transform.localScale=new Vector3(78,.16f,68);floor.GetComponent<Renderer>().sharedMaterial=meadow;UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            Villages();BoundaryTrees();LandmarkGroves();InteriorPlanting();FarmDetails();
            Combine(cover,false);Combine(trees,true);Combine(houses,true);Combine(details,false);
            foreach(var go in instances){var wrapper=go.transform.parent;UnityEngine.Object.DestroyImmediate(go);if(wrapper.childCount==0)UnityEngine.Object.DestroyImmediate(wrapper.gameObject);}
            var stage=root.gameObject.AddComponent<FarmSceneryStage>();stage.restingGardens=new GameObject[map.Plots.Length];
            for(int i=0;i<map.Plots.Length;i++)
            {var garden=Group("Resting garden "+(i+1)+" - no mission component",root);stage.restingGardens[i]=garden.gameObject;Place(i%2==0?"Farm_Carrot":"Farm_Cabbage",map.Plots[i]+Vector3.up*.025f,.45f,garden);}
            // Empty source wrapper transforms retain readable placements and actual solid colliders.
            set.sourceInstances[theme]=count;set.villageHouses[theme]=houseCount;set.interiorTrees[theme]=treeCount;
            GrassTufts();set.renderedBatches[theme]=root.GetComponentsInChildren<MeshRenderer>().Length;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabFolder+"/"+FarmMapTools.Slugs[theme]+"Scenery.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);return prefab;
        }
        [MenuItem("Tools/Farm Guardian/Maps/Build Dense Farm In First Scene")]
        public static void BuildDenseFarm()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Exit Play Mode before building the farm.");
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            config=AssetDatabase.LoadAssetAtPath<FarmConfig>("Assets/FarmGuardian/Data/FarmGuardianConfig.asset");town=config.environmentMaterial;
            sources.Clear();used.Clear();foreach(var t in config.townModel.GetComponentsInChildren<Transform>(true))if(!sources.ContainsKey(t.name))sources[t.name]=t.gameObject;
            foreach(string n in new[]{"Barn","Well","Bench","Fence","Barrell","Crate","Daisy","Farm_Cabbage","Farm_Carrot","Farm_Wheat","Hay_Pile","Hay_Bale","Boulder","Potted_Bush","StonePath","Tree_Tall","Tree_Square","Tree_Pine","House_Blue","House_Purple","House_Red","House_2Story_Purple"})if(!sources.ContainsKey(n))throw new InvalidOperationException("Required supplied model missing: "+n);
            Directory.CreateDirectory(PrefabFolder);AssetDatabase.Refresh();
            set=AssetDatabase.LoadAssetAtPath<FarmScenerySet>(SetPath);
            if(set==null){set=ScriptableObject.CreateInstance<FarmScenerySet>();AssetDatabase.CreateAsset(set,SetPath);}
            else foreach(var item in AssetDatabase.LoadAllAssetsAtPath(SetPath))if(item!=set)UnityEngine.Object.DestroyImmediate(item,true);
            set.themes=new GameObject[4];set.sourceInstances=new int[4];set.renderedBatches=new int[4];set.villageHouses=new int[4];set.interiorTrees=new int[4];
            meadow=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Shared green meadow",color=new Color(.39f,.65f,.23f)};meadow.SetFloat("_Smoothness",.02f);AssetDatabase.AddObjectToAsset(meadow,set);
            var importer=(ModelImporter)AssetImporter.GetAtPath(TownPath);bool wasReadable=importer.isReadable;
            try
            {
                if(!wasReadable){importer.isReadable=true;importer.SaveAndReimport();}
                sources.Clear();foreach(var t in AssetDatabase.LoadAssetAtPath<GameObject>(TownPath).GetComponentsInChildren<Transform>(true))if(!sources.ContainsKey(t.name))sources[t.name]=t.gameObject;
                for(int theme=0;theme<4;theme++)set.themes[theme]=BuildTheme(theme);
                set.sourceNames=used.OrderBy(n=>n).ToArray();EditorUtility.SetDirty(set);AssetDatabase.SaveAssets();
                SaveFirstScene();
            }
            finally {if(!wasReadable){importer.isReadable=false;importer.SaveAndReimport();}}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            var report=new StringBuilder("Source: "+TownPath+"\nMaterial: Assets/FarmGuardian/Materials/Town.mat\nTexture: Assets/UTS_Project/village-town-assets/textures/Gradients.png\n");
            for(int i=0;i<4;i++)report.AppendLine(FarmMapTools.Slugs[i]+": "+set.sourceInstances[i]+" source placements, "+set.renderedBatches[i]+" renderer chunks, "+set.villageHouses[i]+" houses, "+set.interiorTrees[i]+" interior trees");
            report.AppendLine("Used nodes: "+string.Join(", ",set.sourceNames));File.WriteAllText("Logs/FarmGuardian-scenery-built.txt",report.ToString());Debug.Log("FARM_SCENERY_BUILD_OK\n"+report);
        }
        static void SaveFirstScene()
        {
            var scene=EditorSceneManager.OpenScene(FarmGuardianSetup.ScenePath,OpenSceneMode.Single);
            foreach(var item in scene.GetRootGameObjects())if(item.GetComponent<FarmScenePreview>()!=null)UnityEngine.Object.DestroyImmediate(item);
            var owner=new GameObject("Temporary scenery authoring systems");var session=owner.AddComponent<FarmSession>();session.enabled=false;session.config=config;
            var world=owner.AddComponent<FarmWorld>();world.Session=session;
            var spec=config.GetLevel(1);world.Build(spec);
            var preview=world.Root;preview.name="Farm Environment - saved Edit Mode view";preview.gameObject.AddComponent<FarmScenePreview>();
            world.Surface.RemoveData();UnityEngine.Object.DestroyImmediate(world.Surface);
            var actors=preview.Find("InteractiveObjects");if(actors!=null)UnityEngine.Object.DestroyImmediate(actors.gameObject);
            foreach(var f in preview.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh!=null&&string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh)))AssetDatabase.AddObjectToAsset(f.sharedMesh,set);
            foreach(var renderer in preview.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)if(material!=null&&string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))AssetDatabase.AddObjectToAsset(material,set);
            UnityEngine.Object.DestroyImmediate(owner);
            var sun=UnityEngine.Object.FindFirstObjectByType<Light>();sun.intensity=1.15f;sun.color=new Color(1,.95f,.84f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.68f;sun.shadowBias=.04f;sun.transform.rotation=Quaternion.Euler(48,-32,0);
            RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.61f,.69f,.66f);RenderSettings.fog=false;
            var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/FarmGuardian/Materials/FarmDaySky.mat");if(sky==null){sky=new Material(Shader.Find("Skybox/Procedural"));AssetDatabase.CreateAsset(sky,"Assets/FarmGuardian/Materials/FarmDaySky.mat");}sky.SetFloat("_AtmosphereThickness",.8f);sky.SetFloat("_Exposure",1.1f);RenderSettings.skybox=sky;EditorUtility.SetDirty(sky);
            var camera=Camera.main;camera.clearFlags=CameraClearFlags.Skybox;camera.transform.position=new Vector3(0,28,-26);camera.transform.LookAt(Vector3.zero);
            QualitySettings.shadowDistance=45;
            EditorSceneManager.SaveScene(scene);EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(s=>!s.path.Contains("FarmGuardianRedesign")).Select(s=>new EditorBuildSettingsScene(s.path,s.path==FarmGuardianSetup.ScenePath)).ToArray();
        }
    }
}



