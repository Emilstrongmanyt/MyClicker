using MyClicker.App;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class ShopPanel : MonoBehaviour
    {
        readonly ShopRow[] _rows = new ShopRow[4];
        GameObject _root;
        Text _title;
        bool _open;

        static readonly string[] Order =
        {
            ContentIds.Might,
            ContentIds.Fortune,
            ContentIds.Swift,
            ContentIds.Crit
        };

        public bool Open => _open;

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var panel = StoneUi.Panel(parent, "ShopPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.05f, 0.16f, 0.95f, 0.78f);

            _title = StoneUi.Label(panel.transform, "Title", "Forge", 40, TextAnchor.MiddleCenter);
            StoneUi.Place(_title, 0.08f, 0.88f, 0.78f, 0.98f);

            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.82f, 0.88f, 0.96f, 0.98f);

            for (int i = 0; i < Order.Length; i++)
            {
                _rows[i] = BuildRow(panel.transform, skin, Order[i], i);
            }

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
                _root.SetActive(true);
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
                RefreshRow(_rows[i]);
        }

        ShopRow BuildRow(Transform parent, GameConfig.UiSkin skin, string id, int index)
        {
            var row = StoneUi.Panel(parent, "Row_" + id, skin);
            float top = 0.86f - index * 0.18f;
            StoneUi.Place(row, 0.04f, top - 0.16f, 0.96f, top);

            var icon = StoneUi.Icon(row.transform, "Icon", IconFor(id));
            StoneUi.Place(icon, 0.03f, 0.14f, 0.18f, 0.86f);

            var name = StoneUi.Label(row.transform, "Name", TitleFor(id), 28, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.20f, 0.52f, 0.68f, 0.92f);
            var detail = StoneUi.Label(row.transform, "Detail", "", 22, TextAnchor.UpperLeft);
            StoneUi.Place(detail, 0.20f, 0.08f, 0.68f, 0.56f);

            var buy = StoneUi.Button(row.transform, "Buy", "Buy", skin, () => Buy(id));
            StoneUi.Place(buy, 0.70f, 0.16f, 0.97f, 0.84f);

            return new ShopRow { id = id, icon = icon, name = name, detail = detail, buy = buy };
        }

        void Buy(string id)
        {
            var services = GameServices.Instance;
            if (services == null || !services.Economy.TryBuy(id))
                return;
            Refresh();
        }

        void RefreshRow(ShopRow row)
        {
            if (row.name == null)
                return;
            var services = GameServices.Instance;
            var economy = services.Economy;
            var profile = services.Save.Profile;
            int level = profile.UpgradeLevel(row.id);
            long cost = economy.UpgradeCost(row.id);
            bool maxed = economy.IsMaxed(row.id);
            bool can = economy.CanBuy(row.id);
            row.name.text = TitleFor(row.id) + "  Lv " + level;
            row.detail.text = DetailFor(row.id, level, economy);
            var label = row.buy.GetComponentInChildren<Text>();
            if (label != null)
                label.text = maxed ? "MAX" : NumberFmt.Gold(cost);
            row.buy.interactable = can;
            if (row.icon.sprite == null)
                row.icon.sprite = IconFor(row.id);
        }

        static string TitleFor(string id)
        {
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindUpgrade(id) : null;
            if (def != null && !string.IsNullOrEmpty(def.displayName))
                return def.displayName;
            switch (id)
            {
                case ContentIds.Might: return "Might";
                case ContentIds.Fortune: return "Fortune";
                case ContentIds.Swift: return "Swift";
                case ContentIds.Crit: return "Crit";
                default: return id;
            }
        }

        static string DetailFor(string id, int level, EconomyService economy)
        {
            switch (id)
            {
                case ContentIds.Might:
                    return "Tap  " + Mathf.RoundToInt(economy.TapDamage) + "   next +" +
                           Mathf.RoundToInt(GameServices.Instance.Config.economy.mightPerLevel);
                case ContentIds.Fortune:
                    return "Gold  x" + economy.GoldMultiplier.ToString("0.00") + "   next +12%";
                case ContentIds.Swift:
                    return "Auto  " + economy.AutoInterval.ToString("0.00") + "s   DPS " +
                           Mathf.RoundToInt(economy.AutoDps);
                case ContentIds.Crit:
                    return "Crit  " + Mathf.RoundToInt(economy.CritChance * 100f) + "%  x" +
                           economy.CritMultiplier.ToString("0.#");
                default:
                    return "Lv " + level;
            }
        }

        static Sprite IconFor(string id)
        {
            var icons = GameServices.Instance != null ? GameServices.Instance.Catalog.icons : null;
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindUpgrade(id) : null;
            if (def != null && def.icon != null)
                return def.icon;
            if (icons == null)
                return null;
            switch (id)
            {
                case ContentIds.Might: return icons.might;
                case ContentIds.Fortune: return icons.fortune;
                case ContentIds.Swift: return icons.swift;
                case ContentIds.Crit: return icons.crit;
                default: return icons.shop;
            }
        }

        struct ShopRow
        {
            public string id;
            public Image icon;
            public Text name;
            public Text detail;
            public Button buy;
        }
    }
}
