using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MyClicker.App
{
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] float splashSeconds = 0.25f;
        bool _leaving;
        VideoPlayer _player;
        RenderTexture _rt;

        void Start()
        {
            GameServices.Ensure();
            if (!BeginLogo())
                FinishLogo();
        }

        bool BeginLogo()
        {
            string url = LogoUrl();
            var clip = Resources.Load<VideoClip>("SoloDreamsLogo");
            if (string.IsNullOrEmpty(url) && clip == null)
                return false;

            var canvas = Overlay();
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

            _player.loopPointReached += _ => FinishLogo();
            _player.errorReceived += (_, err) =>
            {
                Debug.LogWarning("[MyClicker] Logo video failed: " + err);
                FinishLogo();
            };
            _player.prepareCompleted += prepared =>
            {
                if (_leaving)
                    return;
                prepared.Play();
                float wait = prepared.length > 0.4 ? (float)prepared.length + 0.2f : 6f;
                CancelInvoke();
                Invoke(nameof(FinishLogo), wait);
            };
            _player.Prepare();
            Invoke(nameof(FinishLogo), 12f);
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

        void FinishLogo()
        {
            if (_leaving)
                return;
            _leaving = true;
            CancelInvoke();
            if (_player != null)
                _player.Stop();
            MyClicker.Audio.AudioDirector.Ensure().PlayCreate();
            Invoke(nameof(Go), splashSeconds);
        }

        void Go()
        {
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }

            string scene = GameServices.Instance.Save.HasCharacter ? "Battle" : "CharacterCreate";
            SceneManager.LoadScene(scene);
        }
    }
}
