using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.AI
{
    /// <summary>
    /// AI tank brain: path requests, waypoint following, independent turret aim, and firing logic.
    /// </summary>
    [DisallowMultipleComponent]
    public class AITankController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AStarGrid grid;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform turretPivot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Navigation")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float bodyTurnSpeed = 360f;
        [SerializeField] private float waypointReachDistance = 0.15f;
        [SerializeField] private float repathIntervalSeconds = 1.5f;

        [Header("Combat")]
        [SerializeField] private float turretTurnSpeed = 540f;
        [SerializeField] private float fireRange = 12f;
        [SerializeField] private float fireCooldown = 0.8f;
        [SerializeField] private float losOriginHeight = 0.4f;
        [SerializeField] private float losTargetHeight = 0.4f;
        [SerializeField] private LayerMask lineOfSightMask = ~0;

        private readonly List<Vector3> _waypoints = new List<Vector3>(64);

        private Transform _cachedTransform;
        private WaitForSeconds _repathWait;
        private Coroutine _repathRoutine;

        private int _waypointIndex;
        private float _nextFireTime;

        private void Awake()
        {
            _cachedTransform = transform;

            if (bodyRoot == null)
            {
                bodyRoot = _cachedTransform;
            }

            if (turretPivot == null)
            {
                turretPivot = bodyRoot;
            }

            if (playerTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTarget = player.transform;
                }
            }

            _repathWait = new WaitForSeconds(Mathf.Max(0.05f, repathIntervalSeconds));
        }

        private void OnEnable()
        {
            _repathRoutine = StartCoroutine(RepathLoop());
        }

        private void OnDisable()
        {
            if (_repathRoutine != null)
            {
                StopCoroutine(_repathRoutine);
                _repathRoutine = null;
            }
        }

        private void Update()
        {
            MoveAlongPath();
            AimTurretAtPlayer();
            TryFire();
        }

        public void SetTarget(Transform target)
        {
            playerTarget = target;
        }

        private IEnumerator RepathLoop()
        {
            while (enabled)
            {
                UpdatePathToPlayer();
                yield return _repathWait;
            }
        }

        private void UpdatePathToPlayer()
        {
            if (grid == null || playerTarget == null)
            {
                _waypoints.Clear();
                _waypointIndex = 0;
                return;
            }

            bool found = AStarPathfinder.FindPath(_cachedTransform.position, playerTarget.position, grid, _waypoints);
            _waypointIndex = 0;

            if (!found)
            {
                _waypoints.Clear();
            }
        }

        private void MoveAlongPath()
        {
            if (_waypointIndex >= _waypoints.Count)
            {
                return;
            }

            Vector3 currentPosition = _cachedTransform.position;
            Vector3 waypoint = _waypoints[_waypointIndex];
            waypoint.y = currentPosition.y;

            Vector3 toWaypoint = waypoint - currentPosition;
            toWaypoint.y = 0f;
            float distance = toWaypoint.magnitude;

            if (distance <= waypointReachDistance)
            {
                _waypointIndex++;
                return;
            }

            Vector3 direction = toWaypoint / distance;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            bodyRoot.rotation = Quaternion.RotateTowards(bodyRoot.rotation, targetRotation, bodyTurnSpeed * Time.deltaTime);

            _cachedTransform.position += direction * (moveSpeed * Time.deltaTime);
        }

        private void AimTurretAtPlayer()
        {
            if (turretPivot == null || playerTarget == null)
            {
                return;
            }

            Vector3 turretPos = turretPivot.position;
            Vector3 toTarget = playerTarget.position - turretPos;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            turretPivot.rotation = Quaternion.RotateTowards(turretPivot.rotation, targetRotation, turretTurnSpeed * Time.deltaTime);
        }

        private void TryFire()
        {
            if (projectilePrefab == null || playerTarget == null)
            {
                return;
            }

            if (Time.time < _nextFireTime)
            {
                return;
            }

            Vector3 from = muzzlePoint != null ? muzzlePoint.position : turretPivot.position;
            Vector3 toPlayer = playerTarget.position - from;
            float sqrDistance = toPlayer.sqrMagnitude;
            float fireRangeSqr = fireRange * fireRange;
            if (sqrDistance > fireRangeSqr)
            {
                return;
            }

            if (!HasLineOfSight(from, playerTarget.position))
            {
                return;
            }

            Transform spawnTransform = muzzlePoint != null ? muzzlePoint : turretPivot;
            if (spawnTransform == null)
            {
                spawnTransform = _cachedTransform;
            }

            Instantiate(projectilePrefab, spawnTransform.position, spawnTransform.rotation);
            _nextFireTime = Time.time + fireCooldown;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 target)
        {
            origin.y += losOriginHeight;
            target.y += losTargetHeight;

            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.0001f)
            {
                return true;
            }

            RaycastHit hit;
            if (!Physics.Raycast(origin, direction / distance, out hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            return hit.transform == playerTarget || hit.transform.IsChildOf(playerTarget);
        }
    }
}
