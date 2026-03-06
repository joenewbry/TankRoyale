using UnityEngine;
using UnityEngine.UI;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Builds and drives the in-game HUD.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("HUD Settings")]
        [SerializeField] private Color healthFull = new Color(0.15f, 0.85f, 0.2f);
        [SerializeField] private Color healthMid = new Color(0.95f, 0.8f, 0.05f);
        [SerializeField] private Color healthLow = new Color(0.9f, 0.15f, 0.1f);
        [SerializeField] private Color hudBackground = new Color(0f, 0f, 0f, 0.55f);

        // Runtime
        private TankController _playerTank;
        private int _playerMaxHealth;
        private Image _healthFill;
        private Text _healthLabel;
        private Text _enemyCountText;
        private Text _powerupText;
        private Text _streakText;
        private Text _upgradePopupText;
        private Text _ammoText;
        private float _popupHideAt;
        private WeaponController _weaponController;

        private Canvas _hud;

        private void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                _playerTank = playerGO.GetComponent<TankController>();
                _playerMaxHealth = _playerTank != null ? _playerTank.MaxHealth : 3;
                _weaponController = playerGO.GetComponent<WeaponController>();
            }

            BuildHUD();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyCountChanged += UpdateEnemyCount;
            }

            if (KillstreakManager.Instance != null)
            {
                KillstreakManager.Instance.OnStreakChanged += HandleStreakChanged;
                KillstreakManager.Instance.OnUpgradeActivated += HandleUpgradeActivated;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnemyCountChanged -= UpdateEnemyCount;
            }

            if (KillstreakManager.Instance != null)
            {
                KillstreakManager.Instance.OnStreakChanged -= HandleStreakChanged;
                KillstreakManager.Instance.OnUpgradeActivated -= HandleUpgradeActivated;
            }
        }

        private void Update()
        {
            if (_playerTank != null)
            {
                UpdateHealthBar();
                UpdatePowerupDisplay();
                UpdateStreakDisplay();
                UpdateAmmoDisplay();
            }

            if (_upgradePopupText != null)
            {
                _upgradePopupText.enabled = Time.time < _popupHideAt;
            }
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
            BuildAmmoDisplay(canvasGO);
            BuildStreakCounter(canvasGO);
            BuildUpgradePopup(canvasGO);
        }

        private void BuildHealthBar(GameObject parent)
        {
            var container = MakeRect("HealthContainer", parent, new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(20, 20), new Vector2(300, 50));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            var lblGO = MakeRect("HPLabel", container.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _healthLabel = lblGO.AddComponent<Text>();
            _healthLabel.text = "HP";
            _healthLabel.font = UIUtility.GetBuiltinFont();
            _healthLabel.fontSize = 22;
            _healthLabel.fontStyle = FontStyle.Bold;
            _healthLabel.alignment = TextAnchor.MiddleLeft;
            _healthLabel.color = Color.white;
            var lblr = lblGO.GetComponent<RectTransform>();
            lblr.anchorMin = new Vector2(0, 0);
            lblr.anchorMax = new Vector2(0.18f, 1);
            lblr.offsetMin = new Vector2(8, 0);
            lblr.offsetMax = Vector2.zero;

            var barBG = MakeRect("BarBG", container.gameObject, new Vector2(0.18f, 0.15f), new Vector2(0.97f, 0.85f), Vector2.zero, Vector2.zero);
            var barBGImg = barBG.AddComponent<Image>();
            barBGImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            var barFill = MakeRect("BarFill", barBG.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _healthFill = barFill.AddComponent<Image>();
            _healthFill.color = healthFull;
        }

        private void BuildEnemyCounter(GameObject parent)
        {
            var container = MakeRect("EnemyCounter", parent, new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-220, -70), new Vector2(-20, -20));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            _enemyCountText = container.gameObject.AddComponent<Text>();
            _enemyCountText.font = UIUtility.GetBuiltinFont();
            _enemyCountText.fontSize = 24;
            _enemyCountText.fontStyle = FontStyle.Bold;
            _enemyCountText.alignment = TextAnchor.MiddleCenter;
            _enemyCountText.color = Color.white;

            int startCount = GameManager.Instance != null ? GameManager.Instance.EnemiesRemaining : 3;
            _enemyCountText.text = $"Enemies: {startCount}";
        }

        private void BuildPowerupDisplay(GameObject parent)
        {
            var container = MakeRect("PowerupDisplay", parent, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(20, -70), new Vector2(280, -20));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            _powerupText = container.gameObject.AddComponent<Text>();
            _powerupText.font = UIUtility.GetBuiltinFont();
            _powerupText.fontSize = 20;
            _powerupText.alignment = TextAnchor.MiddleCenter;
            _powerupText.color = Color.white;
            _powerupText.text = "";
        }

        private void BuildAmmoDisplay(GameObject parent)
        {
            var container = MakeRect("AmmoDisplay", parent, new Vector2(0.35f, 0f), new Vector2(0.65f, 0f),
                new Vector2(0f, 20f), new Vector2(0f, 70f));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            _ammoText = container.gameObject.AddComponent<Text>();
            _ammoText.font = UIUtility.GetBuiltinFont();
            _ammoText.fontSize = 22;
            _ammoText.fontStyle = FontStyle.Bold;
            _ammoText.alignment = TextAnchor.MiddleCenter;
            _ammoText.color = Color.white;
            _ammoText.text = "BULLET[LMB]  MISSILE[RMB]";
        }

        private void BuildStreakCounter(GameObject parent)
        {
            var container = MakeRect("StreakCounter", parent, new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-280, 20), new Vector2(-20, 70));
            var bg = container.AddComponent<Image>();
            bg.color = hudBackground;

            _streakText = container.gameObject.AddComponent<Text>();
            _streakText.font = UIUtility.GetBuiltinFont();
            _streakText.fontSize = 22;
            _streakText.fontStyle = FontStyle.Bold;
            _streakText.alignment = TextAnchor.MiddleCenter;
            _streakText.color = Color.white;
            _streakText.text = "STREAK: 0";
        }

        private void BuildUpgradePopup(GameObject parent)
        {
            var popup = MakeRect("UpgradePopup", parent, new Vector2(0.25f, 0.72f), new Vector2(0.75f, 0.88f), Vector2.zero, Vector2.zero);
            _upgradePopupText = popup.gameObject.AddComponent<Text>();
            _upgradePopupText.font = UIUtility.GetBuiltinFont();
            _upgradePopupText.fontSize = 42;
            _upgradePopupText.fontStyle = FontStyle.Bold;
            _upgradePopupText.alignment = TextAnchor.MiddleCenter;
            _upgradePopupText.color = new Color(0.2f, 0.9f, 1f, 1f);
            _upgradePopupText.enabled = false;
            _upgradePopupText.text = string.Empty;
        }

        // ── Update ────────────────────────────────────────────────────────────
        private void UpdateHealthBar()
        {
            if (_healthFill == null || _playerTank == null) return;

            float ratio = _playerMaxHealth > 0
                ? (float)_playerTank.CurrentHealth / _playerMaxHealth
                : 0f;

            _healthFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
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
            if (_powerupText == null || _playerTank == null) return;
            var pm = PowerupManager.Instance;
            if (pm == null) { _powerupText.text = ""; return; }

            string playerId = _playerTank.PlayerId;
            var parts = new System.Text.StringBuilder();

            if (pm.IsPowerupActive(playerId, PowerupManager.RicochetPowerup)) parts.AppendLine("RICOCHET");
            if (pm.IsPowerupActive(playerId, PowerupManager.ArmorPowerup)) parts.AppendLine("ARMOR");
            if (pm.IsPowerupActive(playerId, PowerupManager.BlockbreakerPowerup)) parts.AppendLine("BREAKER");
            if (pm.IsPowerupActive(playerId, PowerupManager.SpeedBoostPowerup)) parts.AppendLine("SPEED BOOST");
            if (pm.IsPowerupActive(playerId, PowerupManager.LootMagnetPowerup)) parts.AppendLine("LOOT MAGNET");
            if (pm.IsPowerupActive(playerId, PowerupManager.DoubleBarrelPowerup)) parts.AppendLine("DOUBLE BARREL");

            _powerupText.text = parts.ToString().TrimEnd();
        }

        private void UpdateStreakDisplay()
        {
            if (_streakText == null || _playerTank == null || KillstreakManager.Instance == null) return;
            int streak = KillstreakManager.Instance.GetStreak(_playerTank.PlayerId);
            _streakText.text = $"STREAK: {streak}";
        }

        private void UpdateAmmoDisplay()
        {
            if (_ammoText == null)
            {
                return;
            }

            if (_weaponController == null && _playerTank != null)
            {
                _weaponController = _playerTank.GetComponent<WeaponController>();
            }

            if (_weaponController == null)
            {
                _ammoText.text = "BULLET[LMB]  MISSILE[RMB]";
                return;
            }

            _ammoText.text = $"BULLET[LMB] INF   MISSILE[RMB] {_weaponController.MissilesRemaining}/{_weaponController.MissileCapacity}";
        }

        private void HandleStreakChanged(string playerId, int streak)
        {
            if (_playerTank == null || playerId != _playerTank.PlayerId) return;
            if (_streakText != null)
            {
                _streakText.text = $"STREAK: {streak}";
            }
        }

        private void HandleUpgradeActivated(string playerId, string upgradeKey)
        {
            if (_playerTank == null || playerId != _playerTank.PlayerId || _upgradePopupText == null) return;

            string label = upgradeKey switch
            {
                var k when k == PowerupManager.SpeedBoostPowerup => "KILLSTREAK: SPEED BOOST ACTIVATED",
                var k when k == PowerupManager.LootMagnetPowerup => "KILLSTREAK: LOOT MAGNET ACTIVATED",
                var k when k == PowerupManager.DoubleBarrelPowerup => "KILLSTREAK: DOUBLE BARREL ACTIVATED",
                _ => "KILLSTREAK UPGRADE ACTIVATED"
            };

            _upgradePopupText.text = label;
            _popupHideAt = Time.time + 1.8f;
            _upgradePopupText.enabled = true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private RectTransform MakeRect(string name, GameObject parent,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }
    }

    public static class RectTransformExtensions
    {
        public static T AddComponent<T>(this RectTransform rt) where T : Component
        {
            return rt.gameObject.AddComponent<T>();
        }
    }
}
