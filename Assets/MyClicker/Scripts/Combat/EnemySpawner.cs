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
            float top = Camera.main != null ? Camera.main.orthographicSize + 1.15f : combat.spawnY.y;
            var pos = new Vector3(Random.Range(-2.6f, 2.6f), top, 0f);
            return SpawnAt(visual, hp, pos, combat.enemySpeed, combat.holdSlack);
        }

        public EnemyController SpawnBoss(UnitVisual visual, float hp)
        {
            var combat = Settings();
            float top = Camera.main != null ? Camera.main.orthographicSize + 1.45f : combat.spawnY.y + 0.4f;
            var pos = new Vector3(0f, top, 0f);
            return SpawnAt(visual, hp, pos, combat.enemySpeed * 0.72f, combat.holdSlack);
        }

        public EnemyController Nearest(Vector3 world)
        {
            Prune();
            EnemyController best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < _alive.Count; i++)
            {
                var enemy = _alive[i];
                if (enemy == null || !enemy.Alive || !enemy.Vulnerable)
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
                if (enemy == null || enemy == skip || !enemy.Alive || !enemy.Vulnerable)
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
            var enemy = hit.GetComponent<EnemyController>();
            return enemy != null && enemy.Vulnerable ? enemy : null;
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
            enemy.Setup(visual, fallback, hp, target, speed, Settings().holdSlack, OnPooledDeath);
            enemy.gameObject.SetActive(true);
            if (!_alive.Contains(enemy))
                _alive.Add(enemy);
            RefreshFormation();
            return enemy;
        }

        void OnPooledDeath(EnemyController enemy)
        {
            _alive.Remove(enemy);
            RefreshFormation();
        }

        public void RefreshFormation()
        {
            Prune();
            var combat = Settings();
            var hero = new Vector3(combat.playerSlot.x, combat.playerSlot.y, 0f);
            var ring = new List<EnemyController>();
            for (int i = 0; i < _alive.Count; i++)
            {
                if (_alive[i] != null && _alive[i].Alive)
                    ring.Add(_alive[i]);
            }

            int n = ring.Count;
            if (n == 0)
                return;

            const float start = 28f;
            const float end = 152f;
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0.5f : i / (float)(n - 1);
                float ang = Mathf.Lerp(start, end, t) * Mathf.Deg2Rad;
                float radius = ring[i].IsBoss ? combat.ringRadiusBoss : combat.ringRadius;
                if (n >= 6 && (i % 2) == 1)
                    radius += 0.42f;
                radius += Mathf.Max(0, n - 4) * 0.06f;
                var hold = hero + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radius;
                ring[i].SetHold(hold, combat.holdSlack);
            }
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
