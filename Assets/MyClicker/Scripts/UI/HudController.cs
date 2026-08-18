using MyClicker.App;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class HudController : MonoBehaviour
    {
        Text _gold;
        Text _wave;
        Text _name;

        void Start()
        {
            var services = GameServices.Ensure();
            var parent = StoneUi.EnsureCanvas();
            var skin = services.Config != null ? services.Config.ui : new Data.GameConfig.UiSkin();

            var top = StoneUi.Panel(parent, "TopBar", skin);
            var topRt = top.rectTransform;
            topRt.anchorMin = new Vector2(0.04f, 0.90f);
            topRt.anchorMax = new Vector2(0.96f, 0.98f);
            topRt.offsetMin = Vector2.zero;
            topRt.offsetMax = Vector2.zero;

            _name = StoneUi.Label(top.transform, "Name", "", 32, TextAnchor.MiddleLeft);
            Place(_name.rectTransform, 0.04f, 0.1f, 0.45f, 0.9f);
            _wave = StoneUi.Label(top.transform, "Wave", "", 32, TextAnchor.MiddleCenter);
            Place(_wave.rectTransform, 0.40f, 0.1f, 0.70f, 0.9f);
            _gold = StoneUi.Label(top.transform, "Gold", "", 32, TextAnchor.MiddleRight);
            Place(_gold.rectTransform, 0.62f, 0.1f, 0.96f, 0.9f);

            var hint = StoneUi.Label(parent, "Hint", "Tap the invaders", 28, TextAnchor.LowerCenter);
            Place(hint.rectTransform, 0.1f, 0.03f, 0.9f, 0.08f);
            Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            if (GameServices.Instance == null)
                return;
            var profile = GameServices.Instance.Save.Profile;
            if (_name != null)
                _name.text = profile.displayName;
            if (_wave != null)
                _wave.text = "Wave " + profile.wave;
            if (_gold != null)
                _gold.text = profile.gold + "g";
        }

        static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
