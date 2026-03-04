using System;
using System.Reflection;
using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Handles player firing cadence, projectile spawning, and projectile powerup flags.
    /// Must live on the same GameObject as TankController.
    /// </summary>
    [DisallowMultipleComponent]
    // RequireComponent removed for editor-script compatibility
    public class WeaponController : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float fireRate = 0.5f;
        [SerializeField] private float bulletSpeed = 15f;

        [Header("Audio")]
        [SerializeField] private AudioClip sfxShot;

        [Header("Powerups")]
        [SerializeField] private PowerupManager powerupManager;

        private TankController _tankController;
        private float _nextFireTime;

        private void Awake()
        {
            _tankController = GetComponent<TankController>();

            if (powerupManager == null)
            {
                powerupManager = FindFirstObjectByType<PowerupManager>();
            }
        }

        /// <summary>
        /// Called by TankController when fire input is pressed.
        /// </summary>
        public void Fire()
        {
            if (Time.time < _nextFireTime)
            {
                return;
            }

            if (_tankController == null || _tankController.FirePoint == null)
            {
                Debug.LogWarning("[WeaponController] Missing TankController or FirePoint reference.");
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogWarning("[WeaponController] projectilePrefab is not assigned.");
                return;
            }

            Transform firePoint = _tankController.FirePoint;
            GameObject projectileObject = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            Rigidbody projectileRigidbody = projectileObject.GetComponent<Rigidbody>();
            if (projectileRigidbody != null)
            {
                projectileRigidbody.velocity = firePoint.forward * bulletSpeed;
            }

            Projectile projectileComponent = projectileObject.GetComponent<Projectile>();
            if (projectileComponent != null)
            {
                string playerId = _tankController.PlayerId;
                projectileComponent.isRicochet = IsPowerupActive(playerId, PowerupManager.RicochetPowerup);
                projectileComponent.isBlockBreaker = IsPowerupActive(playerId, PowerupManager.BlockbreakerPowerup);
            }

            TryPlayShotSfx();
            _nextFireTime = Time.time + Mathf.Max(0.01f, fireRate);
        }

        private bool IsPowerupActive(string playerId, string powerupKey)
        {
            return powerupManager != null && powerupManager.IsPowerupActive(playerId, powerupKey);
        }

        private void TryPlayShotSfx()
        {
            // Requirement: AudioManager.Instance.PlaySFX(sfxShot) if AudioManager.Instance != null.
            // AudioManager may exist in another assembly/package; use reflection so this class compiles
            // even when AudioManager is absent in this branch.
            Type audioManagerType = FindTypeInLoadedAssemblies("AudioManager");
            if (audioManagerType == null)
            {
                return;
            }

            PropertyInfo instanceProperty = audioManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            object audioManagerInstance = instanceProperty?.GetValue(null);
            if (audioManagerInstance == null)
            {
                return;
            }

            MethodInfo playSfxMethod = audioManagerType.GetMethod("PlaySFX", BindingFlags.Public | BindingFlags.Instance);
            if (playSfxMethod != null)
            {
                playSfxMethod.Invoke(audioManagerInstance, new object[] { sfxShot });
            }
        }

        private static Type FindTypeInLoadedAssemblies(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type found = assemblies[i].GetType(typeName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
