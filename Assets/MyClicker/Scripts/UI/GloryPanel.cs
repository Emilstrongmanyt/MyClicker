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
        MutRow[] _muts;
        NodeRow[] _nodes;
        CryRow[] _cries;
        public System.Action RequestDeeds;

        static readonly string[] MutOrder =
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
            StoneUi.Place(title, 0.08f, 0.88f, 0.52f, 0.98f);
            var deeds = StoneUi.Button(panel.transform, "DeedsBtn", "Deeds", skin, () => RequestDeeds?.Invoke());
            StoneUi.Place(deeds, 0.54f, 0.88f, 0.78f, 0.98f);
            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.82f, 0.88f, 0.96f, 0.98f);

            _summary = StoneUi.Label(panel.transform, "Summary", "", 20, TextAnchor.UpperLeft);
            StoneUi.Place(_summary, 0.06f, 0.76f, 0.94f, 0.87f);

            _ascend = StoneUi.Button(panel.transform, "Ascend", "Ascend", skin, Ascend);
            StoneUi.Place(_ascend, 0.08f, 0.64f, 0.92f, 0.74f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(panel.transform, false);
            StoneUi.Place(viewportGo.GetComponent<RectTransform>(), 0.04f, 0.04f, 0.96f, 0.62f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
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

            const float headerH = 36f;
            const float rowH = 112f;
            int muts = MutOrder.Length;
            int nodes = GloryTree.All.Length;
            int cries = 2;
            content.sizeDelta = new Vector2(0f, headerH * 3f + (muts + nodes + cries) * rowH + 16f);

            float y = -6f;
            AddHeader(content, "Mutations", y, headerH);
            y -= headerH;
            _muts = new MutRow[muts];
            for (int i = 0; i < muts; i++)
            {
                _muts[i] = BuildMutRow(content, skin, MutOrder[i], y, rowH);
                y -= rowH;
            }

            AddHeader(content, "Glory tree", y, headerH);
            y -= headerH;
            _nodes = new NodeRow[nodes];
            for (int i = 0; i < nodes; i++)
            {
                _nodes[i] = BuildNodeRow(content, skin, GloryTree.All[i], y, rowH);
                y -= rowH;
            }

            AddHeader(content, "War cries", y, headerH);
            y -= headerH;
            _cries = new CryRow[cries];
            _cries[0] = BuildCryRow(content, skin, "war_cry", "War Cry", "Spend Glory for x2 tap for 15s.", y, rowH, false);
            y -= rowH;
            _cries[1] = BuildCryRow(content, skin, "godstrike", "Godstrike", "Spend Glory for x3.5 tap for 12s.", y, rowH, true);

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
                int renown = services.Deeds != null ? Mathf.RoundToInt(services.Deeds.Renown * 100f) : 0;
                int owned = profile.gloryNodes != null ? profile.gloryNodes.Length : 0;
                int col = Mathf.RoundToInt(economy.CollectionBonus * 100f);
                int shard = Mathf.RoundToInt(economy.ShardBonus * 100f);
                string endless = economy.EndlessOpen
                    ? (profile.cycle > 0 ? "    Endless  " + profile.cycle : "    Endless open")
                    : "";
                _summary.text = "Glory  " + profile.glory + "    Ascensions  " + profile.ascendCount +
                                "    Renown  +" + renown + "%" +
                                "    Nodes  " + owned + "/" + GloryTree.All.Length + endless +
                                "\n" + pendingLine +
                                " Relics " + economy.Relics + "  +" + col +
                                "%    Shards " + economy.Shards + "/" + economy.ShardCap +
                                "  +" + shard + "%. Mutations, nodes, relics, Deeds, and shards stay.";
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

            if (_muts != null)
            {
                for (int i = 0; i < _muts.Length; i++)
                    RefreshMut(_muts[i]);
            }

            if (_nodes != null)
            {
                for (int i = 0; i < _nodes.Length; i++)
                    RefreshNode(_nodes[i]);
            }

            if (_cries != null)
            {
                for (int i = 0; i < _cries.Length; i++)
                    RefreshCry(_cries[i]);
            }
        }

        static void AddHeader(RectTransform parent, string title, float y, float height)
        {
            var label = StoneUi.Label(parent, "Head_" + title, title, 22, TextAnchor.MiddleLeft);
            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.04f, 1f);
            rt.anchorMax = new Vector2(0.96f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, height);
        }

        MutRow BuildMutRow(RectTransform parent, GameConfig.UiSkin skin, string id, float y, float rowH)
        {
            var row = PlaceRow(parent, "Mut_" + id, skin, y, rowH);
            var name = StoneUi.Label(row.transform, "Name", MutTitle(id), 24, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.04f, 0.52f, 0.64f, 0.92f);
            var detail = StoneUi.Label(row.transform, "Detail", "", 18, TextAnchor.UpperLeft);
            StoneUi.Place(detail, 0.04f, 0.08f, 0.64f, 0.54f);
            var buy = StoneUi.Button(row.transform, "Buy", "", skin, () => BuyMut(id));
            StoneUi.Place(buy, 0.66f, 0.14f, 0.96f, 0.86f);
            StoneUi.HideDefaultLabel(buy);
            var price = StoneUi.Price(buy.transform, "Price", 24);
            StoneUi.Place(price.root, 0.04f, 0.10f, 0.96f, 0.90f);
            return new MutRow { id = id, name = name, detail = detail, buy = buy, price = price };
        }

        NodeRow BuildNodeRow(RectTransform parent, GameConfig.UiSkin skin, GloryNode node, float y, float rowH)
        {
            var row = PlaceRow(parent, "Node_" + node.id, skin, y, rowH);
            var name = StoneUi.Label(row.transform, "Name", node.title, 24, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.04f, 0.52f, 0.64f, 0.92f);
            var detail = StoneUi.Label(row.transform, "Detail", node.blurb, 18, TextAnchor.UpperLeft);
            StoneUi.Place(detail, 0.04f, 0.08f, 0.64f, 0.54f);
            var buy = StoneUi.Button(row.transform, "Buy", "", skin, () => BuyNode(node.id));
            StoneUi.Place(buy, 0.66f, 0.14f, 0.96f, 0.86f);
            StoneUi.HideDefaultLabel(buy);
            var price = StoneUi.Price(buy.transform, "Price", 22);
            StoneUi.Place(price.root, 0.04f, 0.10f, 0.96f, 0.90f);
            return new NodeRow { node = node, name = name, detail = detail, buy = buy, price = price, panel = row };
        }

        static Image PlaceRow(RectTransform parent, string name, GameConfig.UiSkin skin, float y, float rowH)
        {
            var row = StoneUi.Panel(parent, name, skin);
            var rt = row.rectTransform;
            rt.anchorMin = new Vector2(0.02f, 1f);
            rt.anchorMax = new Vector2(0.98f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, rowH - 10f);
            return row;
        }

        void BuyMut(string id)
        {
            GameServices.Instance?.Economy.TryBuyMutation(id);
            Refresh();
        }

        void BuyNode(string id)
        {
            GameServices.Instance?.Economy.TryBuyGloryNode(id);
            Refresh();
        }

        CryRow BuildCryRow(RectTransform parent, GameConfig.UiSkin skin, string id, string title, string blurb, float y, float rowH, bool godstrike)
        {
            var row = PlaceRow(parent, "Cry_" + id, skin, y, rowH);
            var name = StoneUi.Label(row.transform, "Name", title, 24, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.04f, 0.52f, 0.64f, 0.92f);
            var detail = StoneUi.Label(row.transform, "Detail", blurb, 18, TextAnchor.UpperLeft);
            StoneUi.Place(detail, 0.04f, 0.08f, 0.64f, 0.54f);
            var buy = StoneUi.Button(row.transform, "Buy", "", skin, () => BuyCry(godstrike));
            StoneUi.Place(buy, 0.66f, 0.14f, 0.96f, 0.86f);
            StoneUi.HideDefaultLabel(buy);
            var price = StoneUi.Price(buy.transform, "Price", 22);
            StoneUi.Place(price.root, 0.04f, 0.10f, 0.96f, 0.90f);
            return new CryRow { godstrike = godstrike, name = name, detail = detail, buy = buy, price = price };
        }

        void BuyCry(bool godstrike)
        {
            var economy = GameServices.Instance != null ? GameServices.Instance.Economy : null;
            if (economy == null)
                return;
            if (godstrike)
                economy.TryBuyGodstrike();
            else
                economy.TryBuyWarCry();
            Refresh();
        }

        void RefreshCry(CryRow row)
        {
            var services = GameServices.Instance;
            if (services == null || row.name == null)
                return;
            var profile = services.Save.Profile;
            var economy = services.Economy;
            int cost = row.godstrike ? EconomyService.GodstrikeCost : EconomyService.WarCryCost;
            bool locked = row.godstrike && profile.ascendCount < 1;
            float left = economy.GloryTapLeft;
            bool mine = left > 0f && (row.godstrike
                ? profile.gloryTapMul >= 3f
                : profile.gloryTapMul > 1f && profile.gloryTapMul < 3f);
            if (row.detail != null)
            {
                if (mine)
                    row.detail.text = "Active  " + EconomyService.FormatBuff(left);
                else
                    row.detail.text = row.godstrike
                        ? "Spend Glory for x3.5 tap for 12s."
                        : "Spend Glory for x2 tap for 15s.";
            }

            if (row.price != null)
            {
                if (locked)
                    row.price.Set("Ascend first", null);
                else
                    row.price.Set(cost.ToString(), GloryIcon());
            }

            if (row.buy != null)
                row.buy.interactable = !locked && profile.glory >= cost;
        }

        void Ascend()
        {
            var services = GameServices.Instance;
            if (services == null || !services.Economy.TryAscend())
                return;
            var battle = Object.FindFirstObjectByType<TapCombatController>();
            int gained = services.Economy.LastAscendGlory;
            Vector3 at = battle != null
                ? new Vector3(0f, -2.2f, 0f)
                : Vector3.zero;
            MyClicker.Audio.FxDirector.Ensure().Ascend(at);
            if (gained > 0)
                battle?.Announce("Ascended — +" + gained + " Glory", 3.2f);
            battle?.RestartRun();
            Hide();
        }

        void RefreshMut(MutRow row)
        {
            var services = GameServices.Instance;
            if (services == null || row.name == null)
                return;
            int rank = services.Save.Profile.MutationLevel(row.id);
            int cost = services.Economy.MutationCost(row.id);
            float bonus = EconomyService.Mutation(rank, PerDecade(row.id));
            row.name.text = MutTitle(row.id) + "  R" + rank;
            row.detail.text = MutBlurb(row.id) + "  +" + Mathf.RoundToInt(bonus * 100f) + "%";
            if (row.price != null)
                row.price.Set(cost.ToString(), GloryIcon());
            if (row.buy != null)
                row.buy.interactable = services.Save.Profile.glory >= cost;
        }

        void RefreshNode(NodeRow row)
        {
            var services = GameServices.Instance;
            if (services == null || row.node == null || row.name == null)
                return;
            var profile = services.Save.Profile;
            bool owned = GloryTree.Has(profile, row.node.id);
            bool can = GloryTree.CanBuy(profile, row.node);
            string lockReason = GloryTree.LockReason(profile, row.node);
            row.name.text = (owned ? "✓  " : "") + row.node.title;
            row.detail.text = row.node.blurb;
            if (row.panel != null)
                row.panel.color = owned
                    ? new Color(1f, 0.92f, 0.7f, 0.95f)
                    : Color.white;
            if (row.price != null)
            {
                if (owned)
                    row.price.Set("Owned", null);
                else if (!can)
                    row.price.Set(string.IsNullOrEmpty(lockReason) ? "Locked" : lockReason, null);
                else if (row.node.cost <= 0)
                    row.price.Set("Free", null);
                else
                    row.price.Set(row.node.cost.ToString(), GloryIcon());
            }

            if (row.buy != null)
                row.buy.interactable = can;
        }

        static float PerDecade(string id)
        {
            var eco = GameServices.Instance != null && GameServices.Instance.Config != null
                ? GameServices.Instance.Config.economy
                : new GameConfig.EconomySettings();
            return id == ContentIds.MutSwift ? eco.mutationSwiftPerDecade : eco.mutationPerDecade;
        }

        static string MutTitle(string id)
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

        static string MutBlurb(string id)
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

        struct NodeRow
        {
            public GloryNode node;
            public Text name;
            public Text detail;
            public Button buy;
            public StoneUi.PriceView price;
            public Image panel;
        }

        struct CryRow
        {
            public bool godstrike;
            public Text name;
            public Text detail;
            public Button buy;
            public StoneUi.PriceView price;
        }
    }
}
