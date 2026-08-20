using MyClicker.App;
using MyClicker.Combat;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class GloryPanel : MonoBehaviour
    {
        GameObject _root;
        bool _open;
        Text _summary;
        Button _ascend;
        MutRow[] _rows;

        static readonly string[] Order =
        {
            ContentIds.MutMight,
            ContentIds.MutFortune,
            ContentIds.MutSwift,
            ContentIds.MutLuck
        };

        public bool Open => _open;

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var panel = StoneUi.Panel(parent, "GloryPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.05f, 0.16f, 0.95f, 0.78f);

            var title = StoneUi.Label(panel.transform, "Title", "Glory", 40, TextAnchor.MiddleCenter);
            StoneUi.Place(title, 0.08f, 0.88f, 0.78f, 0.98f);
            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.82f, 0.88f, 0.96f, 0.98f);

            _summary = StoneUi.Label(panel.transform, "Summary", "", 22, TextAnchor.UpperLeft);
            StoneUi.Place(_summary, 0.06f, 0.72f, 0.94f, 0.86f);

            _ascend = StoneUi.Button(panel.transform, "Ascend", "Ascend", skin, Ascend);
            StoneUi.Place(_ascend, 0.08f, 0.60f, 0.92f, 0.70f);

            _rows = new MutRow[Order.Length];
            for (int i = 0; i < Order.Length; i++)
                _rows[i] = BuildRow(panel.transform, skin, Order[i], i);

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
            var services = GameServices.Instance;
            if (services == null)
                return;
            var profile = services.Save.Profile;
            var economy = services.Economy;
            if (_summary != null)
            {
                int pending = economy.PendingGlory;
                int bosses = economy.RunBosses;
                string pendingLine = bosses > 0
                    ? "This run: " + bosses + (bosses == 1 ? " boss, +" : " bosses, +") + pending + " Glory on ascend."
                    : "Beat bosses this run to bank Glory for your next ascend.";
                _summary.text = "Glory  " + profile.glory + "    Ascensions  " + profile.ascendCount +
                                "\n" + pendingLine + " Unspent Glory still helps offline gold. Relics stay.";
            }

            if (_ascend != null)
            {
                var label = _ascend.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (!economy.CanAscend())
                        label.text = "Beat a boss to ascend";
                    else if (economy.PendingGlory > 0)
                        label.text = "Ascend — +" + economy.PendingGlory + " Glory, keep relics";
                    else
                        label.text = "Ascend — keep relics, reset run";
                }
                _ascend.interactable = economy.CanAscend();
            }

            for (int i = 0; i < _rows.Length; i++)
                RefreshRow(_rows[i]);
        }

        MutRow BuildRow(Transform parent, GameConfig.UiSkin skin, string id, int index)
        {
            var row = StoneUi.Panel(parent, "Mut_" + id, skin);
            float top = 0.56f - index * 0.13f;
            StoneUi.Place(row, 0.05f, top - 0.12f, 0.95f, top);
            var name = StoneUi.Label(row.transform, "Name", Title(id), 24, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.04f, 0.52f, 0.62f, 0.92f);
            var detail = StoneUi.Label(row.transform, "Detail", "", 18, TextAnchor.UpperLeft);
            StoneUi.Place(detail, 0.04f, 0.08f, 0.62f, 0.54f);
            var buy = StoneUi.Button(row.transform, "Buy", "", skin, () => Buy(id));
            StoneUi.Place(buy, 0.66f, 0.14f, 0.96f, 0.86f);
            StoneUi.HideDefaultLabel(buy);
            var price = StoneUi.Price(buy.transform, "Price", 24);
            StoneUi.Place(price.root, 0.04f, 0.10f, 0.96f, 0.90f);
            return new MutRow { id = id, name = name, detail = detail, buy = buy, price = price };
        }

        void Buy(string id)
        {
            GameServices.Instance?.Economy.TryBuyMutation(id);
            Refresh();
        }

        void Ascend()
        {
            var services = GameServices.Instance;
            if (services == null || !services.Economy.TryAscend())
                return;
            var battle = Object.FindFirstObjectByType<TapCombatController>();
            int gained = services.Economy.LastAscendGlory;
            MyClicker.Audio.AudioDirector.Ensure().PlaySfx("ascend");
            Vector3 at = battle != null
                ? new Vector3(0f, -2.2f, 0f)
                : Vector3.zero;
            MyClicker.Audio.FxDirector.Ensure().Ascend(at);
            if (gained > 0)
                battle?.Announce("Ascended — +" + gained + " Glory", 3.2f);
            battle?.RestartRun();
            Hide();
        }

        void RefreshRow(MutRow row)
        {
            var services = GameServices.Instance;
            if (services == null || row.name == null)
                return;
            int rank = services.Save.Profile.MutationLevel(row.id);
            int cost = services.Economy.MutationCost(row.id);
            float bonus = EconomyService.Mutation(rank, PerDecade(row.id));
            row.name.text = Title(row.id) + "  R" + rank;
            row.detail.text = Blurb(row.id) + "  +" + Mathf.RoundToInt(bonus * 100f) + "%";
            if (row.price != null)
                row.price.Set(cost.ToString(), GloryIcon());
            if (row.buy != null)
                row.buy.interactable = services.Save.Profile.glory >= cost;
        }

        static float PerDecade(string id)
        {
            var eco = GameServices.Instance != null && GameServices.Instance.Config != null
                ? GameServices.Instance.Config.economy
                : new GameConfig.EconomySettings();
            return id == ContentIds.MutSwift ? eco.mutationSwiftPerDecade : eco.mutationPerDecade;
        }

        static string Title(string id)
        {
            switch (id)
            {
                case ContentIds.MutMight: return "Mutate Might";
                case ContentIds.MutFortune: return "Mutate Fortune";
                case ContentIds.MutSwift: return "Mutate Swift";
                case ContentIds.MutLuck: return "Mutate Harvest";
                default: return id;
            }
        }

        static string Blurb(string id)
        {
            switch (id)
            {
                case ContentIds.MutMight: return "Tap and auto damage";
                case ContentIds.MutFortune: return "Gold from kills";
                case ContentIds.MutSwift: return "Faster auto-swings";
                case ContentIds.MutLuck: return "Dust, potions, relics";
                default: return "";
            }
        }

        static Sprite GloryIcon()
        {
            var icons = GameServices.Instance != null ? GameServices.Instance.Catalog.icons : null;
            return icons != null ? icons.glory : null;
        }

        struct MutRow
        {
            public string id;
            public Text name;
            public Text detail;
            public Button buy;
            public StoneUi.PriceView price;
        }
    }
}
