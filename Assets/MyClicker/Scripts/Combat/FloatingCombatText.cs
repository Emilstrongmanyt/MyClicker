using MyClicker.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.Combat
{
    public class FloatingCombatText : MonoBehaviour
    {
        Text _label;
        float _life;
        Vector3 _world;
        Vector3 _drift;

        public static void Show(Vector3 world, string text, Color color, int size = 36)
        {
            var canvas = StoneUi.EnsureCanvas();
            var go = new GameObject("Floater", typeof(RectTransform), typeof(Text), typeof(FloatingCombatText));
            go.transform.SetParent(canvas, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = size;
            label.color = color;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = label.rectTransform;
            rt.sizeDelta = new Vector2(240f, 64f);
            var floater = go.GetComponent<FloatingCombatText>();
            floater._label = label;
            floater._world = world + Vector3.up * 0.35f;
            floater._life = 0.7f;
            floater._drift = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(0.9f, 1.4f), 0f);
            floater.Place();
        }

        void Update()
        {
            _life -= Time.deltaTime;
            _world += _drift * Time.deltaTime;
            if (_label != null)
            {
                var c = _label.color;
                c.a = Mathf.Clamp01(_life / 0.35f);
                _label.color = c;
            }

            Place();
            if (_life <= 0f)
                Destroy(gameObject);
        }

        void Place()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            var screen = cam.WorldToScreenPoint(_world);
            transform.position = screen;
        }
    }
}
