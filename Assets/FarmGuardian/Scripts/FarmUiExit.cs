using UnityEngine;

namespace FarmGuardian
{
    public sealed class FarmUiExit : MonoBehaviour
    {
        CanvasGroup group;float elapsed,initial;
        public void Begin()
        {
            foreach(var t in GetComponentsInChildren<Transform>(true))t.name="Leaving - "+t.name;
            foreach(var motion in GetComponentsInChildren<FarmUiMotion>())motion.enabled=false;
            foreach(var hold in GetComponentsInChildren<FarmHoldButton>())hold.enabled=false;
            foreach(var joystick in GetComponentsInChildren<FarmJoystick>())joystick.enabled=false;
            group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;group.interactable=false;initial=group.alpha;
        }
        void Update(){elapsed+=Time.unscaledDeltaTime;group.alpha=initial*(1-Mathf.Clamp01(elapsed/.12f));if(elapsed>=.12f)Destroy(gameObject);}
    }
}
