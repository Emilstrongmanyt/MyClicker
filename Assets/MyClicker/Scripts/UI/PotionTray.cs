using MyClicker.App;
using MyClicker.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class PotionTray : MonoBehaviour
    {
        readonly PotionSlot[] _slots = new PotionSlot[3];

        static readonly string[] Order =
        {
            ContentIds.PotMight,
            ContentIds.PotSwift,
            ContentIds.PotGold
        };

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var tray = StoneUi.Panel(parent, "PotionTray", skin);
            StoneUi.Place(tray, 0.04f, 0.145f, 0.48f, 0.23f);
            for (int i = 0; i < Order.Length; i++)
            {
                _slots[i] = BuildSlot(tray.transform, skin, Order[i], i);
            }
        }

        public void Refresh()
        {
            for (int i = 0; i < _slots.Length; i++)
                RefreshSlot(_slots[i]);
        }

        PotionSlot BuildSlot(Transform parent, GameConfig.UiSkin skin, string id, int index)
        {
            var button = StoneUi.Button(parent, id, "", skin, () => Use(id));
            float x0 = 0.04f + index * 0.32f;
            StoneUi.Place(button, x0, 0.08f, x0 + 0.28f, 0.92f);
            var icon = StoneUi.Icon(button.transform, "Icon", IconFor(id));
            StoneUi.Place(icon, 0.12f, 0.28f, 0.88f, 0.92f);
            var count = StoneUi.Label(button.transform, "Count", "0", 22, TextAnchor.LowerCenter);
            StoneUi.Place(count, 0.05f, 0.02f, 0.95f, 0.38f);
            return new PotionSlot { id = id, button = button, icon = icon, count = count };
        }

        void Use(string id)
        {
            GameServices.Instance?.Economy.TryUsePotion(id);
            Refresh();
        }

        void RefreshSlot(PotionSlot slot)
        {
            if (slot.count == null)
                return;
            var profile = GameServices.Instance.Save.Profile;
            int n = profile.PotionCount(slot.id);
            slot.count.text = n.ToString();
            slot.button.interactable = n > 0;
            if (slot.icon.sprite == null)
                slot.icon.sprite = IconFor(slot.id);
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
        }
    }
}
