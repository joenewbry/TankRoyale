using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private const string ShellPrefabPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Weapon/Weapon_Tank_Shell_01.prefab";
        private const string MissilePrefabPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Weapon/Weapon_Missile_02.prefab";

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject missilePrefab;
        [SerializeField] private bool forceKitWeaponPrefabs = true;
        [SerializeField] private float fireRate = 0.15f;
        [SerializeField] private float bulletSpeed = 34f;
        [SerializeField] private bool useBallisticArc = false;
        [SerializeField] private bool forceStraightShots = true;
        [SerializeField] [Range(0f, 45f)] private float launchAngleDegrees = 14f;
        [SerializeField] private float fallbackSphereScale = 0.2f;
        [SerializeField] private bool useFallbackSphereWhenPrefabMissing = true;
        [SerializeField] private bool forceFallbackSphere = false;

        [Header("Feel")]
        [SerializeField] private float recoilImpulse = 1.6f;
        [SerializeField] private float dualBarrelOffset = 0.28f;
        [SerializeField] private float muzzleSpawnForwardOffset = 0.12f;
        [SerializeField] private float aimRayDistance = 250f;

        [Header("Audio")]
        [SerializeField] private AudioClip sfxShot;

        [Header("Powerups")]
        [SerializeField] private PowerupManager powerupManager;
        [SerializeField] private bool bounceProjectilesByDefault = true;
        [SerializeField] private float missileFireRate = 0.5f;
        [SerializeField] private float missileSpeed = 28f;
        [SerializeField] private int missileCapacity = 8;
        [SerializeField] private bool missilesExplodeOnHit = true;
        [SerializeField] private bool disableBulletImpactFx = true;
        [SerializeField] private bool disableMissileImpactFx = true;
        [SerializeField] private Vector3 shellModelEulerOffset = new Vector3(-90f, 0f, 0f);
        [SerializeField] private Vector3 missileModelEulerOffset = new Vector3(-90f, 0f, 0f);

        private TankController _tankController;
        private float _nextFireTime;
        private float _nextMissileFireTime;
        private int _missilesRemaining;
        private static PhysicsMaterial _bouncyProjectileMaterial;
        private Camera _cachedAimCamera;

        public float BulletSpeed => bulletSpeed;
        public bool UseBallisticArc => useBallisticArc && !forceStraightShots;
        public float LaunchAngleDegrees => launchAngleDegrees;
        public int MissilesRemaining => _missilesRemaining;
        public int MissileCapacity => Mathf.Max(0, missileCapacity);

        private void Awake()
        {
            TryAssignKitWeaponPrefabs();
            _tankController = GetComponent<TankController>();
            _missilesRemaining = Mathf.Max(0, missileCapacity);

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

        public void FireMissile()
        {
            if (Time.time < _nextMissileFireTime)
            {
                return;
            }

            if (_tankController == null)
            {
                _tankController = GetComponent<TankController>();
            }

            if (_tankController == null || _tankController.FirePoint == null)
            {
                return;
            }

            if (_missilesRemaining <= 0)
            {
                return;
            }

            _missilesRemaining = Mathf.Max(0, _missilesRemaining - 1);
            SpawnMissile(_tankController.PlayerId);
            _nextMissileFireTime = Time.time + Mathf.Max(0.05f, missileFireRate);
        }

        private void SpawnProjectile(string playerId, float localRightOffset, bool explosiveRounds)
        {
            Transform firePoint = _tankController.FirePoint;
            Vector3 spawnPos = GetProjectileSpawnPosition(firePoint, localRightOffset);

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
            projectileRigidbody.useGravity = UseBallisticArc;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectileRigidbody.mass = 0.08f;
            projectileRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            projectileRigidbody.linearVelocity = GetLaunchVelocity(firePoint);
            AlignProjectileToVelocity(projectileObject.transform, projectileRigidbody.linearVelocity, shellModelEulerOffset);

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
                IgnoreOwnerCollisions(projectileObject);
            }

            Projectile projectileComponent = projectileObject.GetComponent<Projectile>();
            if (projectileComponent == null)
            {
                projectileComponent = projectileObject.AddComponent<Projectile>();
            }

            if (projectileComponent != null)
            {
                projectileComponent.shooterPlayerId = playerId;
                projectileComponent.SetShooterRoot(_tankController != null ? _tankController.transform : null);
                projectileComponent.isRicochet = bounceProjectilesByDefault || IsPowerupActive(playerId, PowerupManager.RicochetPowerup);
                projectileComponent.isBlockBreaker = IsPowerupActive(playerId, PowerupManager.BlockbreakerPowerup);
                projectileComponent.isExplosive = explosiveRounds;
                projectileComponent.UseAreaBlockDestruction = false;
                projectileComponent.SetPaintColor(GetRandomPaintColor());
                if (disableBulletImpactFx)
                {
                    projectileComponent.ConfigureImpactVisuals(false, false);
                }
            }

            ApplyProjectileVisualColor(projectileObject, projectileComponent != null ? projectileComponent.PaintColor : GetRandomPaintColor());
        }

        private void SpawnMissile(string playerId)
        {
            Transform firePoint = _tankController.FirePoint;
            Vector3 spawnPos = GetProjectileSpawnPosition(firePoint, 0f);
            Quaternion spawnRot = firePoint != null ? firePoint.rotation : transform.rotation;

            GameObject missileObject = missilePrefab != null
                ? Instantiate(missilePrefab, spawnPos, spawnRot)
                : CreateFallbackMissileProjectile(spawnPos, spawnRot);

            if (missileObject == null)
            {
                return;
            }

            Rigidbody rb = missileObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = missileObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 0.14f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearVelocity = GetTargetingLaunchDirection(firePoint != null ? firePoint : transform) * missileSpeed;
            AlignProjectileToVelocity(missileObject.transform, rb.linearVelocity, missileModelEulerOffset);

            Collider collider = missileObject.GetComponent<Collider>();
            if (collider == null)
            {
                SphereCollider generated = missileObject.AddComponent<SphereCollider>();
                generated.radius = 0.14f;
                collider = generated;
            }

            if (collider != null)
            {
                collider.material = GetBouncyProjectileMaterial();
                IgnoreOwnerCollisions(missileObject);
            }

            Projectile projectile = missileObject.GetComponent<Projectile>();
            if (projectile == null)
            {
                projectile = missileObject.AddComponent<Projectile>();
            }

            projectile.shooterPlayerId = playerId;
            projectile.SetShooterRoot(_tankController != null ? _tankController.transform : null);
            projectile.isRicochet = false;
            projectile.isBlockBreaker = false;
            projectile.isExplosive = missilesExplodeOnHit;
            projectile.UseAreaBlockDestruction = true;
            projectile.SetPaintColor(new Color(1f, 0.5f, 0.12f, 1f));
            if (disableMissileImpactFx)
            {
                projectile.ConfigureImpactVisuals(false, false);
            }

            ApplyProjectileVisualColor(missileObject, new Color(1f, 0.6f, 0.2f, 1f));
            _tankController.ApplyRecoil(recoilImpulse * 1.45f);
            TryPlayShotSfx();
        }

        private GameObject CreateFallbackMissileProjectile(Vector3 position, Quaternion rotation)
        {
            GameObject missile = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            missile.name = "Projectile_Missile";
            missile.transform.SetPositionAndRotation(position, rotation);
            missile.transform.localScale = new Vector3(0.18f, 0.34f, 0.18f);
            return missile;
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
                bounciness = 0.92f,
                dynamicFriction = 0.01f,
                staticFriction = 0.01f,
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

            Vector3 launchDirection = GetTargetingLaunchDirection(origin);

            if (UseBallisticArc)
            {
                // Positive pitch should bias upward from current aim.
                Vector3 pitchAxis = Vector3.Cross(Vector3.up, launchDirection);
                if (pitchAxis.sqrMagnitude <= 0.0001f)
                {
                    pitchAxis = origin.right;
                }
                pitchAxis.Normalize();

                launchDirection = Quaternion.AngleAxis(-launchAngleDegrees, pitchAxis) * launchDirection;
                launchDirection.Normalize();
            }

            return launchDirection * bulletSpeed;
        }

        public Vector3 GetProjectileSpawnPosition(Transform firePoint, float localRightOffset = 0f)
        {
            if (firePoint == null)
            {
                return transform.position;
            }

            return firePoint.position
                 + (firePoint.right * localRightOffset)
                 + (firePoint.forward * muzzleSpawnForwardOffset);
        }

        private bool IsPowerupActive(string playerId, string powerupKey)
        {
            return powerupManager != null && powerupManager.IsPowerupActive(playerId, powerupKey);
        }

        private void IgnoreOwnerCollisions(GameObject projectileObject)
        {
            if (projectileObject == null || _tankController == null)
            {
                return;
            }

            Collider[] projectileColliders = projectileObject.GetComponentsInChildren<Collider>(true);
            if (projectileColliders == null || projectileColliders.Length == 0)
            {
                return;
            }

            Collider[] ownerColliders = _tankController.GetComponentsInChildren<Collider>(true);
            for (int p = 0; p < projectileColliders.Length; p++)
            {
                Collider projectileCollider = projectileColliders[p];
                if (projectileCollider == null)
                {
                    continue;
                }

                for (int i = 0; i < ownerColliders.Length; i++)
                {
                    Collider ownerCollider = ownerColliders[i];
                    if (ownerCollider == null || ownerCollider == projectileCollider)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
                }
            }
        }

        private Vector3 GetTargetingLaunchDirection(Transform origin)
        {
            Vector3 launchDirection = _tankController != null ? _tankController.AimForward : origin.forward;
            if (_tankController != null && !_tankController.CompareTag("Player"))
            {
                return launchDirection.normalized;
            }

            Camera cam = GetAimCamera();
            if (cam == null)
            {
                return launchDirection.normalized;
            }

            Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(centerRay, Mathf.Max(10f, aimRayDistance), ~0, QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            bool found = false;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || IsOwnerCollider(hit.collider))
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearest = hit;
                    nearestDistance = hit.distance;
                    found = true;
                }
            }

            Vector3 aimPoint = found ? nearest.point : centerRay.GetPoint(Mathf.Max(10f, aimRayDistance));
            Vector3 toAim = aimPoint - GetProjectileSpawnPosition(origin);
            if (toAim.sqrMagnitude > 0.0001f)
            {
                return toAim.normalized;
            }

            return launchDirection.normalized;
        }

        private Camera GetAimCamera()
        {
            if (_cachedAimCamera == null)
            {
                _cachedAimCamera = Camera.main;
            }

            if (_cachedAimCamera == null && _tankController != null)
            {
                _cachedAimCamera = _tankController.GetComponentInChildren<Camera>(true);
            }

            return _cachedAimCamera;
        }

        private bool IsOwnerCollider(Collider collider)
        {
            if (_tankController == null || collider == null)
            {
                return false;
            }

            Transform t = collider.transform;
            Transform owner = _tankController.transform;
            return t == owner || t.IsChildOf(owner);
        }

        private static Color GetRandomPaintColor()
        {
            return UnityEngine.Random.ColorHSV(
                0f, 1f,
                0.65f, 1f,
                0.7f, 1f);
        }

        private static void ApplyProjectileVisualColor(GameObject projectileObject, Color color)
        {
            if (projectileObject == null)
            {
                return;
            }

            Renderer[] renderers = projectileObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                Material instance = new Material(renderer.sharedMaterial);
                if (instance.HasProperty("_Color"))
                {
                    instance.color = color;
                }

                renderer.material = instance;
            }
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

        private static void AlignProjectileToVelocity(Transform projectileTransform, Vector3 velocity, Vector3 eulerOffset)
        {
            if (projectileTransform == null || velocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion look = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            projectileTransform.rotation = look * Quaternion.Euler(eulerOffset);
        }

        private void TryAssignKitWeaponPrefabs()
        {
            if (!forceKitWeaponPrefabs)
            {
                return;
            }

            forceFallbackSphere = false;

#if UNITY_EDITOR
            GameObject shell = AssetDatabase.LoadAssetAtPath<GameObject>(ShellPrefabPath);
            if (shell != null)
            {
                projectilePrefab = shell;
                useFallbackSphereWhenPrefabMissing = false;
            }

            GameObject missile = AssetDatabase.LoadAssetAtPath<GameObject>(MissilePrefabPath);
            if (missile != null)
            {
                missilePrefab = missile;
            }
#endif
        }
    }
}
