using MyClicker.App;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Audio
{
    public class FxDirector : MonoBehaviour
    {
        public static FxDirector Instance { get; private set; }

        GameObject _furyFire;
        Transform _furyHero;
        float _killGate;
        float _critGate;
        int _live;
        static Material _sparkMat;

        public static FxDirector Ensure()
        {
            if (Instance != null)
                return Instance;
            var host = GameServices.Ensure().gameObject;
            var fx = host.GetComponent<FxDirector>() ?? host.AddComponent<FxDirector>();
            Instance = fx;
            return fx;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            CartoonFX.CFXR_Effect.GlobalDisableCameraShake = true;
            CartoonFX.CFXR_Effect.GlobalDisableLights = true;
        }

        void LateUpdate()
        {
            if (_furyFire == null || _furyHero == null)
                return;
            _furyFire.transform.position = _furyHero.position + Vector3.up * 0.35f;
        }

        public void SetFury(Transform hero, bool on)
        {
            if (!on)
            {
                _furyHero = null;
                if (_furyFire != null)
                    Destroy(_furyFire);
                _furyFire = null;
                return;
            }

            _furyHero = hero;
            if (hero == null)
                return;
            if (_furyFire == null)
            {
                var prefab = Prefab(c => c.furyFire);
                _furyFire = prefab != null
                    ? Spawn(prefab, null, hero.position + Vector3.up * 0.35f, 1.05f, 8)
                    : MakeFallbackLightning(hero.position + Vector3.up * 0.35f);
                StripSmoke(_furyFire);
                KeepAlive(_furyFire);
            }

            if (_furyFire != null)
            {
                _furyFire.SetActive(true);
                _furyFire.transform.position = hero.position + Vector3.up * 0.35f;
                _furyFire.transform.localScale = Vector3.one * 1.05f;
                RestartParticles(_furyFire);
            }
        }

        public void Kill(Vector3 world, bool boss)
        {
            if (boss)
            {
                Play(Prefab(c => c.bossDeath), world, 0.55f);
                return;
            }

            if (Time.unscaledTime < _killGate)
                return;
            _killGate = Time.unscaledTime + 0.08f;
            Play(Prefab(c => c.killPoof), world, 0.45f);
        }

        public void Slam(Vector3 world)
        {
            Play(Prefab(c => c.slamHit), world, 0.7f);
            Spark(world, new Color(1f, 0.82f, 0.35f, 0.95f), 18, 0.55f);
        }

        public void Crit(Vector3 world)
        {
            if (Time.unscaledTime < _critGate)
                return;
            _critGate = Time.unscaledTime + 0.07f;
            Spark(world, new Color(1f, 0.86f, 0.28f, 0.95f), 12, 0.42f);
        }

        public void Sweep(Vector3 world) => Play(Prefab(c => c.sweepTrail), world, 0.55f);

        public void Relic(Vector3 world) => Play(Prefab(c => c.relicGlow), world, 0.7f);

        public void Potion(string id, Vector3 world)
        {
            if (id == ContentIds.PotMight)
                Play(Prefab(c => c.potionFire), world, 0.5f);
            else if (id == ContentIds.PotSwift)
                Play(Prefab(c => c.potionWind), world, 0.55f);
            else
                Play(Prefab(c => c.potionFlash), world, 0.5f);
        }

        public void WaveClear(Vector3 world) => Play(Prefab(c => c.waveFlash), world, 0.8f);

        public void ZoneChange(Vector3 world) => Play(Prefab(c => c.zoneGlow), world, 0.85f);

        public void Ascend(Vector3 world) => Play(Prefab(c => c.ascendBurst), world, 0.9f);

        void Spark(Vector3 world, Color color, int count, float scale)
        {
            var go = new GameObject("HitSpark");
            go.transform.position = world;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = 0.16f;
            main.startLifetime = 0.2f;
            main.startSpeed = 2.6f * scale;
            main.startSize = 0.06f * scale;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var over = ps.colorOverLifetime;
            over.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            over.color = grad;
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.sortingOrder = 12;
            if (_sparkMat == null)
            {
                var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null)
                    _sparkMat = new Material(shader);
            }

            if (_sparkMat != null)
                render.sharedMaterial = _sparkMat;
            ps.Play(true);
            Destroy(go, 0.45f);
        }

        void Play(GameObject prefab, Vector3 world, float scale)
        {
            if (prefab == null || _live >= 6)
                return;
            var go = Spawn(prefab, null, world, scale, 8);
            if (go == null)
                return;
            _live++;
            Destroy(go, 3.2f);
            Invoke(nameof(FreeSlot), 3.2f);
        }

        void FreeSlot()
        {
            _live = Mathf.Max(0, _live - 1);
        }

        static GameObject Spawn(GameObject prefab, Transform parent, Vector3 pos, float scale, int sort)
        {
            var go = Instantiate(prefab);
            go.name = prefab.name;
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * scale;
            }
            else
            {
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * scale;
            }

            StripLights(go);
            Sort(go, sort);
            RestartParticles(go);
            return go;
        }

        static void StripLights(GameObject go)
        {
            var lights = go.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
                lights[i].enabled = false;
        }

        static void Sort(GameObject go, int order)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var render = systems[i].GetComponent<ParticleSystemRenderer>();
                if (render == null)
                    continue;
                render.sortingOrder = order;
            }
        }

        static void RestartParticles(GameObject go)
        {
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Clear(true);
                systems[i].Play(true);
            }
        }

        static void KeepAlive(GameObject go)
        {
            if (go == null)
                return;
            var effects = go.GetComponentsInChildren<CartoonFX.CFXR_Effect>(true);
            for (int i = 0; i < effects.Length; i++)
                effects[i].clearBehavior = CartoonFX.CFXR_Effect.ClearBehavior.None;
            var systems = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.loop = true;
                main.playOnAwake = true;
                systems[i].Play(true);
            }
        }

        static void StripSmoke(GameObject go)
        {
            if (go == null)
                return;
            var transforms = go.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                var child = transforms[i];
                if (child == null || child.gameObject == go)
                    continue;
                string name = child.name;
                if (name.IndexOf("smoke", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("cloud", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                child.gameObject.SetActive(false);
            }
        }

        static GameObject MakeFallbackLightning(Vector3 world)
        {
            var go = new GameObject("FuryAura");
            go.transform.position = world;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.22f;
            main.startSpeed = 1.8f;
            main.startSize = 0.08f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.95f, 1f, 0.95f), new Color(0.45f, 0.7f, 1f, 0.8f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var emission = ps.emission;
            emission.rateOverTime = 48f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.55f;
            var trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = 0.12f;
            trails.dieWithParticles = true;
            var color = ps.colorOverLifetime;
            color.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0f), new GradientColorKey(new Color(0.35f, 0.55f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = grad;
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.sortingOrder = 8;
            render.trailMaterial = render.sharedMaterial;
            ps.Play(true);
            return go;
        }

        static GameObject Prefab(System.Func<Data.GameConfig.FxLibrary, GameObject> pick)
        {
            var config = GameServices.Instance != null ? GameServices.Instance.Config : null;
            if (config == null || config.fx == null)
                return null;
            return pick(config.fx);
        }
    }
}
