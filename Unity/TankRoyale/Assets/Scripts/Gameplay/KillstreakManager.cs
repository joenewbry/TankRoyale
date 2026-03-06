using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Tracks player killstreaks and applies streak-based upgrades.
    /// 1 kill: speed boost
    /// 2 kills: loot magnet
    /// 3 kills: double barrel
    /// </summary>
    public class KillstreakManager : MonoBehaviour
    {
        public static KillstreakManager Instance { get; private set; }

        [Header("Durations")]
        [SerializeField] private float speedBoostDuration = 10f;
        [SerializeField] private float lootMagnetDuration = 12f;
        [SerializeField] private float tier3Duration = 12f;

        private readonly Dictionary<string, int> _streaks = new();

        public event System.Action<string, int> OnStreakChanged;           // playerId, streak
        public event System.Action<string, string> OnUpgradeActivated;     // playerId, powerupKey

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public int GetStreak(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return 0;
            return _streaks.TryGetValue(playerId, out int streak) ? streak : 0;
        }

        public void RegisterKill(string killerPlayerId)
        {
            if (string.IsNullOrWhiteSpace(killerPlayerId)) return;

            int next = GetStreak(killerPlayerId) + 1;
            _streaks[killerPlayerId] = next;
            OnStreakChanged?.Invoke(killerPlayerId, next);

            ApplyUpgradeForStreak(killerPlayerId, next);
        }

        public void ResetStreak(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;
            if (!_streaks.ContainsKey(playerId)) return;

            _streaks[playerId] = 0;
            OnStreakChanged?.Invoke(playerId, 0);
        }

        private void ApplyUpgradeForStreak(string playerId, int streak)
        {
            PowerupManager pm = PowerupManager.Instance;
            if (pm == null) return;

            if (streak >= 1)
            {
                pm.ApplyTimedPowerup(playerId, PowerupManager.SpeedBoostPowerup, speedBoostDuration);
                OnUpgradeActivated?.Invoke(playerId, PowerupManager.SpeedBoostPowerup);
            }

            if (streak >= 2)
            {
                pm.ApplyTimedPowerup(playerId, PowerupManager.LootMagnetPowerup, lootMagnetDuration);
                OnUpgradeActivated?.Invoke(playerId, PowerupManager.LootMagnetPowerup);
            }

            if (streak >= 3)
            {
                pm.ApplyTimedPowerup(playerId, PowerupManager.DoubleBarrelPowerup, tier3Duration);
                OnUpgradeActivated?.Invoke(playerId, PowerupManager.DoubleBarrelPowerup);
            }
        }
    }
}
