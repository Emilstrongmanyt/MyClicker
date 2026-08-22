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
                if (HasGlory(GloryIds.Steel))
                    value += TemperSum() * 1.5f;
                if (Profile.mightBuffLeft > 0f)
                    value *= 1f + Eco.mightPotionBonus;
                if (_focusFuryLeft > 0f)
                    value *= 1f + Eco.focusFuryBonus;
                value *= 1f + 0.08f * Profile.oathVow;
                value *= 1f + Renown();
                value *= 1f + Mutation(Profile.mutationMight, Eco.mutationPerDecade);
                value *= MilestoneMul();
                if (HasGlory(GloryIds.BloodOath))
                    value *= 1.25f;
                if (HasGlory(GloryIds.GiantsDue))
                    value *= 1.5f;
                if (Profile.gloryTapLeft > 0f)
                    value *= Mathf.Max(1f, Profile.gloryTapMul);
                if (_surgeLeft > 0f)
                    value *= SurgeMul;
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
                value *= 1f + 0.08f * Profile.oathTithe;
                value *= 1f + Renown();
                value *= 1f + Mutation(Profile.mutationFortune, Eco.mutationPerDecade);
                if (HasGlory(GloryIds.UnspentTithe))
                    value *= 1f + Mathf.Min(0.4f, 0.004f * Mathf.Max(0, Profile.glory));
                if (HasGlory(GloryIds.Hoard))
                    value *= 1f + RelicCount() * 0.015f;
                value *= MilestoneMul();
                value *= CycleMul();
                var zone = _services.Catalog.ZoneAt(Profile.zone);
                return value * Mathf.Max(0.25f, zone.goldMul);
            }
        }

        public float AutoInterval => AutoIntervalAt(Profile.swiftLevel, includeBuffs: true);

        public float CritChance => CritChanceAt(Profile.critLevel);

        public float CritMultiplier => Eco.critMultiplier + Profile.furyLevel * 0.25f;

        public float CleaveFraction => CleaveAt(Profile.cleaveLevel);

        public float OverclockMul => 1f + 0.12f * Mathf.Max(0, Profile.oathOverclock);

        float Renown()
        {
            return _services.Deeds != null ? _services.Deeds.Renown : 0f;
        }

        public float AutoDps => TapDamage * OverclockMul / Mathf.Max(0.2f, AutoInterval);

        public float CycleMul()
        {
            int cycle = Mathf.Max(0, Profile.cycle);
            if (cycle <= 0)
                return 1f;
            float growth = Eco.endlessCycleGrowth;
            if (growth < 1.05f)
                growth = 1.35f;
            float value = Mathf.Pow(growth, cycle);
            if (HasGlory(GloryIds.DeepRoad))
                value *= 1.08f;
            return value;
        }

        public bool EndlessOpen
        {
            get { return Profile.endlessUnlocked || HasGlory(GloryIds.DeepRoad); }
        }

        public bool TryUnlockEndless()
        {
            if (Profile.endlessUnlocked)
                return true;
            Profile.endlessUnlocked = true;
            _services.Save.MarkDirty();
            return true;
        }

        public double GoldPerSecond
        {
            get
            {
                int wave = Mathf.Max(1, Profile.wave);
                var combat = Combat;
                float hp = combat.enemyBaseHp + combat.enemyHpPerWave * (wave - 1);
                var zone = _services.Catalog.ZoneAt(Profile.zone);
                hp *= Mathf.Max(0.25f, zone.hpMul);
                hp *= CycleMul();
                float kills = AutoDps / Mathf.Max(8f, hp);
                return kills * GoldForKill(wave, false);
            }
        }

        public float Focus => Profile.focus;
        public float FocusFuryLeft => _focusFuryLeft;
        float _focusFuryLeft;
        float _surgeLeft;
        float _surgeCool;
        bool _surgeProc;

        public const float SurgeMul = 4f;
        public const int WarCryCost = 12;
        public const int GodstrikeCost = 40;
        public const float WarCrySeconds = 15f;
        public const float WarCryMul = 2f;
        public const float GodstrikeSeconds = 12f;
        public const float GodstrikeMul = 3.5f;

        public float FurySeconds => Eco.focusFurySeconds > 0f ? Eco.focusFurySeconds : 8f;
        public float SurgeSeconds => 10f;
        public float SurgeLeft => _surgeLeft;
        public float GloryTapLeft => Profile.gloryTapLeft;
        public float GloryTapDuration => Profile.gloryTapDuration > 0.05f ? Profile.gloryTapDuration : WarCrySeconds;

        public float FocusMax
        {
            get
            {
                float max = Combat.focusMax > 0f ? Combat.focusMax : 100f;
                if (HasGlory(GloryIds.FocusWell))
                    max *= 1.25f;
                return max;
            }
        }

        public float FocusRegen
        {
            get
            {
                float regen = Combat.focusRegen > 0f ? Combat.focusRegen : 10f;
                if (HasGlory(GloryIds.FocusWell))
                    regen *= 1.2f;
                return regen;
            }
        }

        public bool HasGlory(string id) => GloryTree.Has(Profile, id);

        public bool HasDeepRoad => HasGlory(GloryIds.DeepRoad);

        public int Relics => RelicCount();
        public int Shards => Profile.bossShards != null ? Profile.bossShards.Length : 0;
        public int ShardCap
        {
            get
            {
                var zones = _services.Catalog != null ? _services.Catalog.zones : null;
                return zones != null && zones.Length > 0 ? zones.Length : 10;
            }
        }

        public float CollectionBonus
        {
            get
            {
                int n = RelicCount();
                if (n >= 12) return 0.10f;
                if (n >= 8) return 0.05f;
                if (n >= 4) return 0.02f;
                return 0f;
            }
        }

        public float ShardBonus => Shards * 0.02f;

        float MilestoneMul() => 1f + CollectionBonus + ShardBonus;

        public bool HasShard(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || Profile.bossShards == null)
                return false;
            for (int i = 0; i < Profile.bossShards.Length; i++)
            {
                if (Profile.bossShards[i] == zoneId)
                    return true;
            }

            return false;
        }

        public bool TryGrantBossShard(string zoneId)
        {
            if (!UnlockShard(zoneId))
                return false;
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
            return true;
        }

        public void EnsureBossShards()
        {
            var zones = _services.Catalog != null ? _services.Catalog.zones : null;
            if (zones == null)
                return;
            bool any = false;
            int cleared = Mathf.Clamp(Profile.bestZone, 0, zones.Length);
            for (int i = 0; i < cleared; i++)
            {
                if (zones[i] == null)
                    continue;
                if (UnlockShard(zones[i].id))
                    any = true;
            }

            if (!any)
                return;
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
        }

        public string CollectionUnlockLine(int relicCount)
        {
            if (relicCount != 4 && relicCount != 8 && relicCount != 12)
                return null;
            return "Collection  " + relicCount + " relics  +" + Mathf.RoundToInt(CollectionBonus * 100f) + "%";
        }

        bool UnlockShard(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId) || HasShard(zoneId))
                return false;
            int n = Profile.bossShards != null ? Profile.bossShards.Length : 0;
            var next = new string[n + 1];
            if (n > 0)
                Array.Copy(Profile.bossShards, next, n);
            next[n] = zoneId;
            Profile.bossShards = next;
            return true;
        }

        int RelicCount()
        {
            return Profile.unlockedGear != null ? Profile.unlockedGear.Length : 0;
        }

        bool BareRun()
        {
            return Profile.mightLevel <= 0
                   && Profile.fortuneLevel <= 0
                   && Profile.swiftLevel <= 0
                   && Profile.critLevel <= 0
                   && Profile.cleaveLevel <= 0
                   && Profile.furyLevel <= 0
                   && Profile.harvestLevel <= 0
                   && Profile.oathTithe <= 0
                   && Profile.oathVow <= 0
                   && Profile.oathOverclock <= 0;
        }

        int TemperSum()
        {
            return Profile.temperWeapon + Profile.temperArmor + Profile.temperHelmet + Profile.temperCape;
        }

        public double UpgradeCost(string id) => CostAt(id, Profile.UpgradeLevel(id));

        public double CostAt(string id, int level)
        {
            var def = _services.Catalog.FindUpgrade(id);
            int baseCost = def != null ? def.baseCost : 15;
            float growth = def != null ? def.costGrowth : 1.18f;
            return Math.Max(1d, Math.Round(baseCost * Math.Pow(growth, level)));
        }

        public double CostFor(string id, int levels)
        {
            if (levels <= 0)
                return 0d;
            double sum = 0d;
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
            double gold = Profile.gold;
            int n = 0;
            int start = Profile.UpgradeLevel(id);
            while (n < cap && n < 500)
            {
                double cost = CostAt(id, start + n);
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
            switch (id)
            {
                case ContentIds.OathTithe:
                    return Profile.kills >= 500;
                case ContentIds.OathVow:
                    return Profile.mightLevel >= 20 || Profile.runBosses > 0;
                case ContentIds.OathOverclock:
                    return IsMaxed(ContentIds.Swift);
            }

            var def = _services.Catalog.FindUpgrade(id);
            if (def == null || string.IsNullOrEmpty(def.requiresId) || def.requiresLevel <= 0)
                return true;
            return Profile.UpgradeLevel(def.requiresId) >= def.requiresLevel;
        }

        public string LockReason(string id)
        {
            if (IsUnlocked(id))
                return null;
            switch (id)
            {
                case ContentIds.OathTithe:
                    return "Needs 500 kills  (" + Profile.kills + ")";
                case ContentIds.OathVow:
                    return "Needs Might 20 or a boss this run";
                case ContentIds.OathOverclock:
                    return "Needs Swift MAX";
            }

            var def = _services.Catalog.FindUpgrade(id);
            if (def == null)
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
                case ContentIds.OathTithe: return "Blood Tithe";
                case ContentIds.OathVow: return "Iron Vow";
                case ContentIds.OathOverclock: return "Overclock";
                default: return id;
            }
        }

        public bool IsMaxed(string id)
        {
            var def = _services.Catalog.FindUpgrade(id);
            int cap = def != null ? def.maxLevel : 200;
            if (Profile.UpgradeLevel(id) >= cap)
                return true;
            return !NextRankHelps(id);
        }

        bool NextRankHelps(string id)
        {
            int level = Profile.UpgradeLevel(id);
            switch (id)
            {
                case ContentIds.Crit:
                    return CritChanceAt(level + 1) > CritChanceAt(level) + 0.0001f;
                case ContentIds.Swift:
                    return AutoIntervalAt(level + 1, includeBuffs: false)
                           < AutoIntervalAt(level, includeBuffs: false) - 0.0001f;
                case ContentIds.Cleave:
                    return CleaveAt(level + 1) > CleaveAt(level) + 0.0001f;
                default:
                    return true;
            }
        }

        float CritChanceAt(int level)
        {
            float value = Mathf.Max(0, level) * Eco.critPerLevel;
            if (_services.Gear != null)
                value += _services.Gear.CritBonus;
            return Mathf.Min(Eco.critChanceCap, value);
        }

        float AutoIntervalAt(int level, bool includeBuffs)
        {
            float interval = Eco.autoIntervalStart * Mathf.Pow(Eco.autoIntervalDecay, Mathf.Max(0, level));
            if (_services.Gear != null)
                interval *= 1f - Mathf.Clamp01(_services.Gear.SwiftBonus);
            if (includeBuffs && Profile.swiftBuffLeft > 0f)
                interval *= 1f - Eco.swiftPotionBonus;
            interval *= 1f - Mathf.Min(0.55f, Mutation(Profile.mutationSwift, Eco.mutationSwiftPerDecade));
            return Mathf.Max(Eco.autoIntervalMin, interval);
        }

        float CleaveAt(int level)
        {
            if (level <= 0)
                return 0f;
            return Mathf.Min(1f, Eco.cleaveBase + (level - 1) * 0.05f);
        }

        public bool TryBuy(string id) => TryBuy(id, 1);

        public bool TryBuy(string id, int mode)
        {
            if (!IsUnlocked(id) || IsMaxed(id))
                return false;
            int n = PlannedLevels(id, mode);
            if (n <= 0)
                return false;
            double cost = CostFor(id, n);
            if (!_services.Save.TrySpendGold(cost))
                return false;
            Profile.SetUpgradeLevel(id, Profile.UpgradeLevel(id) + n);
            Profile.forgeBought += n;
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
            _services.Deeds?.Evaluate();
            return true;
        }

        public double GoldForKill(int wave, bool boss)
        {
            var eco = Eco;
            double raw = boss
                ? eco.goldPerBoss + wave * eco.goldPerBossPerWave
                : eco.goldPerKill + (wave - 1) * eco.goldPerKillPerWave;
            int late = Mathf.Max(0, wave - Mathf.RoundToInt(eco.lateGoldStartWave));
            if (late > 0)
                raw *= Math.Pow(Mathf.Max(1.001f, eco.lateGoldGrowth), late);
            return Math.Max(1d, raw * GoldMultiplier);
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
            Profile.usedPotion = true;
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
            changed |= Tick(ref Profile.gloryTapLeft, dt);
            if (Tick(ref _surgeLeft, dt))
                _surgeCool = 20f;
            Tick(ref _surgeCool, dt);
            TickFocus(dt);
            if (changed)
                _services.NotifyProfile();
        }

        void TickFocus(float dt)
        {
            Profile.focus = Mathf.Min(FocusMax, Profile.focus + FocusRegen * dt);
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
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
            return true;
        }

        public bool TryBuyGloryNode(string id)
        {
            var node = GloryTree.Find(id);
            if (!GloryTree.CanBuy(Profile, node))
                return false;
            if (!GloryTree.Unlock(Profile, node.id))
                return false;
            if (node.cost > 0)
                Profile.glory -= node.cost;
            if (node.id == GloryIds.DeepRoad)
                Profile.endlessUnlocked = true;
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
            return true;
        }

        public void EnsureLegacy()
        {
            if (Profile.ascendCount < 1)
                return;
            if (!GloryTree.Unlock(Profile, GloryIds.Legacy))
                return;
            _services.Save.MarkDirty();
        }

        public void EnsureEndless()
        {
            if (!HasGlory(GloryIds.DeepRoad) || Profile.endlessUnlocked)
                return;
            Profile.endlessUnlocked = true;
            _services.Save.MarkDirty();
        }

        public int LastAscendGlory { get; private set; }
        public int PendingGlory => Mathf.Max(0, Profile.pendingGlory);
        public int RunBosses => Mathf.Max(0, Profile.runBosses);

        public int GloryForBoss(int zone)
        {
            int glory = Eco.gloryPerBoss + Mathf.FloorToInt(Eco.gloryPerBossPerZone * Mathf.Max(0, zone));
            glory += Mathf.Max(0, Profile.cycle) * (2 + Mathf.Max(0, zone));
            return Mathf.Max(2, glory);
        }

        public bool TryBuyWarCry()
        {
            return TryBuyTapBuff(WarCryCost, WarCryMul, WarCrySeconds);
        }

        public bool TryBuyGodstrike()
        {
            if (Profile.ascendCount < 1)
                return false;
            return TryBuyTapBuff(GodstrikeCost, GodstrikeMul, GodstrikeSeconds);
        }

        bool TryBuyTapBuff(int cost, float mul, float seconds)
        {
            if (cost <= 0 || Profile.glory < cost)
                return false;
            Profile.glory -= cost;
            Profile.gloryTapMul = mul;
            Profile.gloryTapDuration = seconds;
            Profile.gloryTapLeft = seconds;
            Profile.tapDamage = TapDamage;
            _services.Save.MarkDirty();
            return true;
        }

        public bool ConsumeSurgeProc()
        {
            if (!_surgeProc)
                return false;
            _surgeProc = false;
            return true;
        }

        void MaybeGlorySurge()
        {
            if (Profile.ascendCount < 1)
                return;
            if (_surgeLeft > 0f || _surgeCool > 0f)
                return;
            if (UnityEngine.Random.value > 0.012f)
                return;
            _surgeLeft = SurgeSeconds;
            _surgeProc = true;
            Profile.tapDamage = TapDamage;
        }

        public bool CanAscend()
        {
            return Profile.zone > 0 || Profile.wave >= Mathf.Max(2, Combat.wavesPerBoss) || Profile.bossesSlain > 0;
        }

        public bool TryAscend()
        {
            if (!CanAscend())
                return false;
            LastAscendGlory = PendingGlory;
            if (LastAscendGlory > 0)
                Profile.glory += LastAscendGlory;
            Profile.pendingGlory = 0;
            Profile.runBosses = 0;
            Profile.ascendCount++;
            int keepMight = HasGlory(GloryIds.KeepMight) ? Profile.mightLevel : 0;
            int keepFortune = HasGlory(GloryIds.KeepFortune) ? Profile.fortuneLevel : 0;
            Profile.wave = 1;
            Profile.zone = 0;
            Profile.cycle = 0;
            Profile.gold = 0;
            Profile.mightLevel = keepMight;
            Profile.fortuneLevel = keepFortune;
            Profile.swiftLevel = 0;
            Profile.critLevel = 0;
            Profile.cleaveLevel = 0;
            Profile.furyLevel = 0;
            Profile.harvestLevel = 0;
            Profile.oathTithe = 0;
            Profile.oathVow = 0;
            Profile.oathOverclock = 0;
            Profile.potMight = 0;
            Profile.potSwift = 0;
            Profile.potGold = 0;
            Profile.mightBuffLeft = 0f;
            Profile.swiftBuffLeft = 0f;
            Profile.goldBuffLeft = 0f;
            Profile.gloryTapLeft = 0f;
            Profile.gloryTapMul = 1f;
            Profile.gloryTapDuration = 0f;
            Profile.focus = 0f;
            _focusFuryLeft = 0f;
            _surgeLeft = 0f;
            _surgeCool = 0f;
            _surgeProc = false;
            GloryTree.Unlock(Profile, GloryIds.Legacy);
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

        public double AwardKill(int wave, bool boss)
        {
            double gold = GoldForKill(wave, boss);
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
                Profile.runBosses++;
                int glory = GloryForBoss(Profile.zone);
                if (glory > 0)
                    Profile.pendingGlory += glory;
                if (BareRun())
                    Profile.usedBareBoss = true;
            }

            MaybeGlorySurge();
            _services.Save.MarkDirty();
            return gold;
        }

        public double EstimateOfflineGold(long seconds)
        {
            var eco = Eco;
            long cap = Math.Max(60, (long)eco.offlineCapHours * 3600L);
            long usable = Math.Min(Math.Max(0, seconds), cap);
            if (usable < 15)
                return 0d;
            float hp = Combat.enemyBaseHp + Combat.enemyHpPerWave * Mathf.Max(0, Profile.wave - 1);
            hp *= CycleMul();
            float kills = AutoDps * usable / Mathf.Max(8f, hp);
            float factor = eco.offlineGoldFactor + Profile.glory * eco.unspentGloryOffline;
            if (HasGlory(GloryIds.NightMarket))
                factor += 0.08f;
            return Math.Max(0d, Math.Floor(kills * GoldForKill(Mathf.Max(1, Profile.wave), false) * factor));
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
