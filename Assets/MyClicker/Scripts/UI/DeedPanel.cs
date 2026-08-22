using MyClicker.App;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class DeedPanel : MonoBehaviour
    {
        GameObject _root;
        bool _open;
        Text _summary;
        DeedRow[] _rows;

        public bool Open => _open;

        public void Build(Transform parent, Data.GameConfig.UiSkin skin)
        {
            var panel = StoneUi.Panel(parent, "DeedPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.05f, 0.16f, 0.95f, 0.78f);

            var title = StoneUi.Label(panel.transform, "Title", "Deeds", 40, TextAnchor.MiddleCenter);
            StoneUi.Place(title, 0.08f, 0.88f, 0.78f, 0.98f);
            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.82f, 0.88f, 0.96f, 0.98f);

            _summary = StoneUi.Label(panel.transform, "Summary", "", 20, TextAnchor.UpperLeft);
            StoneUi.Place(_summary, 0.06f, 0.78f, 0.94f, 0.87f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(panel.transform, false);
            StoneUi.Place(viewportGo.GetComponent<RectTransform>(), 0.04f, 0.04f, 0.96f, 0.77f);
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

            var list = DeedService.All;
            _rows = new DeedRow[list.Length];
            float rowH = 92f;
            content.sizeDelta = new Vector2(0f, list.Length * rowH + 10f);
            for (int i = 0; i < list.Length; i++)
                _rows[i] = BuildRow(content, skin, list[i], i, rowH);

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
            var deeds = GameServices.Instance != null ? GameServices.Instance.Deeds : null;
            if (deeds == null)
                return;
            if (_summary != null)
            {
                _summary.text = "Renown  +" + Mathf.RoundToInt(deeds.Renown * 100f) +
                                "% tap and gold    " + deeds.Count + " / " + DeedService.All.Length +
                                "\nDeeds persist through ascend.";
            }

            for (int i = 0; i < _rows.Length; i++)
                RefreshRow(_rows[i], deeds);
        }

        static DeedRow BuildRow(RectTransform parent, Data.GameConfig.UiSkin skin, DeedDef def, int index, float rowH)
        {
            var row = StoneUi.Panel(parent, "Deed_" + def.id, skin);
            var rt = row.rectTransform;
            rt.anchorMin = new Vector2(0.02f, 1f);
            rt.anchorMax = new Vector2(0.98f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -8f - index * rowH);
            rt.sizeDelta = new Vector2(0f, rowH - 10f);
            var name = StoneUi.Label(row.transform, "Name", def.title, 24, TextAnchor.MiddleLeft);
            StoneUi.Place(name, 0.04f, 0.52f, 0.96f, 0.94f);
            var detail = StoneUi.Label(row.transform, "Detail", def.hint, 18, TextAnchor.UpperLeft);
            StoneUi.Place(detail, 0.04f, 0.08f, 0.96f, 0.54f);
            return new DeedRow { def = def, name = name, detail = detail, panel = row };
        }

        static void RefreshRow(DeedRow row, DeedService deeds)
        {
            if (row.name == null || row.def == null)
                return;
            bool done = deeds.Has(row.def.id);
            int have = Mathf.Min(deeds.Current(row.def), row.def.need);
            row.name.text = (done ? "✓  " : "") + row.def.title;
            row.detail.text = row.def.hint + "  " + have + " / " + row.def.need;
            if (row.panel != null)
                row.panel.color = done
                    ? new Color(1f, 0.92f, 0.7f, 0.95f)
                    : Color.white;
        }

        struct DeedRow
        {
            public DeedDef def;
            public Text name;
            public Text detail;
            public Image panel;
        }
    }
}
