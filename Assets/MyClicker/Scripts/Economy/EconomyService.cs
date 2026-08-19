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
                if (_services.Gear != null)
                    value += _services.Gear.TapBonus;
                if (Profile.mightBuffLeft > 0f)
                    value *= 1f + Eco.mightPotionBonus;
                if (_focusFuryLeft > 0f)
                    value *= 1f + Eco.focusFuryBonus;
                value *= 1f + Mutation(Profile.mutationMight, Eco.mutationPerDecade);
                return value;
            }
        }

        public float GoldMultiplier
        {
            get
            {
                float value = 1f + Profile.fortuneLevel * Eco.fortunePerLevel;
                if (_services.Gear != null)
                    value += _services.Gear.GoldBonus;
                if (Profile.goldBuffLeft > 0f)
                    value *= 1f + Eco.goldPotionBonus;
                value *= 1f + Mutation(Profile.mutationFortune, Eco.mutationPerDecade);
                var zone = _services.Catalog.ZoneAt(Profile.zone);
                return value * Mathf.Max(0.25f, zone.goldMul);
            }
        }

        public float AutoInterval
        {
            get
            {
                float interval = Eco.autoIntervalStart * Mathf.Pow(Eco.autoIntervalDecay, Profile.swiftLevel);
                if (_services.Gear != null)
                    interval *= 1f - Mathf.Clamp01(_services.Gear.SwiftBonus);
                if (Profile.swiftBuffLeft > 0f)
                    interval *= 1f - Eco.swiftPotionBonus;
                interval *= 1f - Mathf.Min(0.55f, Mutation(Profile.mutationSwift, Eco.mutationSwiftPerDecade));
                return Mathf.Max(Eco.autoIntervalMin, interval);
            }
        }

        public float CritChance
        {
            get
            {
                float value = Profile.critLevel * Eco.critPerLevel;
                if (_services.Gear != null)
                    value += _services.Gear.CritBonus;
                return Mathf.Min(Eco.critChanceCap, value);
            }
        }

        public float CritMultiplier => Eco.critMultiplier + Profile.furyLevel * 0.25f;

        public float CleaveFraction
        {
            get
            {
                if (Profile.cleaveLevel <= 0)
                    return 0f;
                return Eco.cleaveBase + (Profile.cleaveLevel - 1) * 0.05f;
            }
        }

        public float AutoDps => TapDamage / Mathf.Max(0.2f, AutoInterval);

        public float GoldPerSecond
        {
            get
            {
                int wave = Mathf.Max(1, Profile.wave);
                var combat = Combat;
                float hp = combat.enemyBaseHp + combat.enemyHpPerWave * (wave - 1);
                var zone = _services.Catalog.ZoneAt(Profile.zone);
                hp *= Mathf.Max(0.25f, zone.hpMul);
                float kills = AutoDps / Mathf.Max(8f, hp);
                return kills * GoldForKill(wave, false);
            }
        }

        public float Focus => Profile.focus;
        public float FocusFuryLeft => _focusFuryLeft;
        float _focusFuryLeft;

        public long UpgradeCost(string id) => CostAt(id, Profile.UpgradeLevel(id));

        public long CostAt(string id, int level)
        {
            var def = _services.Catalog.FindUpgrade(id);
            int baseCost = def != null ? def.baseCost : 15;
            float growth = def != null ? def.costGrowth : 1.18f;
            return Math.Max(1, (long)Math.Round(baseCost * Math.Pow(growth, level)));
        }

        public long CostFor(string id, int levels)
        {
            if (levels <= 0)
                return 0;
            long sum = 0;
            int start = Profile.UpgradeLevel(id);
            for (int i = 0; i < levels; i++)
                sum += CostAt(id, start + i);
            return sum;
        }

        public int MaxAffordable(string id)
        {
            if (!IsUnlocked(id) || IsMaxed(id))
                return 0;
            var def = _services.Catalog.FindUpgrade(id);
            int cap = (def != null ? def.maxLevel : 200) - Profile.UpgradeLevel(id);
            long gold = Profile.gold;
            int n = 0;
            int start = Profile.UpgradeLevel(id);
            while (n < cap && n < 500)
            {
                long cost = CostAt(id, start + n);
                if (gold < cost)
                    break;
                gold -= cost;
                n++;
            }

            return n;
        }

        public int PlannedLevels(string id, int mode)
        {
            int afford = MaxAffordable(id);
            if (afford <= 0)
                return 0;
            if (mode < 0)
                return afford;
            return Mathf.Min(mode, afford);
        }

        public bool CanBuy(string id) => IsUnlocked(id) && MaxAffordable(id) > 0 && !IsMaxed(id);

        public bool IsUnlocked(string id)
        {
            var def = _services.Catalog.FindUpgrade(id);
            if (def == null || string.IsNullOrEmpty(def.requiresId) || def.requiresLevel <= 0)
                return true;
            return Profile.UpgradeLevel(def.requiresId) >= def.requiresLevel;
        }

        public string LockReason(string id)
        {
            var def = _services.Catalog.FindUpgrade(id);
            if (def == null || IsUnlocked(id))
                return null;
            return "Needs " + Title(def.requiresId) + " " + def.requiresLevel;
        }

        static string Title(string id)
        {
            switch (id)
            {
                case ContentIds.Might: return "Might";
                case ContentIds.Fortune: return "Fortune";
                case ContentIds.Swift: return "Swift";
                case ContentIds.Crit: return "Crit";
                case ContentIds.Cleave: return "Cleave";
                case ContentIds.Fury: return "Fury";
                case ContentIds.Harvest: return "Harvest";
                default: return id;
            }
        }

        public bool IsMaxed(string id)
        {
            var def = _services.Catalog.FindUpgrade(id);
            int cap = def != null ? def.maxLevel : 200;
            return Profile.UpgradeLevel(id) >= cap;
        }

        public bool TryBuy(string id) => TryBuy(id, 1);

        public bool TryBuy(string id, int mode)
        {
            if (!IsUnlocked(id) || IsMaxed(id))
                return false;
            int n = PlannedLevels(id, mode);
            if (n <= 0)
                return false;
            long cost = CostFor(id, n);
            if (!_services.Save.TrySpendGold(cost))
                return false;
            Profile.SetUpgradeLevel(id, Profile.UpgradeLevel(id) + n);
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
            int late = Mathf.Max(0, wave - Mathf.RoundToInt(eco.lateGoldStartWave));
            if (late > 0)
                raw *= Math.Pow(Mathf.Max(1.001f, eco.lateGoldGrowth), late);
            return Math.Max(1, (long)Math.Round(raw * GoldMultiplier));
        }

        public int DustForKill(bool boss)
        {
            if (boss)
                return Eco.dustPerBoss + Profile.harvestLevel / 4;
            float chance = Eco.dustDropChance + Profile.harvestLevel * Eco.harvestDustPerLevel;
            chance *= 1f + Mutation(Profile.mutationLuck, Eco.mutationPerDecade);
            return UnityEngine.Random.value < chance ? 1 : 0;
        }

        public string RollPotionDrop(bool boss)
        {
            float chance = boss ? Eco.potionBossDropChance : Eco.potionDropChance;
            chance += Profile.harvestLevel * Eco.harvestPotionPerLevel;
            chance *= 1f + Mutation(Profile.mutationLuck, Eco.mutationPerDecade);
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

        public float PotionBuffLeft(string id)
        {
            switch (id)
            {
                case ContentIds.PotMight: return Profile.mightBuffLeft;
                case ContentIds.PotSwift: return Profile.swiftBuffLeft;
                case ContentIds.PotGold: return Profile.goldBuffLeft;
                default: return 0f;
            }
        }

        public static string FormatBuff(float seconds)
        {
            if (seconds <= 0f)
                return "";
            int whole = Mathf.CeilToInt(seconds);
            if (whole >= 10)
                return whole + "s";
            return seconds.ToString("0.0") + "s";
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
            changed |= Tick(ref _focusFuryLeft, dt);
            TickFocus(dt);
            if (changed)
                _services.NotifyProfile();
        }

        void TickFocus(float dt)
        {
            float max = Combat.focusMax > 0f ? Combat.focusMax : 100f;
            float regen = Combat.focusRegen > 0f ? Combat.focusRegen : 10f;
            Profile.focus = Mathf.Min(max, Profile.focus + regen * dt);
        }

        public bool TrySpendFocus(float cost)
        {
            if (cost <= 0f)
                return true;
            if (Profile.focus < cost)
                return false;
            Profile.focus -= cost;
            _services.NotifyProfile();
            return true;
        }

        public bool TryStartFocusFury()
        {
            if (!TrySpendFocus(Combat.furyCost))
                return false;
            _focusFuryLeft = Mathf.Max(_focusFuryLeft, Eco.focusFurySeconds);
            return true;
        }

        public int MutationCost(string id) => 1 + Profile.MutationLevel(id);

        public bool TryBuyMutation(string id)
        {
            int cost = MutationCost(id);
            if (Profile.glory < cost)
                return false;
            Profile.glory -= cost;
            Profile.SetMutationLevel(id, Profile.MutationLevel(id) + 1);
            _services.Save.MarkDirty();
            return true;
        }

        public bool CanAscend()
        {
            return Profile.zone > 0 || Profile.wave >= Mathf.Max(2, Combat.wavesPerBoss) || Profile.bossesSlain > 0;
        }

        public bool TryAscend()
        {
            if (!CanAscend())
                return false;
            Profile.ascendCount++;
            Profile.wave = 1;
            Profile.zone = 0;
            Profile.gold = 0;
            Profile.mightLevel = 0;
            Profile.fortuneLevel = 0;
            Profile.swiftLevel = 0;
            Profile.critLevel = 0;
            Profile.cleaveLevel = 0;
            Profile.furyLevel = 0;
            Profile.harvestLevel = 0;
            Profile.potMight = 0;
            Profile.potSwift = 0;
            Profile.potGold = 0;
            Profile.mightBuffLeft = 0f;
            Profile.swiftBuffLeft = 0f;
            Profile.goldBuffLeft = 0f;
            Profile.focus = 0f;
            _focusFuryLeft = 0f;
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
            return true;
        }

        public static float Mutation(int spent, float perDecade)
        {
            if (spent <= 0)
                return 0f;
            return Mathf.Max(0.01f, perDecade) * Mathf.Log10(1f + spent);
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
            _services.Gear?.TryRollDrop(boss);
            Profile.kills++;
            if (boss)
            {
                Profile.bossesSlain++;
                int glory = Eco.gloryPerBoss + Mathf.RoundToInt(Eco.gloryPerBossPerZone * Profile.zone);
                if (glory > 0)
                    Profile.glory += glory;
            }

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
            float factor = eco.offlineGoldFactor + Profile.glory * eco.unspentGloryOffline;
            return Math.Max(0, (long)Math.Floor(kills * GoldForKill(Mathf.Max(1, Profile.wave), false) * factor));
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
