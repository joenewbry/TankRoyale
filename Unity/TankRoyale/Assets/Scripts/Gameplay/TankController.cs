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
        [SerializeField] private float maxStepHeight = 0.65f;
        [SerializeField] private float stepProbeDistance = 1.1f;
        [SerializeField] private float stepLiftSpeed = 8f;
        [SerializeField] private float maxStepRisePerSecond = 2.6f;
        [SerializeField] private float stepGroundProbePadding = 0.2f;
        [SerializeField] private float jumpImpulse = 8.4f;
        [SerializeField] private float jumpGravity = 12.5f;
        [SerializeField] private float jumpCooldown = 0.2f;
        [SerializeField] private float landingBounceFactor = 0.22f;
        [SerializeField] private float maxLandingBounce = 1.8f;
        [SerializeField] [Range(0f, 1f)] private float slopeTiltStrength = 0.85f;
        [SerializeField] private float maxSlopeTiltAngle = 34f;
        [SerializeField] private float slopeTiltResponsiveness = 12f;
        [SerializeField] private float groundNormalSmoothing = 14f;
        [SerializeField] private float groundSampleRadius = 0.58f;
        [SerializeField] private float rampGripAcceleration = 14f;
        [SerializeField] private float rampGripNormalMinY = 0.28f;
        [SerializeField] private float rampGripNormalMaxY = 0.96f;
        [SerializeField] private float rampProbeForwardOffset = 0.78f;
        [SerializeField] private float rampProbeHeight = 0.9f;
        [SerializeField] private float rampProbeDownDistance = 1.8f;
        [SerializeField] private float edgeProbeOffset = 0.58f;
        [SerializeField] private float edgeProbeHeight = 0.9f;
        [SerializeField] private float edgeProbeDownDistance = 1.8f;
        [SerializeField] [Range(0f, 1f)] private float edgeTipStrength = 0.42f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 420f;
        [SerializeField] private float turretRotationSpeed = 720f;

        [Header("Mouse Turret")]
        [SerializeField] private float mouseTurretSensitivity = 1.6f;
        [SerializeField] private float minTurretPitch = -15f;
        [SerializeField] private float maxTurretPitch = 45f;

        [Header("Camera Feel")]
        [SerializeField] private bool useMouseForTurretAim = true;

        [Header("Debug Input Logging")]
        [SerializeField] private bool logMouseTurretInput = false;
        [SerializeField] private float mouseLogIntervalSeconds = 0.12f;

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
        [SerializeField] private bool showTrajectoryLine = false;
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
        private Renderer[] _allTankRenderers = new Renderer[0];
        private bool[] _allTankRendererDefaults = new bool[0];
        private Renderer[] _turretRenderers = new Renderer[0];
        private bool[] _turretRendererDefaults = new bool[0];
        private Transform[] _leftTreads = new Transform[0];
        private Transform[] _rightTreads = new Transform[0];
        private Animator[] _treadAnimators = new Animator[0];
        private float _leftTreadSpinVelocity;
        private float _rightTreadSpinVelocity;
        private LineRenderer _trajectoryLine;
        private static Material _trajectoryLineMaterial;
        private float _nextMouseLogTime;
        private float _jumpVelocity;
        private float _jumpOffset;
        private bool _jumpRequested;
        private float _lastJumpTime = -999f;
        private bool _wasGrounded = true;

        public Transform FirePoint => firePoint;
        public string PlayerId => string.IsNullOrWhiteSpace(playerId) ? gameObject.name : playerId;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float CurrentSpeed => _planarVelocity.magnitude;
        public float CurrentSlopeAngle => Vector3.Angle(_groundNormal, Vector3.up);
        public Vector3 CurrentGroundNormal => _groundNormal;
        public Vector3 CurrentBodyForward
        {
            get
            {
                Transform body = tankBody != null ? tankBody : transform;
                Vector3 planar = Vector3.ProjectOnPlane(body.forward, Vector3.up);
                if (planar.sqrMagnitude <= 0.0001f)
                {
                    return transform.forward;
                }
                return planar.normalized;
            }
        }
        public Vector3 CurrentBodyUp
        {
            get
            {
                Transform body = tankBody != null ? tankBody : transform;
                return body != null ? body.up : Vector3.up;
            }
        }
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

            if (CompareTag("Player") && invertMovement)
            {
                // Prevent inverted startup controls in first-person playtests.
                invertMovement = false;
            }

            tankBody = ResolveBodyTransform();
            turret = ResolveTurretTransform();
            firePoint = ResolveFirePointTransform();
            InitializeTurretAimState();
            CacheAllTankRenderers();
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
            if (GameCheatState.GodModeEnabled && CompareTag("Player")) return;

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
            if (Input.GetMouseButtonDown(0) && _weaponController != null && !BuildModeController.IsBuildModeActive)
            {
                _weaponController.Fire();
            }

            if (Input.GetMouseButtonDown(1) && _weaponController != null && !BuildModeController.IsBuildModeActive)
            {
                _weaponController.FireMissile();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpRequested = true;
            }
        }

        private void ReadMovementInput()
        {
            if (ShouldSuppressTankInputForCurrentCameraMode())
            {
                _moveInput = Vector2.zero;
                return;
            }

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
            ApplyRampGrip(basis, throttle, hasThrottleInput, ref groundY, ref groundNormal);
            Vector3 sampledNormal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
            if (_groundNormal.sqrMagnitude <= 0.0001f)
            {
                _groundNormal = sampledNormal;
            }
            else
            {
                float smooth = Mathf.Clamp01(Mathf.Max(0.01f, groundNormalSmoothing) * Time.fixedDeltaTime);
                _groundNormal = Vector3.Slerp(_groundNormal.normalized, sampledNormal, smooth);
            }

            float slopeAngle = Vector3.Angle(_groundNormal, Vector3.up);
            float slopeFactor = Mathf.InverseLerp(0f, Mathf.Max(1f, maxClimbSlopeAngle), slopeAngle);
            Vector3 planarVelocity = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
            float planarSpeed = planarVelocity.magnitude;

            if (slopeAngle <= maxClimbSlopeAngle)
            {
                Vector3 uphillDir = Vector3.ProjectOnPlane(Vector3.up, _groundNormal).normalized;
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
                    Vector3 slopeAdjusted = Vector3.ProjectOnPlane(_planarVelocity, _groundNormal);
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
                    Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, _groundNormal).normalized;
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
            bool canStepAssist = _jumpOffset <= 0.001f;
            if (canStepAssist && TryGetStepClimbTargetY(basis, throttle, hasThrottleInput, out float stepY))
            {
                float climbDelta = Mathf.Max(0f, stepY - _rigidbody.position.y);
                float maxRise = Mathf.Max(0.01f, maxStepRisePerSecond) * Time.fixedDeltaTime;
                float lift = Mathf.Min(climbDelta, maxRise);
                float climbedY = Mathf.MoveTowards(_rigidbody.position.y, _rigidbody.position.y + lift, stepLiftSpeed * Time.fixedDeltaTime);
                groundY = Mathf.Max(groundY, climbedY);
            }

            bool grounded = _jumpOffset <= 0.001f && Mathf.Abs(_rigidbody.position.y - groundY) <= 0.2f;
            if (_jumpRequested && grounded && (Time.time - _lastJumpTime) >= jumpCooldown)
            {
                _jumpVelocity = Mathf.Max(0.1f, jumpImpulse);
                _jumpRequested = false;
                _lastJumpTime = Time.time;
            }
            else if (_jumpRequested && !grounded)
            {
                _jumpRequested = false;
            }

            if (_jumpVelocity != 0f || _jumpOffset > 0f)
            {
                _jumpVelocity -= Mathf.Max(0.1f, jumpGravity) * Time.fixedDeltaTime;
                _jumpOffset = Mathf.Max(0f, _jumpOffset + (_jumpVelocity * Time.fixedDeltaTime));
                if (_jumpOffset <= 0f && _jumpVelocity < 0f)
                {
                    _jumpVelocity = 0f;
                }
            }

            // Springy landing: convert some downward impact into a small rebound.
            if (!_wasGrounded && grounded && _jumpVelocity < -0.25f)
            {
                float rebound = Mathf.Min(maxLandingBounce, -_jumpVelocity * Mathf.Max(0f, landingBounceFactor));
                if (rebound > 0.05f)
                {
                    _jumpVelocity = rebound;
                    _jumpOffset = Mathf.Max(_jumpOffset, 0.01f);
                    grounded = false;
                }
            }

            targetPosition.y = groundY;
            if (_jumpOffset > 0f)
            {
                targetPosition.y += _jumpOffset;
            }
            _rigidbody.MovePosition(targetPosition);
            _wasGrounded = grounded;
        }

        private void ApplyRampGrip(Transform basis, float throttle, bool hasThrottleInput, ref float groundY, ref Vector3 groundNormal)
        {
            if (!hasThrottleInput || basis == null || Mathf.Abs(throttle) < 0.01f)
            {
                return;
            }

            Vector3 planarForward = basis.forward;
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            planarForward.Normalize();
            float sign = Mathf.Sign(throttle);
            Vector3 probeOrigin = _rigidbody.position
                                + (planarForward * (rampProbeForwardOffset * sign))
                                + Vector3.up * Mathf.Max(0.2f, rampProbeHeight);

            if (!Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit hit, Mathf.Max(0.2f, rampProbeDownDistance), ~0, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            if (hit.collider == null || IsSelfCollider(hit.collider))
            {
                return;
            }

            Vector3 hitNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
            if (hitNormal.y < rampGripNormalMinY || hitNormal.y > rampGripNormalMaxY)
            {
                return;
            }

            Vector3 moveDir = planarForward * sign;
            Vector3 alongSlope = Vector3.ProjectOnPlane(moveDir, hitNormal);
            alongSlope.y = 0f;
            if (alongSlope.sqrMagnitude > 0.0001f)
            {
                alongSlope.Normalize();
                _planarVelocity += alongSlope * (rampGripAcceleration * Time.fixedDeltaTime * Mathf.Abs(throttle));
            }

            groundY = Mathf.Max(groundY, hit.point.y + groundSnapHeight);
            groundNormal = Vector3.Slerp(groundNormal, hitNormal, 0.65f);
        }

        private bool TryGetStepClimbTargetY(Transform basis, float throttle, bool hasThrottleInput, out float targetY)
        {
            targetY = _rigidbody.position.y;
            if (!hasThrottleInput || throttle <= 0.1f || basis == null || maxStepHeight <= 0.01f || stepProbeDistance <= 0.01f)
            {
                return false;
            }

            Vector3 direction = basis.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }
            direction.Normalize();

            float lowerHeight = groundSnapHeight + 0.06f;
            Vector3 lowerOrigin = _rigidbody.position + Vector3.up * lowerHeight;
            Vector3 upperOrigin = lowerOrigin + Vector3.up * Mathf.Max(0.1f, maxStepHeight);

            bool lowerBlocked = Physics.Raycast(lowerOrigin, direction, out RaycastHit lowerHit, stepProbeDistance, ~0, QueryTriggerInteraction.Ignore)
                                && !IsSelfCollider(lowerHit.collider);
            if (!lowerBlocked)
            {
                return false;
            }

            // Step assist should only engage against near-vertical obstructions in front.
            if (lowerHit.normal.y > 0.25f)
            {
                return false;
            }

            bool upperBlocked = Physics.Raycast(upperOrigin, direction, out RaycastHit upperHit, stepProbeDistance, ~0, QueryTriggerInteraction.Ignore)
                                && !IsSelfCollider(upperHit.collider);
            if (upperBlocked)
            {
                return false;
            }

            Vector3 probeXZ = _rigidbody.position + direction * (stepProbeDistance + 0.2f);
            Vector3 probeOrigin = probeXZ + Vector3.up * (maxStepHeight + stepGroundProbePadding);
            float downDistance = (maxStepHeight * 2f) + 1.2f;
            if (!TrySampleGroundFrom(probeOrigin, downDistance, out float sampledY, out Vector3 sampledNormal))
            {
                return false;
            }

            // Require a climbable landing surface.
            if (sampledNormal.y < 0.55f)
            {
                return false;
            }

            float climbDelta = sampledY - _rigidbody.position.y;
            if (climbDelta < 0.03f || climbDelta > (maxStepHeight + 0.05f))
            {
                return false;
            }

            targetY = sampledY + groundSnapHeight;
            return true;
        }

        private bool TrySampleGroundFrom(Vector3 origin, float distance, out float y, out Vector3 normal)
        {
            y = _groundHeight;
            normal = Vector3.up;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, Mathf.Max(0.1f, distance), ~0, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            float nearest = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || IsSelfCollider(hit.collider))
                {
                    continue;
                }

                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    y = hit.point.y;
                    normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
                    found = true;
                }
            }

            return found;
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
            targetNormal = ApplyEdgeTipBias(body, targetNormal);
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

        private Vector3 ApplyEdgeTipBias(Transform body, Vector3 baseNormal)
        {
            if (body == null || edgeTipStrength <= 0.001f)
            {
                return baseNormal;
            }

            Vector3 right = body.right;
            Vector3 forward = body.forward;
            right.y = 0f;
            forward.y = 0f;
            if (right.sqrMagnitude <= 0.0001f || forward.sqrMagnitude <= 0.0001f)
            {
                return baseNormal;
            }

            right.Normalize();
            forward.Normalize();
            Vector3 center = _rigidbody != null ? _rigidbody.position : transform.position;

            bool hasFront = ProbeGroundAt(center + forward * edgeProbeOffset);
            bool hasBack = ProbeGroundAt(center - forward * edgeProbeOffset);
            bool hasRight = ProbeGroundAt(center + right * edgeProbeOffset);
            bool hasLeft = ProbeGroundAt(center - right * edgeProbeOffset);

            Vector3 tipBias = Vector3.zero;
            if (!hasFront) tipBias += forward;
            if (!hasBack) tipBias -= forward;
            if (!hasRight) tipBias += right;
            if (!hasLeft) tipBias -= right;

            if (tipBias.sqrMagnitude <= 0.0001f)
            {
                return baseNormal;
            }

            Vector3 tipped = (baseNormal + tipBias.normalized * edgeTipStrength).normalized;
            return Vector3.Slerp(baseNormal, tipped, edgeTipStrength);
        }

        private bool ProbeGroundAt(Vector3 worldPos)
        {
            Vector3 origin = worldPos + Vector3.up * Mathf.Max(0.2f, edgeProbeHeight);
            if (!TrySampleGroundFrom(origin, Mathf.Max(0.2f, edgeProbeDownDistance), out float sampleY, out _))
            {
                return false;
            }

            float currentY = _rigidbody != null ? _rigidbody.position.y : transform.position.y;
            return Mathf.Abs(currentY - sampleY) <= Mathf.Max(0.25f, maxStepHeight + 0.12f);
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
                if (cameraController.IsWorldExplorerMode)
                {
                    return;
                }

                lookForward = cameraController.AimForward;
            }

            Vector3 upAxis = basis.up.sqrMagnitude > 0.0001f ? basis.up.normalized : Vector3.up;
            Vector3 baseForward = Vector3.ProjectOnPlane(basis.forward, upAxis);
            if (baseForward.sqrMagnitude <= 0.0001f)
            {
                baseForward = Vector3.ProjectOnPlane(transform.forward, upAxis);
            }
            if (baseForward.sqrMagnitude <= 0.0001f)
            {
                baseForward = Vector3.forward;
            }
            baseForward.Normalize();

            Vector3 planarAim = Vector3.ProjectOnPlane(lookForward, upAxis);
            if (planarAim.sqrMagnitude <= 0.0001f)
            {
                planarAim = baseForward;
            }

            planarAim.Normalize();

            float targetYaw = Vector3.SignedAngle(baseForward, planarAim, upAxis);
            Vector3 rightAxis = Vector3.Cross(upAxis, planarAim);
            if (rightAxis.sqrMagnitude <= 0.0001f)
            {
                rightAxis = basis.right;
            }
            rightAxis.Normalize();
            float targetPitch = Vector3.SignedAngle(planarAim, lookForward.normalized, rightAxis);
            targetPitch *= mouseTurretSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, minTurretPitch, maxTurretPitch);

            _turretYaw = Mathf.MoveTowardsAngle(_turretYaw, targetYaw, turretRotationSpeed * Time.deltaTime);
            _turretPitch = Mathf.MoveTowards(_turretPitch, targetPitch, turretRotationSpeed * Time.deltaTime);

            Quaternion yawWorld = Quaternion.AngleAxis(_turretYaw, upAxis);
            Quaternion yawedFrame = yawWorld * Quaternion.LookRotation(baseForward, upAxis);
            Vector3 pitchAxis = yawedFrame * Vector3.right;
            Quaternion targetWorld = Quaternion.AngleAxis(_turretPitch, pitchAxis) * yawedFrame;
            Transform parent = turret.parent;
            if (parent != null)
            {
                Quaternion targetLocal = Quaternion.Inverse(parent.rotation) * targetWorld;
                turret.localRotation = Quaternion.RotateTowards(turret.localRotation, targetLocal, turretRotationSpeed * Time.deltaTime);
            }
            else
            {
                turret.rotation = Quaternion.RotateTowards(turret.rotation, targetWorld, turretRotationSpeed * Time.deltaTime);
            }

            if (logMouseTurretInput && Time.unscaledTime >= _nextMouseLogTime)
            {
                float mouseX = Input.GetAxisRaw("Mouse X");
                float mouseY = Input.GetAxisRaw("Mouse Y");
                _nextMouseLogTime = Time.unscaledTime + Mathf.Max(0.02f, mouseLogIntervalSeconds);
                Debug.Log(
                    $"[TurretMouse] mx={mouseX:0.000} my={mouseY:0.000} " +
                    $"look=({lookForward.x:0.000},{lookForward.y:0.000},{lookForward.z:0.000}) " +
                    $"targetYaw={targetYaw:0.00} targetPitch={targetPitch:0.00} " +
                    $"stateYaw={_turretYaw:0.00} statePitch={_turretPitch:0.00}");
            }
        }

        private bool ShouldSuppressTankInputForCurrentCameraMode()
        {
            if (_cachedCamera == null)
            {
                _cachedCamera = aimCamera != null ? aimCamera : Camera.main;
            }

            if (_cachedCamera == null)
            {
                return false;
            }

            CameraController cameraController = _cachedCamera.GetComponent<CameraController>();
            return cameraController != null && cameraController.IsWorldExplorerMode;
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

                Transform generatedFromGeometry = BuildFirePointFromTurretGeometry(turret);
                if (generatedFromGeometry != null)
                {
                    return generatedFromGeometry;
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
            Vector3 origin = turretRoot.position;

            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == turretRoot) continue;

                string n = t.name.ToLowerInvariant();
                bool muzzleNamed = n.Contains("firepoint")
                                   || n.Contains("muzzle")
                                   || n.Contains("barrelend")
                                   || n.Contains("barrel_end")
                                   || n.Contains("barreltip")
                                   || n.Contains("tip")
                                   || n.Contains("nozzle");
                if (!muzzleNamed)
                {
                    continue;
                }

                Vector3 toCandidate = t.position - origin;
                float forwardDist = Vector3.Dot(forward, toCandidate);
                float lateral = Vector3.Cross(forward, toCandidate).magnitude;
                float nameBonus = (n.Contains("firepoint") || n.Contains("muzzle")) ? 4f : 0f;
                float score = (forwardDist * 12f) - (lateral * 3f) + nameBonus;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            return best;
        }

        private Transform BuildFirePointFromTurretGeometry(Transform turretRoot)
        {
            if (turretRoot == null)
            {
                return null;
            }

            Renderer[] renderers = turretRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return null;
            }

            bool initialized = false;
            Bounds combined = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                if (!initialized)
                {
                    combined = r.bounds;
                    initialized = true;
                }
                else
                {
                    combined.Encapsulate(r.bounds);
                }
            }

            if (!initialized)
            {
                return null;
            }

            float forwardExtent = Mathf.Max(0.6f, Vector3.Dot(combined.extents, new Vector3(Mathf.Abs(turretRoot.forward.x), Mathf.Abs(turretRoot.forward.y), Mathf.Abs(turretRoot.forward.z))));
            Vector3 worldPos = combined.center + turretRoot.forward * (forwardExtent + 0.1f);

            GameObject go = new GameObject("FirePoint");
            go.transform.SetParent(turretRoot, true);
            go.transform.position = worldPos;
            go.transform.rotation = turretRoot.rotation;
            return go.transform;
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
            if ((_turretRenderers == null || _turretRenderers.Length == 0)
                && (_allTankRenderers == null || _allTankRenderers.Length == 0))
            {
                return;
            }

            bool hideForCockpit = false;
            CameraController cameraController = _cachedCamera != null ? _cachedCamera.GetComponent<CameraController>() : null;
            if (cameraController != null && CompareTag("Player"))
            {
                hideForCockpit = cameraController.IsInTankMode;
            }

            for (int i = 0; i < _allTankRenderers.Length; i++)
            {
                Renderer renderer = _allTankRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                bool defaultVisible = i < _allTankRendererDefaults.Length ? _allTankRendererDefaults[i] : true;
                renderer.enabled = hideForCockpit ? false : defaultVisible;
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

        private void CacheAllTankRenderers()
        {
            _allTankRenderers = GetComponentsInChildren<Renderer>(true);
            _allTankRendererDefaults = new bool[_allTankRenderers.Length];
            for (int i = 0; i < _allTankRenderers.Length; i++)
            {
                _allTankRendererDefaults[i] = _allTankRenderers[i] != null && _allTankRenderers[i].enabled;
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
            Vector3[] offsets =
            {
                Vector3.zero,
                new Vector3(groundSampleRadius, 0f, 0f),
                new Vector3(-groundSampleRadius, 0f, 0f),
                new Vector3(0f, 0f, groundSampleRadius),
                new Vector3(0f, 0f, -groundSampleRadius)
            };

            float weightedHeight = 0f;
            float totalWeight = 0f;
            Vector3 weightedNormal = Vector3.zero;
            bool foundAny = false;

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 origin = proposedPosition + offsets[i] + Vector3.up * groundProbeHeight;
                if (DebugVisualSettings.ShowRaycasts)
                {
                    Debug.DrawRay(origin, Vector3.down * maxDist, Color.cyan, Time.fixedDeltaTime);
                }

                if (!TrySampleGroundFrom(origin, maxDist, out float sampleY, out Vector3 sampleNormal))
                {
                    continue;
                }

                float weight = (i == 0) ? 1.6f : 1f;
                weightedHeight += sampleY * weight;
                weightedNormal += sampleNormal * weight;
                totalWeight += weight;
                foundAny = true;
            }

            if (!foundAny || totalWeight <= 0.0001f)
            {
                normal = _groundNormal.sqrMagnitude > 0.0001f ? _groundNormal : Vector3.up;
                return _groundHeight;
            }

            _groundHeight = (weightedHeight / totalWeight) + groundSnapHeight;
            normal = weightedNormal.sqrMagnitude > 0.0001f ? (weightedNormal / totalWeight).normalized : Vector3.up;
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

            // Ignore ground-ish contacts to avoid fake hop/jitter while climbing slopes/steps.
            if (bounceDir.y > 0.3f)
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
            Vector3 gravity = wc.UseBallisticArc ? Physics.gravity : Vector3.zero;
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
