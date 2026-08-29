using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class StarChartView : MonoBehaviour
    {
        public RectTransform content;
        public ScrollRect scroll;
        public float minZoom = 0.28f;
        public float maxZoom = 1.25f;
        float _zoom = 0.42f;
        float _lastPinch;

        void Update()
        {
            if (content == null)
                return;
            float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(wheel) > 0.01f)
                ZoomAt(_zoom * (wheel > 0f ? 1.08f : 0.92f));

            var touch = Touchscreen.current;
            if (touch == null)
                return;
            int down = 0;
            Vector2 a = Vector2.zero;
            Vector2 b = Vector2.zero;
            for (int i = 0; i < touch.touches.Count; i++)
            {
                var t = touch.touches[i];
                if (!t.press.isPressed)
                    continue;
                if (down == 0)
                    a = t.position.ReadValue();
                else if (down == 1)
                    b = t.position.ReadValue();
                down++;
            }

            if (down < 2)
            {
                _lastPinch = 0f;
                return;
            }

            float dist = Vector2.Distance(a, b);
            if (_lastPinch > 8f && dist > 8f)
                ZoomAt(_zoom * (dist / _lastPinch));
            _lastPinch = dist;
        }

        public void ZoomBy(float mul)
        {
            ZoomAt(_zoom * mul);
        }

        public void Recenter()
        {
            SetZoom(0.42f);
            if (content != null)
                content.anchoredPosition = Vector2.zero;
            if (scroll != null)
            {
                scroll.velocity = Vector2.zero;
                scroll.StopMovement();
            }
        }

        public void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            if (content != null)
                content.localScale = Vector3.one * _zoom;
        }

        void ZoomAt(float zoom)
        {
            SetZoom(zoom);
        }
    }
}
