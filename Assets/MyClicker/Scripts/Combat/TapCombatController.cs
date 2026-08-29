using System.Collections.Generic;
using MyClicker.App;
using MyClicker.Audio;
using MyClicker.Character;
using MyClicker.Data;
using MyClicker.Economy;
using MyClicker.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MyClicker.Combat
{
    public class TapCombatController : MonoBehaviour
    {
        EnemySpawner _spawner;
        HeroCharacterAdapter _hero;
        float _spawnTimer;
        float _autoTimer;
        int _killsThisWave;
        bool _bossWave;
        bool _awaitingClear;
        string _toast;
        float _toastLife;
        float _strikeAnimLock;
        readonly Queue<float> _tapTimes = new Queue<float>();
        readonly List<float> _dmgAt = new List<float>();
        readonly List<float> _dmgVal = new List<float>();
        const float TapWindow = 1.25f;
        const float DmgWindow = 2f;
        bool _furyFxOn;

        void Start()
        {
            var services = GameServices.Ensure();
            AudioDirector.Ensure();
            FxDirector.Ensure();
            var startZone = services.Catalog.ZoneAt(services.Save.Profile.zone);
            AudioDirector.Ensure().PlayZone(startZone.battleCue);
            StoneUi.EnsureCanvas();
            if (FindFirstObjectByType<HudController>() == null)
                gameObject.AddComponent<HudController>();
            if (FindFirstObjectByType<World.BackgroundController>() == null)
                gameObject.AddComponent<World.BackgroundController>();

            _spawner = GetComponent<EnemySpawner>();
            if (_spawner == null)
                _spawner = gameObject.AddComponent<EnemySpawner>();

            var combat = Settings();
            var pos = new Vector3(combat.playerSlot.x, combat.playerSlot.y, 0f);
            var prefab = HeroPrefabLoader.Load(services.Config);
            if (prefab != null)
            {
                _hero = HeroCharacterAdapter.Spawn(prefab, services.Save.Profile.heroJson, pos, 0.7f);
                services.Gear.Bind(_hero);
            }
            else
                Debug.LogError("[MyClicker] Battle hero prefab is missing.");

            ResolveOffline();
            PrepareWave(services.Save.Profile.wave, fresh: false);
        }

        void Update()
        {
            var services = GameServices.Instance;
            if (services == null)
                return;

            TickSpawns(Time.deltaTime);
            TickAuto(Time.deltaTime);
            TickFuryFx();
            if (_strikeAnimLock > 0f)
                _strikeAnimLock -= Time.deltaTime;

            if (!WasTap(out var screen) || OverUi())
                return;

            RegisterTap();
            AudioDirector.Ensure().PlaySfx("swing");
            var cam = Camera.main;
            if (cam == null)
                return;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            world.z = 0f;
            var enemy = _spawner.AtPoint(world) ?? _spawner.Nearest(world);
            if (enemy == null || !enemy.Alive || !enemy.Vulnerable)
                return;

            Strike(enemy, tap: true);
        }

        void TickSpawns(float dt)
        {
            if (_awaitingClear)
            {
                if (_spawner.AliveCount == 0)
                    AdvanceWave();
                return;
            }

            var combat = Settings();
            if (_bossWave)
            {
                if (_spawner.AliveCount == 0)
                    _awaitingClear = true;
                return;
            }

            var economy = GameServices.Instance.Economy;
            int cap = economy != null ? economy.SpawnCap : combat.maxAlive;
            if (_spawner.AliveCount >= cap)
                return;
            int need = economy != null ? economy.WaveKillNeed : combat.killsPerWave;
            if (_killsThisWave + _spawner.AliveCount >= need)
            {
                if (_spawner.AliveCount == 0)
                    _awaitingClear = true;
                return;
            }

            _spawnTimer -= dt;
            if (_spawnTimer > 0f)
                return;
            int depth = economy != null ? economy.DepthWave : GameServices.Instance.Save.Profile.wave;
            float late = combat.lateSpawnBoost * Mathf.Max(0, depth - 12);
            float interval = combat.spawnInterval * (1f - Mathf.Min(0.45f, late));
            if (economy != null)
                interval *= economy.SpawnIntervalMul;
            _spawnTimer = Mathf.Max(0.16f, interval);
            SpawnRegular();
        }

        void TickAuto(float dt)
        {
            var economy = GameServices.Instance.Economy;
            _autoTimer -= dt;
            if (_autoTimer > 0f)
                return;
            _autoTimer = economy.AutoInterval;
            var enemy = _spawner.Nearest(HeroSlot());
            if (enemy != null && enemy.Alive && enemy.Vulnerable)
                Strike(enemy, tap: false);
        }

        void Strike(EnemyController enemy, bool tap)
        {
            var services = GameServices.Instance;
            float damage = services.Economy.TapDamage;
            if (tap)
            {
                damage *= EconomyService.TapStrikeMul;
                if (enemy.IsBoss)
                    damage *= services.Economy.StarTapBossMul;
            }
            else
            {
                damage *= services.Economy.OverclockMul * EconomyService.AutoHitMul;
                damage *= services.Economy.StarAutoMul / Mathf.Max(0.01f, services.Economy.StarTapMul);
            }

            bool crit = Random.value < services.Economy.CritChance;
            if (crit)
            {
                float critMul = services.Economy.CritMultiplier;
                if (tap)
                    critMul += services.Economy.StarTapCritMul;
                damage *= critMul;
            }

            if (tap)
                AudioDirector.Ensure().PlaySfx(enemy.IsBoss ? "hitArmor" : "hit");
            AnimateHero(enemy);
            Vector3 pos = enemy.transform.position;
            ApplyHit(enemy, damage, crit, tap);
            float cleave = services.Economy.CleaveFraction;
            if (tap)
                cleave = Mathf.Max(cleave, services.Economy.StarTapCleave);
            if (cleave <= 0f)
                return;
            var splash = _spawner.NearestExcept(pos, enemy);
            if (splash != null && splash.Alive)
                ApplyHit(splash, damage * cleave, false, tap);
        }

        void SpawnRegular()
        {
            var services = GameServices.Instance;
            var zone = services.Catalog.ZoneAt(services.Save.Profile.zone);
            var visual = services.Catalog.PickEnemy(zone, services.Save.Profile.wave, services.Save.Profile.cycle, services.Save.Profile.zone);
            _spawner.SpawnRegular(visual, EnemyHp(false));
        }

        void SpawnBoss()
        {
            var services = GameServices.Instance;
            var zone = services.Catalog.ZoneAt(services.Save.Profile.zone);
            var visual = services.Catalog.PickBoss(zone, services.Save.Profile.zone, services.Save.Profile.cycle);
            _spawner.SpawnBoss(visual, EnemyHp(true));
            PlayZoneMusic(zone);
        }

        void PrepareWave(int wave, bool fresh)
        {
            var combat = Settings();
            _killsThisWave = 0;
            _awaitingClear = false;
            _spawnTimer = fresh ? 0.15f : 0.4f;
            _bossWave = wave > 0 && wave % Mathf.Max(1, combat.wavesPerBoss) == 0;
            var zone = GameServices.Instance.Catalog.ZoneAt(GameServices.Instance.Save.Profile.zone);
            PlayZoneMusic(zone);
            if (_bossWave)
            {
                _spawner.Clear();
                SpawnBoss();
            }
        }

        void AdvanceWave()
        {
            var services = GameServices.Instance;
            var combat = Settings();
            var eco = services.Config != null ? services.Config.economy : new GameConfig.EconomySettings();
            bool wasBoss = _bossWave;
            if (!wasBoss)
            {
                double waveGold = eco.goldPerWave * services.Economy.CycleGoldMul();
                if (waveGold > 0d)
                    services.Save.AddGold(waveGold);
            }
            services.Save.Profile.wave++;
            bool shard = false;
            bool looped = false;
            ZoneDef cleared = null;
            if (wasBoss)
            {
                var catalog = services.Catalog;
                var profile = services.Save.Profile;
                cleared = catalog.ZoneAt(profile.zone);
                shard = services.Economy.TryGrantBossShard(cleared != null ? cleared.id : null);
                int last = catalog.zones != null && catalog.zones.Length > 0 ? catalog.zones.Length - 1 : 0;
                bool atEnd = profile.zone >= last;
                if (atEnd && services.Economy.TryUnlockEndless())
                {
                    profile.cycle++;
                    if (profile.cycle > profile.bestCycle)
                        profile.bestCycle = profile.cycle;
                    profile.zone = 0;
                    profile.wave = 1;
                    looped = true;
                }
                else if (catalog.zones != null && catalog.zones.Length > 0)
                {
                    profile.zone = Mathf.Min(profile.zone + 1, last);
                    if (profile.zone > profile.bestZone)
                        profile.bestZone = profile.zone;
                }
            }

            services.Save.MarkDirty();
            if (wasBoss)
            {
                var zone = services.Catalog.ZoneAt(services.Save.Profile.zone);
                string sting;
                if (looped)
                {
                    int stars = services.Economy.GrantLoopStar();
                    sting = "Endless Road  " + services.Save.Profile.cycle + "  ·  +" + stars + " Star";
                    if (stars != 1)
                        sting += "s";
                    if (shard && cleared != null)
                        sting += "  ·  " + cleared.displayName + " shard";
                }
                else
                {
                    sting = zone.displayName;
                    if (shard && cleared != null)
                    {
                        if (zone != null && zone.id == cleared.id)
                            sting = "Shard  " + cleared.displayName;
                        else
                            sting = zone.displayName + "  ·  " + cleared.displayName + " shard";
                    }
                }

                Announce(sting, 2.6f, false);
                FxDirector.Ensure().ZoneChange(HeroSlot() + Vector3.up * 1.4f);
            }
            else
            {
                Announce("Wave " + services.Save.Profile.wave, 1.7f, false);
                FxDirector.Ensure().WaveClear(HeroSlot() + Vector3.up * 1.1f);
            }

            _spawner.Clear();
            PrepareWave(services.Save.Profile.wave, fresh: true);
        }

        public void RestartRun()
        {
            if (_spawner != null)
                _spawner.Clear();
            PrepareWave(GameServices.Instance.Save.Profile.wave, fresh: true);
        }

        public bool TrySlam()
        {
            var economy = GameServices.Instance != null ? GameServices.Instance.Economy : null;
            if (economy == null || !economy.TrySpendFocus(economy.SlamCost))
                return false;
            AudioDirector.Ensure().PlaySfx("slam");
            AudioDirector.Ensure().PlaySfx("twoHand");
            GameServices.Instance.Save.Profile.usedSlam = true;
            GameServices.Instance.Save.MarkDirty();
            var enemy = _spawner != null ? _spawner.Nearest(HeroSlot()) : null;
            FxDirector.Ensure().Slam(enemy != null ? enemy.transform.position : HeroSlot());
            if (enemy == null || !enemy.Alive)
                return true;
            StrikeMul(enemy, economy.SlamMul, tap: true);
            return true;
        }

        public bool TrySweep()
        {
            var economy = GameServices.Instance != null ? GameServices.Instance.Economy : null;
            if (economy == null || !economy.TrySpendFocus(Settings().sweepCost))
                return false;
            AudioDirector.Ensure().PlaySfx("sweep");
            GameServices.Instance.Save.Profile.usedSweep = true;
            GameServices.Instance.Save.MarkDirty();
            FxDirector.Ensure().Sweep(HeroSlot() + Vector3.up * 0.4f);
            if (_spawner == null)
                return true;
            float mul = economy.SweepMul;
            if (economy.ReaperSweep)
            {
                var boss = _spawner.CurrentBoss;
                var enemy = boss != null && boss.Alive && boss.Vulnerable
                    ? boss
                    : _spawner.Nearest(HeroSlot());
                if (enemy != null && enemy.Alive && enemy.Vulnerable)
                    StrikeMul(enemy, mul, tap: true);
                return true;
            }

            var alive = _spawner.Alive;
            for (int i = 0; i < alive.Count; i++)
            {
                var enemy = alive[i];
                if (enemy == null || !enemy.Alive || !enemy.Vulnerable || enemy.IsBoss)
                    continue;
                StrikeMul(enemy, mul, tap: true);
            }

            return true;
        }

        public bool TryFocusFury()
        {
            var economy = GameServices.Instance != null ? GameServices.Instance.Economy : null;
            if (economy == null || !economy.TryStartFocusFury())
                return false;
            AudioDirector.Ensure().PlaySfx("fury");
            GameServices.Instance.Save.Profile.usedFury = true;
            GameServices.Instance.Save.MarkDirty();
            FxDirector.Ensure().SetFury(_hero != null ? _hero.transform : null, true);
            _furyFxOn = true;
            return true;
        }

        void StrikeMul(EnemyController enemy, float mul, bool tap)
        {
            var services = GameServices.Instance;
            float damage = services.Economy.TapDamage * mul;
            bool crit = Random.value < services.Economy.CritChance;
            if (crit)
                damage *= services.Economy.CritMultiplier;
            AnimateHero(enemy);
            Vector3 pos = enemy.transform.position;
            ApplyHit(enemy, damage, crit, tap);
            float cleave = services.Economy.CleaveFraction;
            if (cleave <= 0f)
                return;
            var splash = _spawner.NearestExcept(pos, enemy);
            if (splash != null && splash.Alive && !(splash.IsBoss && mul <= 1.3f))
                ApplyHit(splash, damage * cleave, false, tap);
        }

        void AnimateHero(EnemyController enemy)
        {
            if (_hero == null || enemy == null)
                return;
            _hero.FaceWorldX(enemy.transform.position.x);
            if (_strikeAnimLock > 0f)
                return;
            _hero.PlayStrike();
            _strikeAnimLock = 0.16f;
        }

        public bool ToastPlaque { get; private set; } = true;

        public void Announce(string message, float life = 3f, bool plaque = true)
        {
            ToastPlaque = plaque;
            ShowToast(message, life);
        }

        void ShowToast(string message, float life)
        {
            _toast = message;
            _toastLife = life;
        }

        float EnemyHp(bool boss)
        {
            var economy = GameServices.Instance != null ? GameServices.Instance.Economy : null;
            if (economy != null)
                return economy.CurrentEnemyHp(boss);
            var combat = Settings();
            return combat.enemyBaseHp;
        }

        bool ApplyHit(EnemyController enemy, float damage, bool crit, bool tap)
        {
            RecordDamage(damage);
            bool killed = enemy.Hit(damage);
            if (killed)
                FxDirector.Ensure().Kill(enemy.transform.position, enemy.IsBoss);
            string amount = damage >= 1000f ? NumberFmt.Compact(damage) : Mathf.RoundToInt(damage).ToString();
            FloatingCombatText.Show(
                enemy.transform.position,
                crit ? amount + "!" : amount,
                crit ? new Color(1f, 0.86f, 0.28f) : (tap ? Color.white : new Color(0.85f, 0.9f, 1f)),
                crit ? 44 : 34);
            if (!killed)
                return false;

            int wave = GameServices.Instance.Save.Profile.wave;
            var economy = GameServices.Instance.Economy;
            double gold = economy.AwardKill(wave, enemy.IsBoss);
            FloatingCombatText.Show(enemy.transform.position + Vector3.up * 0.45f, NumberFmt.Signed(gold) + "g", new Color(1f, 0.84f, 0.28f), 30);
            if (economy.ConsumeSurgeProc())
            {
                Announce("Glory Surge", 2.4f, false);
                FxDirector.Ensure().Potion(ContentIds.PotGold, enemy.transform.position + Vector3.up * 0.5f);
            }
            _killsThisWave++;
            int need = GameServices.Instance.Economy != null
                ? GameServices.Instance.Economy.WaveKillNeed
                : Settings().killsPerWave;
            if (enemy.IsBoss || _killsThisWave >= need)
                _awaitingClear = true;
            return true;
        }

        void ResolveOffline()
        {
            var services = GameServices.Instance;
            long last = services.Save.Profile.lastSeenUnix;
            if (last <= 0)
                return;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long away = now - last;
            double gold = services.Economy.EstimateOfflineGold(away);
            if (gold <= 0d)
                return;
            services.Save.AddGold(gold);
            string toast = "While you were away\n+" + NumberFmt.Compact(gold) + " gold";
            int potionAt = services.Economy.HasStar(StarIds.Sleepless) ? 10 * 60 : 15 * 60;
            if (away >= potionAt)
            {
                string potion = OfflinePotion();
                services.Economy.GrantPotion(potion);
                var def = services.Catalog != null ? services.Catalog.FindPotion(potion) : null;
                toast += "\n+" + (def != null ? def.displayName : "Potion");
            }

            if (away >= 30 * 60 && services.Gear != null)
            {
                string relic = services.Gear.TryRollDrop(true, force: true);
                if (!string.IsNullOrEmpty(relic))
                    toast += "\n" + relic;
            }

            ShowToast(toast, 8f);
            FxDirector.Ensure().WaveClear(HeroSlot() + Vector3.up * 1.2f);
        }

        static string OfflinePotion()
        {
            float roll = Random.value;
            if (roll < 0.4f) return ContentIds.PotMight;
            if (roll < 0.75f) return ContentIds.PotSwift;
            return ContentIds.PotGold;
        }

        public string ToastMessage => _toastLife > 0f ? _toast : null;

        public float TapsPerSecond
        {
            get
            {
                PruneTaps();
                return _tapTimes.Count / TapWindow;
            }
        }

        public float DamagePerSecond
        {
            get
            {
                PruneDamage();
                float sum = 0f;
                for (int i = 0; i < _dmgVal.Count; i++)
                    sum += _dmgVal[i];
                return sum / DmgWindow;
            }
        }

        void RegisterTap()
        {
            _tapTimes.Enqueue(Time.unscaledTime);
            PruneTaps();
        }

        void PruneTaps()
        {
            float cutoff = Time.unscaledTime - TapWindow;
            while (_tapTimes.Count > 0 && _tapTimes.Peek() < cutoff)
                _tapTimes.Dequeue();
        }

        void RecordDamage(float value)
        {
            _dmgAt.Add(Time.unscaledTime);
            _dmgVal.Add(value);
            PruneDamage();
        }

        void PruneDamage()
        {
            float cutoff = Time.unscaledTime - DmgWindow;
            while (_dmgAt.Count > 0 && _dmgAt[0] < cutoff)
            {
                _dmgAt.RemoveAt(0);
                _dmgVal.RemoveAt(0);
            }
        }

        void TickFuryFx()
        {
            var economy = GameServices.Instance != null ? GameServices.Instance.Economy : null;
            bool on = economy != null && economy.FocusFuryLeft > 0f;
            if (on == _furyFxOn)
                return;
            _furyFxOn = on;
            FxDirector.Ensure().SetFury(_hero != null ? _hero.transform : null, on);
        }

        static void PlayZoneMusic(ZoneDef zone)
        {
            string cue = zone != null && !string.IsNullOrEmpty(zone.battleCue) ? zone.battleCue : "battle";
            AudioDirector.Ensure().PlayZone(cue);
        }

        void LateUpdate()
        {
            if (_toastLife > 0f)
                _toastLife -= Time.unscaledDeltaTime;
        }

        static GameConfig.CombatSettings Settings()
        {
            var config = GameServices.Instance != null ? GameServices.Instance.Config : null;
            return config != null ? config.combat : new GameConfig.CombatSettings();
        }

        static Vector3 HeroSlot()
        {
            var combat = Settings();
            return new Vector3(combat.playerSlot.x, combat.playerSlot.y, 0f);
        }

        static bool OverUi()
        {
            var es = EventSystem.current;
            if (es == null)
                return false;
            if (es.IsPointerOverGameObject())
                return true;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
                return es.IsPointerOverGameObject(touch.primaryTouch.touchId.ReadValue());
            return false;
        }

        static bool WasTap(out Vector2 screen)
        {
            screen = default;
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                screen = pointer.position.ReadValue();
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                screen = touch.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }

    }
}
