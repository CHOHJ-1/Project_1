using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Museum
{
    public sealed class ArtifactScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private ArtifactItem itemPrefab;
        [SerializeField] private RawImage description;
        [SerializeField] private Text status;
        private readonly List<ArtifactItem> items = new List<ArtifactItem>();
        private readonly List<Texture2D> textures = new List<Texture2D>();
        private readonly List<string> descriptions = new List<string>();
        private Texture2D currentDescription;
        private int selected = -1;

        private Vector2 lastAreaSize;
        private void Awake()
        {
            // These are serialized design previews, not runtime content.
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            description.texture = null;
        }
        private void Update()
        {
            var area = description.rectTransform.parent as RectTransform;
            if (area != null && area.rect.size != lastAreaSize)
            {
                lastAreaSize = area.rect.size;
                FitDescription();
            }
        }
        private void FitDescription()
        {
            if (currentDescription == null) return;
            var area = (RectTransform)description.rectTransform.parent;
            float ratio = (float)currentDescription.width / currentDescription.height;
            float width = Mathf.Min(area.rect.width, area.rect.height * ratio);
            description.rectTransform.sizeDelta = new Vector2(width, width / ratio);
        }
        public void SetStatus(string message) { status.text = message; }
        public void ShowCollection(string folder, string[] ids)
        {
            description.texture = null;
            if (currentDescription != null) Destroy(currentDescription);
            currentDescription = null;
            foreach (var item in items) { item.gameObject.SetActive(false); Destroy(item.gameObject); }
            foreach (var texture in textures) Destroy(texture);
            items.Clear(); textures.Clear(); descriptions.Clear(); selected = -1;
            foreach (string id in ids)
            {
                Texture2D texture = LoadPng(Path.Combine(folder, "Icons", id + ".png"));
                textures.Add(texture);
                var item = Instantiate(itemPrefab, content);
                item.gameObject.name = "Artifact_" + id;
                int index = items.Count;
                item.Bind(texture, () => Select(index));
                items.Add(item);
                descriptions.Add(Path.Combine(folder, "Descriptions", id + ".png"));
            }
            content.anchoredPosition = Vector2.zero;
            ResizeContent();
            if (items.Count > 0) Select(0);
        }
        private void OnRectTransformDimensionsChange() { if (grid != null) ResizeContent(); }
        private void ResizeContent()
        {
            int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount ? Mathf.Max(1, grid.constraintCount) : 1;
            int rows = Mathf.CeilToInt((float)items.Count / columns);
            float height = grid.padding.top + grid.padding.bottom + rows * grid.cellSize.y + Mathf.Max(0, rows - 1) * grid.spacing.y;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
        private void Select(int index)
        {
            if (selected == index) return;
            // Load first, so a read failure does not discard the currently visible image.
            Texture2D next;
            try { next = LoadPng(descriptions[index]); }
            catch (Exception e) { SetStatus("Image error: " + e.Message); return; }
            if (currentDescription != null) Destroy(currentDescription);
            currentDescription = next;
            description.texture = next;
            FitDescription();
            selected = index;
            for (int i = 0; i < items.Count; i++) items[i].SetSelected(i == selected);
        }
        public static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true)) throw new InvalidDataException("Invalid PNG");
                texture.wrapMode = TextureWrapMode.Clamp;
                return texture;
            }
            catch { Destroy(texture); throw; }
        }
        private void OnDestroy()
        {
            foreach (var texture in textures) Destroy(texture);
            if (currentDescription != null) Destroy(currentDescription);
        }
    }
}
