using System.Collections.Generic;
using MyClicker.App;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Combat
{
    public class EnemySpawner : MonoBehaviour
    {
        readonly List<EnemyController> _pool = new List<EnemyController>();
        readonly List<EnemyController> _alive = new List<EnemyController>();

        public IReadOnlyList<EnemyController> Alive => _alive;
        public int AliveCount
        {
            get
            {
                Prune();
                return _alive.Count;
            }
        }

        public bool HasBoss
        {
            get
            {
                Prune();
                for (int i = 0; i < _alive.Count; i++)
                {
                    if (_alive[i] != null && _alive[i].IsBoss && _alive[i].Alive)
                        return true;
                }

                return false;
            }
        }

        public EnemyController CurrentBoss
        {
            get
            {
                Prune();
                for (int i = 0; i < _alive.Count; i++)
                {
                    if (_alive[i] != null && _alive[i].IsBoss && _alive[i].Alive)
                        return _alive[i];
                }

                return null;
            }
        }

        public EnemyController SpawnRegular(UnitVisual visual, float hp)
        {
            var combat = Settings();
            float side = Random.value < 0.5f ? combat.spawnX.x : combat.spawnX.y;
            var pos = new Vector3(side, Random.Range(combat.spawnY.x, combat.spawnY.y), 0f);
            return SpawnAt(visual, hp, pos, combat.enemySpeed, combat.approachStopDistance);
        }

        public EnemyController SpawnBoss(UnitVisual visual, float hp)
        {
            var combat = Settings();
            var pos = new Vector3(0f, combat.spawnY.y + 0.4f, 0f);
            return SpawnAt(visual, hp, pos, combat.enemySpeed * 0.72f, combat.approachStopDistance + 0.35f);
        }

        public EnemyController Nearest(Vector3 world)
        {
            Prune();
            EnemyController best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _alive.Count; i++)
            {
                var enemy = _alive[i];
                if (enemy == null || !enemy.Alive)
                    continue;
                float d = (enemy.transform.position - world).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = enemy;
                }
            }

            return best;
        }

        public EnemyController NearestExcept(Vector3 world, EnemyController skip)
        {
            Prune();
            EnemyController best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _alive.Count; i++)
            {
                var enemy = _alive[i];
                if (enemy == null || enemy == skip || !enemy.Alive)
                    continue;
                float d = (enemy.transform.position - world).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = enemy;
                }
            }

            return best;
        }

        public EnemyController AtPoint(Vector3 world)
        {
            var hit = Physics2D.OverlapPoint(world);
            if (hit == null)
                return null;
            return hit.GetComponent<EnemyController>();
        }

        public void Clear()
        {
            for (int i = 0; i < _alive.Count; i++)
            {
                if (_alive[i] != null)
                    _alive[i].gameObject.SetActive(false);
            }

            _alive.Clear();
        }

        EnemyController SpawnAt(UnitVisual visual, float hp, Vector3 pos, float speed, float stop)
        {
            var combat = Settings();
            var enemy = Get();
            enemy.transform.position = pos;
            Sprite fallback = null;
            if (combat.enemySprites != null && combat.enemySprites.Length > 0)
                fallback = combat.enemySprites[Random.Range(0, combat.enemySprites.Length)];
            if (visual != null && visual.Preview != null)
                fallback = visual.Preview;
            var target = new Vector3(combat.playerSlot.x, combat.playerSlot.y, 0f);
            enemy.Setup(visual, fallback, hp, target, speed, stop, OnPooledDeath);
            enemy.gameObject.SetActive(true);
            if (!_alive.Contains(enemy))
                _alive.Add(enemy);
            return enemy;
        }

        void OnPooledDeath(EnemyController enemy)
        {
            _alive.Remove(enemy);
        }

        void Prune()
        {
            _alive.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
        }

        EnemyController Get()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null && !_pool[i].gameObject.activeInHierarchy)
                    return _pool[i];
            }

            var go = new GameObject("Enemy");
            go.transform.SetParent(transform, false);
            go.AddComponent<SpriteRenderer>();
            var created = go.AddComponent<EnemyController>();
            _pool.Add(created);
            return created;
        }

        static GameConfig.CombatSettings Settings()
        {
            var config = GameServices.Instance != null ? GameServices.Instance.Config : null;
            return config != null ? config.combat : new GameConfig.CombatSettings();
        }
    }
}
