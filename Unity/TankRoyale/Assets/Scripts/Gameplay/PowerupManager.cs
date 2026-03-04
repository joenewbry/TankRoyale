using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Tracks active powerups per player by string keys.
    /// Effects are implemented separately.
    /// </summary>
    public class PowerupManager : MonoBehaviour
    {
        public static PowerupManager Instance { get; private set; }

        public const string RicochetPowerup     = "ricochet";
        public const string ArmorPowerup        = "armor";
        public const string BlockbreakerPowerup = "blockbreaker";
        public const string HealPowerup         = "heal";

        // playerId -> (powerupKey -> isActive)
        private readonly Dictionary<string, Dictionary<string, bool>> _activePowerupsByPlayer = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ApplyPowerup(string playerId, string powerupKey)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                Debug.LogWarning("[PowerupManager] Cannot apply powerup to an empty playerId.");
                return;
            }

            if (string.IsNullOrWhiteSpace(powerupKey))
            {
                Debug.LogWarning($"[PowerupManager] Cannot apply an empty powerup key to player '{playerId}'.");
                return;
            }

            if (!_activePowerupsByPlayer.TryGetValue(playerId, out Dictionary<string, bool> playerPowerups))
            {
                playerPowerups = new Dictionary<string, bool>();
                _activePowerupsByPlayer[playerId] = playerPowerups;
            }

            playerPowerups[powerupKey] = true;
            Debug.Log($"[PowerupManager] Applied powerup '{powerupKey}' to player '{playerId}'.");
        }

        public bool IsPowerupActive(string playerId, string powerupKey)
        {
            return _activePowerupsByPlayer.TryGetValue(playerId, out Dictionary<string, bool> playerPowerups)
                   && playerPowerups.TryGetValue(powerupKey, out bool isActive)
                   && isActive;
        }

        public bool RemovePowerup(string playerId, string powerupKey)
        {
            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(powerupKey))
            {
                return false;
            }

            if (!_activePowerupsByPlayer.TryGetValue(playerId, out Dictionary<string, bool> playerPowerups))
            {
                return false;
            }

            bool removed = playerPowerups.Remove(powerupKey);
            if (playerPowerups.Count == 0)
            {
                _activePowerupsByPlayer.Remove(playerId);
            }

            return removed;
        }

        public void ClearPlayerPowerups(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            _activePowerupsByPlayer.Remove(playerId);
        }
    }
}
