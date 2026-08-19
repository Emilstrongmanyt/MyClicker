using MyClicker.App;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Audio
{
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        AudioSource _music;
        string _currentCue;

        public static AudioDirector Ensure()
        {
            if (Instance != null)
                return Instance;

            var existing = FindFirstObjectByType<AudioDirector>();
            if (existing != null)
            {
                Instance = existing;
                existing.EnsureSource();
                return existing;
            }

            var host = GameServices.Ensure().gameObject;
            var director = host.GetComponent<AudioDirector>();
            if (director == null)
                director = host.AddComponent<AudioDirector>();
            Instance = director;
            director.EnsureSource();
            return director;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureSource();
        }

        public void PlayCue(string cue)
        {
            if (string.IsNullOrEmpty(cue) || cue == _currentCue)
                return;
            var clip = Resolve(cue);
            if (clip == null)
                return;
            EnsureSource();
            _music.clip = clip;
            _music.loop = true;
            _music.volume = 0.42f;
            _music.Play();
            _currentCue = cue;
        }

        public void PlayCreate() => PlayCue("create");
        public void PlayBattle() => PlayCue("battle");
        public void PlayBoss() => PlayCue("boss");
        public void PlayNight() => PlayCue("night");

        void EnsureSource()
        {
            if (_music != null)
                return;
            _music = gameObject.GetComponent<AudioSource>();
            if (_music == null)
                _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;
            _music.priority = 0;
        }

        static AudioClip Resolve(string cue)
        {
            var audio = GameServices.Instance != null && GameServices.Instance.Catalog != null
                ? GameServices.Instance.Catalog.audio
                : null;

            AudioClip clip = null;
            if (audio != null)
            {
                switch (cue)
                {
                    case "create": clip = audio.create; break;
                    case "boss": clip = audio.boss; break;
                    case "night": clip = audio.night; break;
                    default: clip = audio.battle; break;
                }
            }

            if (clip != null)
                return clip;

            switch (cue)
            {
                case "create":
                    return Resources.Load<AudioClip>("ES_Vagabond's Awakening - Dian Shuai");
                case "boss":
                    return Resources.Load<AudioClip>("ES_Return of the Longship - Dian Shuai");
                case "night":
                    return Resources.Load<AudioClip>("ES_Beneath the Old Moon - Adriel Fair");
                default:
                    return Resources.Load<AudioClip>("ES_Dawn of the Long Road - Dian Shuai");
            }
        }
    }
}
