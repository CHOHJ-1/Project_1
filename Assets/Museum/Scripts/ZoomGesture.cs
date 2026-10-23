using UnityEngine;
using UnityEngine.EventSystems;
namespace Museum
{
    // Keep the zoom controller active while the editable overlay starts hidden.
    public sealed class ZoomGesture : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        [SerializeField] private ImageZoom controller;
        public void OnBeginDrag(PointerEventData data) { controller.OnBeginDrag(data); }
        public void OnDrag(PointerEventData data) { controller.OnDrag(data); }
        public void OnScroll(PointerEventData data) { controller.OnScroll(data); }
    }
}
