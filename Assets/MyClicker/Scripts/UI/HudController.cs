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
        Text _taps;
        Text _dps;
        Text _gps;
        Text _hint;
        StoneUi.BannerView _banner;
        Text _sting;
        StoneUi.HealthBarView _bossBar;
        ShopPanel _shop;
        GearPanel _gear;
        GloryPanel _glory;
        StarPanel _stars;
        DeedPanel _deeds;
        PotionTray _potions;
        StatsPanel _stats;
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

            _name = StoneUi.OutlineLabel(parent, "Name", "", 26, TextAnchor.MiddleLeft);
            StoneUi.Place(_name, 0.03f, 0.952f, 0.42f, 0.990f);
            _zone = StoneUi.OutlineLabel(parent, "Zone", "", 20, TextAnchor.MiddleLeft);
            StoneUi.Place(_zone, 0.03f, 0.916f, 0.42f, 0.954f);
            _wave = StoneUi.OutlineLabel(parent, "Wave", "", 20, TextAnchor.MiddleLeft);
            StoneUi.Place(_wave, 0.03f, 0.880f, 0.42f, 0.918f);

            _goldIcon = StoneUi.Icon(parent, "GoldIcon", icons != null ? icons.gold : skin.coinIcon);
            StoneUi.Place(_goldIcon, 0.80f, 0.952f, 0.855f, 0.990f);
            _gold = StoneUi.OutlineLabel(parent, "Gold", "", 26, TextAnchor.MiddleRight);
            StoneUi.Place(_gold, 0.855f, 0.952f, 0.97f, 0.990f);
            _dustIcon = StoneUi.Icon(parent, "DustIcon", icons != null ? icons.dust : null);
            StoneUi.Place(_dustIcon, 0.80f, 0.910f, 0.855f, 0.950f);
            _dust = StoneUi.OutlineLabel(parent, "Dust", "", 22, TextAnchor.MiddleRight);
            StoneUi.Place(_dust, 0.855f, 0.910f, 0.97f, 0.950f);

            _taps = StoneUi.OutlineLabel(parent, "Taps", "", 20, TextAnchor.MiddleRight);
            StoneUi.Place(_taps, 0.72f, 0.868f, 0.97f, 0.908f);
            _dps = StoneUi.OutlineLabel(parent, "Dps", "", 20, TextAnchor.MiddleRight);
            StoneUi.Place(_dps, 0.72f, 0.826f, 0.97f, 0.866f);
            _gps = StoneUi.OutlineLabel(parent, "Gps", "", 20, TextAnchor.MiddleRight);
            StoneUi.Place(_gps, 0.72f, 0.784f, 0.97f, 0.824f);

            _bossBar = StoneUi.HealthBar(parent, "BossBar", skin);
            StoneUi.Place(_bossBar.root.GetComponent<RectTransform>(), 0.58f, 0.688f, 0.97f, 0.772f);
            StoneUi.Bare(_bossBar.root.GetComponent<Image>());
            _bossBar.SetVisible(false);

            _focus = StoneUi.HealthBar(parent, "FocusBar", skin);
            StoneUi.Place(_focus.root.GetComponent<RectTransform>(), 0.04f, 0.155f, 0.48f, 0.212f);
            StoneUi.Bare(_focus.root.GetComponent<Image>());
            OutlineBar(_bossBar);
            OutlineBar(_focus);
            _slam = StoneUi.Button(parent, "Slam", "Slam", skin, null);
            StoneUi.Place(_slam, 0.50f, 0.155f, 0.65f, 0.212f);
            _fury = StoneUi.Button(parent, "FocusFury", "Fury", skin, null);
            StoneUi.Place(_fury, 0.66f, 0.155f, 0.81f, 0.212f);
            _sweep = StoneUi.Button(parent, "Sweep", "Sweep", skin, null);
            StoneUi.Place(_sweep, 0.82f, 0.155f, 0.97f, 0.212f);

            var armoryBtn = StoneUi.Button(parent, "ArmoryButton", "Armory", skin, () =>
            {
                HideMeta();
                _gear.Toggle();
            });
            StoneUi.Place(armoryBtn, 0.42f, 0.018f, 0.68f, 0.108f);
            var armoryIcon = StoneUi.Icon(armoryBtn.transform, "Icon", icons != null ? icons.anvil : null);
            StoneUi.Place(armoryIcon, 0.06f, 0.18f, 0.24f, 0.82f);

            var shopBtn = StoneUi.Button(parent, "ShopButton", "Forge", skin, () =>
            {
                HideMeta();
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
            _sting = StoneUi.Label(parent, "Sting", "", 52, TextAnchor.MiddleCenter);
            StoneUi.Place(_sting, 0.08f, 0.44f, 0.92f, 0.58f);
            _sting.fontStyle = FontStyle.Bold;
            _sting.resizeTextMinSize = 28;
            _sting.resizeTextMaxSize = 56;
            var stingOutline = _sting.gameObject.AddComponent<Outline>();
            stingOutline.effectColor = new Color(0.04f, 0.02f, 0.01f, 1f);
            stingOutline.effectDistance = new Vector2(2.8f, -2.8f);
            var stingShadow = _sting.gameObject.AddComponent<Shadow>();
            stingShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            stingShadow.effectDistance = new Vector2(0f, -3f);
            _sting.gameObject.SetActive(false);

            _shop = gameObject.AddComponent<ShopPanel>();
            _shop.Build(parent, skin);
            _gear = gameObject.AddComponent<GearPanel>();
            _gear.Build(parent, skin);
            _glory = gameObject.AddComponent<GloryPanel>();
            _glory.Build(parent, skin);
            _stars = gameObject.AddComponent<StarPanel>();
            _stars.Build(parent, skin);
            _deeds = gameObject.AddComponent<DeedPanel>();
            _deeds.Build(parent, skin);
            _stats = gameObject.AddComponent<StatsPanel>();
            _stats.Build(parent, skin);
            _shop.RequestGlory = () =>
            {
                HideMeta();
                _glory.Toggle();
            };
            _shop.RequestStars = () =>
            {
                if (_stars != null && _stars.Open)
                {
                    _stars.Hide();
                    return;
                }

                HideMeta();
                _stars.Show();
            };
            _glory.RequestDeeds = () =>
            {
                HideMeta();
                _deeds.Toggle();
            };
            _glory.RequestStars = () =>
            {
                HideMeta();
                _stars.Toggle();
            };
            _gear.RequestStats = () =>
            {
                HideMeta();
                _stats.Toggle();
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
            {
                string name = zone.displayName;
                if (profile.cycle > 0)
                    name += "  Endless " + profile.cycle;
                else if (profile.endlessUnlocked)
                    name += "  Cycle 0";
                _zone.text = name;
            }
            if (_wave != null)
                _wave.text = WaveText(profile, services);
            if (_gold != null)
                _gold.text = NumberFmt.Compact(profile.gold);
            if (_dust != null)
                _dust.text = NumberFmt.Compact(profile.dust);
            if (_taps != null)
                _taps.text = (_battle != null ? _battle.TapsPerSecond : 0f).ToString("0.0") + " taps/s";
            if (_dps != null)
                _dps.text = NumberFmt.Compact(_battle != null ? _battle.DamagePerSecond : 0f) + " dmg/s";
            if (_gps != null)
                _gps.text = NumberFmt.Compact(Math.Max(0d, economy.GoldPerSecond)) + " g/s";

            string toast = _battle != null ? _battle.ToastMessage : null;
            string drop = services.Gear != null && services.Gear.LastDropLife > 0f
                ? services.Gear.LastDrop
                : null;
            bool plain = toast != null && _battle != null && !_battle.ToastPlaque;
            if (_sting != null)
            {
                _sting.gameObject.SetActive(plain);
                _sting.text = plain ? toast : "";
            }
            if (_banner != null)
                _banner.Show(plain ? null : (toast ?? drop));

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
                _focus.Set("Focus", economy.Focus, economy.FocusMax);

            if (_slam != null)
                _slam.interactable = economy.Focus >= economy.SlamCost * economy.FocusCostMul;
            if (_fury != null)
                _fury.interactable = economy.Focus >= combat.furyCost * economy.FocusCostMul;
            if (_sweep != null)
            {
                _sweep.interactable = economy.Focus >= combat.sweepCost * economy.FocusCostMul;
                var label = _sweep.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = economy.ReaperSweep ? "Reaper" : "Sweep";
            }

            _shop?.Refresh();
            _gear?.Refresh();
            _glory?.Refresh();
            _stars?.Refresh();
            _deeds?.Refresh();
            _stats?.Refresh();
            _potions?.Refresh();
        }

        void HideMeta()
        {
            _shop.Hide();
            _gear.Hide();
            _glory.Hide();
            _stars?.Hide();
            _deeds.Hide();
            _stats.Hide();
        }

        void BindFocusTips()
        {
            var combat = GameServices.Instance != null && GameServices.Instance.Config != null
                ? GameServices.Instance.Config.combat
                : new GameConfig.CombatSettings();
            Action hide = () => _tip?.Hide();
            HoldPress.Bind(_slam.gameObject, () => _battle?.TrySlam(), () =>
            {
                float cost = GameServices.Instance != null && GameServices.Instance.Economy != null
                    ? GameServices.Instance.Economy.SlamCost
                    : combat.slamCost;
                ShowFocusTip(
                    "Slam",
                    "Spend " + Mathf.RoundToInt(cost) + " Focus to smash the nearest foe for heavy tap damage.");
            }, hide);
            HoldPress.Bind(_fury.gameObject, () => _battle?.TryFocusFury(), () => ShowFocusTip(
                "Fury",
                "Spend " + Mathf.RoundToInt(combat.furyCost) + " Focus to boost tap and auto damage for a few seconds."), hide);
            HoldPress.Bind(_sweep.gameObject, () => _battle?.TrySweep(), () =>
            {
                bool reaper = GameServices.Instance != null && GameServices.Instance.Economy != null
                    && GameServices.Instance.Economy.ReaperSweep;
                ShowFocusTip(
                    reaper ? "Reaper Sweep" : "Sweep",
                    reaper
                        ? "Spend " + Mathf.RoundToInt(combat.sweepCost) + " Focus to hit the nearest foe, including bosses, for heavy damage."
                        : "Spend " + Mathf.RoundToInt(combat.sweepCost) + " Focus to hit every invader on the field. Does not harm bosses. Spec Reaper Sweep in Glory to change this.");
            }, hide);
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
            string wave = profile.wave > 0 && profile.wave % per == 0
                ? "BOSS"
                : "Wave " + profile.wave;
            if (profile.cycle > 0 || profile.endlessUnlocked)
            {
                int depth = services.Economy != null ? services.Economy.DepthWave : profile.wave;
                wave += "  ·  " + depth;
            }

            return wave;
        }

        static void OutlineBar(StoneUi.HealthBarView bar)
        {
            if (bar == null)
                return;
            OutlineHud(bar.title);
            OutlineHud(bar.value);
        }

        static void OutlineHud(Text label)
        {
            if (label == null || label.GetComponent<Outline>() != null)
                return;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.02f, 0.01f, 0.95f);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
        }
    }
}
