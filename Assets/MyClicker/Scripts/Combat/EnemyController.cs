using System;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Combat
{
    public class EnemyController : MonoBehaviour
    {
        public float MaxHp { get; private set; }
        public float Hp { get; private set; }
        public bool Alive => Hp > 0f && !_dying;
        public bool IsBoss { get; private set; }
        public string DisplayName { get; private set; }
        public UnitVisual Visual { get; private set; }

        Vector3 _target;
        float _speed;
        float _stopDistance;
        SpriteRenderer _renderer;
        ClipPlayer _clip;
        Color _baseColor = Color.white;
        float _flash;
        bool _dying;
        bool _engaged;
        Action<EnemyController> _onDeathFinished;

        public void Setup(UnitVisual visual, Sprite fallback, float hp, Vector3 target, float speed, float stopDistance, Action<EnemyController> onDeathFinished)
        {
            Visual = visual;
            IsBoss = visual != null && visual.isBoss;
            DisplayName = visual != null && !string.IsNullOrEmpty(visual.displayName)
                ? visual.displayName
                : (IsBoss ? "Boss" : "Invader");
            MaxHp = hp;
            Hp = hp;
            _target = target;
            _speed = speed;
            _stopDistance = stopDistance;
            _dying = false;
            _engaged = false;
            _flash = 0f;
            _onDeathFinished = onDeathFinished;

            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = IsBoss ? 12 : 10;
            _renderer.color = _baseColor;
            _renderer.enabled = true;

            if (_clip == null)
                _clip = GetComponent<ClipPlayer>() ?? gameObject.AddComponent<ClipPlayer>();
            _clip.Bind(_renderer);

            float scale = visual != null ? visual.scale : 2.1f;
            transform.localScale = Vector3.one * scale;
            FaceToward(_target);

            var col = GetComponent<CircleCollider2D>();
            if (col == null)
                col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.enabled = true;
            col.radius = IsBoss ? 0.62f : 0.48f;

            Play(UnitClip.Walk, 10f, true);
            if (_renderer.sprite == null && fallback != null)
                _renderer.sprite = fallback;
        }

        public bool Hit(float damage)
        {
            if (!Alive)
                return false;

            Hp = Mathf.Max(0f, Hp - damage);
            _flash = 0.1f;
            if (_renderer != null)
                _renderer.color = Color.white;
            Play(UnitClip.Hurt, 14f, false);

            if (Hp > 0f)
                return false;

            BeginDeath();
            return true;
        }

        void BeginDeath()
        {
            _dying = true;
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
                col.enabled = false;
            Play(UnitClip.Death, 12f, false);
            if (Visual == null || Visual.death == null || Visual.death.Length == 0)
                FinishDeath();
        }

        void FinishDeath()
        {
            gameObject.SetActive(false);
            _onDeathFinished?.Invoke(this);
            _onDeathFinished = null;
        }

        void Update()
        {
            if (_dying)
            {
                if (_clip == null || _clip.Done)
                    FinishDeath();
                return;
            }

            if (!Alive)
                return;

            Vector3 pos = transform.position;
            Vector3 to = _target - pos;
            to.z = 0f;
            float dist = to.magnitude;
            if (dist > _stopDistance)
            {
                transform.position = pos + to.normalized * (_speed * Time.deltaTime);
                FaceToward(_target);

                if (_engaged)
                {
                    _engaged = false;
                    Play(UnitClip.Walk, 10f, true);
                }
            }
            else if (!_engaged)
            {
                _engaged = true;
                Play(UnitClip.Attack, 9f, true);
            }

            if (_flash > 0f && _renderer != null)
            {
                _flash -= Time.deltaTime;
                _renderer.color = Color.Lerp(_baseColor, new Color(1f, 0.42f, 0.42f), Mathf.Clamp01(_flash / 0.1f));
            }
        }

        void FaceToward(Vector3 target)
        {
            float dx = target.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.05f)
                return;
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
            transform.localScale = scale;
        }

        void Play(UnitClip clip, float fps, bool loop)
        {
            if (_clip == null)
                return;
            Sprite[] frames = Visual != null ? Visual.Clip(clip) : null;
            if (frames != null && frames.Length > 0)
                _clip.Play(frames, fps, loop);
        }
    }
}
