using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace MyClicker.App
{
    public class BootLoader : MonoBehaviour
    {
        [SerializeField] float splashSeconds = 0.35f;
        bool _leaving;

        void Start()
        {
            GameServices.Ensure();
            var clip = Resources.Load<VideoClip>("SoloDreamsLogo");
            if (clip == null)
            {
                FinishLogo();
                return;
            }

            PlayLogo(clip);
        }

        void PlayLogo(VideoClip clip)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                FinishLogo();
                return;
            }

            cam.backgroundColor = Color.black;
            var player = cam.gameObject.GetComponent<VideoPlayer>() ?? cam.gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.CameraNearPlane;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.isLooping = false;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.clip = clip;
            player.loopPointReached += _ => FinishLogo();
            player.errorReceived += (_, __) => FinishLogo();
            player.prepareCompleted += prepared => prepared.Play();
            player.Prepare();
            Invoke(nameof(FinishLogo), Mathf.Max(2.2f, (float)clip.length + 0.35f));
        }

        void FinishLogo()
        {
            if (_leaving)
                return;
            _leaving = true;
            CancelInvoke();
            MyClicker.Audio.AudioDirector.Ensure().PlayCreate();
            Invoke(nameof(Go), splashSeconds);
        }

        void Go()
        {
            string scene = GameServices.Instance.Save.HasCharacter ? "Battle" : "CharacterCreate";
            SceneManager.LoadScene(scene);
        }
    }
}
