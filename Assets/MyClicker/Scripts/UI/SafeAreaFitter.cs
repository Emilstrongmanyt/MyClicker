using UnityEngine;

namespace MyClicker.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        Rect _last;

        void OnEnable() => Apply();

        void Update()
        {
            if (_last != Screen.safeArea)
                Apply();
        }

        void Apply()
        {
            var rt = (RectTransform)transform;
            var area = Screen.safeArea;
            _last = area;
            var min = area.position;
            var max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
