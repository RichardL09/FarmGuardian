using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace FarmGuardian
{
    public sealed class FarmSkinPreview : MonoBehaviour
    {
        static int nextStage;
        GameObject stage;RenderTexture texture;Material tint;
        public void Configure(FarmConfig config,bool golden)
        {Configure(config,golden?FarmSkinCatalog.GoldenFarmer:FarmSkinCatalog.Classic);}
        public void Configure(FarmConfig config,string id)
        {
            stage=new GameObject("Skin preview stage");stage.transform.position=new Vector3(2000+(nextStage++%100000)*24,0,0);
            Material material=FarmSkinCatalog.Material(config,id);if(id==FarmSkinCatalog.GoldenFarmer){tint=new Material(material){color=new Color(1,.8f,.4f)};material=tint;}
            var source=FarmSkinCatalog.Model(config,id);if(source==null){Debug.LogError("Skin model missing: "+id);return;}
            var model=FarmUiModels.Model(source,stage.transform,Vector3.zero,1.8f,material);model.localRotation=Quaternion.Euler(0,15,0);
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            var cameraObject=new GameObject("Preview camera");cameraObject.transform.SetParent(stage.transform,false);var camera=cameraObject.AddComponent<Camera>();
            camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.transform.localPosition=new Vector3(0,1.05f,3.4f);camera.transform.LookAt(stage.transform.position+Vector3.up*.95f);camera.fieldOfView=39;camera.nearClipPlane=.1f;camera.farClipPlane=10;
            texture=new RenderTexture(512,512,24);camera.targetTexture=texture;var image=gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;StartCoroutine(Render(camera));
        }
        IEnumerator Render(Camera camera){yield return null;camera.Render();camera.Render();}
        void OnDestroy(){if(stage!=null)Destroy(stage);if(texture!=null){texture.Release();Destroy(texture);}if(tint!=null)Destroy(tint);}
    }
}

