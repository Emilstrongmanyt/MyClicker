using MyClicker.App;
using MyClicker.Data;
using MyClicker.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace MyClicker.UI
{
    public class PotionTray : MonoBehaviour
    {
        readonly PotionSlot[] _slots = new PotionSlot[3];
        StoneUi.ChipView _fury;
        StoneUi.ChipView _surge;
        StoneUi.ChipView _cry;
        StoneUi.TooltipView _tip;

        static readonly string[] Order =
        {
            ContentIds.PotMight,
            ContentIds.PotSwift,
            ContentIds.PotGold
        };

        public void Build(Transform parent, GameConfig.UiSkin skin)
        {
            var tray = StoneUi.Panel(parent, "PotionTray", skin);
            StoneUi.Place(tray, 0.02f, 0.755f, 0.58f, 0.868f);
            StoneUi.Bare(tray);

            for (int i = 0; i < Order.Length; i++)
                _slots[i] = BuildSlot(tray.transform, skin, Order[i], i);

            _fury = PlaceChip(tray.transform, skin, "FuryChip", 3, false);
            _surge = PlaceChip(tray.transform, skin, "SurgeChip", 4, false);
            _cry = PlaceChip(tray.transform, skin, "CryChip", 5, false);
            if (_fury.root != null) _fury.root.SetActive(false);
            if (_surge.root != null) _surge.root.SetActive(false);
            if (_cry.root != null) _cry.root.SetActive(false);

            _tip = StoneUi.Tooltip(parent, "PotionTip", skin);
            StoneUi.Place(_tip.root.GetComponent<RectTransform>(), 0.08f, 0.22f, 0.92f, 0.38f);
        }

        public void BindTooltip(StoneUi.TooltipView tip)
        {
            if (tip == null)
                return;
            _tip = tip;
        }

        public void Refresh()
        {
            for (int i = 0; i < _slots.Length; i++)
                RefreshSlot(_slots[i]);
            RefreshBuffs();
        }

        PotionSlot BuildSlot(Transform parent, GameConfig.UiSkin skin, string id, int index)
        {
            var chip = PlaceChip(parent, skin, id, index, true);
            string captured = id;
            if (chip.button != null)
                HoldPress.Bind(chip.button.gameObject, () => Use(captured), () => ShowTip(captured), () => _tip?.Hide());
            return new PotionSlot { id = id, chip = chip };
        }

        static StoneUi.ChipView PlaceChip(Transform parent, GameConfig.UiSkin skin, string name, int index, bool clickable)
        {
            var chip = StoneUi.Chip(parent, name, skin, clickable);
            float x0 = 0.01f + index * 0.165f;
            StoneUi.Place(chip.root.GetComponent<RectTransform>(), x0, 0.04f, x0 + 0.155f, 0.96f);
            return chip;
        }

        void Use(string id)
        {
            if (GameServices.Instance == null || !GameServices.Instance.Economy.TryUsePotion(id))
                return;
            var hero = Object.FindFirstObjectByType<MyClicker.Character.HeroCharacterAdapter>();
            Vector3 at = hero != null ? hero.transform.position + Vector3.up * 0.35f : Vector3.zero;
            MyClicker.Audio.FxDirector.Ensure().Potion(id, at);
            Refresh();
        }

        void ShowTip(string id)
        {
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindPotion(id) : null;
            string name = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : id;
            string body = def != null ? def.description : "";
            float left = GameServices.Instance != null ? GameServices.Instance.Economy.PotionBuffLeft(id) : 0f;
            if (left > 0f)
                body = (body ?? "") + "\nActive  " + EconomyService.FormatBuff(left) + " remaining.";
            _tip?.Show(name, body);
        }

        void RefreshSlot(PotionSlot slot)
        {
            if (slot.chip == null)
                return;
            var services = GameServices.Instance;
            if (services == null)
                return;
            var profile = services.Save.Profile;
            int n = profile.PotionCount(slot.id);
            float left = services.Economy.PotionBuffLeft(slot.id);
            float duration = DurationFor(slot.id);
            slot.chip.Set(IconFor(slot.id), left, duration, n, n > 0 || left > 0f);
            if (slot.chip.time != null)
                slot.chip.time.color = ColorFor(slot.id);
        }

        void RefreshBuffs()
        {
            var economy = GameServices.Instance != null ? GameServices.Instance.Economy : null;
            var icons = GameServices.Instance != null ? GameServices.Instance.Catalog.icons : null;
            if (economy == null)
                return;

            float fury = economy.FocusFuryLeft;
            SetChip(_fury, icons != null ? icons.crit : null, fury, economy.FurySeconds, fury > 0f, new Color(1f, 0.45f, 0.2f));

            float surge = economy.SurgeLeft;
            SetChip(_surge, icons != null ? icons.glory : null, surge, economy.SurgeSeconds, surge > 0f, new Color(1f, 0.86f, 0.35f));

            float cry = economy.GloryTapLeft;
            SetChip(_cry, icons != null ? icons.might : null, cry, economy.GloryTapDuration, cry > 0f, new Color(1f, 0.72f, 0.42f));
        }

        static void SetChip(StoneUi.ChipView chip, Sprite icon, float left, float duration, bool on, Color timeColor)
        {
            if (chip == null || chip.root == null)
                return;
            chip.root.SetActive(on || left > 0f);
            chip.Set(icon, left, duration, 0, on);
            if (chip.time != null)
                chip.time.color = timeColor;
        }

        static float DurationFor(string id)
        {
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindPotion(id) : null;
            return def != null && def.duration > 0f ? def.duration : 20f;
        }

        static Color ColorFor(string id)
        {
            switch (id)
            {
                case ContentIds.PotMight: return new Color(1f, 0.55f, 0.28f);
                case ContentIds.PotSwift: return new Color(0.55f, 0.82f, 1f);
                case ContentIds.PotGold: return new Color(1f, 0.86f, 0.32f);
                default: return new Color(1f, 0.86f, 0.42f);
            }
        }

        static Sprite IconFor(string id)
        {
            var def = GameServices.Instance != null ? GameServices.Instance.Catalog.FindPotion(id) : null;
            if (def != null && def.icon != null)
                return def.icon;
            var icons = GameServices.Instance != null ? GameServices.Instance.Catalog.icons : null;
            return icons != null ? icons.potion : null;
        }

        struct PotionSlot
        {
            public string id;
            public StoneUi.ChipView chip;
        }
    }
}
