using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankRoyale.Menu
{
    /// <summary>
    /// Minimal shell controller for main menu button actions.
    /// Wire these public methods to UI Button onClick events in Unity.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [SerializeField] private string gameplaySceneName = "Gameplay";

        [Header("Optional Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            ShowMainMenu();
        }

        public void OnPlayPressed()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogError("[MainMenuController] Gameplay scene name is empty.");
                return;
            }

            // TODO(MENU-101): Add transition animation/loading screen hook.
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnSettingsPressed()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void OnBackToMainMenuPressed()
        {
            ShowMainMenu();
        }

        public void OnQuitPressed()
        {
            // TODO(MENU-101): Replace direct quit with confirmation dialog.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
    }
}
