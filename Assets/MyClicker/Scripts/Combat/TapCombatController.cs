using MyClicker.App;
using MyClicker.Character;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyClicker.Combat
{
    public class TapCombatController : MonoBehaviour
    {
        EnemySpawner _spawner;
        int _killsThisWave;
        const int KillsPerWave = 10;

        void Start()
        {
            var services = GameServices.Ensure();
            MyClicker.UI.StoneUi.EnsureCanvas();
            if (FindFirstObjectByType<MyClicker.UI.HudController>() == null)
                gameObject.AddComponent<MyClicker.UI.HudController>();
            if (FindFirstObjectByType<MyClicker.World.BackgroundController>() == null)
                gameObject.AddComponent<MyClicker.World.BackgroundController>();
            _spawner = GetComponent<EnemySpawner>();
            if (_spawner == null)
                _spawner = gameObject.AddComponent<EnemySpawner>();

            var combat = services.Config != null ? services.Config.combat : new Data.GameConfig.CombatSettings();
            var pos = new Vector3(combat.playerSlot.x, combat.playerSlot.y, 0f);
            var prefab = HeroPrefabLoader.Load(services.Config);
            if (prefab != null)
                HeroCharacterAdapter.Spawn(prefab, services.Save.Profile.heroJson, pos, 0.7f);
            else
                Debug.LogError("[MyClicker] Battle hero prefab is missing.");
        }

        void Update()
        {
            _spawner.Tick(Time.deltaTime);
            if (!WasTap(out var screen))
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            var hit = Physics2D.OverlapPoint(world);
            if (hit == null)
                return;

            var enemy = hit.GetComponent<EnemyController>();
            if (enemy == null)
                return;

            float damage = GameServices.Instance.Save.Profile.tapDamage;
            bool killed = enemy.Hit(damage);
            if (!killed)
                return;

            var save = GameServices.Instance.Save;
            var economy = GameServices.Instance.Config != null
                ? GameServices.Instance.Config.economy
                : new Data.GameConfig.EconomySettings();
            save.Profile.kills++;
            save.AddGold(economy.goldPerKill);
            _killsThisWave++;
            if (_killsThisWave >= KillsPerWave)
            {
                _killsThisWave = 0;
                save.Profile.wave++;
                save.AddGold(economy.goldPerWave);
                _spawner.Clear();
            }
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
