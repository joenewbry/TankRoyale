using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Projectile collision behavior including ricochet/block-breaker/armor interactions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float lifetimeSeconds = 5f;
        [SerializeField] private int maxBounces = 3;

        [Header("Runtime Powerup Flags")]
        public bool isRicochet;
        public bool isBlockBreaker;

        private Rigidbody _rigidbody;
        private PowerupManager _powerupManager;
        private int _remainingBounces;
        private Vector3 _lastVelocity;

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
                _lastVelocity = _rigidbody.velocity;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            GameObject hitObject = collision.collider.gameObject;

            if (hitObject.CompareTag("Block"))
            {
                HandleBlockCollision(collision, hitObject);
                return;
            }

            if (hitObject.CompareTag("Player") || hitObject.CompareTag("Enemy"))
            {
                HandleTankCollision(hitObject);
            }
        }

        private void HandleBlockCollision(Collision collision, GameObject blockObject)
        {
            if (isBlockBreaker)
            {
                Destroy(blockObject);

                if (_rigidbody != null)
                {
                    _rigidbody.velocity = _lastVelocity;
                }

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

            Vector3 incomingVelocity = _rigidbody != null ? _rigidbody.velocity : _lastVelocity;
            Vector3 collisionNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
            Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, collisionNormal);

            if (_rigidbody != null)
            {
                _rigidbody.velocity = reflectedVelocity;
                _rigidbody.position += collisionNormal * 0.05f;
            }
            else
            {
                transform.forward = reflectedVelocity.normalized;
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

            string hitTankId = hitTank.PlayerId;
            bool armorActive = _powerupManager != null
                               && _powerupManager.IsPowerupActive(hitTankId, PowerupManager.ArmorPowerup);

            if (armorActive)
            {
                _powerupManager.RemovePowerup(hitTankId, PowerupManager.ArmorPowerup);
            }
            else
            {
                hitTank.TakeDamage(1);
            }

            Destroy(gameObject);
        }
    }
}
