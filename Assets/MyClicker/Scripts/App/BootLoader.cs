using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MyClicker.App
{
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] float fadeSeconds = 0.8f;
        bool _leaving;
        VideoPlayer _player;
        RenderTexture _rt;
        CanvasGroup _fade;
        float _cutAt = -1f;

        void Start()
        {
            GameServices.Ensure();
            if (!BeginLogo())
                StartCoroutine(FadeIntoGame());
        }

        void Update()
        {
            if (_leaving || _cutAt < 0f || _player == null)
                return;
            if (_player.time + 0.02 >= _cutAt)
                CutToFade();
        }

        bool BeginLogo()
        {
            string url = LogoUrl();
            var clip = Resources.Load<VideoClip>("SoloDreamsLogo");
            if (string.IsNullOrEmpty(url) && clip == null)
                return false;

            var canvas = Overlay();
            _fade = canvas.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 1f;
            var raw = canvas.transform.Find("Logo").GetComponent<RawImage>();
            _rt = new RenderTexture(1080, 1920, 0);
            _rt.Create();
            raw.texture = _rt;
            raw.color = Color.white;

            var cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = Color.black;

            _player = gameObject.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.isLooping = false;
            _player.skipOnDrop = true;
            _player.renderMode = VideoRenderMode.RenderTexture;
            _player.targetTexture = _rt;
            _player.aspectRatio = VideoAspectRatio.FitInside;
            _player.audioOutputMode = VideoAudioOutputMode.Direct;
            if (clip != null)
            {
                _player.source = VideoSource.VideoClip;
                _player.clip = clip;
            }
            else
            {
                _player.source = VideoSource.Url;
                _player.url = url;
            }

            _player.errorReceived += (_, err) =>
            {
                Debug.LogWarning("[MyClicker] Logo video failed: " + err);
                CutToFade();
            };
            _player.prepareCompleted += prepared =>
            {
                if (_leaving)
                    return;
                double len = prepared.length;
                if (len < 0.4 && prepared.clip != null)
                    len = prepared.clip.length;
                if (len < 0.4)
                    len = 6.0;
                _cutAt = (float)(len * 0.5);
                prepared.Play();
                CancelInvoke();
                Invoke(nameof(CutToFade), _cutAt + 2f);
            };
            _player.Prepare();
            Invoke(nameof(CutToFade), 12f);
            return true;
        }

        static string LogoUrl()
        {
            string streaming = Path.Combine(Application.streamingAssetsPath, "SoloDreamsLogo.mp4");
            if (File.Exists(streaming))
                return streaming;
            return null;
        }

        Canvas Overlay()
        {
            var go = new GameObject("LogoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            var image = new GameObject("Logo", typeof(RectTransform), typeof(RawImage));
            image.transform.SetParent(go.transform, false);
            var rt = image.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var raw = image.GetComponent<RawImage>();
            raw.color = Color.black;
            raw.raycastTarget = false;
            return canvas;
        }

        void CutToFade()
        {
            if (_leaving)
                return;
            if (_player != null && _player.isPlaying)
                _player.Pause();
            StartCoroutine(FadeIntoGame());
        }

        IEnumerator FadeIntoGame()
        {
            if (_leaving)
                yield break;
            _leaving = true;
            CancelInvoke();

            string scene = GameServices.Instance != null && GameServices.Instance.Save.HasCharacter
                ? "Battle"
                : "CharacterCreate";
            if (scene == "Battle")
                MyClicker.Audio.AudioDirector.Ensure().PlayBattle();
            else
                MyClicker.Audio.AudioDirector.Ensure().PlayCreate();

            if (_fade != null)
                DontDestroyOnLoad(_fade.gameObject);
            DontDestroyOnLoad(gameObject);

            AsyncOperation load = SceneManager.LoadSceneAsync(scene);
            if (load != null)
            {
                while (!load.isDone && load.progress < 0.9f)
                    yield return null;
            }

            float dur = Mathf.Max(0.2f, fadeSeconds);
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / dur);
                if (_fade != null)
                    _fade.alpha = a;
                if (_player != null && _player.audioTrackCount > 0)
                    _player.SetDirectAudioVolume(0, a);
                yield return null;
            }

            if (_player != null)
                _player.Stop();
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }

            if (_fade != null)
                Destroy(_fade.gameObject);
            Destroy(gameObject);
        }
    }
}
