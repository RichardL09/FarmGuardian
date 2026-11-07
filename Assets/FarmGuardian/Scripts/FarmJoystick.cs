using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmGuardian
{
    public sealed class FarmJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public Vector2 Value { get; private set; }
        int activePointer = int.MinValue;
        public void OnPointerDown(PointerEventData data)
        { if (activePointer != int.MinValue) return; activePointer = data.pointerId; OnDrag(data); }
        public void OnDrag(PointerEventData data)
        {
            if (activePointer != data.pointerId) return;
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var point);
            Value = Vector2.ClampMagnitude(point / (rect.rect.width * 0.36f), 1);
            Knob.anchoredPosition = Value * rect.rect.width * 0.30f;
        }
        public void OnPointerUp(PointerEventData data) { if (activePointer == data.pointerId) ResetInput(); }
        public void ResetInput() { Value = Vector2.zero; activePointer = int.MinValue; if (Knob != null) Knob.anchoredPosition = Vector2.zero; }
        void OnDisable() { ResetInput(); }
    }
}
