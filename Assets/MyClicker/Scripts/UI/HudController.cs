using System;
using MyClicker.App;
using MyClicker.Combat;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class HudController : MonoBehaviour
    {
        Text _gold;
        Text _dust;
        Text _wave;
        Text _zone;
        Text _name;
        Text _dps;
        Text _hint;
        StoneUi.BannerView _banner;
        StoneUi.HealthBarView _bossBar;
        ShopPanel _shop;
        GearPanel _gear;
        GloryPanel _glory;
        PotionTray _potions;
        StoneUi.HealthBarView _focus;
        Button _slam;
        Button _fury;
        Button _sweep;
        StoneUi.TooltipView _tip;
        CanvasGroup _hintFade;
        float _hintAge;
        Image _goldIcon;
        Image _dustIcon;
        TapCombatController _battle;
        EnemySpawner _spawner;

        void Start()
        {
            var services = GameServices.Ensure();
            var parent = StoneUi.EnsureCanvas();
            var skin = services.Config != null ? services.Config.ui : new GameConfig.UiSkin();
            var icons = services.Catalog.icons;

            var left = StoneUi.Panel(parent, "TopLeft", skin);
            StoneUi.Place(left, 0.02f, 0.855f, 0.31f, 0.985f);
            SoftPanel(left, 0.72f);
            _name = StoneUi.Label(left.transform, "Name", "", 24, TextAnchor.MiddleLeft);
            StoneUi.Place(_name, 0.07f, 0.62f, 0.94f, 0.96f);
            _zone = StoneUi.Label(left.transform, "Zone", "", 18, TextAnchor.MiddleLeft);
            StoneUi.Place(_zone, 0.07f, 0.32f, 0.94f, 0.64f);
            _wave = StoneUi.Label(left.transform, "Wave", "", 18, TextAnchor.MiddleLeft);
            StoneUi.Place(_wave, 0.07f, 0.04f, 0.94f, 0.34f);

            var right = StoneUi.Panel(parent, "TopRight", skin);
            StoneUi.Place(right, 0.69f, 0.855f, 0.98f, 0.985f);
            SoftPanel(right, 0.72f);
            _goldIcon = StoneUi.Icon(right.transform, "GoldIcon", icons != null ? icons.gold : skin.coinIcon);
            StoneUi.Place(_goldIcon, 0.04f, 0.52f, 0.24f, 0.92f);
            _gold = StoneUi.Label(right.transform, "Gold", "", 26, TextAnchor.MiddleRight);
            StoneUi.Place(_gold, 0.24f, 0.50f, 0.96f, 0.94f);
            _dustIcon = StoneUi.Icon(right.transform, "DustIcon", icons != null ? icons.dust : null);
            StoneUi.Place(_dustIcon, 0.04f, 0.08f, 0.24f, 0.48f);
            _dust = StoneUi.Label(right.transform, "Dust", "", 22, TextAnchor.MiddleRight);
            StoneUi.Place(_dust, 0.24f, 0.06f, 0.96f, 0.50f);

            _dps = StoneUi.Label(parent, "Dps", "", 18, TextAnchor.MiddleRight);
            StoneUi.Place(_dps, 0.50f, 0.808f, 0.98f, 0.850f);

            _bossBar = StoneUi.HealthBar(parent, "BossBar", skin);
            StoneUi.Place(_bossBar.root.GetComponent<RectTransform>(), 0.64f, 0.718f, 0.98f, 0.802f);
            SoftPanel(_bossBar.root.GetComponent<Image>(), 0.58f);
            _bossBar.SetVisible(false);

            _focus = StoneUi.HealthBar(parent, "FocusBar", skin);
            StoneUi.Place(_focus.root.GetComponent<RectTransform>(), 0.04f, 0.155f, 0.48f, 0.212f);
            _slam = StoneUi.Button(parent, "Slam", "Slam", skin, null);
            StoneUi.Place(_slam, 0.50f, 0.155f, 0.65f, 0.212f);
            _fury = StoneUi.Button(parent, "FocusFury", "Fury", skin, null);
            StoneUi.Place(_fury, 0.66f, 0.155f, 0.81f, 0.212f);
            _sweep = StoneUi.Button(parent, "Sweep", "Sweep", skin, null);
            StoneUi.Place(_sweep, 0.82f, 0.155f, 0.97f, 0.212f);

            var armoryBtn = StoneUi.Button(parent, "ArmoryButton", "Armory", skin, () =>
            {
                _shop.Hide();
                _glory.Hide();
                _gear.Toggle();
            });
            StoneUi.Place(armoryBtn, 0.42f, 0.018f, 0.68f, 0.108f);
            var armoryIcon = StoneUi.Icon(armoryBtn.transform, "Icon", icons != null ? icons.anvil : null);
            StoneUi.Place(armoryIcon, 0.06f, 0.18f, 0.24f, 0.82f);

            var shopBtn = StoneUi.Button(parent, "ShopButton", "Forge", skin, () =>
            {
                _gear.Hide();
                _glory.Hide();
                _shop.Toggle();
            });
            StoneUi.Place(shopBtn, 0.70f, 0.018f, 0.96f, 0.108f);
            var shopIcon = StoneUi.Icon(shopBtn.transform, "Icon", icons != null ? icons.shop : null);
            StoneUi.Place(shopIcon, 0.06f, 0.18f, 0.24f, 0.82f);

            _hint = StoneUi.Label(parent, "Hint", "Tap anywhere to strike", 24, TextAnchor.LowerCenter);
            StoneUi.Place(_hint, 0.08f, 0.112f, 0.92f, 0.155f);
            _hintFade = _hint.gameObject.AddComponent<CanvasGroup>();
            _hintFade.blocksRaycasts = false;
            if (services.Save.Profile.seenTapHint)
                _hintFade.alpha = 0f;
            _banner = StoneUi.Banner(parent, "Toast", skin);
            StoneUi.Place(_banner.root.GetComponent<RectTransform>(), 0.07f, 0.41f, 0.93f, 0.59f);

            _shop = gameObject.AddComponent<ShopPanel>();
            _shop.Build(parent, skin);
            _gear = gameObject.AddComponent<GearPanel>();
            _gear.Build(parent, skin);
            _glory = gameObject.AddComponent<GloryPanel>();
            _glory.Build(parent, skin);
            _shop.RequestGlory = () =>
            {
                _gear.Hide();
                _shop.Hide();
                _glory.Toggle();
            };
            _potions = gameObject.AddComponent<PotionTray>();
            _potions.Build(parent, skin);

            _tip = StoneUi.Tooltip(parent, "HoldTip", skin);
            StoneUi.Place(_tip.root.GetComponent<RectTransform>(), 0.08f, 0.22f, 0.92f, 0.38f);
            _potions.BindTooltip(_tip);
            BindFocusTips();

            _battle = FindFirstObjectByType<TapCombatController>();
            _spawner = FindFirstObjectByType<EnemySpawner>();
            services.ProfileChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (GameServices.Instance != null)
                GameServices.Instance.ProfileChanged -= Refresh;
        }

        void Update()
        {
            TickHint();
            Refresh();
        }

        void TickHint()
        {
            if (_hintFade == null || GameServices.Instance == null)
                return;
            if (GameServices.Instance.Save.Profile.seenTapHint)
            {
                _hintFade.alpha = 0f;
                return;
            }

            _hintAge += Time.unscaledDeltaTime;
            if (_hintAge < 30f)
            {
                _hintFade.alpha = 1f;
                return;
            }

            _hintFade.alpha = Mathf.MoveTowards(_hintFade.alpha, 0f, Time.unscaledDeltaTime / 1.2f);
            if (_hintFade.alpha > 0.01f)
                return;
            _hintFade.alpha = 0f;
            GameServices.Instance.Save.Profile.seenTapHint = true;
            GameServices.Instance.Save.MarkDirty();
        }

        void Refresh()
        {
            if (GameServices.Instance == null)
                return;
            var services = GameServices.Instance;
            var profile = services.Save.Profile;
            var zone = services.Catalog.ZoneAt(profile.zone);
            var economy = services.Economy;

            if (_name != null)
                _name.text = profile.displayName;
            if (_zone != null)
                _zone.text = zone.displayName;
            if (_wave != null)
                _wave.text = WaveText(profile, services);
            if (_gold != null)
                _gold.text = NumberFmt.Compact(profile.gold);
            if (_dust != null)
                _dust.text = NumberFmt.Compact(profile.dust);
            if (_dps != null)
            {
                _dps.text = "Tap " + Mathf.RoundToInt(economy.TapDamage) + "  Auto " +
                            Mathf.RoundToInt(economy.AutoDps) + "/s  " +
                            NumberFmt.Compact(Mathf.Max(0, Mathf.RoundToInt(economy.GoldPerSecond))) + "g/s" +
                            BuffLine(profile);
            }

            if (_banner != null)
            {
                string toast = _battle != null ? _battle.ToastMessage : null;
                string drop = services.Gear != null && services.Gear.LastDropLife > 0f
                    ? services.Gear.LastDrop
                    : null;
                _banner.Show(toast ?? drop);
            }

            var boss = _spawner != null ? _spawner.CurrentBoss : null;
            bool showBoss = boss != null && boss.Alive;
            if (_bossBar != null)
            {
                _bossBar.SetVisible(showBoss);
                if (showBoss)
                    _bossBar.Set(boss.DisplayName, boss.Hp, boss.MaxHp);
            }

            var combat = services.Config != null ? services.Config.combat : new GameConfig.CombatSettings();
            if (_focus != null)
                _focus.Set("Focus", economy.Focus, combat.focusMax > 0f ? combat.focusMax : 100f);

            if (_slam != null)
                _slam.interactable = economy.Focus >= combat.slamCost;
            if (_fury != null)
                _fury.interactable = economy.Focus >= combat.furyCost;
            if (_sweep != null)
                _sweep.interactable = economy.Focus >= combat.sweepCost;

            _shop?.Refresh();
            _gear?.Refresh();
            _glory?.Refresh();
            _potions?.Refresh();
        }

        static string BuffLine(PlayerProfile profile)
        {
            string line = "";
            AppendBuff(ref line, "Ember", profile.mightBuffLeft);
            AppendBuff(ref line, "Gale", profile.swiftBuffLeft);
            AppendBuff(ref line, "Gilded", profile.goldBuffLeft);
            var fury = GameServices.Instance != null ? GameServices.Instance.Economy.FocusFuryLeft : 0f;
            AppendBuff(ref line, "Fury", fury);
            return line;
        }

        void BindFocusTips()
        {
            var combat = GameServices.Instance != null && GameServices.Instance.Config != null
                ? GameServices.Instance.Config.combat
                : new GameConfig.CombatSettings();
            Action hide = () => _tip?.Hide();
            HoldPress.Bind(_slam.gameObject, () => _battle?.TrySlam(), () => ShowFocusTip(
                "Slam",
                "Spend " + Mathf.RoundToInt(combat.slamCost) + " Focus to smash the nearest foe for heavy tap damage."), hide);
            HoldPress.Bind(_fury.gameObject, () => _battle?.TryFocusFury(), () => ShowFocusTip(
                "Fury",
                "Spend " + Mathf.RoundToInt(combat.furyCost) + " Focus to boost tap and auto damage for a few seconds."), hide);
            HoldPress.Bind(_sweep.gameObject, () => _battle?.TrySweep(), () => ShowFocusTip(
                "Sweep",
                "Spend " + Mathf.RoundToInt(combat.sweepCost) + " Focus to hit every invader on the field. Does not harm bosses."), hide);
        }

        void ShowFocusTip(string title, string body)
        {
            _tip?.Show(title, body);
        }

        static string WaveText(PlayerProfile profile, GameServices services)
        {
            int per = 10;
            if (services.Config != null)
                per = Mathf.Max(1, services.Config.combat.wavesPerBoss);
            if (profile.wave > 0 && profile.wave % per == 0)
                return "BOSS";
            return "Wave " + profile.wave;
        }

        static void AppendBuff(ref string line, string name, float left)
        {
            if (left <= 0f)
                return;
            line += "   " + name + " " + EconomyService.FormatBuff(left);
        }

        static void SoftPanel(Image panel, float alpha)
        {
            if (panel == null)
                return;
            var color = panel.color;
            color.a = alpha;
            panel.color = color;
            panel.raycastTarget = false;
        }
    }
}
