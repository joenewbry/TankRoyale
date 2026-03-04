using UnityEngine;

namespace TankRoyale.Menu
{
    /// <summary>
    /// Minimal settings shell to persist options and expose UI callbacks.
    /// </summary>
    public class SettingsMenuController : MonoBehaviour
    {
        private const string MasterVolumePref = "settings.masterVolume";
        private const string FullscreenPref = "settings.fullscreen";
        private const string QualityPref = "settings.qualityIndex";

        [Header("Optional Panels")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject mainMenuPanel;

        [Header("Default Values")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultMasterVolume = 1f;

        private float _masterVolume;
        private bool _fullscreen;
        private int _qualityIndex;

        private void Awake()
        {
            LoadSavedSettings();
        }

        public float CurrentMasterVolume => _masterVolume;
        public bool IsFullscreen => _fullscreen;
        public int CurrentQualityIndex => _qualityIndex;

        public void OnMasterVolumeChanged(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumePref, _masterVolume);
            PlayerPrefs.Save();

            // TODO(MENU-101): Route volume to AudioMixer group.
        }

        public void OnFullscreenToggled(bool isFullscreen)
        {
            _fullscreen = isFullscreen;
            Screen.fullScreen = _fullscreen;

            PlayerPrefs.SetInt(FullscreenPref, _fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void OnQualityLevelChanged(int qualityIndex)
        {
            int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
            _qualityIndex = Mathf.Clamp(qualityIndex, 0, maxQualityIndex);

            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(_qualityIndex, true);
            }

            PlayerPrefs.SetInt(QualityPref, _qualityIndex);
            PlayerPrefs.Save();
        }

        public void OnBackPressed()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
        }

        private void LoadSavedSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(MasterVolumePref, defaultMasterVolume);
            _fullscreen = PlayerPrefs.GetInt(FullscreenPref, Screen.fullScreen ? 1 : 0) == 1;

            int maxQualityIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
            int defaultQuality = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, maxQualityIndex);
            _qualityIndex = PlayerPrefs.GetInt(QualityPref, defaultQuality);
            _qualityIndex = Mathf.Clamp(_qualityIndex, 0, maxQualityIndex);

            // Apply loaded values.
            Screen.fullScreen = _fullscreen;
            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(_qualityIndex, true);
            }

            // TODO(MENU-101): Push loaded values into bound UI elements on scene start.
        }
    }
}
