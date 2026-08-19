using System.Collections.Generic;
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

        public string LastDrop;
        public float LastDropLife;

        public void Bind(HeroCharacterAdapter hero)
        {
            _hero = hero;
            if (_hero == null)
                return;
            _hero.Pool = HeroCharacterAdapter.GearPool.Owned;
            _hero.IsOwned = id => Profile.HasGear(id);
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

        public int OwnedCount(string slot)
        {
            return _hero != null ? _hero.SlotCount(slot) : 0;
        }

        public void Cycle(string slot, int delta)
        {
            if (_hero == null || _hero.SlotCount(slot) <= 1)
                return;
            _hero.Cycle(slot, delta);
            Profile.heroJson = _hero.ToJson();
            _services.Save.MarkDirty();
        }

        public string TryRollDrop(bool boss)
        {
            if (_hero == null)
                return null;
            float chance = boss ? Eco.gearBossDropChance : Eco.gearDropChance;
            chance += _services.Save.Profile.harvestLevel * Eco.harvestGearPerLevel;
            chance *= 1f + EconomyService.Mutation(_services.Save.Profile.mutationLuck, Eco.mutationPerDecade);
            if (UnityEngine.Random.value > chance)
                return null;

            var slots = new List<string>(HeroCharacterAdapter.GearSlots);
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                string tmp = slots[i];
                slots[i] = slots[j];
                slots[j] = tmp;
            }

            foreach (var slot in slots)
            {
                var pool = _hero.LootIds(slot);
                var fresh = new List<string>();
                for (int i = 0; i < pool.Count; i++)
                {
                    if (!Profile.HasGear(pool[i]))
                        fresh.Add(pool[i]);
                }

                if (fresh.Count == 0)
                    continue;

                string id = fresh[UnityEngine.Random.Range(0, fresh.Count)];
                if (!Profile.UnlockGear(id))
                    continue;

                bool firstRelic = _hero.WearingStarter(slot);
                if (firstRelic)
                    _hero.EquipById(slot, id);
                Profile.heroJson = _hero.ToJson();
                _services.Save.MarkDirty();
                string label = PrettyId(id);
                LastDrop = (firstRelic ? "Equipped " : "Found ") + label;
                LastDropLife = 4.4f;
                return LastDrop;
            }

            return null;
        }

        public void TickDropToast(float dt)
        {
            if (LastDropLife > 0f)
                LastDropLife -= dt;
        }

        static string PrettyId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "Relic";
            int dot = id.LastIndexOf('.');
            string name = dot >= 0 && dot + 1 < id.Length ? id.Substring(dot + 1) : id;
            return name.Replace(" [Paint]", "").Replace('_', ' ');
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
            int tier = CollectionTier(id);
            return tier + seed % 5;
        }

        static int CollectionTier(string id)
        {
            string hay = (id ?? "").ToLowerInvariant();
            if (hay.Contains(".basic.") || hay.Contains("oldcape") || hay.Contains("grandmacape") || hay.Contains("cottton"))
                return 2;
            if (hay.Contains("bonus"))
                return 7;
            if (hay.Contains("knight") || hay.Contains("viking"))
                return 10;
            if (hay.Contains("samurai") || hay.Contains("sandlord") || hay.Contains("swamplord") || hay.Contains("throne"))
                return 13;
            return 8;
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
