using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Tracks active powerups per player by string keys.
    /// Supports optional timed powerups.
    /// </summary>
    public class PowerupManager : MonoBehaviour
    {
        public static PowerupManager Instance { get; private set; }

        public const string RicochetPowerup       = "ricochet";
        public const string ArmorPowerup          = "armor";
        public const string BlockbreakerPowerup   = "blockbreaker";
        public const string HealPowerup           = "heal";

        // Killstreak upgrades
        public const string SpeedBoostPowerup     = "speed_boost";
        public const string LootMagnetPowerup     = "loot_magnet";
        public const string DoubleBarrelPowerup   = "double_barrel";
        public const string ExplosiveRoundsPowerup= "explosive_rounds";

        // playerId -> (powerupKey -> isActive)
        private readonly Dictionary<string, Dictionary<string, bool>> _activePowerupsByPlayer = new();

        // (playerId:powerupKey) -> running timer coroutine
        private readonly Dictionary<string, Coroutine> _timers = new();

        public event System.Action<string, string> OnPowerupApplied;
        public event System.Action<string, string> OnPowerupRemoved;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ApplyPowerup(string playerId, string powerupKey)
        {
            if (!Validate(playerId, powerupKey)) return;

            if (!_activePowerupsByPlayer.TryGetValue(playerId, out Dictionary<string, bool> playerPowerups))
            {
                playerPowerups = new Dictionary<string, bool>();
                _activePowerupsByPlayer[playerId] = playerPowerups;
            }

            playerPowerups[powerupKey] = true;
            OnPowerupApplied?.Invoke(playerId, powerupKey);
            Debug.Log($"[PowerupManager] Applied '{powerupKey}' to '{playerId}'.");
        }

        public void ApplyTimedPowerup(string playerId, string powerupKey, float durationSeconds)
        {
            if (!Validate(playerId, powerupKey)) return;

            ApplyPowerup(playerId, powerupKey);

            string timerKey = BuildTimerKey(playerId, powerupKey);
            if (_timers.TryGetValue(timerKey, out Coroutine existing) && existing != null)
            {
                StopCoroutine(existing);
            }

            _timers[timerKey] = StartCoroutine(RemoveAfterDelay(playerId, powerupKey, Mathf.Max(0.05f, durationSeconds), timerKey));
        }

        public bool IsPowerupActive(string playerId, string powerupKey)
        {
            return _activePowerupsByPlayer.TryGetValue(playerId, out Dictionary<string, bool> playerPowerups)
                   && playerPowerups.TryGetValue(powerupKey, out bool isActive)
                   && isActive;
        }

        public bool RemovePowerup(string playerId, string powerupKey)
        {
            if (!Validate(playerId, powerupKey)) return false;
            if (!_activePowerupsByPlayer.TryGetValue(playerId, out Dictionary<string, bool> playerPowerups)) return false;

            bool removed = playerPowerups.Remove(powerupKey);
            if (playerPowerups.Count == 0)
            {
                _activePowerupsByPlayer.Remove(playerId);
            }

            string timerKey = BuildTimerKey(playerId, powerupKey);
            if (_timers.TryGetValue(timerKey, out Coroutine timer) && timer != null)
            {
                StopCoroutine(timer);
                _timers.Remove(timerKey);
            }

            if (removed)
            {
                OnPowerupRemoved?.Invoke(playerId, powerupKey);
            }

            return removed;
        }

        public void ClearPlayerPowerups(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;

            if (_activePowerupsByPlayer.TryGetValue(playerId, out var playerPowerups))
            {
                foreach (string key in new List<string>(playerPowerups.Keys))
                {
                    RemovePowerup(playerId, key);
                }
            }

            _activePowerupsByPlayer.Remove(playerId);
        }

        private IEnumerator RemoveAfterDelay(string playerId, string powerupKey, float durationSeconds, string timerKey)
        {
            yield return new WaitForSeconds(durationSeconds);
            RemovePowerup(playerId, powerupKey);
            _timers.Remove(timerKey);
        }

        private static bool Validate(string playerId, string powerupKey)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                Debug.LogWarning("[PowerupManager] playerId cannot be empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(powerupKey))
            {
                Debug.LogWarning($"[PowerupManager] powerupKey cannot be empty for player '{playerId}'.");
                return false;
            }

            return true;
        }

        private static string BuildTimerKey(string playerId, string powerupKey)
        {
            return $"{playerId}:{powerupKey}";
        }
    }
}
