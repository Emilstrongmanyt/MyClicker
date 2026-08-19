using MyClicker.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public static class StoneUi
    {
        public static Image Icon(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.15f);
            image.raycastTarget = false;
            return image;
        }

        public static void HideDefaultLabel(Button button)
        {
            if (button == null)
                return;
            var label = button.transform.Find("Label");
            if (label != null)
                label.gameObject.SetActive(false);
        }

        public static PriceView Price(Transform parent, string name, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var amount = Label(go.transform, "Amount", "", size, TextAnchor.MiddleCenter);
            Place(amount, 0.04f, 0.08f, 0.96f, 0.92f);
            var icon = Icon(go.transform, "Icon", null);
            Place(icon, 0.70f, 0.16f, 0.96f, 0.84f);
            icon.gameObject.SetActive(false);
            return new PriceView { root = go.GetComponent<RectTransform>(), amount = amount, icon = icon };
        }

        public static TooltipView Tooltip(Transform parent, string name, GameConfig.UiSkin skin)
        {
            var frame = Panel(parent, name, skin);
            frame.raycastTarget = false;
            var title = Label(frame.transform, "Title", "", 28, TextAnchor.MiddleLeft);
            Place(title, 0.06f, 0.62f, 0.94f, 0.94f);
            var body = Label(frame.transform, "Body", "", 22, TextAnchor.UpperLeft);
            Place(body, 0.06f, 0.08f, 0.94f, 0.64f);
            var outline = body.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.85f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
            frame.gameObject.SetActive(false);
            return new TooltipView { root = frame.gameObject, title = title, body = body };
        }

        public static BannerView Banner(Transform parent, string name, GameConfig.UiSkin skin)
        {
            var frame = Panel(parent, name, skin);
            frame.raycastTarget = false;
            if (skin != null && skin.bannerVictory != null)
            {
                frame.sprite = skin.bannerVictory;
                frame.type = Image.Type.Sliced;
                frame.color = Color.white;
            }
            else
                frame.color = new Color(0.10f, 0.07f, 0.05f, 0.94f);

            var text = Label(frame.transform, "Text", "", 42, TextAnchor.MiddleCenter);
            Place(text, 0.06f, 0.10f, 0.94f, 0.90f);
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = 44;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.04f, 0.02f, 0.01f, 1f);
            outline.effectDistance = new Vector2(2.6f, -2.6f);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(0f, -3f);
            frame.gameObject.SetActive(false);
            return new BannerView { root = frame.gameObject, text = text };
        }

        public static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(Component component, float x0, float y0, float x1, float y1)
        {
            Place(component.GetComponent<RectTransform>(), x0, y0, x1, y1);
        }

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
            label.raycastTarget = false;
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
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.color = new Color(0.82f, 0.16f, 0.14f, 1f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            return slider;
        }

        public static HealthBarView HealthBar(Transform parent, string name, GameConfig.UiSkin skin)
        {
            var frame = Panel(parent, name, skin);
            var inset = new GameObject("Track", typeof(RectTransform), typeof(Image));
            inset.transform.SetParent(frame.transform, false);
            Place(inset.GetComponent<RectTransform>(), 0.035f, 0.10f, 0.965f, 0.58f);
            var track = inset.GetComponent<Image>();
            track.sprite = SolidSprite();
            track.color = new Color(0.08f, 0.07f, 0.06f, 0.96f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(inset.transform, false);
            Stretch(fillGo.GetComponent<RectTransform>(), 0, 0);
            var fill = fillGo.GetComponent<Image>();
            fill.sprite = SolidSprite();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.color = new Color(0.28f, 0.82f, 0.32f, 1f);
            fill.raycastTarget = false;

            var title = Label(frame.transform, "Title", "", 24, TextAnchor.MiddleLeft);
            Place(title, 0.04f, 0.64f, 0.70f, 0.96f);
            var value = Label(frame.transform, "Value", "", 22, TextAnchor.MiddleRight);
            Place(value, 0.55f, 0.64f, 0.96f, 0.96f);
            return new HealthBarView { root = frame.gameObject, fill = fill, title = title, value = value };
        }

        public sealed class PriceView
        {
            public RectTransform root;
            public Text amount;
            public Image icon;

            public void Set(string text, Sprite currency)
            {
                if (amount != null)
                    amount.text = text ?? "";
                bool showIcon = currency != null;
                if (icon != null)
                {
                    icon.sprite = currency;
                    icon.color = showIcon ? Color.white : new Color(1f, 1f, 1f, 0.15f);
                    icon.gameObject.SetActive(showIcon);
                }

                if (amount == null)
                    return;
                if (showIcon)
                {
                    Place(amount, 0.02f, 0.08f, 0.66f, 0.92f);
                    amount.alignment = TextAnchor.MiddleRight;
                }
                else
                {
                    Place(amount, 0.04f, 0.08f, 0.96f, 0.92f);
                    amount.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        public sealed class TooltipView
        {
            public GameObject root;
            public Text title;
            public Text body;

            public void Show(string heading, string text)
            {
                if (root != null)
                {
                    root.SetActive(true);
                    BringFront(root);
                }
                if (title != null)
                    title.text = heading ?? "";
                if (body != null)
                    body.text = text ?? "";
            }

            public void Hide()
            {
                if (root != null)
                    root.SetActive(false);
            }
        }

        public sealed class BannerView
        {
            public GameObject root;
            public Text text;

            public void Show(string message)
            {
                bool on = !string.IsNullOrEmpty(message);
                if (root != null)
                    root.SetActive(on);
                if (text != null)
                    text.text = on ? message : "";
            }
        }

        public sealed class HealthBarView
        {
            public GameObject root;
            public Image fill;
            public Text title;
            public Text value;

            public void SetVisible(bool visible)
            {
                if (root != null)
                    root.SetActive(visible);
            }

            public void Set(string name, float current, float max)
            {
                float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
                if (fill != null)
                {
                    fill.fillAmount = pct;
                    fill.sprite = SolidSprite();
                    if (pct > 0.75f)
                        fill.color = new Color(0.28f, 0.82f, 0.32f, 1f);
                    else if (pct >= 0.25f)
                        fill.color = new Color(1f, 0.62f, 0.14f, 1f);
                    else
                        fill.color = new Color(0.90f, 0.16f, 0.12f, 1f);
                }

                if (title != null)
                    title.text = name;
                if (value != null)
                    value.text = Mathf.CeilToInt(Mathf.Max(0f, current)) + " / " + Mathf.CeilToInt(max);
            }
        }

        public static void BringFront(GameObject go)
        {
            if (go != null)
                go.transform.SetAsLastSibling();
        }

        static Sprite _solid;

        public static Sprite SolidSprite()
        {
            if (_solid != null)
                return _solid;
            var tex = Texture2D.whiteTexture;
            _solid = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 4f);
            _solid.name = "Solid";
            return _solid;
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
