using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace FarmGuardian
{
    // An isolated, live 3D map rendered behind the level buttons. Both the road
    // and UI use DesignNodes, so they share one projection at every screen size.
    public sealed class FarmLevelMapView : MonoBehaviour
    {
        public static Vector2[] DesignNodes()
        {
            var points=new Vector2[15];
            for(int i=0;i<points.Length;i++)
            {
                int row=i/5,col=row%2==0?i%5:4-i%5;
                points[i]=new Vector2(-650+col*325,-275+row*250+Mathf.Sin(col*1.7f+row)*42);
            }
            return points;
        }

        readonly List<Transform> trees=new List<Transform>();
        readonly List<Quaternion> treeRest=new List<Quaternion>();
        readonly List<Transform> blooms=new List<Transform>();
        readonly List<Transform> blades=new List<Transform>();
        readonly List<Quaternion> bladeRest=new List<Quaternion>();
        readonly List<Material> materials=new List<Material>();
        readonly Dictionary<string,GameObject> sources=new Dictionary<string,GameObject>();
        Transform stage; Camera mapCamera; RenderTexture texture; FarmConfig config;
        float age;
        static int stageSequence;
        const int MapLayer=30;
        // Overscan covers tablet and ultrawide margins while the UI stays in
        // its safe 1920x1080 design area. Projection uses this same extent.
        const float BackgroundWidth=2560,BackgroundHeight=1440;
        public Camera MapCamera=>mapCamera;
        public Transform Stage=>stage;
        public IReadOnlyList<Transform> Trees=>trees;
        public Vector3 GroundPoint(Vector2 design)
        {
            var ray=mapCamera.ViewportPointToRay(new Vector3(.5f+design.x/BackgroundWidth,.5f+design.y/BackgroundHeight,0));
            var ground=new Plane(Vector3.up,stage.position);
            ground.Raycast(ray,out float distance);return ray.GetPoint(distance);
        }
        public void Build(Transform parent,FarmConfig farmConfig)
        {
            config=farmConfig;
            foreach(var t in config.townModel.GetComponentsInChildren<Transform>(true))
                if(!sources.ContainsKey(t.name))sources.Add(t.name,t.gameObject);
            stage=new GameObject("Level map - original farm assets").transform;
            stage.position=new Vector3(4000+(stageSequence++%100)*250,0,4000);
            var cameraObject=new GameObject("Level map camera",typeof(Camera));
            cameraObject.transform.SetParent(stage,false);mapCamera=cameraObject.GetComponent<Camera>();
            mapCamera.orthographic=true;mapCamera.orthographicSize=17*BackgroundHeight/1080;
            mapCamera.transform.localPosition=new Vector3(0,38,-30);
            mapCamera.transform.LookAt(stage.position);
            mapCamera.nearClipPlane=.1f;mapCamera.farClipPlane=150;
            mapCamera.clearFlags=CameraClearFlags.SolidColor;mapCamera.backgroundColor=new Color(.51f,.72f,.32f);
            mapCamera.cullingMask=1<<MapLayer;mapCamera.allowHDR=false;mapCamera.allowMSAA=true;
            texture=new RenderTexture(1920,1080,24){name="Animated farm level map",antiAliasing=2};
            texture.Create();mapCamera.targetTexture=texture;
            var backdrop=FarmUiFactory.Rect("Live farm map background",parent,new Vector2(BackgroundWidth,BackgroundHeight),Vector2.zero);
            var image=backdrop.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;

            var turf=new GameObject("Meadow",typeof(MeshFilter),typeof(MeshRenderer));
            turf.transform.SetParent(stage,false);turf.transform.localPosition=Vector3.down*.04f;
            var mesh=new Mesh{name="Map meadow ground"};
            mesh.vertices=new[]{new Vector3(-90,0,-90),new Vector3(-90,0,90),new Vector3(90,0,90),new Vector3(90,0,-90)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();
            turf.GetComponent<MeshFilter>().sharedMesh=mesh;
            turf.GetComponent<MeshRenderer>().sharedMaterial=Surface(new Color(.50f,.70f,.29f));
            groundMesh=mesh;
            BuildRoad();BuildVillage();
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=MapLayer;
        }
        Mesh groundMesh,roadMesh;
        Material Surface(Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=color};
            materials.Add(m);return m;
        }
        Vector2 Curve(Vector2[] nodes,int i,float t)
        {
            var a=nodes[Mathf.Max(0,i-1)];var b=nodes[i];var c=nodes[i+1];var d=nodes[Mathf.Min(nodes.Length-1,i+2)];
            return .5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
        }
        void BuildRoad()
        {
            var nodes=DesignNodes();var vertices=new List<Vector3>();var triangles=new List<int>();
            Vector3 previous=GroundPoint(nodes[0]);float stoneDistance=0;
            for(int i=0;i<nodes.Length-1;i++)for(int sample=1;sample<=18;sample++)
            {
                var p=Curve(nodes,i,sample/18f);var next=GroundPoint(p);
                var direction=(next-previous).normalized;var side=Vector3.Cross(Vector3.up,direction)*1.18f;
                int n=vertices.Count;
                vertices.Add(previous-side-stage.position+Vector3.up*.025f);
                vertices.Add(previous+side-stage.position+Vector3.up*.025f);
                vertices.Add(next+side-stage.position+Vector3.up*.025f);
                vertices.Add(next-side-stage.position+Vector3.up*.025f);
                triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                stoneDistance+=Vector3.Distance(previous,next);
                if(stoneDistance>1.15f)
                {
                    stoneDistance=0;var stone=Place("StonePath",p,.045f,Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg);
                    if(stone!=null)
                    {
                        // Fit the actual source road tile to the lane without relying on its thin height.
                        var renderers=stone.GetComponentsInChildren<Renderer>();
                        if(renderers.Length>0)
                        {
                            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                            float fit=1.45f/Mathf.Max(.01f,Mathf.Max(bounds.size.x,bounds.size.z));
                            stone.localScale=new Vector3(fit,1,fit);stone.position+=Vector3.up*.035f;
                        }
                    }
                }
                previous=next;
            }
            roadMesh=new Mesh{name="Winding farm road"};roadMesh.SetVertices(vertices);roadMesh.SetTriangles(triangles,0);roadMesh.RecalculateNormals();
            var go=new GameObject("Sandy road with original StonePath tiles",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(stage,false);
            go.GetComponent<MeshFilter>().sharedMesh=roadMesh;go.GetComponent<MeshRenderer>().sharedMaterial=Surface(new Color(.91f,.72f,.40f));
        }
        Transform Place(string name,Vector2 p,float height,float yaw=0)
        {
            if(!sources.TryGetValue(name,out var source))return null;
            var t=FarmUiModels.Model(source,stage,GroundPoint(p)-stage.position,height,config.environmentMaterial);
            t.name="Map "+name;t.localRotation=Quaternion.Euler(0,yaw,0);
            foreach(var r in t.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.On;
            return t;
        }
        void Tree(string name,Vector2 p,float height,float yaw)
        {
            var tree=Place(name,p,height,yaw);if(tree==null)return;
            trees.Add(tree);treeRest.Add(tree.localRotation);
        }
        void BuildVillage()
        {
            // Landmark pockets and foreground foliage frame the route, as in the reference.
            Place("Barn",new Vector2(-820,120),5.2f,20);
            var asset=Resources.Load<FarmWindmillAsset>("FarmWindmill");
            if(asset!=null&&asset.Body!=null&&asset.Rotor!=null)
            {
                var mill=new GameObject("Map Windmill").transform;mill.SetParent(stage,false);
                mill.position=GroundPoint(new Vector2(570,365));mill.localRotation=Quaternion.Euler(0,155,0);mill.localScale=Vector3.one*4.2f;
                var body=new GameObject("Original windmill tower",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(mill,false);
                body.GetComponent<MeshFilter>().sharedMesh=asset.Body;body.GetComponent<MeshRenderer>().sharedMaterial=config.environmentMaterial;
                var rotor=new GameObject("Map windmill blades",typeof(MeshFilter),typeof(MeshRenderer));
                rotor.transform.SetParent(mill,false);rotor.transform.localPosition=asset.Pivot;
                rotor.GetComponent<MeshFilter>().sharedMesh=asset.Rotor;rotor.GetComponent<MeshRenderer>().sharedMaterial=config.environmentMaterial;
                blades.Add(rotor.transform);bladeRest.Add(rotor.transform.localRotation);rotorAxis=asset.Axis;
            }
            else Place("Windmill",new Vector2(570,365),4.2f,155);
            Place("Pond",new Vector2(820,340),.35f);
            Place("House_Purple",new Vector2(-270,-110),2.8f,160);
            Place("House_Blue",new Vector2(290,100),2.5f,175);
            Place("House_Red",new Vector2(-280,360),2.5f,170);
            Place("Well",new Vector2(30,90),1.4f);
            Place("Hay_Cart",new Vector2(-430,-140),1.1f,35);
            Place("Hay_Bale",new Vector2(-850,-60),1,15);
            Place("Hay_Bale",new Vector2(-780,-85),.8f);
            for(int i=0;i<7;i++)
            {
                Place("Fence",new Vector2(-470+i*150,360),.8f);
                Place("Fence",new Vector2(-480+i*155,-430),.7f);
                Place(i%2==0?"Farm_Cabbage":"Farm_Carrot",new Vector2(-500+i*165,-410),.35f);
            }
            for(int i=0;i<18;i++)
            {
                float x=-1000+i*118;
                Tree(i%3==0?"Tree_Pine":"Tree_Square",new Vector2(x,370+(i%3)*25),3.4f+(i%3)*.55f,i*29);
                Tree(i%2==0?"Tree_Tall":"Tree_Square",new Vector2(x,-570+(i%2)*32),3.5f+(i%3)*.45f,i*41);
            }
            for(int i=0;i<5;i++)
            {
                Tree("Tree_Square",new Vector2(-1000,310-i*170),3.4f,i*32);
                Tree("Tree_Tall",new Vector2(1000,350-i*175),3.7f,i*23);
            }
            var nodes=DesignNodes();
            // Small orchards and flower beds fill the spaces between the road's bends.
            for(int row=0;row<2;row++)for(int col=0;col<6;col++)
            {
                var centre=new Vector2(-780+col*305,row==0?-155:100);
                if(col==2||col==3&&row==1)continue; // House and well clearings.
                Tree(col%2==0?"Tree_Square":"Tree_Pine",centre,2.6f,col*47);
                Tree("Tree_Square",centre+new Vector2(65,10),1.8f,col*23);
                Place("Potted_Bush",centre+new Vector2(-38,-10),.75f);
                for(int flower=0;flower<7;flower++)
                {
                    var p=centre+new Vector2(-65+flower*19,-30+Mathf.Sin(flower*2)*13);
                    var bloom=Place("Daisy",p,.38f+(flower%3)*.12f,flower*35);
                    if(bloom!=null)blooms.Add(bloom);
                }
            }
            // Existing crop meshes form a kitchen garden beside the purple house.
            for(int row=0;row<2;row++)for(int col=0;col<3;col++)
                Place(row==0?"Farm_Cabbage":"Farm_Carrot",new Vector2(-130+col*65,-120-row*55),.45f);
            for(int i=0;i<5;i++)Place("Fence",new Vector2(430+i*90,-435),.75f);
            for(int i=0;i<3&&config.animalModels.Length>0;i++)
            {
                var animal=FarmUiModels.Model(config.animalModels[0],stage,GroundPoint(new Vector2(500+i*105,-410))-stage.position,1.2f,config.animalMaterial);
                animal.name="Map pasture cow";animal.localRotation=Quaternion.Euler(0,150+i*20,0);
                var animator=animal.GetComponentInChildren<Animator>();
                if(animator!=null&&config.animalAnimators.Length>0)
                {
                    animator.runtimeAnimatorController=config.animalAnimators[0];animator.applyRootMotion=false;
                    animator.updateMode=AnimatorUpdateMode.UnscaledTime;animator.Play("Idle",0,i*.3f);
                }
            }
            for(int i=0;i<65;i++)
            {
                var p=new Vector2(-870+(i%13)*145,-365+(i/13)*170);
                float distance=float.MaxValue;
                for(int j=0;j<nodes.Length-1;j++)for(int s=0;s<=8;s++)distance=Mathf.Min(distance,Vector2.Distance(p,Curve(nodes,j,s/8f)));
                if(distance<100)continue;
                if(i%5==0)Tree("Tree_Square",p,1.8f,i*29);
                else
                {
                    var bloom=Place(i%3==0?"Boulder":"Daisy",p,i%3==0?.4f:.5f,i*33);
                    if(bloom!=null&&i%3!=0)blooms.Add(bloom);
                }
            }
        }
        Vector3 rotorAxis=Vector3.forward;
        void Update()
        {
            age+=Time.unscaledDeltaTime;
            for(int i=0;i<trees.Count;i++)trees[i].localRotation=treeRest[i]*Quaternion.Euler(Mathf.Sin(age*.85f+i*.73f)*.7f,0,Mathf.Sin(age*1.05f+i*.61f)*1.6f);
            for(int i=0;i<blooms.Count;i++)blooms[i].localRotation=Quaternion.Euler(0,i*33,Mathf.Sin(age*1.3f+i)*3);
            for(int i=0;i<blades.Count;i++)blades[i].localRotation=bladeRest[i]*Quaternion.AngleAxis(age*12,rotorAxis);
        }
        void OnDisable(){if(mapCamera!=null)mapCamera.enabled=false;}
        void OnDestroy()
        {
            if(mapCamera!=null)mapCamera.targetTexture=null;
            if(texture!=null){texture.Release();Destroy(texture);}
            if(stage!=null)Destroy(stage.gameObject);
            if(groundMesh!=null)Destroy(groundMesh);if(roadMesh!=null)Destroy(roadMesh);
            foreach(var material in materials)Destroy(material);
        }
    }
}
