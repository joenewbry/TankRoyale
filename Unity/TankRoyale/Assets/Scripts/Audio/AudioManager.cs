using UnityEngine;

namespace TankRoyale.Audio
{
    /// <summary>
    /// Global audio controller with pooled SFX sources and a dedicated music source.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        private const int SfxPoolSize = 10;

        public static AudioManager Instance { get; private set; }

        [Header("Global Sound Library")]
        [SerializeField] private SoundLibrary soundLibrary;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip sfxShot;
        [SerializeField] private AudioClip sfxExplosion;
        [SerializeField] private AudioClip sfxBlockDestroy;
        [SerializeField] private AudioClip sfxPowerupPickup;
        [SerializeField] private AudioClip sfxArmorActivate;
        [SerializeField] private AudioClip sfxTankEngine;

        [Header("Music Clips")]
        [SerializeField] private AudioClip musicMain;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        private readonly AudioSource[] _sfxSources = new AudioSource[SfxPoolSize];
        private AudioSource _musicSource;
        private int _nextSfxSourceIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResolveSoundLibrary();
            CacheClipsFromLibrary();
            BuildAudioSourcePool();
            RefreshVolumes();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource source = _sfxSources[_nextSfxSourceIndex];
            _nextSfxSourceIndex = (_nextSfxSourceIndex + 1) % SfxPoolSize;

            source.loop = false;
            source.clip = clip;
            source.volume = sfxVolume * masterVolume;
            source.Play();
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null || _musicSource == null)
            {
                return;
            }

            if (_musicSource.clip != clip)
            {
                _musicSource.clip = clip;
            }

            _musicSource.loop = loop;
            _musicSource.volume = masterVolume;

            if (!_musicSource.isPlaying)
            {
                _musicSource.Play();
            }
        }

        public void StopMusic()
        {
            if (_musicSource == null)
            {
                return;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            RefreshVolumes();
        }

        public void SetSFXVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            RefreshVolumes();
        }

        private void ResolveSoundLibrary()
        {
            if (soundLibrary != null)
            {
                return;
            }

            soundLibrary = SoundLibrary.GlobalInstance;

            if (soundLibrary == null)
            {
                soundLibrary = Resources.Load<SoundLibrary>("SoundLibrary");
            }

            if (soundLibrary == null)
            {
                soundLibrary = SoundLibrary.GetOrCreateGlobalInstance();
            }
        }

        private void CacheClipsFromLibrary()
        {
            if (soundLibrary == null)
            {
                return;
            }

            sfxShot = soundLibrary.SfxShot;
            sfxExplosion = soundLibrary.SfxExplosion;
            sfxBlockDestroy = soundLibrary.SfxBlockDestroy;
            sfxPowerupPickup = soundLibrary.SfxPowerupPickup;
            sfxArmorActivate = soundLibrary.SfxArmorActivate;
            sfxTankEngine = soundLibrary.SfxTankEngine;
            musicMain = soundLibrary.MusicMain;
        }

        private void BuildAudioSourcePool()
        {
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                _sfxSources[i] = source;
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
        }

        private void RefreshVolumes()
        {
            float normalizedMaster = Mathf.Clamp01(masterVolume);
            float normalizedSfx = Mathf.Clamp01(sfxVolume);
            float sfxOutputVolume = normalizedMaster * normalizedSfx;

            for (int i = 0; i < _sfxSources.Length; i++)
            {
                AudioSource source = _sfxSources[i];
                if (source != null)
                {
                    source.volume = sfxOutputVolume;
                }
            }

            if (_musicSource != null)
            {
                _musicSource.volume = normalizedMaster;
            }
        }

        // Optional helpers for known game events.
        public void PlayShotSFX() => PlaySFX(sfxShot);
        public void PlayExplosionSFX() => PlaySFX(sfxExplosion);
        public void PlayBlockDestroySFX() => PlaySFX(sfxBlockDestroy);
        public void PlayPowerupPickupSFX() => PlaySFX(sfxPowerupPickup);
        public void PlayArmorActivateSFX() => PlaySFX(sfxArmorActivate);
        public void PlayTankEngineSFX() => PlaySFX(sfxTankEngine);
        public void PlayMainMusic(bool loop = true) => PlayMusic(musicMain, loop);
    }
}
