using UnityEngine;

namespace MyClicker.Combat
{
    public class EnemyController : MonoBehaviour
    {
        public float MaxHp { get; private set; }
        public float Hp { get; private set; }
        public bool Alive => Hp > 0f;

        Vector3 _target;
        float _speed;
        SpriteRenderer _renderer;
        Color _baseColor = Color.white;
        float _flash;

        public void Setup(Sprite sprite, float hp, Vector3 target, float speed)
        {
            MaxHp = hp;
            Hp = hp;
            _target = target;
            _speed = speed;
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null)
            {
                if (sprite != null)
                    _renderer.sprite = sprite;
                _renderer.color = _baseColor;
            }

            var col = GetComponent<CircleCollider2D>();
            if (col == null)
                col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;
            transform.localScale = Vector3.one * 1.15f;
        }

        public bool Hit(float damage)
        {
            if (!Alive)
                return false;
            Hp -= damage;
            _flash = 0.12f;
            if (_renderer != null)
                _renderer.color = Color.white;
            if (Hp <= 0f)
            {
                Hp = 0f;
                gameObject.SetActive(false);
                return true;
            }

            return false;
        }

        void Update()
        {
            if (!Alive)
                return;

            Vector3 pos = transform.position;
            Vector3 to = _target - pos;
            to.z = 0f;
            if (to.magnitude > 0.08f)
                transform.position = pos + to.normalized * (_speed * Time.deltaTime);

            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                if (_renderer != null)
                    _renderer.color = Color.Lerp(new Color(1f, 0.45f, 0.45f), _baseColor, 1f - Mathf.Clamp01(_flash / 0.12f));
            }
        }
    }
}
