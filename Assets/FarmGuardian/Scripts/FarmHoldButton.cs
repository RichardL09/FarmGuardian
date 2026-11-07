using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmGuardian
{
    public sealed class FarmHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> Changed;
        bool held;
        int pointer;
        public void OnPointerDown(PointerEventData data)
        {
            if (held) return;
            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null && !button.interactable) return;
            pointer = data.pointerId; held = true; Changed?.Invoke(true);
        }
        public void OnPointerUp(PointerEventData data)
        { if (held && data.pointerId == pointer) { held = false; Changed?.Invoke(false); } }
        void OnDisable() { if (held) { held = false; Changed?.Invoke(false); } }
    }
}
