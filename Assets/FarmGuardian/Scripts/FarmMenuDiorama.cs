using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FarmGuardian
{
    // Menu-only presentation; never creates gameplay actors or changes saved progress.
    public sealed class FarmMenuDiorama : MonoBehaviour
    {
        Transform root, farmer; FarmCharacterAnimation rig;
        readonly List<Transform> flowers=new List<Transform>();
        readonly List<Transform> animals=new List<Transform>();
        Material grass; bool visible, lightingSaved; float age;
        Camera menuCamera; float oldFov; Color oldBackground; CameraClearFlags oldClear;
        Light sun; Color oldSunColor, oldAmbient; float oldIntensity,oldShadow; Quaternion oldSunRotation;
        AmbientMode oldAmbientMode;
        public Transform Farmer=>farmer;
        public Transform Stage=>root;
        public IReadOnlyList<Transform> Animals=>animals;
        public bool Visible=>visible;
        Transform Group(string name,Transform parent)
        {var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        public void Show(FarmConfig config,bool show)
        {
            if(root==null&&show)Build(config);
            if(show&&!visible)EnterLighting();
            if(!show&&visible)RestoreLighting();
            visible=show;
            if(root!=null)root.gameObject.SetActive(show);
            var camera=Camera.main;if(camera==null)return;
            var follow=camera.GetComponent<FarmCamera>();if(follow!=null)follow.enabled=!show;
            if(show)Frame(camera);
        }
        void Build(FarmConfig config)
        {
            root=Group("Main Menu - 3D Farm Diorama",transform);root.position=new Vector3(1000,0,0);
            var environment=Group("Background - existing village assets",root);
            var foreground=Group("Foreground - farmer and friends",root);
            grass=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Menu meadow (runtime)",color=new Color(.45f,.64f,.28f)};
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Menu meadow";floor.transform.SetParent(environment,false);
            floor.transform.localPosition=Vector3.down*.26f;floor.transform.localScale=new Vector3(160,.5f,160);
            floor.GetComponent<Renderer>().sharedMaterial=grass;
            floor.GetComponent<Collider>().enabled=false;Destroy(floor.GetComponent<Collider>());
            Add(config,"Barn",environment,new Vector3(-10,0,10),6.5f,20);
            Add(config,"Windmill",environment,new Vector3(-1,0,18),8,15);
            string[] houses={"House_Blue","House_Purple","House_Red"};
            for(int i=0;i<6;i++)Add(config,houses[i%3],environment,new Vector3(-17+i*7,0,24+(i%2)*4),3.5f,170);
            string[] trees={"Tree_Square","Tree_Tall","Tree_Pine"};
            for(int i=0;i<18;i++)Add(config,trees[i%3],environment,new Vector3(-30+i*3.6f,0,32+Mathf.Sin(i)*3),5+(i%3),i*29);
            for(int i=0;i<7;i++)
            {
                Add(config,"Fence",environment,new Vector3(-13+i*3.2f,0,5.8f),.9f);
                Add(config,"Farm_Cabbage",environment,new Vector3(1+i*1.6f,0,10),.48f);
                Add(config,"Farm_Carrot",environment,new Vector3(1+i*1.6f,0,12),.48f);
            }
            Add(config,"Tree_Square",environment,new Vector3(-15,0,1),7);
            Add(config,"Tree_Tall",environment,new Vector3(14,0,10),6);
            Add(config,"Hay_Bale",foreground,new Vector3(-10,0,1),1.2f);
            Add(config,"Hay_Cart",environment,new Vector3(-5,0,9),1.5f);
            Add(config,"Barrell",environment,new Vector3(-7,0,6),.9f);
            Add(config,"Well",environment,new Vector3(8,0,7),1.8f);
            Add(config,"Pond",environment,new Vector3(12,0,16),.3f);
            for(int i=0;i<9;i++)
            {
                float x=-12+i*2.7f;
                Add(config,"Fence",environment,new Vector3(x,0,-8),.72f);
                Add(config,"Boulder",environment,new Vector3(x+.6f,0,-7.5f),.30f+(i%3)*.12f,i*43);
                for(int j=0;j<7;j++)
                {
                    var bloom=Add(config,"Daisy",environment,new Vector3(x+Mathf.Sin(j*2.3f)*.85f,0,-7.4f+Mathf.Cos(j*1.7f)*.65f),.28f+(j%3)*.07f,j*41);
                    if(bloom!=null&&j==0)flowers.Add(bloom);
                }
            }
            for(int i=0;i<15;i++)Add(config,"StonePath",environment,new Vector3(1+Mathf.Sin(i*.25f)*2,.015f,-5+i*1.7f),.035f);
            for(int i=0;i<40;i++)
            {
                float x=-12+(i%10)*2.5f,z=-4+(i/10)*3.2f;
                if(x>-8&&x<-2&&z<3)continue;
                var plant=Add(config,i%4==0?"Potted_Bush":"Daisy",environment,new Vector3(x,0,z),i%4==0?.55f:.28f,i*37);
                if(plant!=null&&i%4!=0)flowers.Add(plant);
            }
            farmer=FarmUiModels.Model(config.farmerModel,foreground,new Vector3(-4.6f,0,-3.8f),4.1f,config.farmerMaterial);
            farmer.name="Menu Farmer";farmer.localRotation=Quaternion.Euler(0,168,0);
            rig=farmer.GetComponentInChildren<FarmCharacterAnimation>();if(rig!=null)rig.enabled=false;
            var animator=farmer.GetComponentInChildren<Animator>();
            if(animator!=null){animator.applyRootMotion=false;animator.enabled=false;}
            Vector3[] places={new Vector3(-7.2f,0,-3.7f),new Vector3(-2.2f,0,-4),new Vector3(-6,0,-5.5f),new Vector3(-3,0,-5.7f)};
            float[] heights={1.8f,1.4f,.8f,1};
            for(int i=0;i<Mathf.Min(4,config.animalModels.Length);i++)
            {
                var animal=FarmUiModels.Model(config.animalModels[i],foreground,places[i],heights[i],config.animalMaterial);
                animal.name="Menu "+config.animalNames[i];animal.localRotation=Quaternion.Euler(0,165+i*8,0);animals.Add(animal);
                var a=animal.GetComponentInChildren<Animator>();
                if(a!=null&&i<config.animalAnimators.Length)
                {a.runtimeAnimatorController=config.animalAnimators[i];a.applyRootMotion=false;a.updateMode=AnimatorUpdateMode.UnscaledTime;a.speed=.75f+i*.09f;a.Play("Idle",0,i*.23f);}
            }
            foreach(var r in foreground.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.On;
        }
        Transform Add(FarmConfig config,string name,Transform parent,Vector3 p,float height,float yaw=0)
        {
            var source=FarmUiModels.Prop(config,name);if(source==null)return null;
            var t=FarmUiModels.Model(source,parent,p,height,config.environmentMaterial);t.localRotation=Quaternion.Euler(0,yaw,0);
            if(height>2)foreach(var r in t.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.On;
            return t;
        }
        public void RefreshFraming(){if(visible&&Camera.main!=null)Frame(Camera.main);}
        void Frame(Camera camera)
        {
            camera.orthographic=false;
            float aspect=Mathf.Max(.5f,camera.aspect);
            camera.fieldOfView=2*Mathf.Atan(Mathf.Tan(24*Mathf.Deg2Rad)*Mathf.Max(1,(16f/9)/aspect))*Mathf.Rad2Deg;
            camera.transform.position=root.position+new Vector3(0,7,-19);
            camera.transform.LookAt(root.position+new Vector3(0,2.3f,4));
        }
        void LateUpdate()
        {
            if(!visible||root==null)return;
            age+=Time.unscaledDeltaTime;RefreshFraming();
            float turn=Mathf.SmoothStep(-1,1,Mathf.Clamp01((Mathf.Sin(age*.24f)+.65f)/1.3f));
            farmer.localRotation=Quaternion.Euler(0,168+turn*14,0);
            if(rig!=null&&rig.body!=null)
            {
                rig.body.localRotation=Quaternion.Euler(Mathf.Sin(age*.9f)*1.1f,Mathf.Sin(age*.43f)*2,Mathf.Sin(age*.7f)*.65f);
                if(rig.leftArm!=null)rig.leftArm.localRotation=Quaternion.Euler(-6+Mathf.Sin(age*.8f)*2,0,-4);
                if(rig.rightArm!=null)rig.rightArm.localRotation=Quaternion.Euler(-6+Mathf.Sin(age*.8f+.8f)*2,0,4);
            }
            for(int i=0;i<flowers.Count;i++)flowers[i].localRotation=Quaternion.Euler(0,i*37,Mathf.Sin(age*.7f+i*1.4f)*2);
        }
        void EnterLighting()
        {
            menuCamera=Camera.main;if(menuCamera==null)return;
            oldFov=menuCamera.fieldOfView;oldBackground=menuCamera.backgroundColor;oldClear=menuCamera.clearFlags;
            menuCamera.clearFlags=CameraClearFlags.SolidColor;menuCamera.backgroundColor=new Color(.62f,.83f,.94f);
            oldAmbient=RenderSettings.ambientLight;oldAmbientMode=RenderSettings.ambientMode;
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.76f,.80f,.85f);
            foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;break;}
            if(sun!=null)
            {
                oldSunColor=sun.color;oldIntensity=sun.intensity;oldShadow=sun.shadowStrength;oldSunRotation=sun.transform.rotation;
                sun.color=new Color(1,.95f,.86f);sun.intensity=1.1f;sun.shadowStrength=.45f;sun.transform.rotation=Quaternion.Euler(45,-30,0);
            }
            lightingSaved=true;
        }
        void RestoreLighting()
        {
            if(!lightingSaved)return;
            if(menuCamera!=null){menuCamera.fieldOfView=oldFov;menuCamera.backgroundColor=oldBackground;menuCamera.clearFlags=oldClear;}
            RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientLight=oldAmbient;
            if(sun!=null){sun.color=oldSunColor;sun.intensity=oldIntensity;sun.shadowStrength=oldShadow;sun.transform.rotation=oldSunRotation;}
            lightingSaved=false;
        }
        void OnDisable(){RestoreLighting();visible=false;if(root!=null)root.gameObject.SetActive(false);}
        void OnDestroy(){RestoreLighting();if(root!=null)Destroy(root.gameObject);if(grass!=null)Destroy(grass);}
    }
}
