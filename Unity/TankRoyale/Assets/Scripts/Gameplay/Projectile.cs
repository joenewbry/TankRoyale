using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Projectile collision behavior including ricochet/block-breaker/armor/explosive interactions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifetimeSeconds = 3f;
        [SerializeField] private int maxBounces = 2;
        [Header("Paintball Splatter")]
        [SerializeField] private bool spawnPaintSplatters = true;
        [SerializeField] private Color splatterColor = new Color(0.95f, 0.2f, 0.3f, 1f);
        [SerializeField] private float minSplatterSize = 0.18f;
        [SerializeField] private float maxSplatterSize = 0.42f;
        [SerializeField] private float splatterLifetime = 16f;
        [SerializeField] private float splatterSurfaceOffset = 0.01f;

        [Header("Explosive")]
        [SerializeField] private float explosionRadius = 2.4f;
        [SerializeField] private bool breakBlocksOnAnyHit = true;
        [SerializeField] private bool spawnImpactFog = true;
        [SerializeField] private float impactFogLifetime = 0.9f;
        [SerializeField] private float impactFogScale = 0.8f;

        [Header("Runtime Powerup Flags")]
        public bool isRicochet;
        public bool isBlockBreaker;
        public bool isExplosive;

        [Header("Shooter")]
        public string shooterPlayerId;   // Set by WeaponController — prevents self-damage
        private Transform _shooterRoot;

        private Rigidbody _rigidbody;
        private PowerupManager _powerupManager;
        private int _remainingBounces;
        private Vector3 _lastVelocity;
        private Vector3 _lastBouncePoint;
        private Vector3 _lastBounceNormal;
        private Vector3 _lastBounceOut;
        private Vector3 _lastPosition;
        private const float MinCollisionRadius = 0.12f;
        private float _collisionRadius = MinCollisionRadius;
        private bool _positionInitialized;
        private Color _paintColor;
        public Color PaintColor => _paintColor;
        private static Texture2D _splatTexture;
        private bool _hasImpacted;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _remainingBounces = Mathf.Max(0, maxBounces);
            _paintColor = splatterColor;
            CacheCollisionRadius();
        }

        private void Start()
        {
            _powerupManager = FindFirstObjectByType<PowerupManager>();
            StartCoroutine(SelfDestructTimer());
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null)
            {
                return;
            }

            Vector3 currentPosition = _rigidbody.position;
            if (!_positionInitialized)
            {
                _lastPosition = currentPosition;
                _positionInitialized = true;
            }

            Vector3 travel = currentPosition - _lastPosition;
            float distance = travel.magnitude;
            if (distance > 0.0001f)
            {
                Vector3 direction = travel / distance;
                RaycastHit[] hits = Physics.SphereCastAll(
                    _lastPosition,
                    _collisionRadius,
                    direction,
                    distance,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                RaycastHit nearest = default;
                bool foundHit = false;
                float nearestDistance = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit candidate = hits[i];
                    if (candidate.collider == null || IsSelfCollider(candidate.collider))
                    {
                        continue;
                    }

                    if (candidate.distance < nearestDistance)
                    {
                        nearestDistance = candidate.distance;
                        nearest = candidate;
                        foundHit = true;
                    }
                }

                if (foundHit)
                {
                    _rigidbody.position = nearest.point - direction * Mathf.Max(0.005f, _collisionRadius * 0.2f);
                    HandleImpact(nearest.collider, nearest.point, nearest.normal);
                    _lastPosition = _rigidbody.position;
                    return;
                }
            }

            _lastVelocity = _rigidbody.linearVelocity;
            _lastPosition = currentPosition;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.collider == null)
            {
                return;
            }

            Vector3 point = transform.position;
            Vector3 normal = -transform.forward;
            if (collision.contacts != null && collision.contacts.Length > 0)
            {
                point = collision.contacts[0].point;
                normal = collision.contacts[0].normal;
            }

            HandleImpact(collision.collider, point, normal);
        }

        private void HandleImpact(Collider hitCollider, Vector3 point, Vector3 normal)
        {
            if (hitCollider == null)
            {
                return;
            }

            if (IsShooterCollider(hitCollider))
            {
                return;
            }

            _hasImpacted = true;
            SpawnImpactFogAt(point);

            GameObject hitObject = hitCollider.gameObject;
            TargetCactus cactus = hitCollider.GetComponentInParent<TargetCactus>();
            if (cactus != null)
            {
                cactus.TakeHit(1);
                SpawnPaintSplat(point, normal, hitCollider.transform);
                Destroy(gameObject);
                return;
            }

            DestructibleProp destructibleProp = hitCollider.GetComponentInParent<DestructibleProp>();
            if (destructibleProp != null)
            {
                destructibleProp.ApplyHit(1);
                SpawnPaintSplat(point, normal, hitCollider.transform);
                Destroy(gameObject);
                return;
            }

            if (hitObject.CompareTag("Player") || hitObject.CompareTag("Enemy"))
            {
                HandleTankCollision(hitCollider, point, normal);
                return;
            }

            HandleWorldCollision(hitCollider, point, normal, hitObject);
        }

        private void HandleWorldCollision(Collider hitCollider, Vector3 point, Vector3 normal, GameObject hitObject)
        {
            if (breakBlocksOnAnyHit && IsBreakableWorldBlock(hitObject))
            {
                SpawnPaintSplat(point, normal, hitCollider.transform);
                Destroy(GetBreakableBlockRoot(hitObject));
                Destroy(gameObject);
                return;
            }

            if (isBlockBreaker && hitObject.CompareTag("Block"))
            {
                SpawnPaintSplat(point, normal, hitCollider.transform);
                Destroy(hitObject);

                if (_rigidbody != null)
                {
                    _rigidbody.linearVelocity = _lastVelocity;
                }

                return;
            }

            if (isExplosive)
            {
                SpawnPaintSplat(point, normal, hitCollider.transform);
                Explode(transform.position);
                Destroy(gameObject);
                return;
            }

            if (isRicochet)
            {
                Ricochet(normal, point, hitCollider.transform);
                return;
            }

            SpawnPaintSplat(point, normal, hitCollider.transform);
            Destroy(gameObject);
        }

        private static bool IsBreakableWorldBlock(GameObject hitObject)
        {
            if (hitObject == null)
            {
                return false;
            }

            if (IsGroundLike(hitObject))
            {
                return false;
            }

            if (hitObject.CompareTag("Block"))
            {
                return true;
            }

            string name = hitObject.name;
            return name.Contains("BuildBlock_")
                   || name.Contains("TargetCactus");
        }

        private static GameObject GetBreakableBlockRoot(GameObject hitObject)
        {
            if (hitObject == null)
            {
                return null;
            }

            Transform t = hitObject.transform;
            Transform best = t;
            while (t != null)
            {
                if (t.CompareTag("Block")
                    || t.name.Contains("BuildBlock_")
                    || t.name.Contains("TargetCactus"))
                {
                    if (IsGroundLike(t.gameObject))
                    {
                        break;
                    }
                    best = t;
                }
                t = t.parent;
            }

            return best != null ? best.gameObject : hitObject;
        }

        private static bool IsGroundLike(GameObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            string n = obj.name;
            return n.Contains("3D_Tile_Ground")
                   || n.Contains("Ground_Desert")
                   || n.Contains("Tile_Ground")
                   || n.Contains("Flat_Ground");
        }

        private void Ricochet(Vector3 collisionNormal, Vector3 point, Transform hitParent)
        {
            if (_remainingBounces <= 0)
            {
                SpawnPaintSplat(point, collisionNormal, hitParent);
                Destroy(gameObject);
                return;
            }

            if (collisionNormal.sqrMagnitude <= 0.0001f)
            {
                collisionNormal = -transform.forward;
            }

            Vector3 incoming = _rigidbody != null ? _rigidbody.linearVelocity : _lastVelocity;
            Vector3 reflectedVelocity = Vector3.Reflect(incoming, collisionNormal);
            reflectedVelocity *= 0.97f;
            SpawnPaintSplat(point, collisionNormal, hitParent);

            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = reflectedVelocity;
                _rigidbody.position += collisionNormal * 0.02f;
            }
            else
            {
                transform.forward = reflectedVelocity.normalized;
            }

            _lastBouncePoint = point;
            _lastBounceNormal = collisionNormal;
            _lastBounceOut = reflectedVelocity.normalized;

            if (DebugVisualSettings.ShowBounceNormals)
            {
                Debug.DrawRay(_lastBouncePoint, _lastBounceNormal, Color.green, 1f);
                Debug.DrawRay(_lastBouncePoint, _lastBounceOut, Color.magenta, 1f);
            }

            _remainingBounces--;
            if (_remainingBounces <= 0)
            {
                Destroy(gameObject);
            }
        }

        private void HandleTankCollision(Collider hitCollider, Vector3 point, Vector3 normal)
        {
            GameObject tankObject = hitCollider.gameObject;
            TankController hitTank = tankObject.GetComponentInParent<TankController>();
            if (hitTank == null)
            {
                SpawnPaintSplat(point, normal, hitCollider.transform);
                Destroy(gameObject);
                return;
            }

            // Don't damage the tank that fired this
            if (!string.IsNullOrEmpty(shooterPlayerId) && hitTank.PlayerId == shooterPlayerId)
            {
                return;
            }

            if (isExplosive)
            {
                SpawnPaintSplat(point, normal, hitCollider.transform);
                Explode(transform.position);
                Destroy(gameObject);
                return;
            }

            ApplyDamageToTank(hitTank);
            ApplyImpactToTank(hitTank, normal);
            SpawnPaintSplat(point, normal, hitCollider.transform);
            Destroy(gameObject);
        }

        private void ApplyDamageToTank(TankController hitTank)
        {
            if (hitTank == null) return;

            string hitTankId = hitTank.PlayerId;
            bool armorActive = _powerupManager != null
                               && _powerupManager.IsPowerupActive(hitTankId, PowerupManager.ArmorPowerup);

            if (armorActive)
            {
                _powerupManager.RemovePowerup(hitTankId, PowerupManager.ArmorPowerup);
            }
            else
            {
                hitTank.TakeDamage(1, shooterPlayerId);
            }
        }

        private void ApplyImpactToTank(TankController hitTank, Vector3 impactNormal)
        {
            if (hitTank == null || impactNormal.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 incoming = _rigidbody != null ? _rigidbody.linearVelocity : _lastVelocity;
            float impactSpeed = incoming.magnitude;
            if (impactSpeed <= 0.01f)
            {
                return;
            }

            Vector3 pushDir = -impactNormal.normalized;
            float impulse = (impactSpeed / 12f) * Mathf.Max(0.1f, hitTank.ProjectileImpactStrength);
            hitTank.ApplyImpactImpulse(pushDir * impulse);
        }

        private void Explode(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, explosionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                TankController tank = hits[i].GetComponentInParent<TankController>();
                if (tank == null) continue;

                if (!string.IsNullOrEmpty(shooterPlayerId) && tank.PlayerId == shooterPlayerId)
                {
                    continue;
                }

                ApplyDamageToTank(tank);
            }
        }

        private void SpawnPaintSplat(Collision collision)
        {
            if (!spawnPaintSplatters)
            {
                return;
            }

            Vector3 point = transform.position;
            Vector3 normal = -transform.forward;
            Transform parent = null;

            if (collision != null)
            {
                parent = collision.collider != null ? collision.collider.transform : null;
                if (collision.contacts != null && collision.contacts.Length > 0)
                {
                    ContactPoint cp = collision.contacts[0];
                    point = cp.point;
                    normal = cp.normal;
                }
            }

            if (normal.sqrMagnitude <= 0.0001f)
            {
                normal = Vector3.up;
            }

            SpawnSplatCluster(point, normal, parent);
        }

        private void SpawnPaintSplat(Vector3 point, Vector3 normal, Transform parent)
        {
            if (!spawnPaintSplatters)
            {
                return;
            }

            if (normal.sqrMagnitude <= 0.0001f)
            {
                normal = Vector3.up;
            }

            SpawnSplatCluster(point, normal, parent);
        }

        private void SpawnSplatCluster(Vector3 point, Vector3 normal, Transform parent)
        {
            int droplets = Random.Range(3, 6);
            for (int i = 0; i < droplets; i++)
            {
                float spread = i == 0 ? 0f : Random.Range(0.01f, 0.07f);
                Vector3 tangent = Vector3.Cross(normal, Vector3.up);
                if (tangent.sqrMagnitude < 0.0001f)
                {
                    tangent = Vector3.Cross(normal, Vector3.right);
                }
                tangent.Normalize();
                Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
                Vector2 jitter = Random.insideUnitCircle * spread;
                Vector3 offset = tangent * jitter.x + bitangent * jitter.y;

                float size = Random.Range(minSplatterSize, maxSplatterSize) * (i == 0 ? 1f : Random.Range(0.25f, 0.6f));
                CreateSingleSplat(point + offset, normal, parent, size);
            }
        }

        private void CreateSingleSplat(Vector3 point, Vector3 normal, Transform parent, float size)
        {
            GameObject splat = GameObject.CreatePrimitive(PrimitiveType.Quad);
            splat.name = "PaintSplat";
            if (parent != null)
            {
                splat.transform.SetParent(parent, true);
            }

            splat.transform.position = point + normal * splatterSurfaceOffset;
            splat.transform.rotation = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float stretch = Random.Range(0.75f, 1.35f);
            splat.transform.localScale = new Vector3(size * stretch, size / stretch, size);

            Collider c = splat.GetComponent<Collider>();
            if (c != null)
            {
                Destroy(c);
            }

            MeshRenderer renderer = splat.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetSplatterMaterial(_paintColor);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            Destroy(splat, splatterLifetime);
        }

        private void CacheCollisionRadius()
        {
            SphereCollider sphere = GetComponent<SphereCollider>();
            if (sphere != null)
            {
                float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
                _collisionRadius = Mathf.Max(MinCollisionRadius, sphere.radius * scale);
                return;
            }

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
                _collisionRadius = Mathf.Max(MinCollisionRadius, capsule.radius * scale);
                return;
            }

            Collider c = GetComponent<Collider>();
            if (c != null)
            {
                _collisionRadius = Mathf.Max(MinCollisionRadius, c.bounds.extents.magnitude * 0.25f);
            }
        }

        private bool IsSelfCollider(Collider collider)
        {
            if (collider == null) return true;
            Transform t = collider.transform;
            return t == transform || t.IsChildOf(transform) || IsShooterCollider(collider);
        }

        public void SetShooterRoot(Transform shooterRoot)
        {
            _shooterRoot = shooterRoot;
        }

        private bool IsShooterCollider(Collider collider)
        {
            if (_shooterRoot == null || collider == null)
            {
                return false;
            }

            Transform t = collider.transform;
            return t == _shooterRoot || t.IsChildOf(_shooterRoot);
        }

        public void SetPaintColor(Color color)
        {
            _paintColor = color;
            splatterColor = color;
        }

        public void ConfigureImpactVisuals(bool paintSplatters, bool impactFog)
        {
            spawnPaintSplatters = paintSplatters;
            spawnImpactFog = impactFog;
        }

        private Material GetSplatterMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.color = color;
            material.mainTexture = GetSplatTexture();

            return material;
        }

        private static Texture2D GetSplatTexture()
        {
            if (_splatTexture != null)
            {
                return _splatTexture;
            }

            const int size = 64;
            _splatTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _splatTexture.wrapMode = TextureWrapMode.Clamp;
            _splatTexture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Vector2.Distance(p, center);
                    float radial = 1f - Mathf.Clamp01(d / radius);

                    // Add blob noise to break circular silhouette.
                    float nx = Mathf.PerlinNoise(x * 0.13f, y * 0.13f);
                    float ny = Mathf.PerlinNoise((x + 23) * 0.09f, (y + 41) * 0.11f);
                    float noise = (nx * 0.65f) + (ny * 0.35f);

                    float alpha = Mathf.Clamp01((radial * radial * 1.25f) + (noise - 0.45f) * 0.6f);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    _splatTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            _splatTexture.Apply(false, true);
            return _splatTexture;
        }

        private void OnDrawGizmosSelected()
        {
            if (!DebugVisualSettings.ShowBounceNormals)
            {
                return;
            }

            if (_lastBounceNormal.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawRay(_lastBouncePoint, _lastBounceNormal);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(_lastBouncePoint, _lastBounceOut);
        }

        private System.Collections.IEnumerator SelfDestructTimer()
        {
            yield return new WaitForSeconds(Mathf.Max(0.2f, lifetimeSeconds));

            if (this == null || !gameObject.activeInHierarchy)
            {
                yield break;
            }

            if (!_hasImpacted)
            {
                SpawnImpactFogAt(transform.position);
            }

            Destroy(gameObject);
        }

        private void SpawnImpactFogAt(Vector3 position)
        {
            if (!spawnImpactFog)
            {
                return;
            }

            GameObject fog = new GameObject("ImpactFog");
            fog.transform.position = position + Vector3.up * 0.05f;
            ParticleSystem ps = fog.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.duration = impactFogLifetime;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, impactFogLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.85f);
            main.startSize = new ParticleSystem.MinMaxCurve(impactFogScale * 0.35f, impactFogScale);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.75f, 0.75f, 0.6f));
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 14, 22)
            });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = Mathf.Max(0.12f, impactFogScale * 0.45f);

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.65f, 0.65f, 0.65f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.55f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(g);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            ps.Play();
            Destroy(fog, Mathf.Max(0.6f, impactFogLifetime + 0.5f));
        }
    }
}
