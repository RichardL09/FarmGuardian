using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace FarmGuardian
{
public static class FarmUiModels
    {
        public static Transform Model(GameObject source,Transform parent,Vector3 position,float height,Material material)
        {
            var wrapper=new GameObject("Visual - "+source.name).transform;wrapper.SetParent(parent,false);wrapper.localPosition=position;
            var go=Object.Instantiate(source,wrapper,false);go.transform.localPosition=Vector3.zero;
            foreach(var c in go.GetComponentsInChildren<Collider>()) {c.enabled=false;Object.Destroy(c);}
            foreach(var r in go.GetComponentsInChildren<Renderer>())
            { if(material!=null){var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=material;r.sharedMaterials=slots;}r.shadowCastingMode=ShadowCastingMode.Off; }
            if(go.GetComponent<FarmCharacterAnimation>()!=null) {wrapper.localScale=Vector3.one*(height/1.8f);return wrapper;}
            var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return wrapper;
            Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            go.transform.localScale*=height/Mathf.Max(.01f,b.size.y);
            b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            go.transform.position+=new Vector3(wrapper.position.x-b.center.x,wrapper.position.y-b.min.y,wrapper.position.z-b.center.z);return wrapper;
        }
        public static GameObject Prop(FarmConfig config,string name)
        { foreach(var t in config.townModel.GetComponentsInChildren<Transform>(true))if(t.name==name)return t.gameObject;return null; }
    }
}

