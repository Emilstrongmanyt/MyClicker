using MyClicker.App;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Audio
{
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        AudioSource _music;
        AudioSource[] _sfx;
        int _sfxIndex;
        string _currentCue;
        AudioSource _slice;
        float _sliceUntil;

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

        void Update()
        {
            if (_slice == null || !_slice.isPlaying || _sliceUntil <= 0f)
                return;
            if (Time.unscaledTime < _sliceUntil)
                return;
            _slice.Stop();
            _sliceUntil = 0f;
        }

        public void PlayCue(string cue)
        {
            if (string.IsNullOrEmpty(cue) || cue == _currentCue)
                return;
            var clip = ResolveMusic(cue);
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

        public void PlayZone(string cue)
        {
            if (string.IsNullOrEmpty(cue))
                cue = "battle";
            PlayCue(cue);
        }

        public void PlaySfx(string cue, float volume = 0.7f)
        {
            var def = Sfx(cue);
            if (def.clip == null)
                return;
            if (def.length > 0.02f)
            {
                PlaySlice(def.clip, def.start, def.length, volume * def.volume);
                return;
            }

            EnsureSource();
            var source = NextSfx();
            source.PlayOneShot(def.clip, volume * def.volume);
        }

        public void PlaySting(string resourceName, float volume = 0.55f)
        {
            var clip = Resources.Load<AudioClip>(resourceName);
            if (clip == null)
                return;
            EnsureSource();
            NextSfx().PlayOneShot(clip, volume);
        }

        void PlaySlice(AudioClip clip, float start, float length, float volume)
        {
            EnsureSource();
            if (_slice == null)
            {
                _slice = gameObject.AddComponent<AudioSource>();
                _slice.playOnAwake = false;
                _slice.loop = false;
                _slice.spatialBlend = 0f;
                _slice.priority = 64;
            }

            _slice.Stop();
            _slice.clip = clip;
            _slice.volume = volume;
            float begin = Mathf.Clamp(start, 0f, Mathf.Max(0f, (float)clip.length - 0.02f));
            _slice.time = begin;
            _slice.Play();
            _sliceUntil = Time.unscaledTime + Mathf.Max(0.05f, length);
        }

        void EnsureSource()
        {
            if (_music == null)
            {
                _music = gameObject.GetComponent<AudioSource>();
                if (_music == null)
                    _music = gameObject.AddComponent<AudioSource>();
                _music.playOnAwake = false;
                _music.loop = true;
                _music.spatialBlend = 0f;
                _music.priority = 0;
            }

            if (_sfx != null)
                return;
            _sfx = new AudioSource[3];
            for (int i = 0; i < _sfx.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.priority = 80;
                _sfx[i] = source;
            }
        }

        AudioSource NextSfx()
        {
            var source = _sfx[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % _sfx.Length;
            return source;
        }

        static AudioClip ResolveMusic(string cue)
        {
            var audio = GameServices.Instance != null && GameServices.Instance.Catalog != null
                ? GameServices.Instance.Catalog.audio
                : null;
            if (audio != null)
            {
                switch (cue)
                {
                    case "create": if (audio.create != null) return audio.create; break;
                    case "boss": if (audio.boss != null) return audio.boss; break;
                    case "night": if (audio.night != null) return audio.night; break;
                    case "night-2": if (audio.night2 != null) return audio.night2; break;
                    case "night-3": if (audio.night3 != null) return audio.night3; break;
                    case "day-2": if (audio.day2 != null) return audio.day2; break;
                    case "day-3": if (audio.day3 != null) return audio.day3; break;
                    case "battle": if (audio.battle != null) return audio.battle; break;
                }
            }

            switch (cue)
            {
                case "create":
                    return Resources.Load<AudioClip>("ES_Vagabond's Awakening - Dian Shuai");
                case "boss":
                    return Resources.Load<AudioClip>("ES_Return of the Longship - Dian Shuai");
                case "night":
                    return Resources.Load<AudioClip>("ES_Beneath the Old Moon - Adriel Fair");
                case "night-2":
                    return Resources.Load<AudioClip>("BGM Night");
                case "night-3":
                    return Resources.Load<AudioClip>("BGM Night (2)");
                case "day-2":
                    return Resources.Load<AudioClip>("BGM Day");
                case "day-3":
                    return Resources.Load<AudioClip>("BGM Day (2)");
                default:
                    return Resources.Load<AudioClip>("ES_Dawn of the Long Road - Dian Shuai");
            }
        }

        struct SfxDef
        {
            public AudioClip clip;
            public float start;
            public float length;
            public float volume;
        }

        static SfxDef Sfx(string cue)
        {
            SfxDef def;
            def.clip = null;
            def.start = 0f;
            def.length = 0f;
            def.volume = 1f;
            switch (cue)
            {
                case "swing":
                    def.clip = Clip("Swing+Miss");
                    def.length = 0.28f;
                    def.volume = 0.55f;
                    break;
                case "hit":
                    def.clip = Clip("Hit non armored enemy");
                    def.length = 0.32f;
                    def.volume = 0.65f;
                    break;
                case "hitArmor":
                    def.clip = Clip("Hit armored enemy");
                    def.length = 0.36f;
                    def.volume = 0.7f;
                    break;
                case "slam":
                    def.clip = Clip("Sweep and Slam SFX");
                    def.length = 0.55f;
                    def.volume = 0.85f;
                    break;
                case "sweep":
                    def.clip = Clip("Sweep and Slam SFX");
                    def.start = 0.7f;
                    def.length = 0.6f;
                    def.volume = 0.8f;
                    break;
                case "fury":
                    def.clip = Clip("Fury SFX");
                    def.length = 1.15f;
                    def.volume = 0.75f;
                    break;
                case "forge":
                    def.clip = Clip("Forge Upgrade SFX");
                    def.length = 0.55f;
                    def.volume = 0.7f;
                    break;
                case "armory":
                    def.clip = Clip("Armory Upgrade SFX");
                    def.volume = 0.7f;
                    break;
                case "relic":
                    def.clip = Clip("Gear Pick up and Equip SFX");
                    def.volume = 0.8f;
                    break;
                case "equip":
                    def.clip = Clip("Gear Pick up and Equip SFX");
                    def.volume = 0.65f;
                    break;
                case "twoHand":
                    def.clip = Clip("2Handed hit armored enemy");
                    def.length = 0.45f;
                    def.volume = 0.75f;
                    break;
                case "ascend":
                    def.clip = Clip("Epic BGM Segment (Ascended+Glory)");
                    def.length = 4.5f;
                    def.volume = 0.6f;
                    break;
            }

            return def;
        }

        static AudioClip Clip(string name)
        {
            return Resources.Load<AudioClip>(name);
        }
    }
}
