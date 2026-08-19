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
        Text _name;
        Text _dps;
        Text _hint;
        Text _offline;
        StoneUi.HealthBarView _bossBar;
        ShopPanel _shop;
        PotionTray _potions;
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

            var top = StoneUi.Panel(parent, "TopBar", skin);
            StoneUi.Place(top, 0.03f, 0.905f, 0.97f, 0.985f);

            _name = StoneUi.Label(top.transform, "Name", "", 28, TextAnchor.MiddleLeft);
            StoneUi.Place(_name, 0.03f, 0.12f, 0.38f, 0.88f);
            _wave = StoneUi.Label(top.transform, "Wave", "", 26, TextAnchor.MiddleCenter);
            StoneUi.Place(_wave, 0.36f, 0.12f, 0.70f, 0.88f);

            _goldIcon = StoneUi.Icon(top.transform, "GoldIcon", icons != null ? icons.gold : skin.coinIcon);
            StoneUi.Place(_goldIcon, 0.70f, 0.18f, 0.78f, 0.82f);
            _gold = StoneUi.Label(top.transform, "Gold", "", 28, TextAnchor.MiddleRight);
            StoneUi.Place(_gold, 0.77f, 0.12f, 0.97f, 0.88f);

            var sub = StoneUi.Panel(parent, "SubBar", skin);
            StoneUi.Place(sub, 0.03f, 0.845f, 0.97f, 0.90f);
            _dustIcon = StoneUi.Icon(sub.transform, "DustIcon", icons != null ? icons.dust : null);
            StoneUi.Place(_dustIcon, 0.03f, 0.15f, 0.11f, 0.85f);
            _dust = StoneUi.Label(sub.transform, "Dust", "", 24, TextAnchor.MiddleLeft);
            StoneUi.Place(_dust, 0.12f, 0.1f, 0.42f, 0.9f);
            _dps = StoneUi.Label(sub.transform, "Dps", "", 24, TextAnchor.MiddleRight);
            StoneUi.Place(_dps, 0.44f, 0.1f, 0.97f, 0.9f);

            _bossBar = StoneUi.HealthBar(parent, "BossBar", skin);
            StoneUi.Place(_bossBar.root.GetComponent<RectTransform>(), 0.08f, 0.745f, 0.92f, 0.835f);
            _bossBar.SetVisible(false);

            _shop = gameObject.AddComponent<ShopPanel>();
            _shop.Build(parent, skin);
            _potions = gameObject.AddComponent<PotionTray>();
            _potions.Build(parent, skin);

            var shopBtn = StoneUi.Button(parent, "ShopButton", "Forge", skin, () => _shop.Toggle());
            StoneUi.Place(shopBtn, 0.55f, 0.145f, 0.96f, 0.23f);
            var shopIcon = StoneUi.Icon(shopBtn.transform, "Icon", icons != null ? icons.shop : null);
            StoneUi.Place(shopIcon, 0.06f, 0.18f, 0.22f, 0.82f);

            _hint = StoneUi.Label(parent, "Hint", "Tap anywhere to strike", 24, TextAnchor.LowerCenter);
            StoneUi.Place(_hint, 0.08f, 0.085f, 0.92f, 0.13f);
            _offline = StoneUi.Label(parent, "Offline", "", 26, TextAnchor.MiddleCenter);
            StoneUi.Place(_offline, 0.08f, 0.30f, 0.92f, 0.38f);

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

        void Update() => Refresh();

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
            if (_wave != null)
                _wave.text = zone.displayName + "  " + profile.wave;
            if (_gold != null)
                _gold.text = NumberFmt.Compact(profile.gold);
            if (_dust != null)
                _dust.text = NumberFmt.Compact(profile.dust) + " dust";
            if (_dps != null)
            {
                string buff = profile.mightBuffLeft > 0f || profile.swiftBuffLeft > 0f || profile.goldBuffLeft > 0f
                    ? "  BUFF"
                    : "";
                _dps.text = "Tap " + Mathf.RoundToInt(economy.TapDamage) + "   Auto " +
                            Mathf.RoundToInt(economy.AutoDps) + "/s" + buff;
            }

            if (_offline != null)
            {
                string msg = _battle != null ? _battle.OfflineMessage : null;
                _offline.text = msg ?? "";
            }

            var boss = _spawner != null ? _spawner.CurrentBoss : null;
            bool showBoss = boss != null && boss.Alive;
            if (_bossBar != null)
            {
                _bossBar.SetVisible(showBoss);
                if (showBoss)
                    _bossBar.Set(boss.DisplayName, boss.Hp, boss.MaxHp);
            }

            _shop?.Refresh();
            _potions?.Refresh();
        }
    }
}
