using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Builds and drives the in-game HUD: player health bar, enemy count, active powerup display.
    /// Hooks into GameManager events. Created procedurally — no prefab required.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("HUD Settings")]
        [SerializeField] private Color healthFull     = new Color(0.15f, 0.85f, 0.2f);
        [SerializeField] private Color healthMid      = new Color(0.95f, 0.8f, 0.05f);
        [SerializeField] private Color healthLow      = new Color(0.9f, 0.15f, 0.1f);
        [SerializeField] private Color hudBackground  = new Color(0f, 0f, 0f, 0.55f);

        // Runtime
        private TankController _playerTank;
        private int _playerMaxHealth;
        private Image _healthFill;
        private Text _healthLabel;
        private Text _enemyCountText;
        private Text _powerupText;

        private Canvas _hud;

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                _playerTank = playerGO.GetComponent<TankController>();
                _playerMaxHealth = _playerTank != null ? _playerTank.MaxHealth : 3;
            }

            BuildHUD();

            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyCountChanged += UpdateEnemyCount;
            }
        }

        private void Update()
        {
            if (_playerTank == null) return;
            UpdateHealthBar();
            UpdatePowerupDisplay();
        }

        // ── Build ─────────────────────────────────────────────────────────────
        private void BuildHUD()
        {
            var canvasGO = new GameObject("HUDCanvas");
            _hud = canvasGO.AddComponent<Canvas>();
            _hud.renderMode = RenderMode.ScreenSpaceOverlay;
            _hud.sortingOrder = 50;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            BuildHealthBar(canvasGO);
            BuildEnemyCounter(canvasGO);
            BuildPowerupDisplay(canvasGO);
        }

        // ── Health bar (bottom-left) ──────────────────────────────────────────
        private void BuildHealthBar(GameObject parent)
        {
            // Container
            var container = MakeRect("HealthContainer", parent, new Vector2(0,0), new Vector2(0,0),
                new Vector2(20, 20), new Vector2(300, 50));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            // Label
            var lblGO = MakeRect("HPLabel", container.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _healthLabel = lblGO.AddComponent<Text>();
            _healthLabel.text = "HP";
            _healthLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _healthLabel.fontSize = 22;
            _healthLabel.fontStyle = FontStyle.Bold;
            _healthLabel.alignment = TextAnchor.MiddleLeft;
            _healthLabel.color = Color.white;
            var lblr = lblGO.GetComponent<RectTransform>();
            lblr.anchorMin = new Vector2(0, 0); lblr.anchorMax = new Vector2(0.18f, 1);
            lblr.offsetMin = new Vector2(8,0); lblr.offsetMax = Vector2.zero;

            // Bar background
            var barBG = MakeRect("BarBG", container.gameObject, new Vector2(0.18f,0.15f), new Vector2(0.97f,0.85f), Vector2.zero, Vector2.zero);
            var barBGImg = barBG.AddComponent<Image>();
            barBGImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Bar fill
            var barFill = MakeRect("BarFill", barBG.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _healthFill = barFill.AddComponent<Image>();
            _healthFill.color = healthFull;
        }

        // ── Enemy counter (top-right) ─────────────────────────────────────────
        private void BuildEnemyCounter(GameObject parent)
        {
            var container = MakeRect("EnemyCounter", parent, new Vector2(1,1), new Vector2(1,1),
                new Vector2(-220, -70), new Vector2(-20, -20));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            _enemyCountText = container.gameObject.AddComponent<Text>();
            _enemyCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _enemyCountText.fontSize = 24;
            _enemyCountText.fontStyle = FontStyle.Bold;
            _enemyCountText.alignment = TextAnchor.MiddleCenter;
            _enemyCountText.color = Color.white;

            int startCount = GameManager.Instance != null ? GameManager.Instance.EnemiesRemaining : 3;
            _enemyCountText.text = $"🎯 Enemies: {startCount}";
        }

        // ── Powerup display (top-left) ────────────────────────────────────────
        private void BuildPowerupDisplay(GameObject parent)
        {
            var container = MakeRect("PowerupDisplay", parent, new Vector2(0,1), new Vector2(0,1),
                new Vector2(20, -70), new Vector2(220, -20));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            _powerupText = container.gameObject.AddComponent<Text>();
            _powerupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _powerupText.fontSize = 20;
            _powerupText.alignment = TextAnchor.MiddleCenter;
            _powerupText.color = Color.white;
            _powerupText.text = "";
        }

        // ── Update ────────────────────────────────────────────────────────────
        private void UpdateHealthBar()
        {
            if (_healthFill == null || _playerTank == null) return;

            float ratio = _playerMaxHealth > 0
                ? (float)_playerTank.CurrentHealth / _playerMaxHealth
                : 0f;

            // Scale fill
            _healthFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);

            // Color
            _healthFill.color = ratio > 0.6f ? healthFull : ratio > 0.3f ? healthMid : healthLow;

            if (_healthLabel != null)
                _healthLabel.text = $"HP {_playerTank.CurrentHealth}/{_playerMaxHealth}";
        }

        private void UpdateEnemyCount(int count)
        {
            if (_enemyCountText != null)
                _enemyCountText.text = $"Enemies: {count}";
        }

        private void UpdatePowerupDisplay()
        {
            if (_powerupText == null) return;
            var pm = PowerupManager.Instance;
            if (pm == null) { _powerupText.text = ""; return; }

            var parts = new System.Text.StringBuilder();
            if (pm.IsPowerupActive("player", PowerupManager.RicochetPowerup))     parts.AppendLine("RICOCHET");
            if (pm.IsPowerupActive("player", PowerupManager.ArmorPowerup))        parts.AppendLine("ARMOR");
            if (pm.IsPowerupActive("player", PowerupManager.BlockbreakerPowerup)) parts.AppendLine("BREAKER");
            if (pm.IsPowerupActive("player", PowerupManager.HealPowerup))         parts.AppendLine("HEALED");

            _powerupText.text = parts.ToString().TrimEnd();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private RectTransform MakeRect(string name, GameObject parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.offsetMin  = offsetMin;
            rt.offsetMax  = offsetMax;
            return rt;
        }
    }
}
