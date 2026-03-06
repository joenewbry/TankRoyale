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
        [SerializeField] private float fireRate = 0.15f;
        [SerializeField] private float bulletSpeed = 20f;
        [SerializeField] private bool useBallisticArc = true;
        [SerializeField] [Range(0f, 45f)] private float launchAngleDegrees = 14f;
        [SerializeField] private float fallbackSphereScale = 0.2f;
        [SerializeField] private bool useFallbackSphereWhenPrefabMissing = true;
        [SerializeField] private bool forceFallbackSphere = false;

        [Header("Feel")]
        [SerializeField] private float recoilImpulse = 1.6f;
        [SerializeField] private float dualBarrelOffset = 0.28f;

        [Header("Audio")]
        [SerializeField] private AudioClip sfxShot;

        [Header("Powerups")]
        [SerializeField] private PowerupManager powerupManager;
        [SerializeField] private bool bounceProjectilesByDefault = true;

        private TankController _tankController;
        private float _nextFireTime;
        private static PhysicsMaterial _bouncyProjectileMaterial;

        public float BulletSpeed => bulletSpeed;
        public bool UseBallisticArc => useBallisticArc;
        public float LaunchAngleDegrees => launchAngleDegrees;

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
            if (Time.time < _nextFireTime) return;

            if (_tankController == null)
            {
                _tankController = GetComponent<TankController>();
            }

            if (_tankController == null || _tankController.FirePoint == null)
            {
                Debug.LogWarning("[WeaponController] Missing TankController or FirePoint reference.");
                return;
            }

            if (!forceFallbackSphere && projectilePrefab == null && !useFallbackSphereWhenPrefabMissing)
            {
                Debug.LogWarning("[WeaponController] projectilePrefab is not assigned.");
                return;
            }

            string playerId = _tankController.PlayerId;
            bool doubleBarrel = IsPowerupActive(playerId, PowerupManager.DoubleBarrelPowerup);
            bool explosiveRounds = IsPowerupActive(playerId, PowerupManager.ExplosiveRoundsPowerup);

            if (doubleBarrel)
            {
                SpawnProjectile(playerId, -dualBarrelOffset, explosiveRounds);
                SpawnProjectile(playerId, dualBarrelOffset, explosiveRounds);
            }
            else
            {
                SpawnProjectile(playerId, 0f, explosiveRounds);
            }

            _tankController.ApplyRecoil(recoilImpulse * (doubleBarrel ? 1.2f : 1f));
            TryPlayShotSfx();
            _nextFireTime = Time.time + Mathf.Max(0.01f, fireRate);
        }

        private void SpawnProjectile(string playerId, float localRightOffset, bool explosiveRounds)
        {
            Transform firePoint = _tankController.FirePoint;
            Vector3 spawnPos = firePoint.position + (firePoint.right * localRightOffset);

            GameObject projectileObject = !forceFallbackSphere && projectilePrefab != null
                ? Instantiate(projectilePrefab, spawnPos, firePoint.rotation)
                : CreateFallbackSphereProjectile(spawnPos, firePoint.rotation);

            if (projectileObject == null)
            {
                return;
            }

            Rigidbody projectileRigidbody = projectileObject.GetComponent<Rigidbody>();
            if (projectileRigidbody == null)
            {
                projectileRigidbody = projectileObject.AddComponent<Rigidbody>();
            }

            projectileRigidbody.isKinematic = false;
            projectileRigidbody.useGravity = useBallisticArc;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectileRigidbody.mass = 0.08f;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            projectileRigidbody.linearVelocity = GetLaunchVelocity(firePoint);

            Collider collider = projectileObject.GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider generated = projectileObject.AddComponent<SphereCollider>();
                generated.radius = Mathf.Max(0.05f, fallbackSphereScale * 0.5f);
                collider = generated;
            }

            if (collider != null)
            {
                collider.material = GetBouncyProjectileMaterial();
            }

            Projectile projectileComponent = projectileObject.GetComponent<Projectile>();
            if (projectileComponent == null)
            {
                projectileComponent = projectileObject.AddComponent<Projectile>();
            }

            if (projectileComponent != null)
            {
                projectileComponent.shooterPlayerId = playerId;
                projectileComponent.isRicochet = bounceProjectilesByDefault || IsPowerupActive(playerId, PowerupManager.RicochetPowerup);
                projectileComponent.isBlockBreaker = IsPowerupActive(playerId, PowerupManager.BlockbreakerPowerup);
                projectileComponent.isExplosive = explosiveRounds;
            }
        }

        private GameObject CreateFallbackSphereProjectile(Vector3 position, Quaternion rotation)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile_Sphere";
            projectile.transform.SetPositionAndRotation(position, rotation);
            projectile.transform.localScale = Vector3.one * fallbackSphereScale;

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = projectile.AddComponent<Rigidbody>();
            }

            rb.interpolation = RigidbodyInterpolation.Interpolate;

            return projectile;
        }

        private PhysicsMaterial GetBouncyProjectileMaterial()
        {
            if (_bouncyProjectileMaterial != null)
            {
                return _bouncyProjectileMaterial;
            }

            _bouncyProjectileMaterial = new PhysicsMaterial("TankRoyale_Projectile_Bounce")
            {
                bounciness = 0.6f,
                dynamicFriction = 0.05f,
                staticFriction = 0.05f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Minimum
            };

            return _bouncyProjectileMaterial;
        }

        public Vector3 GetLaunchVelocity(Transform origin)
        {
            if (origin == null)
            {
                return Vector3.zero;
            }

            Vector3 launchDirection = origin.forward;
            if (_tankController != null)
            {
                launchDirection = _tankController.AimForward;
            }

            if (useBallisticArc)
            {
                float angleRad = launchAngleDegrees * Mathf.Deg2Rad;
                Vector3 planarForward = Vector3.ProjectOnPlane(launchDirection, Vector3.up).normalized;
                if (planarForward.sqrMagnitude <= 0.0001f)
                {
                    planarForward = origin.forward;
                }

                launchDirection = (planarForward + Vector3.up * Mathf.Tan(angleRad)).normalized;
            }

            return launchDirection * bulletSpeed;
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
