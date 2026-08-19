using MyClicker.App;
using MyClicker.Character;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Economy
{
    public class GearService
    {
        readonly GameServices _services;
        HeroCharacterAdapter _hero;

        public GearService(GameServices services)
        {
            _services = services;
        }

        public void Bind(HeroCharacterAdapter hero)
        {
            _hero = hero;
        }

        PlayerProfile Profile => _services.Save.Profile;
        GameConfig.EconomySettings Eco =>
            _services.Config != null ? _services.Config.economy : new GameConfig.EconomySettings();

        public float TapBonus
        {
            get
            {
                float value = Look(Slot.Weapon) * 1.15f + Profile.temperWeapon * 3f;
                value += Look(Slot.Armor) * 0.35f + Profile.temperArmor * 1.2f;
                return value;
            }
        }

        public float GoldBonus
        {
            get { return Look(Slot.Armor) * 0.018f + Profile.temperArmor * 0.02f; }
        }

        public float CritBonus
        {
            get { return Look(Slot.Helmet) * 0.008f + Profile.temperHelmet * 0.006f; }
        }

        public float SwiftBonus
        {
            get { return Look(Slot.Cape) * 0.015f + Profile.temperCape * 0.018f; }
        }

        public string Label(string slot)
        {
            return _hero != null ? _hero.SlotLabel(slot) : slot;
        }

        public string BonusText(string slot)
        {
            int look = Look(slot);
            int temper = Profile.TemperLevel(slot);
            switch (slot)
            {
                case Slot.Weapon:
                    return "+" + Mathf.RoundToInt(look * 1.15f + temper * 3f) + " tap";
                case Slot.Armor:
                    return "+" + Mathf.RoundToInt(look * 0.35f + temper * 1.2f) + " tap   +" +
                           Mathf.RoundToInt((look * 0.018f + temper * 0.02f) * 100f) + "% gold";
                case Slot.Helmet:
                    return "+" + Mathf.RoundToInt((look * 0.008f + temper * 0.006f) * 100f) + "% crit";
                case Slot.Cape:
                    return "+" + Mathf.RoundToInt((look * 0.015f + temper * 0.018f) * 100f) + "% swift";
                default:
                    return "";
            }
        }

        public int TemperCost(string slot)
        {
            int rank = Profile.TemperLevel(slot);
            return Mathf.Max(1, Mathf.RoundToInt(Eco.temperBaseCost * Mathf.Pow(Eco.temperCostGrowth, rank)));
        }

        public bool TryTemper(string slot)
        {
            if (!_services.Save.TrySpendDust(TemperCost(slot)))
                return false;
            Profile.SetTemperLevel(slot, Profile.TemperLevel(slot) + 1);
            _services.Save.MarkDirty();
            return true;
        }

        public void Cycle(string slot, int delta)
        {
            if (_hero == null)
                return;
            _hero.Cycle(slot, delta);
            var profile = Profile;
            profile.heroJson = _hero.ToJson();
            _services.Save.MarkDirty();
        }

        public int CraftCost(string potionId)
        {
            switch (potionId)
            {
                case ContentIds.PotMight: return Eco.craftMightCost;
                case ContentIds.PotSwift: return Eco.craftSwiftCost;
                case ContentIds.PotGold: return Eco.craftGoldCost;
                default: return 12;
            }
        }

        public bool TryCraft(string potionId)
        {
            if (!_services.Save.TrySpendDust(CraftCost(potionId)))
                return false;
            _services.Economy.GrantPotion(potionId);
            return true;
        }

        int Look(string slot)
        {
            string id = _hero != null ? _hero.SlotId(slot) : slot;
            int seed = Stable(id);
            return 3 + seed % 10;
        }

        static int Stable(string value)
        {
            unchecked
            {
                int hash = 5381;
                if (string.IsNullOrEmpty(value))
                    return hash;
                for (int i = 0; i < value.Length; i++)
                    hash = ((hash << 5) + hash) ^ value[i];
                return hash & 0x7fffffff;
            }
        }

        public static class Slot
        {
            public const string Weapon = "Weapon";
            public const string Armor = "Armor";
            public const string Helmet = "Helmet";
            public const string Cape = "Cape";
        }
    }
}
