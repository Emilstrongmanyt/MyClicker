using MyClicker.App;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class PotionTray : MonoBehaviour
    {
        readonly PotionSlot[] _slots = new PotionSlot[3];
        StoneUi.TooltipView _tip;

        static readonly string[] Order =
        {
            ContentIds.PotMight,
            ContentIds.PotSwift,
            ContentIds.PotGold
        };

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var tray = StoneUi.Panel(parent, "PotionTray", skin);
            StoneUi.Place(tray, 0.04f, 0.018f, 0.40f, 0.108f);
            for (int i = 0; i < Order.Length; i++)
                _slots[i] = BuildSlot(tray.transform, skin, Order[i], i);

            _tip = StoneUi.Tooltip(parent, "PotionTip", skin);
            StoneUi.Place(_tip.root.GetComponent<RectTransform>(), 0.08f, 0.22f, 0.92f, 0.38f);
        }

        public void BindTooltip(StoneUi.TooltipView tip)
        {
            if (tip == null)
                return;
            _tip = tip;
        }

        public void Refresh()
        {
            for (int i = 0; i < _slots.Length; i++)
                RefreshSlot(_slots[i]);
        }

        PotionSlot BuildSlot(Transform parent, GameConfig.UiSkin skin, string id, int index)
        {
            var button = StoneUi.Button(parent, id, "", skin, null);
            float x0 = 0.04f + index * 0.32f;
            StoneUi.Place(button, x0, 0.08f, x0 + 0.28f, 0.92f);
            StoneUi.HideDefaultLabel(button);
            var icon = StoneUi.Icon(button.transform, "Icon", IconFor(id));
            StoneUi.Place(icon, 0.12f, 0.32f, 0.88f, 0.94f);
            var count = StoneUi.Label(button.transform, "Count", "0", 18, TextAnchor.UpperRight);
            StoneUi.Place(count, 0.42f, 0.68f, 0.96f, 0.98f);
            var timer = StoneUi.Label(button.transform, "Timer", "", 20, TextAnchor.LowerCenter);
            StoneUi.Place(timer, 0.04f, 0.02f, 0.96f, 0.34f);
            timer.color = new Color(1f, 0.86f, 0.42f);
            string captured = id;
            HoldPress.Bind(button.gameObject, () => Use(captured), () => ShowTip(captured), () => _tip?.Hide());
            return new PotionSlot { id = id, button = button, icon = icon, count = count, timer = timer };
        }

        void Use(string id)
        {
            if (GameServices.Instance == null || !GameServices.Instance.Economy.TryUsePotion(id))
                return;
            var hero = Object.FindFirstObjectByType<MyClicker.Character.HeroCharacterAdapter>();
            Vector3 at = hero != null ? hero.transform.position + Vector3.up * 0.35f : Vector3.zero;
            MyClicker.Audio.FxDirector.Ensure().Potion(id, at);
            Refresh();
        }

        void ShowTip(string id)
        {
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindPotion(id) : null;
            string name = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : id;
            string body = def != null ? def.description : "";
            float left = GameServices.Instance != null ? GameServices.Instance.Economy.PotionBuffLeft(id) : 0f;
            if (left > 0f)
                body = (body ?? "") + "\nActive  " + EconomyService.FormatBuff(left) + " remaining.";
            _tip?.Show(name, body);
        }

        void RefreshSlot(PotionSlot slot)
        {
            if (slot.count == null)
                return;
            var services = GameServices.Instance;
            var profile = services.Save.Profile;
            int n = profile.PotionCount(slot.id);
            float left = services.Economy.PotionBuffLeft(slot.id);
            slot.count.text = n.ToString();
            if (slot.timer != null)
            {
                slot.timer.text = left > 0f ? EconomyService.FormatBuff(left) : "";
                slot.timer.color = ColorFor(slot.id);
            }

            if (slot.icon != null)
            {
                if (slot.icon.sprite == null)
                    slot.icon.sprite = IconFor(slot.id);
                slot.icon.color = n > 0 || left > 0f ? Color.white : new Color(1f, 1f, 1f, 0.38f);
            }

            if (slot.button != null)
                slot.button.interactable = true;
        }

        static Color ColorFor(string id)
        {
            switch (id)
            {
                case ContentIds.PotMight: return new Color(1f, 0.55f, 0.28f);
                case ContentIds.PotSwift: return new Color(0.55f, 0.82f, 1f);
                case ContentIds.PotGold: return new Color(1f, 0.86f, 0.32f);
                default: return new Color(1f, 0.86f, 0.42f);
            }
        }

        static Sprite IconFor(string id)
        {
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindPotion(id) : null;
            if (def != null && def.icon != null)
                return def.icon;
            var icons = GameServices.Instance != null ? GameServices.Instance.Catalog.icons : null;
            return icons != null ? icons.potion : null;
        }

        struct PotionSlot
        {
            public string id;
            public Button button;
            public Image icon;
            public Text count;
            public Text timer;
        }
    }
}
