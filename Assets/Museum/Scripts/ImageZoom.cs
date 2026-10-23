using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Museum
{
    public sealed class ImageZoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RawImage source;
        [SerializeField] private RawImage enlarged;
        [SerializeField] private GameObject overlay;
        [SerializeField, Range(1, 10)] private float maximumScale = 4;
        private float scale = 1;
        private float lastPinch;
        private Vector2 baseSize;
        private void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(() => overlay.SetActive(false));
            overlay.SetActive(false);
        }
        private void Open()
        {
            if (source.texture == null) return;
            overlay.SetActive(true);
            enlarged.texture = source.texture;
            Canvas.ForceUpdateCanvases();
            var viewport = (RectTransform)enlarged.rectTransform.parent;
            float aspect = (float)source.texture.width / source.texture.height;
            float width = Mathf.Min(viewport.rect.width, viewport.rect.height * aspect);
            baseSize = new Vector2(width, width / aspect);
            scale = 1; lastPinch = 0;
            enlarged.rectTransform.sizeDelta = baseSize;
            enlarged.rectTransform.anchoredPosition = Vector2.zero;
        }
        private void Update()
        {
            if (!overlay.activeSelf) return;
            // Refresh/USB updates must not leave the overlay referencing a destroyed texture.
            if (enlarged.texture != source.texture) { overlay.SetActive(false); return; }
            if (Input.touchCount == 2)
            {
                float distance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                if (lastPinch > 0) SetScale(scale * distance / lastPinch);
                lastPinch = distance;
            }
            else lastPinch = 0;
            if (Input.GetKeyDown(KeyCode.Escape)) overlay.SetActive(false);
        }
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data)
        {
            if (!overlay.activeSelf || Input.touchCount > 1) return;
            var parent = (RectTransform)enlarged.rectTransform.parent;
            Vector2 before, after;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position - data.delta, data.pressEventCamera, out before);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera, out after);
            enlarged.rectTransform.anchoredPosition += after - before;
            ClampPosition();
        }
        public void OnScroll(PointerEventData data) { if (overlay.activeSelf) SetScale(scale * (1 + data.scrollDelta.y * 0.12f)); }
        private void SetScale(float value)
        {
            scale = Mathf.Clamp(value, 1, maximumScale);
            enlarged.rectTransform.sizeDelta = baseSize * scale;
            ClampPosition();
        }
        private void ClampPosition()
        {
            var viewport = (RectTransform)enlarged.rectTransform.parent;
            Vector2 limits = Vector2.Max(Vector2.zero, (enlarged.rectTransform.sizeDelta - viewport.rect.size) / 2);
            Vector2 p = enlarged.rectTransform.anchoredPosition;
            enlarged.rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -limits.x, limits.x), Mathf.Clamp(p.y, -limits.y, limits.y));
        }
    }
}
