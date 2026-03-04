using UnityEngine;

namespace TankRoyale.MobClash
{
    /// <summary>
    /// Basic autonomous creature that advances, finds enemies, and attacks.
    /// </summary>
    [DisallowMultipleComponent]
    public class MobClashCreature : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField, Min(1)] private int maxHealth = 10;
        [SerializeField, Min(1)] private int attackDamage = 2;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.25f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.4f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 0.75f;

        [Header("Movement")]
        [SerializeField, Min(30f)] private float turnSpeedDegrees = 360f;

        private MobClashEncounterLoop _encounter;
        private MobClashCreature _target;
        private Vector3 _advanceDirection;
        private float _nextAttackTime;
        private int _currentHealth;

        public MobClashTeam Team { get; private set; }
        public bool IsAlive => _currentHealth > 0;

        public void Initialize(MobClashEncounterLoop encounter, MobClashTeam team, Vector3 advanceDirection)
        {
            _encounter = encounter;
            Team = team;
            _advanceDirection = advanceDirection.sqrMagnitude > 0.001f ? advanceDirection.normalized : Vector3.forward;
            _target = null;
            _nextAttackTime = 0f;
            _currentHealth = Mathf.Max(1, maxHealth);
        }

        private void Update()
        {
            if (!IsAlive || _encounter == null || !_encounter.EncounterActive)
            {
                return;
            }

            if (_target == null || !_target.IsAlive)
            {
                _target = _encounter.FindNearestEnemy(this);
            }

            if (_target != null)
            {
                Vector3 toTarget = _target.transform.position - transform.position;
                toTarget.y = 0f;
                float sqrDistance = toTarget.sqrMagnitude;

                if (sqrDistance <= attackRange * attackRange)
                {
                    FaceDirection(toTarget);
                    TryAttack();
                    return;
                }

                Move(toTarget.normalized);
                return;
            }

            // No target available: continue crossing board.
            Move(_advanceDirection);
        }

        public void TakeDamage(int amount)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            if (_currentHealth > 0)
            {
                return;
            }

            _encounter.NotifyCreatureDefeated(this);
            Destroy(gameObject);
        }

        private void TryAttack()
        {
            if (_target == null || !_target.IsAlive)
            {
                return;
            }

            if (Time.time < _nextAttackTime)
            {
                return;
            }

            _nextAttackTime = Time.time + attackCooldown;
            _target.TakeDamage(attackDamage);
        }

        private void Move(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.position += direction * (moveSpeed * Time.deltaTime);
            FaceDirection(direction);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeedDegrees * Time.deltaTime);
        }
    }
}
