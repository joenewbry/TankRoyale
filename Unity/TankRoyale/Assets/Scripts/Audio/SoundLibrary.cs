using UnityEngine;

namespace TankRoyale.Audio
{
    /// <summary>
    /// Global ScriptableObject containing references to all game audio clips.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "TankRoyale/Audio/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        public static SoundLibrary GlobalInstance { get; private set; }

        public static SoundLibrary GetOrCreateGlobalInstance()
        {
            if (GlobalInstance == null)
            {
                GlobalInstance = CreateInstance<SoundLibrary>();
                GlobalInstance.name = "RuntimeSoundLibrary";
            }

            return GlobalInstance;
        }

        [Header("SFX")]
        [SerializeField] private AudioClip sfxShot;
        [SerializeField] private AudioClip sfxExplosion;
        [SerializeField] private AudioClip sfxBlockDestroy;
        [SerializeField] private AudioClip sfxPowerupPickup;
        [SerializeField] private AudioClip sfxArmorActivate;
        [SerializeField] private AudioClip sfxTankEngine;

        [Header("Music")]
        [SerializeField] private AudioClip musicMain;

        public AudioClip SfxShot => sfxShot;
        public AudioClip SfxExplosion => sfxExplosion;
        public AudioClip SfxBlockDestroy => sfxBlockDestroy;
        public AudioClip SfxPowerupPickup => sfxPowerupPickup;
        public AudioClip SfxArmorActivate => sfxArmorActivate;
        public AudioClip SfxTankEngine => sfxTankEngine;
        public AudioClip MusicMain => musicMain;

        private void OnEnable()
        {
            if (GlobalInstance == null || GlobalInstance == this)
            {
                GlobalInstance = this;
            }
        }
    }
}
