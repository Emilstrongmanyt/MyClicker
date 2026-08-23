using MyClicker.App;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class StatsPanel : MonoBehaviour
    {
        GameObject _root;
        bool _open;
        Text _body;

        public bool Open => _open;

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var panel = StoneUi.Panel(parent, "StatsPanel", skin);
            _root = panel.gameObject;
            StoneUi.Place(panel, 0.05f, 0.16f, 0.95f, 0.78f);

            var title = StoneUi.Label(panel.transform, "Title", "Hero Stats", 40, TextAnchor.MiddleCenter);
            StoneUi.Place(title, 0.08f, 0.88f, 0.78f, 0.98f);
            var close = StoneUi.Button(panel.transform, "Close", "X", skin, Hide);
            StoneUi.Place(close, 0.82f, 0.88f, 0.96f, 0.98f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(panel.transform, false);
            StoneUi.Place(viewportGo.GetComponent<RectTransform>(), 0.05f, 0.05f, 0.95f, 0.86f);
            viewportGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            _body = StoneUi.Label(viewportGo.transform, "Body", "", 22, TextAnchor.UpperLeft);
            var rt = _body.rectTransform;
            rt.anchorMin = new Vector2(0.03f, 0f);
            rt.anchorMax = new Vector2(0.97f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.verticalOverflow = VerticalWrapMode.Overflow;
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
            if (!_open || _body == null)
                return;
            var services = GameServices.Instance;
            if (services == null)
                return;
            var profile = services.Save.Profile;
            var eco = services.Economy;

            string buffs = "";
            if (profile.mightBuffLeft > 0f) buffs += "  Ember " + EconomyService.FormatBuff(profile.mightBuffLeft);
            if (profile.swiftBuffLeft > 0f) buffs += "  Gale " + EconomyService.FormatBuff(profile.swiftBuffLeft);
            if (profile.goldBuffLeft > 0f) buffs += "  Gilded " + EconomyService.FormatBuff(profile.goldBuffLeft);
            if (eco.FocusFuryLeft > 0f) buffs += "  Fury " + EconomyService.FormatBuff(eco.FocusFuryLeft);
            if (eco.SurgeLeft > 0f) buffs += "  Surge " + EconomyService.FormatBuff(eco.SurgeLeft);
            if (string.IsNullOrEmpty(buffs))
                buffs = "  none";

            int nodes = profile.gloryNodes != null ? profile.gloryNodes.Length : 0;
            _body.text =
                "Tap  " + NumberFmt.Compact(eco.TapDamage) +
                "\nAuto  " + eco.AutoInterval.ToString("0.00") + "s   DPS " + NumberFmt.Compact(eco.AutoDps) +
                "\nOverclock  x" + eco.OverclockMul.ToString("0.00") +
                "\nCrit  " + Mathf.RoundToInt(eco.CritChance * 100f) + "%  x" + eco.CritMultiplier.ToString("0.#") +
                "\nCleave  " + Mathf.RoundToInt(eco.CleaveFraction * 100f) + "%" +
                "\nGold  x" + eco.GoldMultiplier.ToString("0.00") + "   " + NumberFmt.Compact(eco.GoldPerSecond) + " g/s" +
                "\nFocus  " + Mathf.RoundToInt(eco.Focus) + " / " + Mathf.RoundToInt(eco.FocusMax) +
                "   regen " + eco.FocusRegen.ToString("0.0") + "/s" +
                "\nSlam  x" + eco.SlamMul.ToString("0.0") +
                "   Fury  x" + (1f + eco.FuryBonus).ToString("0.00") + "  " + eco.FurySeconds.ToString("0.0") + "s" +
                "   Sweep  x" + eco.SweepMul.ToString("0.0") +
                "\nRenown  +" + Mathf.RoundToInt(services.Deeds != null ? services.Deeds.Renown * 100f : 0f) + "%" +
                "\nCollection  +" + Mathf.RoundToInt(eco.CollectionBonus * 100f) +
                "%   Shards  " + eco.Shards + "/" + eco.ShardCap + "  +" + Mathf.RoundToInt(eco.ShardBonus * 100f) + "%" +
                "\nRelics  " + eco.Relics +
                "\nGlory  " + profile.glory + "   pending  " + eco.PendingGlory +
                "\nAscensions  " + profile.ascendCount + "   Endless  " + profile.cycle +
                "   best  " + profile.bestCycle +
                "\nCycle HP  x" + eco.CycleMul().ToString("0.00") +
                "   gold  x" + eco.CycleGoldMul().ToString("0.00") +
                "\nWave need  " + eco.WaveKillNeed + "   spawn cap  " + eco.SpawnCap +
                "\nWave spawn  x" + eco.SpawnIntervalMul.ToString("0.00") +
                "   walk  x" + eco.WalkSpeedMul.ToString("0.00") +
                "\nTalent nodes  " + nodes + " / " + GloryTree.All.Length +
                "\nBuffs" + buffs;
        }
    }
}
