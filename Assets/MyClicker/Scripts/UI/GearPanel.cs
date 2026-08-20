using MyClicker.App;
using MyClicker.Character;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class GearPanel : MonoBehaviour
    {
        GameObject _root;
        bool _open;
        GearRow[] _rows;
        CraftRow[] _crafts;

        static readonly string[] Slots = HeroCharacterAdapter.GearSlots;
        static readonly string[] Potions =
        {
            ContentIds.PotMight,
            ContentIds.PotSwift,
            ContentIds.PotGold
        };

        public bool Open => _open;

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var panel = StoneUi.Panel(parent, "GearPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.04f, 0.16f, 0.96f, 0.78f);

            var title = StoneUi.Label(panel.transform, "Title", "Armory", 40, TextAnchor.MiddleCenter);
            StoneUi.Place(title, 0.08f, 0.90f, 0.78f, 0.98f);
            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.82f, 0.90f, 0.96f, 0.98f);

            _rows = new GearRow[Slots.Length];
            for (int i = 0; i < Slots.Length; i++)
                _rows[i] = BuildGearRow(panel.transform, skin, Slots[i], i);

            var craftTitle = StoneUi.Label(panel.transform, "CraftTitle", "Craft", 26, TextAnchor.MiddleLeft);
            StoneUi.Place(craftTitle, 0.05f, 0.20f, 0.60f, 0.26f);

            _crafts = new CraftRow[Potions.Length];
            for (int i = 0; i < Potions.Length; i++)
                _crafts[i] = BuildCraft(panel.transform, skin, Potions[i], i);

            Hide();
        }

        public void Toggle()
        {
            if (_open) Hide();
            else Show();
        }

        public void Show()
        {
            _open = true;
            if (_root != null)
            {
                _root.SetActive(true);
                StoneUi.BringFront(_root);
            }
            Refresh();
        }

        public void Hide()
        {
            _open = false;
            if (_root != null)
                _root.SetActive(false);
        }

        public void Refresh()
        {
            if (!_open)
                return;
            for (int i = 0; i < _rows.Length; i++)
                RefreshGear(_rows[i]);
            for (int i = 0; i < _crafts.Length; i++)
                RefreshCraft(_crafts[i]);
        }

        GearRow BuildGearRow(Transform parent, GameConfig.UiSkin skin, string slot, int index)
        {
            var row = StoneUi.Panel(parent, "Gear_" + slot, skin);
            float top = 0.88f - index * 0.15f;
            StoneUi.Place(row, 0.04f, top - 0.14f, 0.96f, top);

            var name = StoneUi.Label(row.transform, "Name", slot, 24, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.03f, 0.52f, 0.46f, 0.92f);
            var detail = StoneUi.Label(row.transform, "Detail", "", 20, TextAnchor.UpperLeft);
            StoneUi.Place(detail, 0.03f, 0.08f, 0.46f, 0.54f);

            var prev = StoneUi.Button(row.transform, "Prev", "<", skin, () => Cycle(slot, -1));
            StoneUi.Place(prev, 0.47f, 0.18f, 0.58f, 0.82f);
            var next = StoneUi.Button(row.transform, "Next", ">", skin, () => Cycle(slot, 1));
            StoneUi.Place(next, 0.59f, 0.18f, 0.70f, 0.82f);
            var temper = StoneUi.Button(row.transform, "Temper", "", skin, () => Temper(slot));
            StoneUi.Place(temper, 0.72f, 0.16f, 0.97f, 0.84f);
            StoneUi.HideDefaultLabel(temper);
            var price = StoneUi.Price(temper.transform, "Price", 26);
            StoneUi.Place(price.root, 0.04f, 0.10f, 0.96f, 0.90f);

            return new GearRow { slot = slot, name = name, detail = detail, temper = temper, prev = prev, next = next, price = price };
        }

        CraftRow BuildCraft(Transform parent, GameConfig.UiSkin skin, string id, int index)
        {
            var button = StoneUi.Button(parent, "Craft_" + id, "", skin, () => Craft(id));
            float x0 = 0.04f + index * 0.32f;
            StoneUi.Place(button, x0, 0.04f, x0 + 0.30f, 0.18f);
            StoneUi.HideDefaultLabel(button);
            var name = StoneUi.Label(button.transform, "Name", PotionName(id), 20, TextAnchor.MiddleCenter);
            StoneUi.Place(name, 0.04f, 0.52f, 0.96f, 0.94f);
            var price = StoneUi.Price(button.transform, "Price", 22);
            StoneUi.Place(price.root, 0.06f, 0.06f, 0.94f, 0.52f);
            return new CraftRow { id = id, button = button, name = name, price = price };
        }

        void Cycle(string slot, int delta)
        {
            GameServices.Instance?.Gear.Cycle(slot, delta);
            Refresh();
        }

        void Temper(string slot)
        {
            GameServices.Instance?.Gear.TryTemper(slot);
            Refresh();
        }

        void Craft(string id)
        {
            GameServices.Instance?.Gear.TryCraft(id);
            Refresh();
        }

        void RefreshGear(GearRow row)
        {
            var gear = GameServices.Instance != null ? GameServices.Instance.Gear : null;
            if (gear == null || row.name == null)
                return;
            int rank = GameServices.Instance.Save.Profile.TemperLevel(row.slot);
            int owned = gear.OwnedCount(row.slot);
            bool canCycle = gear.CanCycle(row.slot);
            row.name.text = row.slot + "  " + gear.Label(row.slot);
            row.detail.text = owned <= 0
                ? "Starter look. Relics drop in battle."
                : gear.BonusText(row.slot) + "   T" + rank + "   " + owned + " relics";
            if (row.price != null)
                row.price.Set(NumberFmt.Compact(gear.TemperCost(row.slot)), DustIcon());
            row.temper.interactable = GameServices.Instance.Save.Profile.dust >= gear.TemperCost(row.slot);
            if (row.prev != null)
                row.prev.interactable = canCycle;
            if (row.next != null)
                row.next.interactable = canCycle;
        }

        void RefreshCraft(CraftRow row)
        {
            var gear = GameServices.Instance != null ? GameServices.Instance.Gear : null;
            if (gear == null || row.button == null)
                return;
            int cost = gear.CraftCost(row.id);
            if (row.name != null)
                row.name.text = PotionName(row.id);
            if (row.price != null)
                row.price.Set(NumberFmt.Compact(cost), DustIcon());
            row.button.interactable = GameServices.Instance.Save.Profile.dust >= cost;
        }

        static Sprite DustIcon()
        {
            var icons = GameServices.Instance != null ? GameServices.Instance.Catalog.icons : null;
            return icons != null ? icons.dust : null;
        }

        static string PotionName(string id)
        {
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindPotion(id) : null;
            if (def != null && !string.IsNullOrEmpty(def.displayName))
                return def.displayName;
            switch (id)
            {
                case ContentIds.PotMight: return "Ember";
                case ContentIds.PotSwift: return "Gale";
                case ContentIds.PotGold: return "Gilded";
                default: return id;
            }
        }

        struct GearRow
        {
            public string slot;
            public Text name;
            public Text detail;
            public Button temper;
            public Button prev;
            public Button next;
            public StoneUi.PriceView price;
        }

        struct CraftRow
        {
            public string id;
            public Button button;
            public Text name;
            public StoneUi.PriceView price;
        }
    }
}
