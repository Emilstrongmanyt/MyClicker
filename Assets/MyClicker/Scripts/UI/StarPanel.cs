using MyClicker.App;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class StarPanel : MonoBehaviour
    {
        const float Chart = 6400f;

        GameObject _root;
        bool _open;
        Text _summary;
        Text _detail;
        Button _buy;
        Button _respec;
        RectTransform _content;
        NodeView[] _nodes;
        EdgeView[] _edges;
        StarChartView _chart;
        string _selected;
        int _paintKey = int.MinValue;

        public bool Open => _open;

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            StarTree.Ensure();
            var panel = StoneUi.Panel(parent, "StarPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.02f, 0.10f, 0.98f, 0.90f);

            var title = StoneUi.Label(panel.transform, "Title", "Stars", 36, TextAnchor.MiddleLeft);
            StoneUi.Place(title, 0.04f, 0.90f, 0.28f, 0.99f);
            var minus = StoneUi.Button(panel.transform, "ZoomOut", "−", skin, () => _chart?.ZoomBy(0.82f));
            StoneUi.Place(minus, 0.30f, 0.90f, 0.42f, 0.99f);
            var plus = StoneUi.Button(panel.transform, "ZoomIn", "+", skin, () => _chart?.ZoomBy(1.22f));
            StoneUi.Place(plus, 0.44f, 0.90f, 0.56f, 0.99f);
            var home = StoneUi.Button(panel.transform, "Home", "Center", skin, () => _chart?.Recenter());
            StoneUi.Place(home, 0.58f, 0.90f, 0.76f, 0.99f);
            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.84f, 0.90f, 0.96f, 0.99f);

            _summary = StoneUi.Label(panel.transform, "Summary", "", 18, TextAnchor.MiddleLeft);
            StoneUi.Place(_summary, 0.04f, 0.84f, 0.62f, 0.90f);
            _respec = StoneUi.Button(panel.transform, "Respec", "Respec", skin, Respec);
            StoneUi.Place(_respec, 0.64f, 0.84f, 0.96f, 0.90f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(panel.transform, false);
            StoneUi.Place(viewportGo.GetComponent<RectTransform>(), 0.03f, 0.22f, 0.97f, 0.83f);
            var vpImage = viewportGo.GetComponent<Image>();
            vpImage.color = new Color(0.04f, 0.03f, 0.05f, 0.72f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = true;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            _content = contentGo.GetComponent<RectTransform>();
            _content.anchorMin = new Vector2(0.5f, 0.5f);
            _content.anchorMax = new Vector2(0.5f, 0.5f);
            _content.pivot = new Vector2(0.5f, 0.5f);
            _content.sizeDelta = new Vector2(Chart, Chart);
            _content.anchoredPosition = Vector2.zero;

            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = _content;
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Unrestricted;
            scroll.scrollSensitivity = 80f;
            scroll.inertia = true;

            _chart = viewportGo.AddComponent<StarChartView>();
            _chart.content = _content;
            _chart.scroll = scroll;
            _chart.minZoom = 0.28f;
            _chart.maxZoom = 1.25f;
            _chart.Recenter();

            var catalog = GameServices.Instance != null ? GameServices.Instance.Catalog : null;
            var all = StarTree.All;
            _edges = BuildEdges(_content, all);
            _nodes = new NodeView[all.Length];
            for (int i = 0; i < all.Length; i++)
                _nodes[i] = MakeNode(_content, skin, all[i], catalog);
            AddRegionLabels(_content);

            _detail = StoneUi.Label(panel.transform, "Detail", "Tap a star.", 18, TextAnchor.UpperLeft);
            StoneUi.Place(_detail, 0.04f, 0.04f, 0.68f, 0.21f);
            _buy = StoneUi.Button(panel.transform, "Buy", "Buy", skin, BuySelected);
            StoneUi.Place(_buy, 0.70f, 0.05f, 0.96f, 0.20f);

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

            if (_chart != null)
                _chart.Recenter();
            if (string.IsNullOrEmpty(_selected))
                _selected = StarIds.FirstLight;
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
            int unspent = StarTree.Unspent(profile);
            int spent = StarTree.Spent(profile);
            if (_summary != null)
                _summary.text = unspent + "★ to spend   ·   tap a lit star to allocate   ·   pinch or +/−";
            if (_respec != null)
            {
                var label = _respec.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = spent > 0 ? "Respec  +" + spent + "★" : "Respec";
                _respec.interactable = spent > 0;
            }

            int key = unspent + spent * 1000 + (_selected != null ? _selected.GetHashCode() : 0);
            if (key != _paintKey)
            {
                _paintKey = key;
                if (_nodes != null)
                {
                    for (int i = 0; i < _nodes.Length; i++)
                        PaintNode(_nodes[i], profile);
                }

                if (_edges != null)
                {
                    for (int i = 0; i < _edges.Length; i++)
                        PaintEdge(_edges[i], profile);
                }
            }

            PaintDetail(profile);
        }

        static void AddRegionLabels(RectTransform parent)
        {
            AddRegionLabel(parent, "TAP", 30f);
            AddRegionLabel(parent, "AUTO", 90f);
            AddRegionLabel(parent, "ENDLESS", 150f);
            AddRegionLabel(parent, "FOCUS", 210f);
            AddRegionLabel(parent, "GOLD", 270f);
            AddRegionLabel(parent, "CRAFT", 330f);
        }

        static void AddRegionLabel(RectTransform parent, string title, float ang)
        {
            float rad = ang * Mathf.Deg2Rad;
            var label = StoneUi.Label(parent, "Reg_" + title, title, 32, TextAnchor.MiddleCenter);
            var rt = label.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 56f);
            rt.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * 780f;
            label.color = new Color(1f, 0.9f, 0.62f, 0.92f);
            label.raycastTarget = false;
            label.transform.SetAsLastSibling();
        }

        static EdgeView[] BuildEdges(RectTransform parent, StarNode[] all)
        {
            var list = new System.Collections.Generic.List<EdgeView>(all.Length * 2);
            for (int i = 0; i < all.Length; i++)
            {
                var node = all[i];
                if (node.neighbors == null)
                    continue;
                for (int n = 0; n < node.neighbors.Length; n++)
                {
                    if (string.CompareOrdinal(node.id, node.neighbors[n]) >= 0)
                        continue;
                    var other = StarTree.Find(node.neighbors[n]);
                    if (other == null)
                        continue;
                    list.Add(new EdgeView { a = node.id, b = other.id, image = MakeEdge(parent, node, other) });
                }
            }

            return list.ToArray();
        }

        static Image MakeEdge(RectTransform parent, StarNode from, StarNode to)
        {
            var go = new GameObject("Edge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = StoneUi.SolidSprite();
            image.color = new Color(1f, 0.82f, 0.40f, 0.22f);
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Vector2 a = new Vector2(from.x, from.y);
            Vector2 b = new Vector2(to.x, to.y);
            Vector2 delta = b - a;
            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(Mathf.Max(8f, delta.magnitude), from.kind == StarKind.Minor ? 5f : 7f);
            rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            go.transform.SetAsFirstSibling();
            return image;
        }

        NodeView MakeNode(RectTransform parent, GameConfig.UiSkin skin, StarNode def, ContentCatalog catalog)
        {
            float size = def.kind == StarKind.Keystone ? 108f : (def.kind == StarKind.Notable ? 76f : 56f);
            var go = new GameObject("Star_" + def.id, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var frame = go.GetComponent<Image>();
            frame.sprite = FrameFor(def.kind, catalog, skin);
            frame.type = frame.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            frame.preserveAspect = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(def.x, def.y);
            var button = go.GetComponent<Button>();
            string id = def.id;
            button.onClick.AddListener(() => Select(id));

            var icon = StoneUi.Icon(go.transform, "Icon", IconFor(def, catalog));
            StoneUi.Place(icon, 0.18f, 0.18f, 0.82f, 0.82f);
            icon.raycastTarget = false;
            var ring = StoneUi.Icon(go.transform, "Ring", StoneUi.SolidSprite());
            StoneUi.Place(ring, -0.08f, -0.08f, 1.08f, 1.08f);
            ring.color = new Color(1f, 0.92f, 0.45f, 0f);
            ring.raycastTarget = false;
            ring.transform.SetAsFirstSibling();
            return new NodeView { node = def, button = button, frame = frame, icon = icon, ring = ring };
        }

        static Sprite IconFor(StarNode def, ContentCatalog catalog)
        {
            if (catalog == null)
                return null;
            var sprite = catalog.FindStarIcon(def.iconId);
            if (sprite != null)
                return sprite;
            var icons = catalog.icons;
            if (icons == null)
                return null;
            if (def.statTag == StarTags.Tap || def.iconId == "picto_sword")
                return icons.might;
            if (def.statTag == StarTags.Auto || def.iconId == "picto_time")
                return icons.swift;
            if (def.statTag == StarTags.Gold || def.iconId == "picto_gold")
                return icons.gold;
            if (def.statTag == StarTags.Regen || def.iconId == "picto_flask")
                return icons.potion;
            if (def.statTag == StarTags.Relic || def.iconId == "picto_hammer")
                return icons.anvil;
            if (def.kind == StarKind.Keystone)
                return icons.glory;
            return icons.might;
        }

        static Sprite FrameFor(StarKind kind, ContentCatalog catalog, GameConfig.UiSkin skin)
        {
            var icons = catalog != null ? catalog.icons : null;
            if (icons != null)
            {
                if (kind == StarKind.Keystone && icons.starFrameKeystone != null)
                    return icons.starFrameKeystone;
                if (kind == StarKind.Notable && icons.starFrameNotable != null)
                    return icons.starFrameNotable;
                if (kind == StarKind.Minor && icons.starFrameMinor != null)
                    return icons.starFrameMinor;
                if (icons.starFrameMinor != null)
                    return icons.starFrameMinor;
            }

            return skin != null ? skin.buttonNormal : null;
        }

        void PaintNode(NodeView row, PlayerProfile profile)
        {
            if (row.node == null || row.frame == null)
                return;
            bool owned = StarTree.Has(profile, row.node.id);
            bool can = StarTree.CanBuy(profile, row.node);
            bool selected = row.node.id == _selected;
            Color color;
            if (owned)
                color = new Color(1f, 0.86f, 0.38f, 1f);
            else if (can)
                color = new Color(0.95f, 0.93f, 0.85f, 1f);
            else
                color = new Color(0.38f, 0.36f, 0.34f, 0.7f);
            if (selected)
                color = new Color(1f, 0.95f, 0.7f, 1f);
            row.frame.color = color;
            if (row.icon != null)
                row.icon.color = owned || can ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            if (row.ring != null)
            {
                if (selected)
                    row.ring.color = new Color(1f, 0.9f, 0.35f, 0.55f);
                else if (can && !owned)
                    row.ring.color = new Color(0.55f, 1f, 0.62f, 0.42f);
                else
                    row.ring.color = new Color(1f, 0.9f, 0.35f, 0f);
            }
        }

        void PaintEdge(EdgeView edge, PlayerProfile profile)
        {
            if (edge.image == null)
                return;
            bool a = StarTree.Has(profile, edge.a);
            bool b = StarTree.Has(profile, edge.b);
            if (a && b)
                edge.image.color = new Color(1f, 0.84f, 0.32f, 0.85f);
            else if (a || b)
                edge.image.color = new Color(1f, 0.82f, 0.45f, 0.45f);
            else
                edge.image.color = new Color(0.55f, 0.48f, 0.35f, 0.16f);
        }

        void Select(string id)
        {
            _selected = id;
            var services = GameServices.Instance;
            var node = StarTree.Find(id);
            if (services != null && node != null && node.kind != StarKind.Keystone &&
                StarTree.CanBuy(services.Save.Profile, node))
                services.Economy.TryBuyStar(id);
            _paintKey = int.MinValue;
            Refresh();
        }

        void PaintDetail(PlayerProfile profile)
        {
            var node = StarTree.Find(_selected);
            if (node == null)
            {
                if (_detail != null)
                    _detail.text = "Path from the center. Minors are small, gold rings are notables, large rings are keystones.";
                if (_buy != null)
                    _buy.interactable = false;
                return;
            }

            bool owned = StarTree.Has(profile, node.id);
            bool can = StarTree.CanBuy(profile, node);
            string lockReason = StarTree.LockReason(profile, node);
            string kind = node.kind == StarKind.Keystone ? "Keystone" : (node.kind == StarKind.Notable ? "Notable" : "Minor");
            if (_detail != null)
            {
                string cost = node.cost <= 0 ? "Free" : node.cost + "★";
                _detail.text = node.title + "  ·  " + kind + "  ·  " + cost + "\n" + node.blurb +
                               (string.IsNullOrEmpty(lockReason) ? "" : "\n" + lockReason);
            }

            if (_buy != null)
            {
                var label = _buy.GetComponentInChildren<Text>();
                if (label != null)
                {
                    if (owned)
                        label.text = "Owned";
                    else if (can && node.kind == StarKind.Keystone)
                        label.text = "Buy  " + node.cost + "★";
                    else if (can)
                        label.text = "Allocated";
                    else
                        label.text = "Locked";
                }

                _buy.interactable = can && node.kind == StarKind.Keystone;
            }
        }

        void BuySelected()
        {
            if (string.IsNullOrEmpty(_selected))
                return;
            GameServices.Instance?.Economy.TryBuyStar(_selected);
            _paintKey = int.MinValue;
            Refresh();
        }

        void Respec()
        {
            var services = GameServices.Instance;
            if (services == null || !services.Economy.TryRespecStars())
                return;
            _paintKey = int.MinValue;
            Refresh();
        }

        struct NodeView
        {
            public StarNode node;
            public Button button;
            public Image frame;
            public Image icon;
            public Image ring;
        }

        struct EdgeView
        {
            public string a;
            public string b;
            public Image image;
        }
    }
}
