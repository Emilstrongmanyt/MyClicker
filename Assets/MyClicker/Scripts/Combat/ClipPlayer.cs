using UnityEngine;

namespace MyClicker.Combat
{
    public class ClipPlayer : MonoBehaviour
    {
        SpriteRenderer _renderer;
        Sprite[] _frames;
        float _fps = 10f;
        bool _loop = true;
        float _time;
        int _index;
        bool _done;

        public bool Done => _done;
        public SpriteRenderer Renderer => _renderer;

        public void Bind(SpriteRenderer renderer)
        {
            _renderer = renderer;
        }

        public void Play(Sprite[] frames, float fps, bool loop)
        {
            _frames = frames;
            _fps = Mathf.Max(1f, fps);
            _loop = loop;
            _time = 0f;
            _index = 0;
            _done = frames == null || frames.Length == 0;
            Apply();
        }

        void Update()
        {
            if (_done || _frames == null || _frames.Length == 0)
                return;

            if (_frames.Length == 1)
            {
                if (_loop)
                    return;
                _time += Time.deltaTime;
                if (_time >= 1f / _fps)
                    _done = true;
                return;
            }

            _time += Time.deltaTime * _fps;
            int next = (int)_time;
            if (next == _index)
                return;

            if (next >= _frames.Length)
            {
                if (_loop)
                {
                    _time -= _frames.Length;
                    _index = (int)_time;
                }
                else
                {
                    _index = _frames.Length - 1;
                    _done = true;
                }
            }
            else
            {
                _index = next;
            }

            Apply();
        }

        void Apply()
        {
            if (_renderer == null || _frames == null || _frames.Length == 0)
                return;
            int i = Mathf.Clamp(_index, 0, _frames.Length - 1);
            if (_frames[i] != null)
                _renderer.sprite = _frames[i];
        }
    }
}
