using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmGuardian
{
    public sealed class FarmUiMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public bool Panel;
        float elapsed;
        bool hover, pressed;
        CanvasGroup group;
        Button button;
        void Start()
        {
            button = GetComponent<Button>();
            if (Panel) { group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; transform.localScale = Vector3.one * 0.98f; }
        }
        void Update()
        {
            if (Panel)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.18f); t = 1 - Mathf.Pow(1 - t, 3);
                group.alpha = t; transform.localScale = Vector3.one * Mathf.Lerp(0.98f, 1, t);
                if (t >= 1) enabled = false;
                return;
            }
            float target = button != null && !button.interactable ? 1 : pressed ? 0.97f : 1;
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, 1 - Mathf.Exp(-22 * Time.unscaledDeltaTime));
        }
        public void OnPointerEnter(PointerEventData data) { hover = true; }
        public void OnPointerExit(PointerEventData data) { hover = false; pressed = false; }
        public void OnPointerDown(PointerEventData data) { pressed = true; }
        public void OnPointerUp(PointerEventData data) { pressed = false; }
        void OnDisable() { hover = pressed = false; if(!Panel)transform.localScale=Vector3.one; }
    }
}
