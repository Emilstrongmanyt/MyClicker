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
                float value = 0f;
                if (RelicOn(Slot.Weapon))
                    value += 14f;
                if (RelicOn(Slot.Armor))
                    value += 5f;
                value += Profile.temperWeapon * 3f + Profile.temperArmor * 1.2f;
                return value;
            }
        }

        public float GoldBonus
        {
            get { return (RelicOn(Slot.Armor) ? 0.08f : 0f) + Profile.temperArmor * 0.02f; }
        }

        public float CritMulBonus
        {
            get { return (RelicOn(Slot.Helmet) ? 0.4f : 0f) + Profile.temperHelmet * 0.06f; }
        }

        public float FocusRegenBonus
        {
            get { return (RelicOn(Slot.Helmet) ? 0.15f : 0f) + Profile.temperHelmet * 0.03f; }
        }

        public float OverclockBonus
        {
            get { return (RelicOn(Slot.Cape) ? 0.15f : 0f) + Profile.temperCape * 0.03f; }
        }

        public string Label(string slot)
        {
            return _hero != null ? _hero.SlotLabel(slot) : slot;
        }

        public string BonusText(string slot)
        {
            bool relic = RelicOn(slot);
            int t = Profile.TemperLevel(slot);
            switch (slot)
            {
                case Slot.Weapon:
                {
                    float tap = (relic ? 14f : 0f) + t * 3f;
                    if (!relic && t <= 0)
                        return "Starter look. Relics drop in battle.";
                    return "+" + tap.ToString("0") + " tap" + (relic ? ". Any relic." : ". Starter.");
                }
                case Slot.Armor:
                {
                    float tap = (relic ? 5f : 0f) + t * 1.2f;
                    float gold = (relic ? 8f : 0f) + t * 2f;
                    if (!relic && t <= 0)
                        return "Starter look. Relics drop in battle.";
                    return "+" + tap.ToString("0.#") + " tap  +" + gold.ToString("0") + "% gold" +
                           (relic ? ". Any relic." : ". Starter.");
                }
                case Slot.Helmet:
                {
                    float mul = (relic ? 0.4f : 0f) + t * 0.06f;
                    float regen = (relic ? 15f : 0f) + t * 3f;
                    if (!relic && t <= 0)
                        return "Starter look. Relics drop in battle.";
                    return "+" + mul.ToString("0.00") + " crit mul  +" + regen.ToString("0") + "% Focus regen" +
                           (relic ? ". Any relic." : ". Starter.");
                }
                case Slot.Cape:
                {
                    float auto = (relic ? 15f : 0f) + t * 3f;
                    if (!relic && t <= 0)
                        return "Starter look. Relics drop in battle.";
                    return "+" + auto.ToString("0") + "% auto damage" + (relic ? ". Any relic." : ". Starter.");
                }
                default:
                    return "";
            }
        }

        public int TemperCost(string slot)
        {
            int rank = Profile.TemperLevel(slot);
            float cost = Eco.temperBaseCost * Mathf.Pow(Eco.temperCostGrowth, rank);
            if (_services.Economy != null && _services.Economy.HasStar(StarIds.Smith))
                cost *= 0.85f;
            return Mathf.Max(1, Mathf.RoundToInt(cost));
        }

        public bool TryTemper(string slot)
        {
            if (!_services.Save.TrySpendDust(TemperCost(slot)))
                return false;
            Profile.SetTemperLevel(slot, Profile.TemperLevel(slot) + 1);
            Profile.usedTemper = true;
            if (_services.Economy != null)
                Profile.tapDamage = _services.Economy.TapDamage;
            _services.Save.MarkDirty();
            MyClicker.Audio.AudioDirector.Ensure().PlaySfx("armory");
            return true;
        }

        public int OwnedCount(string slot)
        {
            return _hero != null ? _hero.SlotCount(slot) : 0;
        }

        public bool CanCycle(string slot)
        {
            if (_hero == null)
                return false;
            int n = _hero.SlotCount(slot);
            if (n > 1)
                return true;
            if (n <= 0)
                return false;
            return _hero.SlotIndex(slot) < 0 || _hero.WearingStarter(slot);
        }

        public void Cycle(string slot, int delta)
        {
            if (_hero == null || OwnedCount(slot) <= 0)
                return;
            _hero.Cycle(slot, delta);
            Profile.heroJson = _hero.ToJson();
            _services.Save.MarkDirty();
            MyClicker.Audio.AudioDirector.Ensure().PlaySfx("equip");
        }

        public string TryRollDrop(bool boss, bool force = false)
        {
            if (_hero == null)
                return null;
            if (!force)
            {
                float chance = boss ? Eco.gearBossDropChance : Eco.gearDropChance;
                chance += _services.Save.Profile.harvestLevel * Eco.harvestGearPerLevel;
                chance *= 1f + EconomyService.Mutation(_services.Save.Profile.mutationLuck, Eco.mutationPerDecade);
                if (_services.Economy != null && _services.Economy.HasGlory(GloryIds.RelicSense))
                    chance *= 1.25f;
                if (_services.Economy != null && _services.Economy.HasStar(StarIds.RelicMagnet))
                    chance *= 1.10f;
                if (_services.Economy != null)
                    chance *= 1f + 0.025f * StarTree.MinorRank(Profile, StarTags.Relic);
                if (UnityEngine.Random.value > chance)
                    return null;
            }

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

                _services.Save.MarkDirty();
                _services.Save.PersistNow();
                if (!Profile.seenArmoryHint)
                {
                    Profile.seenArmoryHint = true;
                    LastDrop = "You found " + PrettyId(id) + "\nEquip it in Armory";
                }
                else
                    LastDrop = "You found " + PrettyId(id);
                string collection = _services.Economy != null
                    ? _services.Economy.CollectionUnlockLine(Profile.unlockedGear != null ? Profile.unlockedGear.Length : 0)
                    : null;
                if (!string.IsNullOrEmpty(collection))
                    LastDrop += "\n" + collection;
                LastDropLife = 4.8f;
                MyClicker.Audio.AudioDirector.Ensure().PlaySfx("relic");
                if (_hero != null)
                    MyClicker.Audio.FxDirector.Ensure().Relic(_hero.transform.position + Vector3.up * 0.6f);
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

        bool RelicOn(string slot)
        {
            if (_hero == null || OwnedCount(slot) <= 0)
                return false;
            return !_hero.WearingStarter(slot);
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
