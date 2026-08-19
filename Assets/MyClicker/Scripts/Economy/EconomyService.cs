using System;
using MyClicker.App;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Economy
{
    public class EconomyService
    {
        readonly GameServices _services;

        public EconomyService(GameServices services)
        {
            _services = services;
        }

        PlayerProfile Profile => _services.Save.Profile;
        GameConfig.EconomySettings Eco =>
            _services.Config != null ? _services.Config.economy : new GameConfig.EconomySettings();
        GameConfig.CombatSettings Combat =>
            _services.Config != null ? _services.Config.combat : new GameConfig.CombatSettings();

        public float TapDamage
        {
            get
            {
                float value = Combat.tapDamage + Profile.mightLevel * Eco.mightPerLevel;
                if (Profile.mightBuffLeft > 0f)
                    value *= 1f + Eco.mightPotionBonus;
                return value;
            }
        }

        public float GoldMultiplier
        {
            get
            {
                float value = 1f + Profile.fortuneLevel * Eco.fortunePerLevel;
                if (Profile.goldBuffLeft > 0f)
                    value *= 1f + Eco.goldPotionBonus;
                var zone = _services.Catalog.ZoneAt(Profile.zone);
                return value * Mathf.Max(0.25f, zone.goldMul);
            }
        }

        public float AutoInterval
        {
            get
            {
                float interval = Eco.autoIntervalStart * Mathf.Pow(Eco.autoIntervalDecay, Profile.swiftLevel);
                if (Profile.swiftBuffLeft > 0f)
                    interval *= 1f - Eco.swiftPotionBonus;
                return Mathf.Max(Eco.autoIntervalMin, interval);
            }
        }

        public float CritChance => Mathf.Min(Eco.critChanceCap, Profile.critLevel * Eco.critPerLevel);

        public float CritMultiplier => Eco.critMultiplier;

        public float AutoDps => TapDamage / Mathf.Max(0.2f, AutoInterval);

        public long UpgradeCost(string id)
        {
            var def = _services.Catalog.FindUpgrade(id);
            int level = Profile.UpgradeLevel(id);
            int baseCost = def != null ? def.baseCost : 15;
            float growth = def != null ? def.costGrowth : 1.18f;
            return Math.Max(1, (long)Math.Round(baseCost * Math.Pow(growth, level)));
        }

        public bool CanBuy(string id) => Profile.gold >= UpgradeCost(id) && !IsMaxed(id);

        public bool IsMaxed(string id)
        {
            var def = _services.Catalog.FindUpgrade(id);
            int cap = def != null ? def.maxLevel : 200;
            return Profile.UpgradeLevel(id) >= cap;
        }

        public bool TryBuy(string id)
        {
            if (IsMaxed(id))
                return false;
            if (!_services.Save.TrySpendGold(UpgradeCost(id)))
                return false;
            Profile.SetUpgradeLevel(id, Profile.UpgradeLevel(id) + 1);
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
            return true;
        }

        public long GoldForKill(int wave, bool boss)
        {
            var eco = Eco;
            double raw = boss
                ? eco.goldPerBoss + wave * eco.goldPerBossPerWave
                : eco.goldPerKill + (wave - 1) * eco.goldPerKillPerWave;
            return Math.Max(1, (long)Math.Round(raw * GoldMultiplier));
        }

        public int DustForKill(bool boss)
        {
            if (boss)
                return Eco.dustPerBoss;
            return UnityEngine.Random.value < Eco.dustDropChance ? 1 : 0;
        }

        public string RollPotionDrop(bool boss)
        {
            float chance = boss ? Eco.potionBossDropChance : Eco.potionDropChance;
            if (UnityEngine.Random.value > chance)
                return null;
            float roll = UnityEngine.Random.value;
            if (roll < 0.4f) return ContentIds.PotMight;
            if (roll < 0.75f) return ContentIds.PotSwift;
            return ContentIds.PotGold;
        }

        public void GrantPotion(string id, int count = 1)
        {
            if (string.IsNullOrEmpty(id) || count <= 0)
                return;
            Profile.SetPotionCount(id, Profile.PotionCount(id) + count);
            _services.Save.MarkDirty();
        }

        public bool TryUsePotion(string id)
        {
            if (Profile.PotionCount(id) <= 0)
                return false;
            var def = _services.Catalog.FindPotion(id);
            float duration = def != null ? def.duration : 20f;
            switch (id)
            {
                case ContentIds.PotMight: Profile.mightBuffLeft = Mathf.Max(Profile.mightBuffLeft, duration); break;
                case ContentIds.PotSwift: Profile.swiftBuffLeft = Mathf.Max(Profile.swiftBuffLeft, duration); break;
                case ContentIds.PotGold: Profile.goldBuffLeft = Mathf.Max(Profile.goldBuffLeft, duration); break;
                default: return false;
            }

            Profile.SetPotionCount(id, Profile.PotionCount(id) - 1);
            _services.Save.MarkDirty();
            return true;
        }

        public void TickBuffs(float dt)
        {
            bool changed = false;
            changed |= Tick(ref Profile.mightBuffLeft, dt);
            changed |= Tick(ref Profile.swiftBuffLeft, dt);
            changed |= Tick(ref Profile.goldBuffLeft, dt);
            if (changed)
                _services.NotifyProfile();
        }

        public long AwardKill(int wave, bool boss)
        {
            long gold = GoldForKill(wave, boss);
            _services.Save.AddGold(gold);
            int dust = DustForKill(boss);
            if (dust > 0)
                _services.Save.AddDust(dust);
            string potion = RollPotionDrop(boss);
            if (!string.IsNullOrEmpty(potion))
                GrantPotion(potion);
            Profile.kills++;
            if (boss)
                Profile.bossesSlain++;
            _services.Save.MarkDirty();
            return gold;
        }

        public long EstimateOfflineGold(long seconds)
        {
            var eco = Eco;
            long cap = Math.Max(60, (long)eco.offlineCapHours * 3600L);
            long usable = Math.Min(Math.Max(0, seconds), cap);
            if (usable < 15)
                return 0;
            float hp = Combat.enemyBaseHp + Combat.enemyHpPerWave * Mathf.Max(0, Profile.wave - 1);
            float kills = AutoDps * usable / Mathf.Max(8f, hp);
            return Math.Max(0, (long)Math.Floor(kills * GoldForKill(Mathf.Max(1, Profile.wave), false) * eco.offlineGoldFactor));
        }

        static bool Tick(ref float value, float dt)
        {
            if (value <= 0f)
                return false;
            value = Mathf.Max(0f, value - dt);
            return value <= 0f;
        }
    }
}
