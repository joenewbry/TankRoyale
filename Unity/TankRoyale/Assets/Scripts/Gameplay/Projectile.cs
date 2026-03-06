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

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _remainingBounces = Mathf.Max(0, maxBounces);
        }

        private void Start()
        {
            _powerupManager = FindFirstObjectByType<PowerupManager>();
            Destroy(gameObject, lifetimeSeconds);
        }

        private void FixedUpdate()
        {
            if (_rigidbody != null)
            {
                _lastVelocity = _rigidbody.linearVelocity;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            GameObject hitObject = collision.collider.gameObject;

            if (hitObject.CompareTag("Player") || hitObject.CompareTag("Enemy"))
            {
                HandleTankCollision(hitObject);
                return;
            }

            HandleWorldCollision(collision, hitObject);
        }

        private void HandleWorldCollision(Collision collision, GameObject hitObject)
        {
            if (isBlockBreaker && hitObject.CompareTag("Block"))
            {
                Destroy(hitObject);

                if (_rigidbody != null)
                {
                    _rigidbody.linearVelocity = _lastVelocity;
                }

                return;
            }

            if (isExplosive)
            {
                Explode(transform.position);
                Destroy(gameObject);
                return;
            }

            if (isRicochet)
            {
                Ricochet(collision);
                return;
            }

            Destroy(gameObject);
        }

        private void Ricochet(Collision collision)
        {
            if (_remainingBounces <= 0)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 collisionNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
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

            _lastBouncePoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
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

        private void HandleTankCollision(GameObject tankObject)
        {
            TankController hitTank = tankObject.GetComponentInParent<TankController>();
            if (hitTank == null)
            {
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
                Explode(transform.position);
                Destroy(gameObject);
                return;
            }

            ApplyDamageToTank(hitTank);
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
