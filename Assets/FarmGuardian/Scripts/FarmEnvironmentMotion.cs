using UnityEngine;
namespace FarmGuardian
{
    public sealed class FarmEnvironmentMotion : MonoBehaviour
    {
        public FarmSession Session;float windTime;
        public float WindTime=>windTime;
        void Update(){if(Session!=null&&Session.Playing)windTime+=Time.deltaTime;Shader.SetGlobalFloat("_FarmWindTime",windTime);}
    }
}
