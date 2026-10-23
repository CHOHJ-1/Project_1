using System;
using UnityEngine;
using UnityEngine.UI;

namespace Museum
{
    public sealed class ArtifactItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RawImage icon;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private Color normalColor = new Color(0.15f, 0.23f, 0.3f, 1);
        [SerializeField] private Color selectedColor = new Color(0.15f, 0.75f, 0.9f, 1);
        public void Bind(Texture2D texture, Action onClick)
        {
            icon.texture = texture;
            float aspect = (float)texture.width / texture.height;
            var fitter = icon.GetComponent<AspectRatioFitter>();
            if (fitter != null) fitter.aspectRatio = aspect;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
        public void SetSelected(bool selected)
        {
            selectionBorder.color = selected ? selectedColor : normalColor;
        }
    }
}
