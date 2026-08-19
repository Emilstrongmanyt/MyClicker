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
        float _spawnTimer;
        float _autoTimer;
        int _killsThisWave;
        bool _bossWave;
        bool _awaitingClear;
        OfflineToast _offline;

        void Start()
        {
            var services = GameServices.Ensure();
            AudioDirector.Ensure().PlayBattle();
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
                HeroCharacterAdapter.Spawn(prefab, services.Save.Profile.heroJson, pos, 0.7f);
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

            if (!WasTap(out var screen) || OverUi())
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            world.z = 0f;
            var enemy = _spawner.AtPoint(world) ?? _spawner.Nearest(world);
            if (enemy == null || !enemy.Alive)
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

            if (_spawner.AliveCount >= combat.maxAlive)
                return;
            if (_killsThisWave + _spawner.AliveCount >= combat.killsPerWave)
            {
                if (_spawner.AliveCount == 0)
                    _awaitingClear = true;
                return;
            }

            _spawnTimer -= dt;
            if (_spawnTimer > 0f)
                return;
            _spawnTimer = combat.spawnInterval;
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
            if (enemy != null && enemy.Alive)
                Strike(enemy, tap: false);
        }

        void Strike(EnemyController enemy, bool tap)
        {
            var services = GameServices.Instance;
            float damage = services.Economy.TapDamage;
            bool crit = Random.value < services.Economy.CritChance;
            if (crit)
                damage *= services.Economy.CritMultiplier;

            Vector3 pos = enemy.transform.position;
            bool killed = enemy.Hit(damage);
            FloatingCombatText.Show(
                pos,
                crit ? Mathf.RoundToInt(damage) + "!" : Mathf.RoundToInt(damage).ToString(),
                crit ? new Color(1f, 0.86f, 0.28f) : (tap ? Color.white : new Color(0.85f, 0.9f, 1f)),
                crit ? 44 : 34);

            if (!killed)
                return;

            int wave = services.Save.Profile.wave;
            long gold = services.Economy.AwardKill(wave, enemy.IsBoss);
            FloatingCombatText.Show(pos + Vector3.up * 0.45f, NumberFmt.Signed(gold) + "g", new Color(1f, 0.84f, 0.28f), 30);
            _killsThisWave++;
            if (enemy.IsBoss || _killsThisWave >= Settings().killsPerWave)
                _awaitingClear = true;
        }

        void SpawnRegular()
        {
            var services = GameServices.Instance;
            var zone = services.Catalog.ZoneAt(services.Save.Profile.zone);
            var visual = services.Catalog.PickEnemy(zone, services.Save.Profile.wave);
            _spawner.SpawnRegular(visual, EnemyHp(false));
        }

        void SpawnBoss()
        {
            var services = GameServices.Instance;
            var zone = services.Catalog.ZoneAt(services.Save.Profile.zone);
            var visual = services.Catalog.FindBoss(zone.bossId) ?? services.Catalog.FindUnit(zone.bossId);
            if (visual == null && services.Catalog.bosses != null && services.Catalog.bosses.Length > 0)
                visual = services.Catalog.bosses[Mathf.Clamp(services.Save.Profile.zone, 0, services.Catalog.bosses.Length - 1)];
            _spawner.SpawnBoss(visual, EnemyHp(true));
            AudioDirector.Ensure().PlayCue(string.IsNullOrEmpty(zone.bossCue) ? "boss" : zone.bossCue);
        }

        void PrepareWave(int wave, bool fresh)
        {
            var combat = Settings();
            _killsThisWave = 0;
            _awaitingClear = false;
            _spawnTimer = fresh ? 0.15f : 0.4f;
            _bossWave = wave > 0 && wave % Mathf.Max(1, combat.wavesPerBoss) == 0;
            if (_bossWave)
            {
                _spawner.Clear();
                SpawnBoss();
            }
            else
            {
                AudioDirector.Ensure().PlayBattle();
            }
        }

        void AdvanceWave()
        {
            var services = GameServices.Instance;
            var combat = Settings();
            var eco = services.Config != null ? services.Config.economy : new GameConfig.EconomySettings();
            bool wasBoss = _bossWave;
            services.Save.AddGold(wasBoss ? 0 : eco.goldPerWave);
            services.Save.Profile.wave++;
            if (wasBoss)
            {
                var catalog = services.Catalog;
                if (catalog.zones != null && catalog.zones.Length > 0)
                    services.Save.Profile.zone = Mathf.Min(services.Save.Profile.zone + 1, catalog.zones.Length - 1);
            }

            services.Save.MarkDirty();
            _spawner.Clear();
            PrepareWave(services.Save.Profile.wave, fresh: true);
        }

        float EnemyHp(bool boss)
        {
            var services = GameServices.Instance;
            var combat = Settings();
            var zone = services.Catalog.ZoneAt(services.Save.Profile.zone);
            int wave = Mathf.Max(1, services.Save.Profile.wave);
            float hp = combat.enemyBaseHp + combat.enemyHpPerWave * (wave - 1);
            hp *= Mathf.Max(0.25f, zone.hpMul);
            if (boss)
                hp *= combat.bossHpMul;
            return hp;
        }

        void ResolveOffline()
        {
            var services = GameServices.Instance;
            long last = services.Save.Profile.lastSeenUnix;
            if (last <= 0)
                return;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long gold = services.Economy.EstimateOfflineGold(now - last);
            if (gold <= 0)
                return;
            services.Save.AddGold(gold);
            _offline = new OfflineToast { gold = gold, life = 4.2f };
        }

        public string OfflineMessage =>
            _offline.life > 0f ? "While you were away  +" + NumberFmt.Compact(_offline.gold) + "g" : null;

        void LateUpdate()
        {
            if (_offline.life > 0f)
                _offline.life -= Time.unscaledDeltaTime;
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

        struct OfflineToast
        {
            public long gold;
            public float life;
        }
    }
}
