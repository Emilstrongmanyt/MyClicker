using System.Collections.Generic;
using MyClicker.App;
using UnityEngine;

namespace MyClicker.Combat
{
    public class EnemySpawner : MonoBehaviour
    {
        readonly List<EnemyController> _pool = new List<EnemyController>();
        readonly List<EnemyController> _alive = new List<EnemyController>();
        float _timer;

        public IReadOnlyList<EnemyController> Alive => _alive;

        public void Tick(float dt)
        {
            var config = GameServices.Instance.Config;
            var combat = config != null ? config.combat : new Data.GameConfig.CombatSettings();
            _alive.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);

            _timer -= dt;
            if (_timer > 0f || _alive.Count >= combat.maxAlive)
                return;

            _timer = combat.spawnInterval;
            Spawn(combat);
        }

        public void Clear()
        {
            foreach (var enemy in _alive)
            {
                if (enemy != null)
                    enemy.gameObject.SetActive(false);
            }

            _alive.Clear();
        }

        void Spawn(Data.GameConfig.CombatSettings combat)
        {
            var enemy = Get(combat);
            float side = Random.value < 0.5f ? combat.spawnX.x : combat.spawnX.y;
            var pos = new Vector3(side, Random.Range(combat.spawnY.x, combat.spawnY.y), 0f);
            enemy.transform.position = pos;
            Sprite sprite = null;
            if (combat.enemySprites != null && combat.enemySprites.Length > 0)
                sprite = combat.enemySprites[Random.Range(0, combat.enemySprites.Length)];
            int wave = GameServices.Instance.Save.Profile.wave;
            float hp = combat.enemyBaseHp + combat.enemyHpPerWave * (wave - 1);
            enemy.Setup(sprite, hp, new Vector3(combat.playerSlot.x, combat.playerSlot.y, 0f), combat.enemySpeed);
            enemy.gameObject.SetActive(true);
            _alive.Add(enemy);
        }

        EnemyController Get(Data.GameConfig.CombatSettings combat)
        {
            foreach (var enemy in _pool)
            {
                if (enemy != null && !enemy.gameObject.activeInHierarchy)
                    return enemy;
            }

            GameObject go = null;
            if (combat.enemyPrefabs != null && combat.enemyPrefabs.Length > 0)
            {
                var prefab = combat.enemyPrefabs[Random.Range(0, combat.enemyPrefabs.Length)];
                if (prefab != null)
                    go = Instantiate(prefab);
            }

            if (go == null)
            {
                go = new GameObject("Enemy");
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 10;
            }

            go.transform.SetParent(transform, false);
            var enemyNew = go.GetComponent<EnemyController>();
            if (enemyNew == null)
                enemyNew = go.AddComponent<EnemyController>();
            if (go.GetComponent<Collider2D>() == null)
            {
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.55f;
            }

            _pool.Add(enemyNew);
            return enemyNew;
        }
    }
}
