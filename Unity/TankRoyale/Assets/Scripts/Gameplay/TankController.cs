using UnityEngine;
using TankRoyale.Audio;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// 3D top-down tank controller with independent body and turret rotation.
    /// Movement occurs on XZ plane (Y up).
    /// </summary>
    [DisallowMultipleComponent]
    public class TankController : MonoBehaviour
    {

        [Header("Identity")]
        [SerializeField] private string playerId;

        [Header("References")]
        [SerializeField] private Transform tankBody;
        [SerializeField] private Transform turret;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Camera aimCamera;

        [Header("Movement Feel")]
        [SerializeField] private float moveSpeed = 8.5f;
        [SerializeField] private float speedBoostMultiplier = 1.35f;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float deceleration = 38f;
        [SerializeField] private float lateralGrip = 10f;         // lower = more drift
        [SerializeField] private float groundSnapHeight = 0.08f;
        [SerializeField] private float groundProbeHeight = 1.8f;
        [SerializeField] private bool invertMovement = false;
        [SerializeField] private bool useDigitalTankInput = true;
        [SerializeField] private float maxClimbSlopeAngle = 42f;
        [SerializeField] private float steepSlopeSlideAccel = 6f;
        [SerializeField] private float minClimbEntrySpeed = 2.6f;
        [SerializeField] private float slopeDrag = 2.2f;
        [SerializeField] private float uphillDeceleration = 4.5f;
        [SerializeField] private float idleBrake = 18f;
        [SerializeField] private float idleStopSpeed = 0.08f;
        [SerializeField] private bool allowPassiveSlopeSlide = false;
        [SerializeField] [Range(0f, 1f)] private float slopeTiltStrength = 0.85f;
        [SerializeField] private float maxSlopeTiltAngle = 18f;
        [SerializeField] private float slopeTiltResponsiveness = 12f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 420f;
        [SerializeField] private float turretRotationSpeed = 720f;

        [Header("Mouse Turret")]
        [SerializeField] private float mouseTurretSensitivity = 1.6f;
        [SerializeField] private float minTurretPitch = -15f;
        [SerializeField] private float maxTurretPitch = 45f;

        [Header("Camera Feel")]
        [SerializeField] private bool useMouseForTurretAim = true;

        [Header("Treads")]
        [SerializeField] private bool animateTreads = true;
        [SerializeField] private bool syncDetachedTreadsToBody = true;
        [SerializeField] private bool useLegacyTreadRotationFallback = false;
        [SerializeField] private float treadMaxSpinSpeed = 900f;
        [SerializeField] private float treadSpinAcceleration = 2800f;
        [SerializeField] private bool invertLeftTreadRotation = false;
        [SerializeField] private bool invertRightTreadRotation = true;
        [SerializeField] private Vector3 treadSpinAxis = new Vector3(1f, 0f, 0f);

        [Header("Trajectory Line")]
        [SerializeField] private bool showTrajectoryLine = true;
        [SerializeField] private int trajectorySegments = 24;
        [SerializeField] private float trajectoryStepSeconds = 0.08f;
        [SerializeField] private float trajectoryLineWidth = 0.03f;
        [SerializeField] private Color trajectoryLineColor = new Color(1f, 0.8f, 0.2f, 0.9f);

        [Header("Health")]
        [SerializeField] private int maxHealth = 3;

        [Header("Impact / Bounce")]
        [SerializeField] private float collisionBounceStrength = 2.2f;
        [SerializeField] private float collisionBounceDamping = 0.8f;
        [SerializeField] private float projectileImpactStrength = 2.8f;

        // Requires Rigidbody with: Freeze Rotation only (top-down tank feel)
        private Rigidbody _rigidbody;
        private Camera _cachedCamera;
        private Vector2 _moveInput;
        private WeaponController _weaponController;
        private Vector3 _planarVelocity;
        private float _groundHeight;
        private Vector3 _groundNormal = Vector3.up;
        private int currentHealth;

        private float _turretYaw;
        private float _turretPitch;
        private bool _turretAimInitialized;
        private Renderer[] _turretRenderers = new Renderer[0];
        private bool[] _turretRendererDefaults = new bool[0];
        private Transform[] _leftTreads = new Transform[0];
        private Transform[] _rightTreads = new Transform[0];
        private Animator[] _treadAnimators = new Animator[0];
        private float _leftTreadSpinVelocity;
        private float _rightTreadSpinVelocity;
        private LineRenderer _trajectoryLine;
        private static Material _trajectoryLineMaterial;

        public Transform FirePoint => firePoint;
        public string PlayerId => string.IsNullOrWhiteSpace(playerId) ? gameObject.name : playerId;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float CurrentSpeed => _planarVelocity.magnitude;
        public float CurrentSlopeAngle => Vector3.Angle(_groundNormal, Vector3.up);
        public float ProjectileImpactStrength => projectileImpactStrength;
        public float CurrentTurnInput => _moveInput.x;
        public Vector3 AimForward
        {
            get
            {
                if (_cachedCamera == null)
                {
                    _cachedCamera = aimCamera != null ? aimCamera : Camera.main;
                }

                if (_cachedCamera != null)
                {
                    CameraController cc = _cachedCamera.GetComponent<CameraController>();
                    if (cc != null)
                    {
                        return cc.AimForward.normalized;
                    }

                    return _cachedCamera.transform.forward.normalized;
                }

                Transform basis = tankBody != null ? tankBody : transform;
                return basis.forward;
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.solverIterations = Mathf.Max(_rigidbody.solverIterations, 10);
            _rigidbody.solverVelocityIterations = Mathf.Max(_rigidbody.solverVelocityIterations, 10);

            _weaponController = GetComponent<WeaponController>();
            _cachedCamera = aimCamera != null ? aimCamera : Camera.main;
            currentHealth = Mathf.Max(1, maxHealth);
            _groundHeight = transform.position.y;

            if (string.IsNullOrWhiteSpace(playerId))
            {
                playerId = gameObject.name;
            }

            tankBody = ResolveBodyTransform();
            turret = ResolveTurretTransform();
            firePoint = ResolveFirePointTransform();
            InitializeTurretAimState();
            CacheTurretRenderers();
            EnsureTrajectoryLine();
        }

        private void Update()
        {
            ReadMovementInput();
            UpdateTreadVisuals();
            UpdateTrajectoryLine();
            HandleFireInput();
        }

        private void LateUpdate()
        {
            UpdateTurretVisibilityByViewMode();
            // Run turret stabilization after movement/physics updates to reduce visual jitter.
            RotateTurretToMouse();
        }

        private void FixedUpdate()
        {
            MoveTank();
            RotateBodyFromTurnInput();
        }

        public void TakeDamage(int amount, string attackerPlayerId = null)
        {
            if (!gameObject.activeInHierarchy || amount <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            Debug.Log($"[{name}] took {amount} damage — health {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Debug.Log($"[{name}] destroyed (killer: {attackerPlayerId ?? "unknown"})");
                ExplosionEffect.Spawn(transform.position, scale: 1.5f);
                AudioManager.Instance?.PlayExplosionSFX();

                // Reset this tank's own streak and report kill before deactivating.
                KillstreakManager.Instance?.ResetStreak(PlayerId);
                GameManager.Instance?.NotifyTankDestroyed(this, attackerPlayerId);

                gameObject.SetActive(false);
            }
        }

        public void Heal(int amount)
        {
            if (!gameObject.activeInHierarchy || amount <= 0) return;
            int prev = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"[{name}] healed {currentHealth - prev} HP — health {currentHealth}/{maxHealth}");
        }

        public void ApplyRecoil(float impulse)
        {
            if (impulse <= 0f) return;
            Vector3 recoilDir = tankBody != null ? -tankBody.forward : -transform.forward;
            _planarVelocity += recoilDir * impulse;
        }

        public void ApplyImpactImpulse(Vector3 worldImpulse)
        {
            Vector3 planarImpulse = new Vector3(worldImpulse.x, 0f, worldImpulse.z);
            _planarVelocity += planarImpulse;
        }

        private void HandleFireInput()
        {
            if (_weaponController == null) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                _weaponController.Fire();
            }
        }

        private void ReadMovementInput()
        {
            // Tank input: x = turn (+1 left, -1 right), y = throttle (+1 forward, -1 backward)
            float x;
            float y;

            if (useDigitalTankInput)
            {
                int right = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1 : 0;
                int left = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1 : 0;
                int up = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1 : 0;
                int down = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1 : 0;
                // Keep turn sign aligned with tank-track math: +x = left turn (A), -x = right turn (D).
                x = left - right;
                y = up - down;
            }
            else
            {
                x = -Input.GetAxisRaw("Horizontal");
                y = Input.GetAxisRaw("Vertical");
            }

            if (invertMovement)
            {
                x = -x;
                y = -y;
            }

            _moveInput.x = x;
            _moveInput.y = y;

            if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }
        }

        private void MoveTank()
        {
            float targetSpeed = GetCurrentMoveSpeed();
            Transform basis = tankBody != null ? tankBody : transform;
            float throttle = _moveInput.y;
            bool hasThrottleInput = Mathf.Abs(throttle) > 0.001f;
            Vector3 desiredDirection = basis.forward * throttle;
            desiredDirection.y = 0f;
            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }
            Vector3 desiredVelocity = desiredDirection * targetSpeed;

            float accel = desiredVelocity.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, desiredVelocity, accel * Time.fixedDeltaTime);

            // Drift control: bleed side-slip in body space.
            Vector3 localVelocity = basis.InverseTransformDirection(_planarVelocity);
            localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, lateralGrip * Time.fixedDeltaTime);
            _planarVelocity = basis.TransformDirection(localVelocity);

            // Idle brake: if not throttling, aggressively damp drift to prevent creeping.
            if (!hasThrottleInput)
            {
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, Vector3.zero, idleBrake * Time.fixedDeltaTime);
            }

            Vector3 projectedPosition = _rigidbody.position + new Vector3(_planarVelocity.x, 0f, _planarVelocity.z) * Time.fixedDeltaTime;
            Vector3 groundNormal;
            float groundY = SampleGroundHeight(projectedPosition, out groundNormal);
            _groundNormal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
            float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            float slopeFactor = Mathf.InverseLerp(0f, Mathf.Max(1f, maxClimbSlopeAngle), slopeAngle);
            Vector3 planarVelocity = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
            float planarSpeed = planarVelocity.magnitude;

            if (slopeAngle <= maxClimbSlopeAngle)
            {
                Vector3 uphillDir = Vector3.ProjectOnPlane(Vector3.up, groundNormal).normalized;
                Vector3 uphillPlanar = new Vector3(uphillDir.x, 0f, uphillDir.z);
                if (uphillPlanar.sqrMagnitude > 0.0001f)
                {
                    uphillPlanar.Normalize();
                }

                float uphillDot = planarSpeed > 0.001f && uphillPlanar.sqrMagnitude > 0.0001f
                    ? Vector3.Dot(planarVelocity.normalized, uphillPlanar)
                    : 0f;
                float requiredClimbSpeed = Mathf.Lerp(minClimbEntrySpeed * 0.65f, minClimbEntrySpeed * 1.35f, slopeFactor);

                // Require momentum to climb: low-speed uphill input starts a slide back down.
                if (hasThrottleInput && throttle > 0.01f && uphillDot > 0.15f && planarSpeed < requiredClimbSpeed)
                {
                    Vector3 downhill = uphillPlanar.sqrMagnitude > 0.0001f ? -uphillPlanar : Vector3.zero;
                    float slideTargetSpeed = Mathf.Max(minClimbEntrySpeed * 0.35f, requiredClimbSpeed * 0.5f);
                    Vector3 slideTarget = downhill * slideTargetSpeed;
                    _planarVelocity = Vector3.MoveTowards(_planarVelocity, slideTarget, steepSlopeSlideAccel * Time.fixedDeltaTime);
                }
                else
                {
                    Vector3 slopeAdjusted = Vector3.ProjectOnPlane(_planarVelocity, groundNormal);
                    _planarVelocity = new Vector3(slopeAdjusted.x, 0f, slopeAdjusted.z);

                    float drag = slopeDrag * (0.35f + slopeFactor);
                    _planarVelocity *= 1f / (1f + drag * Time.fixedDeltaTime);

                    if (uphillDot > 0f)
                    {
                        _planarVelocity -= uphillPlanar * (uphillDot * uphillDeceleration * (0.5f + slopeFactor) * Time.fixedDeltaTime);
                    }
                }
            }
            else
            {
                if (hasThrottleInput || allowPassiveSlopeSlide)
                {
                    Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    _planarVelocity += new Vector3(downhill.x, 0f, downhill.z) * (steepSlopeSlideAccel * Time.fixedDeltaTime);
                }
            }

            if (!hasThrottleInput && _planarVelocity.magnitude < idleStopSpeed)
            {
                _planarVelocity = Vector3.zero;
            }

            if (!hasThrottleInput && slopeAngle > 2f)
            {
                float idleSlopeBrake = idleBrake * (1f + slopeFactor * 2.5f);
                _planarVelocity = Vector3.MoveTowards(_planarVelocity, Vector3.zero, idleSlopeBrake * Time.fixedDeltaTime);
                if (_planarVelocity.magnitude < idleStopSpeed * 1.25f)
                {
                    _planarVelocity = Vector3.zero;
                }
            }

            Vector3 targetPosition = _rigidbody.position + new Vector3(_planarVelocity.x, 0f, _planarVelocity.z) * Time.fixedDeltaTime;
            targetPosition.y = groundY;
            _rigidbody.MovePosition(targetPosition);
        }

        private void RotateBodyFromTurnInput()
        {
            float turn = _moveInput.x;

            Transform body = tankBody != null ? tankBody : transform;
            if (Mathf.Abs(turn) > 0.001f)
            {
                body.Rotate(0f, turn * rotationSpeed * Time.fixedDeltaTime, 0f, Space.World);
            }

            ApplySlopeTilt(body);
        }

        private void ApplySlopeTilt(Transform body)
        {
            if (body == null)
            {
                return;
            }

            Vector3 targetNormal = _groundNormal.sqrMagnitude > 0.0001f ? _groundNormal.normalized : Vector3.up;
            float slopeAngle = Vector3.Angle(Vector3.up, targetNormal);
            if (slopeAngle > maxSlopeTiltAngle && slopeAngle > 0.001f)
            {
                float t = maxSlopeTiltAngle / slopeAngle;
                targetNormal = Vector3.Slerp(Vector3.up, targetNormal, t);
            }

            targetNormal = Vector3.Slerp(Vector3.up, targetNormal, Mathf.Clamp01(slopeTiltStrength));
            Vector3 forwardOnSlope = Vector3.ProjectOnPlane(body.forward, targetNormal);
            if (forwardOnSlope.sqrMagnitude <= 0.0001f)
            {
                forwardOnSlope = Vector3.ProjectOnPlane(transform.forward, targetNormal);
            }
            if (forwardOnSlope.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(forwardOnSlope.normalized, targetNormal);
            body.rotation = Quaternion.Slerp(
                body.rotation,
                targetRotation,
                Mathf.Clamp01(slopeTiltResponsiveness * Time.fixedDeltaTime));
        }

        private void RotateTurretToMouse()
        {
            if (turret == null)
            {
                return;
            }

            if (!useMouseForTurretAim)
            {
                return;
            }

            if (!_turretAimInitialized)
            {
                InitializeTurretAimState();
            }

            if (_cachedCamera == null)
            {
                _cachedCamera = aimCamera != null ? aimCamera : Camera.main;
            }

            Transform basis = tankBody != null ? tankBody : transform;
            Vector3 lookForward = _cachedCamera != null ? _cachedCamera.transform.forward : basis.forward;
            CameraController cameraController = _cachedCamera != null ? _cachedCamera.GetComponent<CameraController>() : null;
            if (cameraController != null)
            {
                lookForward = cameraController.AimForward;
            }

            Vector3 planarAim = Vector3.ProjectOnPlane(lookForward, Vector3.up);
            if (planarAim.sqrMagnitude <= 0.0001f)
            {
                planarAim = basis.forward;
            }

            planarAim.Normalize();

            // Keep turret world aim independent from hull rotation.
            float targetYaw = Mathf.Atan2(planarAim.x, planarAim.z) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Asin(Mathf.Clamp(lookForward.y, -1f, 1f)) * Mathf.Rad2Deg;
            targetPitch *= mouseTurretSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minTurretPitch, maxTurretPitch);

            _turretYaw = Mathf.MoveTowardsAngle(_turretYaw, targetYaw, turretRotationSpeed * Time.deltaTime);
            _turretPitch = Mathf.MoveTowards(_turretPitch, targetPitch, turretRotationSpeed * Time.deltaTime);

            Quaternion targetWorld = Quaternion.Euler(_turretPitch, _turretYaw, 0f);
            turret.rotation = Quaternion.RotateTowards(turret.rotation, targetWorld, turretRotationSpeed * Time.deltaTime);
        }

        private Transform ResolveBodyTransform()
        {
            if (tankBody != null) return tankBody;

            Transform explicitBody = transform.Find("TankBody");
            if (explicitBody != null) return explicitBody;

            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name.ToLowerInvariant();
                if (n.Contains("body") || n.Contains("hull") || n.Contains("chassis"))
                    return all[i];
            }

            return transform;
        }

        private Transform ResolveTurretTransform()
        {
            if (turret != null) return turret;

            Transform explicitTurret = transform.Find("Turret");
            if (explicitTurret != null) return explicitTurret;

            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name.ToLowerInvariant();
                if (n.Contains("turret") || n.Contains("cannon") || n.Contains("gun"))
                    return all[i];
            }

            return tankBody != null ? tankBody : transform;
        }

        private Transform ResolveFirePointTransform()
        {
            if (firePoint != null) return firePoint;

            if (turret != null)
            {
                Transform explicitFirePoint = turret.Find("FirePoint");
                if (explicitFirePoint != null) return explicitFirePoint;

                Transform turretMuzzle = FindBestMuzzleFromTurret(turret);
                if (turretMuzzle != null)
                {
                    return turretMuzzle;
                }
            }

            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name.ToLowerInvariant();
                if (n.Contains("firepoint") || n.Contains("muzzle") || n.Contains("barrelend"))
                    return all[i];
            }

            // Fallback: synthesize one so firing always works.
            Transform parent = turret != null ? turret : transform;
            GameObject go = new GameObject("FirePoint");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.2f, 1.6f);
            go.transform.localRotation = Quaternion.identity;
            return go.transform;
        }

        private Transform FindBestMuzzleFromTurret(Transform turretRoot)
        {
            Transform[] all = turretRoot.GetComponentsInChildren<Transform>(true);
            Transform best = null;
            float bestScore = float.NegativeInfinity;
            Vector3 forward = turretRoot.forward;

            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == turretRoot) continue;

                string n = t.name.ToLowerInvariant();
                bool muzzleNamed = n.Contains("firepoint") || n.Contains("muzzle") || n.Contains("barrel") || n.Contains("cannon");
                if (!muzzleNamed)
                {
                    continue;
                }

                Vector3 toCandidate = t.position - turretRoot.position;
                float score = Vector3.Dot(forward, toCandidate) * 10f + toCandidate.magnitude;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            return best;
        }

        private void InitializeTurretAimState()
        {
            if (turret == null)
            {
                _turretAimInitialized = false;
                return;
            }

            Vector3 worldEuler = turret.rotation.eulerAngles;
            _turretYaw = NormalizeDegrees(worldEuler.y);
            _turretPitch = NormalizeDegrees(worldEuler.x);
            _turretAimInitialized = true;

            _turretYaw = Mathf.Repeat(_turretYaw, 360f);
            if (_turretYaw > 180f)
                _turretYaw -= 360f;

            _turretPitch = Mathf.Clamp(_turretPitch, minTurretPitch, maxTurretPitch);
        }

        private void CacheTurretRenderers()
        {
            if (turret == null)
            {
                _turretRenderers = new Renderer[0];
                _turretRendererDefaults = new bool[0];
                return;
            }

            _turretRenderers = turret.GetComponentsInChildren<Renderer>(true);
            _turretRendererDefaults = new bool[_turretRenderers.Length];
            for (int i = 0; i < _turretRenderers.Length; i++)
            {
                _turretRendererDefaults[i] = _turretRenderers[i] != null && _turretRenderers[i].enabled;
            }
        }

        private void UpdateTurretVisibilityByViewMode()
        {
            if (_turretRenderers == null || _turretRenderers.Length == 0)
            {
                return;
            }

            bool hideForCockpit = false;
            CameraController cameraController = _cachedCamera != null ? _cachedCamera.GetComponent<CameraController>() : null;
            if (cameraController != null && CompareTag("Player"))
            {
                hideForCockpit = cameraController.IsInTankMode;
            }

            for (int i = 0; i < _turretRenderers.Length; i++)
            {
                Renderer renderer = _turretRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                bool defaultVisible = i < _turretRendererDefaults.Length ? _turretRendererDefaults[i] : true;
                renderer.enabled = hideForCockpit ? false : defaultVisible;
            }
        }

        private float NormalizeDegrees(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        private float SampleGroundHeight(Vector3 proposedPosition, out Vector3 normal)
        {
            float maxDist = groundProbeHeight * 4f;
            Vector3 origin = proposedPosition + Vector3.up * groundProbeHeight;
            normal = Vector3.up;
            if (DebugVisualSettings.ShowRaycasts)
            {
                Debug.DrawRay(origin, Vector3.down * maxDist, Color.cyan, Time.fixedDeltaTime);
            }

            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDist, ~0, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                RaycastHit nearest = default;
                bool found = false;
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
                        found = true;
                    }
                }

                if (found)
                {
                    _groundHeight = nearest.point.y + groundSnapHeight;
                    normal = nearest.normal.sqrMagnitude > 0.0001f ? nearest.normal.normalized : Vector3.up;
                    return _groundHeight;
                }
            }

            return _groundHeight;
        }

        private bool IsSelfCollider(Collider collider)
        {
            if (collider == null)
            {
                return true;
            }

            Transform colliderTransform = collider.transform;
            return colliderTransform == transform || colliderTransform.IsChildOf(transform);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.contactCount <= 0 || collisionBounceStrength <= 0.001f)
            {
                return;
            }

            ContactPoint contact = collision.GetContact(0);
            Vector3 bounceDir = contact.normal.sqrMagnitude > 0.0001f ? contact.normal.normalized : Vector3.zero;
            if (bounceDir == Vector3.zero)
            {
                return;
            }

            float approachSpeed = Mathf.Abs(Vector3.Dot(_planarVelocity, -bounceDir));
            if (approachSpeed <= 0.02f)
            {
                return;
            }

            float impulse = approachSpeed * collisionBounceStrength;
            _planarVelocity += new Vector3(bounceDir.x, 0f, bounceDir.z) * impulse;
            _planarVelocity *= 1f / (1f + Mathf.Max(0f, collisionBounceDamping));
        }

        private float GetCurrentMoveSpeed()
        {
            bool hasSpeedBoost = PowerupManager.Instance != null
                                 && PowerupManager.Instance.IsPowerupActive(PlayerId, PowerupManager.SpeedBoostPowerup);
            return moveSpeed * (hasSpeedBoost ? speedBoostMultiplier : 1f);
        }

        private void UpdateTreadVisuals()
        {
            if (!animateTreads)
            {
                return;
            }

            if ((_leftTreads == null || _leftTreads.Length == 0) && (_rightTreads == null || _rightTreads.Length == 0))
            {
                ResolveTreadTransforms();
            }

            float leftInput = Mathf.Clamp(_moveInput.y - _moveInput.x, -1f, 1f);
            float rightInput = Mathf.Clamp(_moveInput.y + _moveInput.x, -1f, 1f);

            UpdateTreadAnimatorParams(leftInput, rightInput);

            if (syncDetachedTreadsToBody)
            {
                SyncTreadHierarchyToBody();
            }

            bool hasAnimatorDrivenTreads = _treadAnimators != null && _treadAnimators.Length > 0;
            if (!useLegacyTreadRotationFallback || hasAnimatorDrivenTreads)
            {
                return;
            }

            float leftTarget = leftInput * treadMaxSpinSpeed * (invertLeftTreadRotation ? -1f : 1f);
            float rightTarget = rightInput * treadMaxSpinSpeed * (invertRightTreadRotation ? -1f : 1f);

            bool leftDirectionFlip = Mathf.Abs(leftTarget) > 0.01f
                                     && Mathf.Abs(_leftTreadSpinVelocity) > 0.01f
                                     && Mathf.Sign(leftTarget) != Mathf.Sign(_leftTreadSpinVelocity);
            bool rightDirectionFlip = Mathf.Abs(rightTarget) > 0.01f
                                      && Mathf.Abs(_rightTreadSpinVelocity) > 0.01f
                                      && Mathf.Sign(rightTarget) != Mathf.Sign(_rightTreadSpinVelocity);

            if (leftDirectionFlip)
            {
                _leftTreadSpinVelocity = leftTarget;
            }
            else
            {
                _leftTreadSpinVelocity = Mathf.MoveTowards(
                    _leftTreadSpinVelocity,
                    leftTarget,
                    treadSpinAcceleration * Time.deltaTime);
            }

            if (rightDirectionFlip)
            {
                _rightTreadSpinVelocity = rightTarget;
            }
            else
            {
                _rightTreadSpinVelocity = Mathf.MoveTowards(
                    _rightTreadSpinVelocity,
                    rightTarget,
                    treadSpinAcceleration * Time.deltaTime);
            }

            float leftSpin = _leftTreadSpinVelocity * Time.deltaTime;
            float rightSpin = _rightTreadSpinVelocity * Time.deltaTime;
            Vector3 axis = treadSpinAxis.sqrMagnitude > 0.0001f ? treadSpinAxis.normalized : Vector3.forward;

            for (int i = 0; i < _leftTreads.Length; i++)
            {
                if (_leftTreads[i] != null) _leftTreads[i].Rotate(axis, leftSpin, Space.Self);
            }

            for (int i = 0; i < _rightTreads.Length; i++)
            {
                if (_rightTreads[i] != null) _rightTreads[i].Rotate(axis, rightSpin, Space.Self);
            }
        }

        private void ResolveTreadTransforms()
        {
            // Search from tank root (not only body) because many prefabs place tracks as siblings of hull.
            Transform root = transform;
            Transform sideBasis = tankBody != null ? tankBody : transform;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            System.Collections.Generic.List<Transform> left = new System.Collections.Generic.List<Transform>(4);
            System.Collections.Generic.List<Transform> right = new System.Collections.Generic.List<Transform>(4);

            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                string n = t.name.ToLowerInvariant();
                if (!n.Contains("track") && !n.Contains("tread") && !n.Contains("wheel"))
                {
                    continue;
                }

                bool isLeft = n.Contains("left") || n.Contains("_l") || n.Contains(".l");
                bool isRight = n.Contains("right") || n.Contains("_r") || n.Contains(".r");

                if (!isLeft && !isRight)
                {
                    // Use body orientation for side detection so tracks follow visible mesh orientation.
                    float sideDot = Vector3.Dot(sideBasis.right, (t.position - sideBasis.position));
                    isLeft = sideDot < 0f;
                    isRight = !isLeft;
                }

                if (isLeft)
                {
                    left.Add(t);
                }
                else if (isRight)
                {
                    right.Add(t);
                }
            }

            _leftTreads = left.ToArray();
            _rightTreads = right.ToArray();

            System.Collections.Generic.List<Animator> animators = new System.Collections.Generic.List<Animator>(8);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null) continue;

                string n = t.name.ToLowerInvariant();
                if (!n.Contains("track") && !n.Contains("tread") && !n.Contains("wheel"))
                {
                    continue;
                }

                Animator a = t.GetComponent<Animator>();
                if (a != null && !animators.Contains(a))
                {
                    a.applyRootMotion = false;
                    animators.Add(a);
                }
            }

            _treadAnimators = animators.ToArray();
            if (syncDetachedTreadsToBody)
            {
                SyncTreadHierarchyToBody();
            }
        }

        private void SyncTreadHierarchyToBody()
        {
            Transform body = tankBody != null ? tankBody : transform;
            if (body == null)
            {
                return;
            }

            ReparentDetachedTreads(_leftTreads, body);
            ReparentDetachedTreads(_rightTreads, body);
        }

        private static void ReparentDetachedTreads(Transform[] treads, Transform body)
        {
            if (treads == null || body == null)
            {
                return;
            }

            for (int i = 0; i < treads.Length; i++)
            {
                Transform tread = treads[i];
                if (tread == null || tread == body)
                {
                    continue;
                }

                if (!tread.IsChildOf(body))
                {
                    tread.SetParent(body, true);
                }
            }
        }

        private void UpdateTreadAnimatorParams(float leftInput, float rightInput)
        {
            if (_treadAnimators == null || _treadAnimators.Length == 0)
            {
                return;
            }

            float throttle = _moveInput.y;
            float turn = _moveInput.x;
            float speed = _planarVelocity.magnitude;
            bool driveMoving = Mathf.Abs(throttle) > 0.01f;
            float leftTrack = leftInput;
            float rightTrack = rightInput;

            for (int i = 0; i < _treadAnimators.Length; i++)
            {
                Animator a = _treadAnimators[i];
                if (a == null || !a.isActiveAndEnabled) continue;
                a.applyRootMotion = false;
                a.speed = driveMoving ? 1f : 0f;

                // Set common names; only existing params are used.
                TrySetAnimatorFloat(a, "Throttle", throttle);
                TrySetAnimatorFloat(a, "Turn", turn);
                TrySetAnimatorFloat(a, "Speed", speed);
                TrySetAnimatorFloat(a, "Move", throttle);
                TrySetAnimatorFloat(a, "LeftTrack", leftTrack);
                TrySetAnimatorFloat(a, "RightTrack", rightTrack);
                TrySetAnimatorFloat(a, "Direction", throttle);
                TrySetAnimatorFloat(a, "Forward", driveMoving ? 1f : 0f);
            }
        }

        private static void TrySetAnimatorFloat(Animator animator, string paramName, float value)
        {
            if (animator == null || string.IsNullOrEmpty(paramName)) return;

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Float && parameters[i].name == paramName)
                {
                    animator.SetFloat(paramName, value);
                    return;
                }
            }
        }

        private void EnsureTrajectoryLine()
        {
            if (_trajectoryLine != null)
            {
                return;
            }

            GameObject lineGo = new GameObject("TrajectoryLine");
            lineGo.transform.SetParent(transform, false);
            _trajectoryLine = lineGo.AddComponent<LineRenderer>();
            _trajectoryLine.useWorldSpace = true;
            _trajectoryLine.loop = false;
            _trajectoryLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trajectoryLine.receiveShadows = false;
            _trajectoryLine.textureMode = LineTextureMode.Stretch;
            _trajectoryLine.numCornerVertices = 2;
            _trajectoryLine.numCapVertices = 2;

            if (_trajectoryLineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                _trajectoryLineMaterial = new Material(shader);
            }

            _trajectoryLine.sharedMaterial = _trajectoryLineMaterial;
            _trajectoryLine.enabled = false;
        }

        private void UpdateTrajectoryLine()
        {
            if (_trajectoryLine == null)
            {
                EnsureTrajectoryLine();
            }

            if (_trajectoryLine == null)
            {
                return;
            }

            if (!showTrajectoryLine || !DebugVisualSettings.ShowTrajectoryLine || _weaponController == null || firePoint == null)
            {
                _trajectoryLine.enabled = false;
                return;
            }

            int segments = Mathf.Max(2, trajectorySegments);
            float step = Mathf.Max(0.01f, trajectoryStepSeconds);

            _trajectoryLine.enabled = true;
            _trajectoryLine.positionCount = segments;
            _trajectoryLine.startColor = trajectoryLineColor;
            _trajectoryLine.endColor = trajectoryLineColor;
            _trajectoryLine.startWidth = trajectoryLineWidth;
            _trajectoryLine.endWidth = trajectoryLineWidth;

            Vector3 pos = _weaponController.GetProjectileSpawnPosition(firePoint);
            Vector3 vel = _weaponController.GetLaunchVelocity(firePoint);
            Vector3 gravity = _weaponController.UseBallisticArc ? Physics.gravity : Vector3.zero;

            _trajectoryLine.SetPosition(0, pos);
            int used = 1;

            for (int i = 1; i < segments; i++)
            {
                Vector3 next = pos + vel * step + 0.5f * gravity * (step * step);
                Vector3 segment = next - pos;
                RaycastHit hit;
                if (Physics.Raycast(pos, segment.normalized, out hit, segment.magnitude, ~0, QueryTriggerInteraction.Ignore) && !IsSelfCollider(hit.collider))
                {
                    _trajectoryLine.SetPosition(i, hit.point);
                    used = i + 1;
                    break;
                }

                _trajectoryLine.SetPosition(i, next);
                used = i + 1;
                vel += gravity * step;
                pos = next;
            }

            if (used < segments)
            {
                _trajectoryLine.positionCount = used;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!DebugVisualSettings.ShowProjectileArc)
            {
                return;
            }

            WeaponController wc = GetComponent<WeaponController>();
            if (wc == null || firePoint == null)
            {
                return;
            }

            Vector3 velocity = wc.GetLaunchVelocity(firePoint);
            Vector3 pos = wc.GetProjectileSpawnPosition(firePoint);
            Vector3 gravity = Physics.gravity;
            float dt = 0.1f;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < 30; i++)
            {
                Vector3 next = pos + velocity * dt + 0.5f * gravity * (dt * dt);
                Gizmos.DrawLine(pos, next);
                velocity += gravity * dt;
                pos = next;
            }
        }
    }
}
