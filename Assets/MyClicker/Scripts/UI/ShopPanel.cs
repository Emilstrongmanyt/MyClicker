using MyClicker.App;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class ShopPanel : MonoBehaviour
    {
        ShopRow[] _rows = new ShopRow[0];
        GameObject _root;
        bool _open;

        static readonly string[] Order =
        {
            ContentIds.Might,
            ContentIds.Cleave,
            ContentIds.Fortune,
            ContentIds.Harvest,
            ContentIds.Swift,
            ContentIds.Crit,
            ContentIds.Fury
        };

        public bool Open => _open;

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var panel = StoneUi.Panel(parent, "ShopPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.05f, 0.16f, 0.95f, 0.78f);

            var title = StoneUi.Label(panel.transform, "Title", "Forge", 40, TextAnchor.MiddleCenter);
            StoneUi.Place(title, 0.08f, 0.88f, 0.78f, 0.98f);

            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.82f, 0.88f, 0.96f, 0.98f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(panel.transform, false);
            StoneUi.Place(viewportGo.GetComponent<RectTransform>(), 0.03f, 0.04f, 0.97f, 0.86f);
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            _rows = new ShopRow[Order.Length];
            float rowH = 118f;
            content.sizeDelta = new Vector2(0f, Order.Length * rowH + 12f);
            for (int i = 0; i < Order.Length; i++)
                _rows[i] = BuildRow(content, skin, Order[i], i, rowH);

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

        ShopRow BuildRow(RectTransform parent, GameConfig.UiSkin skin, string id, int index, float rowH)
        {
            var row = StoneUi.Panel(parent, "Row_" + id, skin);
            var rt = row.rectTransform;
            rt.anchorMin = new Vector2(0.02f, 1f);
            rt.anchorMax = new Vector2(0.98f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -8f - index * rowH);
            rt.sizeDelta = new Vector2(0f, rowH - 10f);

            var icon = StoneUi.Icon(row.transform, "Icon", IconFor(id));
            StoneUi.Place(icon, 0.03f, 0.14f, 0.18f, 0.86f);

            var name = StoneUi.Label(row.transform, "Name", TitleFor(id), 26, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.20f, 0.52f, 0.68f, 0.92f);
            var detail = StoneUi.Label(row.transform, "Detail", "", 20, TextAnchor.UpperLeft);
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
            bool unlocked = economy.IsUnlocked(row.id);
            bool maxed = economy.IsMaxed(row.id);
            bool can = economy.CanBuy(row.id);
            row.name.text = TitleFor(row.id) + "  Lv " + level;
            row.detail.text = unlocked ? DetailFor(row.id, economy) : economy.LockReason(row.id);
            var label = row.buy.GetComponentInChildren<Text>();
            if (label != null)
            {
                if (!unlocked)
                    label.text = "Lock";
                else if (maxed)
                    label.text = "MAX";
                else
                    label.text = NumberFmt.Gold(cost);
            }

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
                case ContentIds.Cleave: return "Cleave";
                case ContentIds.Fury: return "Fury";
                case ContentIds.Harvest: return "Harvest";
                default: return id;
            }
        }

        static string DetailFor(string id, EconomyService economy)
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
                case ContentIds.Cleave:
                    return "Splash  " + Mathf.RoundToInt(economy.CleaveFraction * 100f) + "% to a nearby foe";
                case ContentIds.Fury:
                    return "Crit damage  x" + economy.CritMultiplier.ToString("0.00") + "   next +0.25";
                case ContentIds.Harvest:
                    return "More dust and potion drops each rank";
                default:
                    return "";
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
                case ContentIds.Cleave: return icons.skull != null ? icons.skull : icons.might;
                case ContentIds.Fury: return icons.heart != null ? icons.heart : icons.crit;
                case ContentIds.Harvest: return icons.dust != null ? icons.dust : icons.fortune;
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
