using MyClicker.App;
using MyClicker.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyClicker.Character
{
    public class CharacterCreatorController : MonoBehaviour
    {
        HeroCharacterAdapter _hero;
        InputField _nameField;
        string _currentSlot = "Hair";

        void Start()
        {
            var services = GameServices.Ensure();
            var prefab = HeroPrefabLoader.Load(services.Config);
            if (prefab == null)
            {
                Debug.LogError("[MyClicker] Character creator needs the HeroEditor Human prefab.");
                BuildUi();
                return;
            }

            _hero = HeroCharacterAdapter.Spawn(prefab, services.Save.Profile.heroJson, new Vector3(0f, -1.6f, 0f), 0.85f);
            BuildUi();
        }

        void BuildUi()
        {
            var safe = StoneUi.EnsureCanvas();
            var skin = GameServices.Instance.Config != null ? GameServices.Instance.Config.ui : new Data.GameConfig.UiSkin();

            var title = StoneUi.Label(safe, "Title", "Create Your Hero", 64, TextAnchor.UpperCenter);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.08f, 0.90f);
            titleRt.anchorMax = new Vector2(0.92f, 0.98f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            _nameField = StoneUi.Input(safe, "Name", "Name your hero", skin);
            var nameRt = _nameField.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.18f, 0.82f);
            nameRt.anchorMax = new Vector2(0.82f, 0.88f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            _nameField.text = GameServices.Instance.Save.Profile.displayName;

            var slotBar = StoneUi.Panel(safe, "Slots", skin);
            var slotRt = slotBar.GetComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0.05f, 0.22f);
            slotRt.anchorMax = new Vector2(0.95f, 0.36f);
            slotRt.offsetMin = Vector2.zero;
            slotRt.offsetMax = Vector2.zero;

            float i = 0;
            string[] slots = { "Hair", "Eyes", "Armor", "Helmet", "Weapon", "Cape" };
            foreach (var slot in slots)
            {
                string captured = slot;
                var button = StoneUi.Button(slotBar.transform, slot, slot, skin, () => _currentSlot = captured);
                var rt = button.GetComponent<RectTransform>();
                float x0 = 0.03f + i * 0.16f;
                rt.anchorMin = new Vector2(x0, 0.18f);
                rt.anchorMax = new Vector2(x0 + 0.15f, 0.82f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                i++;
            }

            var prev = StoneUi.Button(safe, "Prev", "<", skin, () => Cycle(-1));
            Place(prev, 0.08f, 0.42f, 0.22f, 0.52f);
            var next = StoneUi.Button(safe, "Next", ">", skin, () => Cycle(1));
            Place(next, 0.78f, 0.42f, 0.92f, 0.52f);

            var confirm = StoneUi.Button(safe, "Confirm", "Enter Battle", skin, Confirm);
            Place(confirm, 0.18f, 0.06f, 0.82f, 0.16f);
        }

        void Cycle(int delta)
        {
            if (_hero != null)
                _hero.Cycle(_currentSlot, delta);
        }

        void Confirm()
        {
            var profile = GameServices.Instance.Save.Profile;
            if (_nameField != null && !string.IsNullOrWhiteSpace(_nameField.text))
                profile.displayName = _nameField.text.Trim();
            if (_hero != null)
                profile.heroJson = _hero.ToJson();
            GameServices.Instance.Save.MarkCharacterCreated();
            SceneManager.LoadScene("Battle");
        }

        static void Place(Button button, float x0, float y0, float x1, float y1)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
