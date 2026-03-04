using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Singleton GameManager — monitors tank states, triggers win/loss, manages game flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private float gameOverDelay = 1.5f;
        [SerializeField] private string mainMenuScene = "MainMenu";

        // Runtime references (found on Start)
        private TankController _playerTank;
        private List<TankController> _enemyTanks = new List<TankController>();

        // UI (created procedurally)
        private Canvas _overlayCanvas;
        private GameObject _winPanel;
        private GameObject _lossPanel;
        private Text _winText;
        private Text _lossText;

        private bool _gameOver;

        public bool GameOver => _gameOver;
        public int EnemiesRemaining
        {
            get
            {
                int count = 0;
                foreach (var t in _enemyTanks)
                    if (t != null && t.gameObject.activeSelf) count++;
                return count;
            }
        }

        // ── Events ────────────────────────────────────────────────────────────
        public event System.Action<int> OnEnemyCountChanged;   // enemies remaining
        public event System.Action OnPlayerWon;
        public event System.Action OnPlayerLost;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Find tanks by tag
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _playerTank = playerObj.GetComponent<TankController>();

            foreach (var go in GameObject.FindGameObjectsWithTag("Enemy"))
            {
                var tc = go.GetComponent<TankController>();
                if (tc != null) _enemyTanks.Add(tc);
            }

            Debug.Log($"[GameManager] Player: {(_playerTank != null ? _playerTank.name : "NONE")}, Enemies: {_enemyTanks.Count}");

            BuildOverlayUI();
            StartCoroutine(MonitorGame());
        }

        // ── Build UI ─────────────────────────────────────────────────────────
        private void BuildOverlayUI()
        {
            // Canvas
            var canvasGO = new GameObject("GameOverCanvas");
            _overlayCanvas = canvasGO.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            _winPanel  = BuildPanel(canvasGO, "YOU WIN!",    new Color(0.1f, 0.7f, 0.2f, 0.92f));
            _lossPanel = BuildPanel(canvasGO, "GAME OVER",   new Color(0.7f, 0.1f, 0.1f, 0.92f));

            _winPanel.SetActive(false);
            _lossPanel.SetActive(false);
        }

        private GameObject BuildPanel(GameObject parent, string title, Color bg)
        {
            // Background
            var panel = new GameObject(title + "_Panel");
            panel.transform.SetParent(parent.transform, false);
            var img = panel.AddComponent<Image>();
            img.color = bg;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Title label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(panel.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.text = title;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 72;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            var lr = labelGO.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.1f, 0.55f);
            lr.anchorMax = new Vector2(0.9f, 0.85f);
            lr.offsetMin = lr.offsetMax = Vector2.zero;

            // Restart button
            var btnGO = new GameObject("RestartButton");
            btnGO.transform.SetParent(panel.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(1f, 1f, 1f, 0.25f);
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(RestartGame);
            var br = btnGO.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0.3f, 0.25f);
            br.anchorMax = new Vector2(0.7f, 0.45f);
            br.offsetMin = br.offsetMax = Vector2.zero;

            var btnLabelGO = new GameObject("BtnLabel");
            btnLabelGO.transform.SetParent(btnGO.transform, false);
            var btnLabel = btnLabelGO.AddComponent<Text>();
            btnLabel.text = "PLAY AGAIN";
            btnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnLabel.fontSize = 36;
            btnLabel.fontStyle = FontStyle.Bold;
            btnLabel.alignment = TextAnchor.MiddleCenter;
            btnLabel.color = Color.white;
            var blr = btnLabelGO.GetComponent<RectTransform>();
            blr.anchorMin = Vector2.zero;
            blr.anchorMax = Vector2.one;
            blr.offsetMin = blr.offsetMax = Vector2.zero;

            return panel;
        }

        // ── Monitor coroutine ─────────────────────────────────────────────────
        private IEnumerator MonitorGame()
        {
            int lastEnemyCount = _enemyTanks.Count;

            while (!_gameOver)
            {
                yield return new WaitForSeconds(0.25f);

                // Check enemy count change
                int current = EnemiesRemaining;
                if (current != lastEnemyCount)
                {
                    lastEnemyCount = current;
                    OnEnemyCountChanged?.Invoke(current);
                }

                // Win: all enemies gone
                if (current == 0 && _enemyTanks.Count > 0)
                {
                    StartCoroutine(TriggerGameOver(won: true));
                    yield break;
                }

                // Loss: player gone
                if (_playerTank != null && !_playerTank.gameObject.activeSelf)
                {
                    StartCoroutine(TriggerGameOver(won: false));
                    yield break;
                }
            }
        }

        private IEnumerator TriggerGameOver(bool won)
        {
            _gameOver = true;
            yield return new WaitForSeconds(gameOverDelay);

            if (won)
            {
                _winPanel.SetActive(true);
                OnPlayerWon?.Invoke();
                Debug.Log("[GameManager] PLAYER WON");
            }
            else
            {
                _lossPanel.SetActive(true);
                OnPlayerLost?.Invoke();
                Debug.Log("[GameManager] PLAYER LOST");
            }
        }

        // ── Actions ───────────────────────────────────────────────────────────
        public void RestartGame()
        {
            _gameOver = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
