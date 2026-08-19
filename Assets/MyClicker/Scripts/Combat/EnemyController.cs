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
        public bool Engaged => _engaged;
        public bool Vulnerable { get; private set; }
        public float EngagedSeconds { get; private set; }
        public string DisplayName { get; private set; }
        public UnitVisual Visual { get; private set; }

        Vector3 _target;
        Vector3 _hold;
        float _speed;
        float _stopDistance;
        SpriteRenderer _renderer;
        ClipPlayer _clip;
        Color _baseColor = Color.white;
        float _flash;
        bool _dying;
        bool _engaged;
        bool _animLock;
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
            _hold = target + Vector3.up * Mathf.Max(0.8f, stopDistance);
            _speed = speed;
            _stopDistance = Mathf.Max(0.08f, stopDistance);
            _dying = false;
            _engaged = false;
            _animLock = false;
            Vulnerable = false;
            EngagedSeconds = 0f;
            _flash = 0f;
            _onDeathFinished = onDeathFinished;

            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            _renderer.enabled = true;
            _renderer.color = new Color(1f, 1f, 1f, 0.75f);
            _renderer.sortingOrder = IsBoss ? 12 : 10;

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
            if (!Alive || !Vulnerable)
                return false;

            Hp = Mathf.Max(0f, Hp - damage);
            _flash = 0.1f;
            if (_renderer != null)
                _renderer.color = Color.white;
            Play(UnitClip.Hurt, 14f, false);
            _animLock = true;

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
            Vector3 to = _hold - pos;
            to.z = 0f;
            float dist = to.magnitude;
            bool atHold = dist <= _stopDistance;
            if (!atHold)
            {
                transform.position = pos + to.normalized * (_speed * Time.deltaTime);
                FaceToward(_hold);
                if (_engaged)
                {
                    _engaged = false;
                    EngagedSeconds = 0f;
                    if (!_animLock)
                        Play(UnitClip.Walk, 10f, true);
                }
            }
            else
            {
                FaceToward(_target);
                if (!_engaged)
                {
                    _engaged = true;
                    EngagedSeconds = 0f;
                    if (!_animLock)
                        Play(UnitClip.Attack, 10f, true);
                }
                else
                    EngagedSeconds += Time.deltaTime;
            }

            if (_animLock && (_clip == null || _clip.Done))
            {
                _animLock = false;
                Play(atHold ? UnitClip.Attack : UnitClip.Walk, atHold ? 10f : 10f, true);
            }

            if (_flash > 0f && _renderer != null)
            {
                _flash -= Time.deltaTime;
                _renderer.color = Color.Lerp(_baseColor, new Color(1f, 0.42f, 0.42f), Mathf.Clamp01(_flash / 0.1f));
            }

            RefreshOnScreen();
        }

        void RefreshOnScreen()
        {
            bool inside = FullyOnScreen();
            if (inside == Vulnerable)
                return;
            Vulnerable = inside;
            if (_renderer != null && _flash <= 0f)
                _renderer.color = inside ? _baseColor : new Color(1f, 1f, 1f, 0.72f);
        }

        bool FullyOnScreen()
        {
            if (_renderer == null || !_renderer.enabled)
                return false;
            var cam = Camera.main;
            if (cam == null)
                return true;
            var bounds = _renderer.bounds;
            Vector3 min = cam.ViewportToWorldPoint(new Vector3(0.02f, 0.02f, 0f));
            Vector3 max = cam.ViewportToWorldPoint(new Vector3(0.98f, 0.98f, 0f));
            float x0 = Mathf.Max(bounds.min.x, Mathf.Min(min.x, max.x));
            float x1 = Mathf.Min(bounds.max.x, Mathf.Max(min.x, max.x));
            float y0 = Mathf.Max(bounds.min.y, Mathf.Min(min.y, max.y));
            float y1 = Mathf.Min(bounds.max.y, Mathf.Max(min.y, max.y));
            float visible = Mathf.Max(0f, x1 - x0) * Mathf.Max(0f, y1 - y0);
            float area = Mathf.Max(0.001f, bounds.size.x * bounds.size.y);
            return visible / area >= 0.92f;
        }

        public void SetHold(Vector3 hold, float slack)
        {
            _hold = hold;
            if (slack > 0f)
                _stopDistance = slack;
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
