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
        [SerializeField] private float lifetimeSeconds = 5f;
        [SerializeField] private int maxBounces = 3;
        [Header("Paintball Splatter")]
        [SerializeField] private bool spawnPaintSplatters = true;
        [SerializeField] private Color splatterColor = new Color(0.95f, 0.2f, 0.3f, 1f);
        [SerializeField] private float minSplatterSize = 0.18f;
        [SerializeField] private float maxSplatterSize = 0.42f;
        [SerializeField] private float splatterLifetime = 16f;
        [SerializeField] private float splatterSurfaceOffset = 0.01f;

        [Header("Explosive")]
        [SerializeField] private float explosionRadius = 2.4f;

        [Header("Runtime Powerup Flags")]
        public bool isRicochet;
        public bool isBlockBreaker;
        public bool isExplosive;

        [Header("Shooter")]
        public string shooterPlayerId;   // Set by WeaponController — prevents self-damage

        private Rigidbody _rigidbody;
        private PowerupManager _powerupManager;
        private int _remainingBounces;
        private Vector3 _lastVelocity;
        private Vector3 _lastBouncePoint;
        private Vector3 _lastBounceNormal;
        private Vector3 _lastBounceOut;
        private Vector3 _lastPosition;
        private float _collisionRadius = 0.08f;
        private bool _positionInitialized;
        private static Material _splatterMaterial;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _remainingBounces = Mathf.Max(0, maxBounces);
            CacheCollisionRadius();
        }

        private void Start()
        {
            _powerupManager = FindFirstObjectByType<PowerupManager>();
            Destroy(gameObject, lifetimeSeconds);
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
                RaycastHit hit;
                if (Physics.SphereCast(_lastPosition, _collisionRadius, travel / distance, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null && !IsSelfCollider(hit.collider))
                    {
                        _rigidbody.position = hit.point - (travel / distance) * Mathf.Max(0.005f, _collisionRadius * 0.2f);
                        HandleImpact(hit.collider, hit.point, hit.normal);
                        _lastPosition = _rigidbody.position;
                        return;
                    }
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

            GameObject hitObject = hitCollider.gameObject;
            if (hitObject.CompareTag("Player") || hitObject.CompareTag("Enemy"))
            {
                HandleTankCollision(hitCollider, point, normal);
                return;
            }

            HandleWorldCollision(hitCollider, point, normal, hitObject);
        }

        private void HandleWorldCollision(Collider hitCollider, Vector3 point, Vector3 normal, GameObject hitObject)
        {
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
                Ricochet(normal, point);
                return;
            }

            SpawnPaintSplat(point, normal, hitCollider.transform);
            Destroy(gameObject);
        }

        private void Ricochet(Vector3 collisionNormal, Vector3 point)
        {
            if (_remainingBounces <= 0)
            {
                Destroy(gameObject);
                return;
            }

            if (collisionNormal.sqrMagnitude <= 0.0001f)
            {
                collisionNormal = -transform.forward;
            }

            Vector3 incoming = _rigidbody != null ? _rigidbody.linearVelocity : _lastVelocity;
            Vector3 reflectedVelocity = Vector3.Reflect(incoming, collisionNormal);
            reflectedVelocity *= 0.9f;

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

            GameObject splat = GameObject.CreatePrimitive(PrimitiveType.Quad);
            splat.name = "PaintSplat";
            if (parent != null)
            {
                splat.transform.SetParent(parent, true);
            }

            float size = Random.Range(minSplatterSize, maxSplatterSize);
            splat.transform.position = point + normal * splatterSurfaceOffset;
            splat.transform.rotation = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            splat.transform.localScale = new Vector3(size, size, size);

            Collider c = splat.GetComponent<Collider>();
            if (c != null)
            {
                Destroy(c);
            }

            MeshRenderer renderer = splat.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetSplatterMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            Destroy(splat, splatterLifetime);
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

            GameObject splat = GameObject.CreatePrimitive(PrimitiveType.Quad);
            splat.name = "PaintSplat";
            if (parent != null)
            {
                splat.transform.SetParent(parent, true);
            }

            float size = Random.Range(minSplatterSize, maxSplatterSize);
            splat.transform.position = point + normal * splatterSurfaceOffset;
            splat.transform.rotation = Quaternion.LookRotation(normal, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            splat.transform.localScale = new Vector3(size, size, size);

            Collider c = splat.GetComponent<Collider>();
            if (c != null)
            {
                Destroy(c);
            }

            MeshRenderer renderer = splat.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetSplatterMaterial();
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
                _collisionRadius = Mathf.Max(0.04f, sphere.radius * scale);
                return;
            }

            CapsuleCollider capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
                _collisionRadius = Mathf.Max(0.04f, capsule.radius * scale);
                return;
            }

            Collider c = GetComponent<Collider>();
            if (c != null)
            {
                _collisionRadius = Mathf.Max(0.04f, c.bounds.extents.magnitude * 0.25f);
            }
        }

        private bool IsSelfCollider(Collider collider)
        {
            if (collider == null) return true;
            Transform t = collider.transform;
            return t == transform || t.IsChildOf(transform);
        }

        private Material GetSplatterMaterial()
        {
            if (_splatterMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                _splatterMaterial = new Material(shader);
            }

            _splatterMaterial.color = splatterColor;
            return _splatterMaterial;
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
    }
}
