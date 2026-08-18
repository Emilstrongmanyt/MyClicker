using MyClicker.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public static class StoneUi
    {
        public static Transform EnsureCanvas()
        {
            if (Camera.main != null)
            {
                Camera.main.orthographic = true;
                Camera.main.orthographicSize = 8f;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            var safe = canvas.transform.Find("SafeArea");
            if (safe == null)
            {
                var go = new GameObject("SafeArea", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                go.AddComponent<SafeAreaFitter>();
                safe = go.transform;
            }

            return safe;
        }

        public static Text Label(Transform parent, string name, string text, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = anchor;
            label.fontSize = size;
            label.color = new Color(0.95f, 0.90f, 0.80f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = size;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return label;
        }

        public static Image Panel(Transform parent, string name, GameConfig.UiSkin skin)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = skin != null ? skin.panel : null;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : new Color(0.22f, 0.18f, 0.14f, 0.92f);
            return image;
        }

        public static Button Button(Transform parent, string name, string caption, GameConfig.UiSkin skin, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = skin != null ? skin.buttonNormal : null;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : new Color(0.45f, 0.32f, 0.20f, 1f);
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var label = Label(go.transform, "Label", caption, 36, TextAnchor.MiddleCenter);
            var rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 6);
            rt.offsetMax = new Vector2(-8, -6);
            return button;
        }

        public static InputField Input(Transform parent, string name, string placeholder, GameConfig.UiSkin skin)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = skin != null ? skin.panel : null;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : new Color(0.16f, 0.13f, 0.10f, 0.95f);

            var text = Label(go.transform, "Text", "", 40, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 16, 8);
            var ph = Label(go.transform, "Placeholder", placeholder, 34, TextAnchor.MiddleCenter);
            ph.color = new Color(0.75f, 0.70f, 0.62f, 0.7f);
            Stretch(ph.rectTransform, 16, 8);

            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = ph;
            field.characterLimit = 16;
            return field;
        }

        public static Slider Bar(Transform parent, string name, GameConfig.UiSkin skin)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Stretch(bg.GetComponent<RectTransform>(), 0, 0);
            var bgImage = bg.GetComponent<Image>();
            bgImage.sprite = skin != null ? skin.hpBackground : null;
            bgImage.color = bgImage.sprite != null ? Color.white : new Color(0.12f, 0.10f, 0.09f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>(), 6, 4);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>(), 0, 0);
            var fillImage = fill.GetComponent<Image>();
            fillImage.sprite = skin != null ? skin.hpFill : null;
            fillImage.color = fillImage.sprite != null ? Color.white : new Color(0.72f, 0.18f, 0.16f, 1f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            return slider;
        }

        static void Stretch(RectTransform rt, float x, float y)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(x, y);
            rt.offsetMax = new Vector2(-x, -y);
        }
    }
}
