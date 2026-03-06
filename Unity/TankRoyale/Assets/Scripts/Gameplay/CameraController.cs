using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        private const int InTankMode = 0;
        private const int StareDownMuzzleMode = 1;
        private const int TopOfTankMode = 2;
        private const int OverheadMode = 3;
        private const int WorldExplorerMode = 4;
        private const int SideInspectionMode = 5;
        private const int DriveAssistMode = 6;
        private const int ZDriveMode = 7;
        private const int CameraModeCount = 8;

        [Header("References")]
        [SerializeField] private Transform playerTank;
        [SerializeField] private Transform playerTurret;

        [Header("IN_TANK")]
        [SerializeField] private Vector3 cockpitLocalOffset = new Vector3(0f, 1.3f, 0.2f);
        [SerializeField] private bool showCockpitTargetOverlay = true;
        [SerializeField] private Color cockpitOverlayColor = new Color(0.2f, 1f, 0.5f, 0.9f);
        [SerializeField] private float overlaySize = 22f;
        [SerializeField] private float overlayThickness = 2f;
        [SerializeField] [Range(0.05f, 0.25f)] private float cockpitMaskPercent = 0.10f;
        [SerializeField] private Color cockpitMaskColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private bool showTargetArrow = true;
        [SerializeField] private bool showTurnDirectionArrow = true;
        [SerializeField] private float inTankClimbLift = 0.35f;

        [Header("IN_TANK Minimap")]
        [SerializeField] private bool showInTankMinimap = true;
        [SerializeField] private float minimapRange = 50f;
        [SerializeField] private float minimapSize = 136f;
        [SerializeField] private Vector2 minimapScreenOffset = new Vector2(-20f, 20f); // from top-right

        [Header("STARE_DOWN_MUZZLE")]
        [SerializeField] private float muzzleViewDistance = 7f;
        [SerializeField] private float muzzleViewHeight = 0.45f;

        [Header("TOP_OF_TANK")]
        [SerializeField] private Vector3 topOfTankLocalOffset = new Vector3(0f, 5.2f, -2.6f);
        [SerializeField] private float topOfTankPitch = 75f;
        [SerializeField] private float topOfTankFollowSpeed = 14f;

        [Header("OVERHEAD_VIEW")]
        [SerializeField] private float overheadHeight = 20f;
        [SerializeField] private float overheadOrthoSize = 12f;

        [Header("SIDE_INSPECTION")]
        [SerializeField] private Vector3 sideInspectionWorldOffset = new Vector3(9f, 3.4f, 0f);
        [SerializeField] private float sideInspectionLookHeight = 1.15f;
        [SerializeField] private float sideInspectionOrthoSize = 4.75f;
        [SerializeField] private float sideInspectionFollowSpeed = 10f;
        [SerializeField] private Color stageTelemetryColor = new Color(0.96f, 1f, 0.45f, 1f);

        [Header("DRIVE_ASSIST")]
        [SerializeField] private Vector3 driveAssistLocalOffset = new Vector3(0f, 2.25f, -5.5f);
        [SerializeField] private float driveAssistFollowSpeed = 11f;
        [SerializeField] private float driveAssistBodyUpBlend = 0.55f;

        [Header("Z_DRIVE")]
        [SerializeField] private Vector3 zDriveLocalOffset = new Vector3(0f, 2.35f, -6.2f);
        [SerializeField] private float zDriveFollowSpeed = 12f;
        [SerializeField] private float zDriveBodyUpBlend = 0.55f;
        [SerializeField] private float zDriveDefaultPitch = 12f;
        [SerializeField] private KeyCode zDriveKey = KeyCode.Z;

        [Header("Look")]
        [SerializeField] private float mouseLookSensitivity = 2f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 45f;
        [SerializeField] private bool lockCursorInFirstPerson = true;

        [Header("WORLD_EXPLORER")]
        [SerializeField] private float worldMoveSpeed = 16f;
        [SerializeField] private float worldFastMultiplier = 2f;
        [SerializeField] private float worldLookSensitivity = 2f;

        [Header("Follow")]
        [SerializeField] private float followSpeed = 8f;
        [SerializeField] private float nonWorldAnchorSmoothing = 14f;
        [SerializeField] private KeyCode switchKey = KeyCode.Tab;
        [SerializeField] private bool allowModeToggle = true;

        [Header("Lighting")]
        [SerializeField] private bool autoCreateFillLight = true;
        [SerializeField] private Color fillLightColor = new Color(0.9f, 0.93f, 1f, 1f);
        [SerializeField] private float fillLightIntensity = 0.38f;
        [SerializeField] private Vector3 fillLightEuler = new Vector3(32f, -120f, 0f);
        [SerializeField] private bool autoCreateSkyCornerLights = true;
        [SerializeField] private Color skyCornerLightColor = new Color(1f, 0.96f, 0.9f, 1f);
        [SerializeField] private float skyCornerLightIntensity = 1.65f;
        [SerializeField] private float skyCornerHeight = 16f;
        [SerializeField] private float skyCornerRangePadding = 12f;

        private Camera _camera;
        private int _mode = ZDriveMode;
        private float _yaw;
        private float _pitch = 10f;
        private bool _lookInitialized;
        private float _mouseDriveYaw;
        private float _mouseDrivePitch = 12f;
        private bool _mouseDriveLookInitialized;

        private float _worldYaw;
        private float _worldPitch;
        private bool _worldLookInitialized;

        private TankController _playerTankController;
        private Rigidbody _playerTankRigidbody;
        private Vector3 _smoothedTankAnchor;
        private bool _smoothedTankAnchorInitialized;
        private Transform _telemetryReferenceRoot;

        private static Texture2D _overlayPixel;
        public bool IsInTankMode => _mode == InTankMode;
        public bool IsWorldExplorerMode => _mode == WorldExplorerMode;
        public bool IsTopOfTankMode => _mode == TopOfTankMode;
        public bool IsOverheadMode => _mode == OverheadMode;
        public bool IsStareDownMode => _mode == StareDownMuzzleMode;
        public bool IsSideInspectionMode => _mode == SideInspectionMode;
        public bool IsDriveAssistMode => _mode == DriveAssistMode;
        public bool IsZDriveMode => _mode == ZDriveMode;
        public bool IsMouseDriveMode => _mode == DriveAssistMode || _mode == ZDriveMode;

        public Vector3 AimForward
        {
            get
            {
                if (_mode == WorldExplorerMode)
                {
                    return transform.forward;
                }

                return IsMouseDriveMode ? GetMouseDriveAimForward() : GetMouseDrivenAimForward();
            }
        }

        private void Start()
        {
            _camera = GetComponent<Camera>();
            ResolveReferences();
            InitializeLookState();
            EnsureFillLight();
            SetMode(_mode);
            SnapToCurrentModeTarget();
        }

        private void LateUpdate()
        {
            HandleModeInput();

            ResolveReferences();
            UpdateSmoothedTankAnchor();

            if (_mode == WorldExplorerMode)
            {
                HandleWorldExplorerInput();
                return;
            }

            HandleMouseLook();

            Vector3 targetPosition = GetTargetPosition();
            Quaternion targetRotation = GetTargetRotation();
            float activeFollowSpeed = _mode == TopOfTankMode
                ? topOfTankFollowSpeed
                : (_mode == SideInspectionMode
                    ? sideInspectionFollowSpeed
                    : (_mode == DriveAssistMode
                        ? driveAssistFollowSpeed
                        : (_mode == ZDriveMode ? zDriveFollowSpeed : followSpeed)));

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * activeFollowSpeed);
            if (targetRotation != Quaternion.identity)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * activeFollowSpeed);
            }
        }

        private void ResolveReferences()
        {
            if (playerTank == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTank = playerObject.transform;
                    _playerTankController = playerObject.GetComponent<TankController>();
                }
            }

            if (playerTank == null)
            {
                TankController fallbackTank = FindFirstObjectByType<TankController>();
                if (fallbackTank != null)
                {
                    playerTank = fallbackTank.transform;
                    _playerTankController = fallbackTank;
                }
            }

            if (_playerTankController == null && playerTank != null)
            {
                _playerTankController = playerTank.GetComponent<TankController>();
            }

            if (_playerTankRigidbody == null && playerTank != null)
            {
                _playerTankRigidbody = playerTank.GetComponent<Rigidbody>();
            }

            if (_playerTankController != null && _playerTankController.TurretTransform != null)
            {
                playerTurret = _playerTankController.TurretTransform;
            }
            else if (playerTurret == null && playerTank != null)
            {
                Transform foundTurret = playerTank.Find("Turret");
                if (foundTurret == null) foundTurret = playerTank.Find("turret");

                if (foundTurret == null)
                {
                    Transform[] children = playerTank.GetComponentsInChildren<Transform>(true);
                    for (int i = 0; i < children.Length; i++)
                    {
                        string childName = children[i].name;
                        if (childName == "Turret" || childName == "turret")
                        {
                            foundTurret = children[i];
                            break;
                        }
                    }
                }

                playerTurret = foundTurret;
            }
        }

        private void SetMode(int newMode)
        {
            int previousMode = _mode;
            _mode = WrapModeIndex(newMode);
            ApplyModeSettings();
            if (_mode == InTankMode)
            {
                _yaw = 0f;
                _pitch = 0f;
                _lookInitialized = true;
            }

            if (_mode == WorldExplorerMode)
            {
                SyncWorldLookToTransform();
            }
            else if (IsMouseDriveMode)
            {
                SyncMouseDriveLookToCurrentView(previousMode);
            }
            else
            {
                if (Cursor.lockState == CursorLockMode.Locked && lockCursorInFirstPerson)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private void ApplyModeSettings()
        {
            if (_camera == null)
            {
                return;
            }

            if (_mode == OverheadMode)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = overheadOrthoSize;
            }
            else if (_mode == SideInspectionMode)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = sideInspectionOrthoSize;
            }
            else
            {
                _camera.orthographic = false;
                _camera.fieldOfView = 60f;
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (_mode == OverheadMode)
            {
                return GetTankAnchorPosition() + Vector3.up * overheadHeight;
            }

            if (_mode == SideInspectionMode)
            {
                return GetTankAnchorPosition() + sideInspectionWorldOffset;
            }

            if (_mode == DriveAssistMode)
            {
                Quaternion aimRotation = GetMouseDriveLookRotation(driveAssistBodyUpBlend);
                return GetTankAnchorPosition() + (aimRotation * driveAssistLocalOffset);
            }

            if (_mode == ZDriveMode)
            {
                Quaternion aimRotation = GetMouseDriveLookRotation(zDriveBodyUpBlend);
                return GetTankAnchorPosition() + (aimRotation * zDriveLocalOffset);
            }

            if (_mode == InTankMode)
            {
                float climbLift = 0f;
                if (_playerTankController != null)
                {
                    climbLift = Mathf.Clamp01(_playerTankController.CurrentSlopeAngle / 35f) * inTankClimbLift;
                }

                if (playerTurret != null)
                {
                    return playerTurret.TransformPoint(cockpitLocalOffset) + Vector3.up * climbLift;
                }

                return playerTank != null ? playerTank.position + Vector3.up * (cockpitLocalOffset.y + climbLift) : transform.position;
            }

            if (_mode == StareDownMuzzleMode)
            {
                Vector3 anchor = GetTankAnchorPosition();
                Vector3 forward = GetStableTankForward();
                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                if (right.sqrMagnitude <= 0.0001f)
                {
                    right = Vector3.right;
                }

                // Fixed 45-degree monitor view used to verify muzzle/projectile origin.
                return anchor
                    + (Vector3.up * Mathf.Max(2f, muzzleViewDistance * 0.92f))
                    - (forward * Mathf.Max(2f, muzzleViewDistance * 0.9f))
                    + (right * 0.35f);
            }

            if (_mode == TopOfTankMode)
            {
                if (playerTank != null)
                {
                    Vector3 planarForward = GetStableTankForward();
                    Vector3 planarRight = Vector3.Cross(Vector3.up, planarForward).normalized;
                    if (planarRight.sqrMagnitude <= 0.0001f)
                    {
                        planarRight = Vector3.right;
                    }

                    Vector3 anchor = GetTankAnchorPosition();
                    return anchor
                         + planarRight * topOfTankLocalOffset.x
                         + Vector3.up * topOfTankLocalOffset.y
                         + planarForward * topOfTankLocalOffset.z;
                }

                return transform.position;
            }

            return transform.position;
        }

        private Quaternion GetTargetRotation()
        {
            if (_mode == OverheadMode)
            {
                return Quaternion.LookRotation(Vector3.down, Vector3.forward);
            }

            if (_mode == SideInspectionMode)
            {
                Vector3 lookTarget = GetTankAnchorPosition() + Vector3.up * Mathf.Max(0.25f, sideInspectionLookHeight);
                Vector3 to = lookTarget - GetTargetPosition();
                if (to.sqrMagnitude <= 0.0001f)
                {
                    to = Vector3.left;
                }

                return Quaternion.LookRotation(to.normalized, Vector3.up);
            }

            if (_mode == DriveAssistMode)
            {
                return GetMouseDriveLookRotation(driveAssistBodyUpBlend);
            }

            if (_mode == ZDriveMode)
            {
                return GetMouseDriveLookRotation(zDriveBodyUpBlend);
            }

            if (_mode == InTankMode)
            {
                return GetMouseLookRotation(0.8f);
            }

            if (_mode == StareDownMuzzleMode)
            {
                Vector3 lookTarget = GetTankAnchorPosition() + Vector3.up * Mathf.Max(0.8f, muzzleViewHeight);
                Transform muzzle = GetMuzzleTransform();
                if (muzzle != null)
                {
                    lookTarget = muzzle.position;
                }

                Vector3 from = GetTargetPosition();
                Vector3 to = lookTarget - from;
                if (to.sqrMagnitude <= 0.0001f)
                {
                    to = GetStableTankForward();
                }

                return Quaternion.LookRotation(to.normalized, GetCameraUpVector(0.35f));
            }

            if (_mode == TopOfTankMode)
            {
                Vector3 forward = GetStableTankForward();
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                Vector3 lookForward = Quaternion.Euler(topOfTankPitch, yaw, 0f) * Vector3.forward;
                return Quaternion.LookRotation(lookForward.normalized, GetCameraUpVector(0.45f));
            }

            return transform.rotation;
        }

        private Quaternion GetMouseLookRotation(float bodyBlend)
        {
            Vector3 up = GetCameraUpVector(bodyBlend);
            Vector3 baseForward = GetStableTankForward();
            Vector3 flattened = Vector3.ProjectOnPlane(baseForward, up);
            if (flattened.sqrMagnitude <= 0.0001f)
            {
                flattened = Vector3.ProjectOnPlane(transform.forward, up);
            }
            if (flattened.sqrMagnitude <= 0.0001f)
            {
                flattened = Vector3.forward;
            }

            Quaternion baseRotation = Quaternion.LookRotation(flattened.normalized, up);
            Quaternion yawRotation = Quaternion.AngleAxis(_yaw, up);
            Vector3 pitchAxis = (yawRotation * baseRotation) * Vector3.right;
            Quaternion pitchRotation = Quaternion.AngleAxis(_pitch, pitchAxis);
            Quaternion final = pitchRotation * yawRotation * baseRotation;
            return Quaternion.LookRotation(final * Vector3.forward, up);
        }

        private Quaternion GetMouseDriveLookRotation(float bodyBlend)
        {
            EnsureMouseDriveLookInitialized();

            Vector3 up = GetCameraUpVector(bodyBlend);
            Quaternion yawRotation = Quaternion.AngleAxis(_mouseDriveYaw, Vector3.up);
            Vector3 yawedForward = yawRotation * Vector3.forward;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, yawedForward);
            if (pitchAxis.sqrMagnitude <= 0.0001f)
            {
                pitchAxis = Vector3.right;
            }

            Quaternion pitchRotation = Quaternion.AngleAxis(_mouseDrivePitch, pitchAxis.normalized);
            Vector3 lookForward = pitchRotation * yawedForward;
            if (Vector3.Cross(up, lookForward).sqrMagnitude <= 0.0001f)
            {
                lookForward = Vector3.ProjectOnPlane(lookForward, up);
                if (lookForward.sqrMagnitude <= 0.0001f)
                {
                    lookForward = Vector3.ProjectOnPlane(Vector3.forward, up);
                }
            }

            return Quaternion.LookRotation(lookForward.normalized, up);
        }

        private Transform GetMuzzleTransform()
        {
            if (_playerTankController != null && _playerTankController.FirePoint != null)
            {
                return _playerTankController.FirePoint;
            }

            return playerTurret;
        }

        private void SnapToCurrentModeTarget()
        {
            transform.position = GetTargetPosition();
            Quaternion targetRotation = GetTargetRotation();
            if (targetRotation != Quaternion.identity)
            {
                transform.rotation = targetRotation;
            }
        }

        private void InitializeLookState()
        {
            if (_lookInitialized)
            {
                return;
            }

            _yaw = 0f;
            _pitch = 0f;
            _lookInitialized = true;
        }

        private void HandleMouseLook()
        {
            if (_mode == WorldExplorerMode)
            {
                if (Cursor.lockState == CursorLockMode.Locked && lockCursorInFirstPerson)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            if ((_mode == InTankMode || IsMouseDriveMode) && lockCursorInFirstPerson)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            float yawDelta = Input.GetAxisRaw("Mouse X") * mouseLookSensitivity;
            float pitchDelta = -Input.GetAxisRaw("Mouse Y") * mouseLookSensitivity;

            if (IsMouseDriveMode)
            {
                EnsureMouseDriveLookInitialized();
                _mouseDriveYaw += yawDelta;
                _mouseDrivePitch = Mathf.Clamp(_mouseDrivePitch + pitchDelta, minPitch, maxPitch);
                return;
            }

            if (!_lookInitialized)
            {
                InitializeLookState();
            }

            _yaw += yawDelta;
            _pitch = Mathf.Clamp(_pitch + pitchDelta, minPitch, maxPitch);
        }

        private void HandleWorldExplorerInput()
        {
            if (!_worldLookInitialized)
            {
                SyncWorldLookToTransform();
            }

            float speed = worldMoveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? worldFastMultiplier : 1f);

            int right = (Input.GetKey(KeyCode.L) ? 1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
            int left = (Input.GetKey(KeyCode.J) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? 1 : 0);
            int forward = (Input.GetKey(KeyCode.I) ? 1 : 0) + (Input.GetKey(KeyCode.W) ? 1 : 0);
            int back = (Input.GetKey(KeyCode.K) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? 1 : 0);
            int up = (Input.GetKey(KeyCode.U) ? 1 : 0) + (Input.GetKey(KeyCode.E) ? 1 : 0) + (Input.GetKey(KeyCode.PageUp) ? 1 : 0);
            int down = (Input.GetKey(KeyCode.O) ? 1 : 0) + (Input.GetKey(KeyCode.Q) ? 1 : 0) + (Input.GetKey(KeyCode.PageDown) ? 1 : 0);

            Vector3 move = transform.forward * (forward - back)
                         + transform.right * (right - left)
                         + Vector3.up * (up - down);

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            transform.position += move * (speed * Time.deltaTime);

            bool allowLook = Input.GetMouseButton(1);
            if (allowLook)
            {
                if (lockCursorInFirstPerson || Input.GetMouseButton(2))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                _worldYaw += Input.GetAxisRaw("Mouse X") * worldLookSensitivity;
                _worldPitch = Mathf.Clamp(_worldPitch - Input.GetAxisRaw("Mouse Y") * worldLookSensitivity, -89f, 89f);
                transform.rotation = Quaternion.Euler(_worldPitch, _worldYaw, 0f);
            }
            else
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }

        private static float NormalizePitch(float pitch)
        {
            float p = pitch;
            while (p > 180f) p -= 360f;
            while (p < -180f) p += 360f;
            return Mathf.Clamp(p, -89f, 89f);
        }

        private Vector3 GetStableTankForward()
        {
            if (_playerTankController != null)
            {
                Vector3 bodyForward = _playerTankController.CurrentBodyForward;
                if (bodyForward.sqrMagnitude > 0.0001f)
                {
                    return bodyForward.normalized;
                }
            }

            if (playerTank == null)
            {
                return transform.forward;
            }

            Vector3 planarForward = Vector3.ProjectOnPlane(playerTank.forward, Vector3.up);
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            }
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = Vector3.forward;
            }

            return planarForward.normalized;
        }

        private Vector3 GetCameraUpVector(float bodyBlend)
        {
            if (playerTank == null)
            {
                return Vector3.up;
            }

            if (_playerTankController != null)
            {
                Vector3 bodyUp = _playerTankController.CurrentBodyUp;
                if (bodyUp.sqrMagnitude > 0.0001f)
                {
                    return Vector3.Slerp(Vector3.up, bodyUp.normalized, Mathf.Clamp01(bodyBlend)).normalized;
                }

                Vector3 groundNormal = _playerTankController.CurrentGroundNormal;
                if (groundNormal.sqrMagnitude > 0.0001f)
                {
                    return Vector3.Slerp(Vector3.up, groundNormal.normalized, Mathf.Clamp01(bodyBlend)).normalized;
                }
            }

            Vector3 tankUp = playerTank.up.sqrMagnitude > 0.0001f ? playerTank.up.normalized : Vector3.up;
            return Vector3.Slerp(Vector3.up, tankUp, Mathf.Clamp01(bodyBlend)).normalized;
        }

        private Vector3 GetMouseDrivenAimForward()
        {
            Vector3 baseForward = GetStableTankForward();
            Quaternion yawRotation = Quaternion.AngleAxis(_yaw, Vector3.up);
            Vector3 yawed = yawRotation * baseForward;
            Vector3 pitchAxis = Vector3.Cross(Vector3.up, yawed);
            if (pitchAxis.sqrMagnitude <= 0.0001f)
            {
                pitchAxis = Vector3.right;
            }

            Quaternion pitchRotation = Quaternion.AngleAxis(_pitch, pitchAxis.normalized);
            Vector3 aimed = pitchRotation * yawed;
            return aimed.sqrMagnitude > 0.0001f ? aimed.normalized : baseForward;
        }

        private Vector3 GetMouseDriveAimForward()
        {
            float bodyBlend = _mode == DriveAssistMode ? driveAssistBodyUpBlend : zDriveBodyUpBlend;
            return GetMouseDriveLookRotation(bodyBlend) * Vector3.forward;
        }

        private Vector3 GetTankAnchorPosition()
        {
            if (_smoothedTankAnchorInitialized)
            {
                return _smoothedTankAnchor;
            }

            if (playerTank != null)
            {
                return playerTank.position;
            }

            return transform.position;
        }

        private void UpdateSmoothedTankAnchor()
        {
            if (playerTank == null)
            {
                _smoothedTankAnchorInitialized = false;
                return;
            }

            Vector3 raw = playerTank.position;

            if (!_smoothedTankAnchorInitialized)
            {
                _smoothedTankAnchor = raw;
                _smoothedTankAnchorInitialized = true;
                return;
            }

            float t = Mathf.Clamp01(nonWorldAnchorSmoothing * Time.deltaTime);
            _smoothedTankAnchor = Vector3.Lerp(_smoothedTankAnchor, raw, t);
        }

        private void OnGUI()
        {
            EnsureOverlayPixel();

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(12f, 10f, 700f, 24f), "CAMERA: " + GetModeName(_mode) + " (Tab / Shift+Tab, Z for Z_DRIVE)", labelStyle);

            if (ShouldDrawStageTelemetry())
            {
                DrawStageTelemetry();
            }

            if (!ShouldDrawCockpitHud())
            {
                return;
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            Color old = GUI.color;
            if (_mode == InTankMode)
            {
                DrawCockpitMask();
            }

            GUI.color = cockpitOverlayColor;
            DrawRect(cx - overlaySize * 0.5f, cy - overlayThickness * 0.5f, overlaySize, overlayThickness);
            DrawRect(cx - overlayThickness * 0.5f, cy - overlaySize * 0.5f, overlayThickness, overlaySize);
            DrawTankCockpitFrame();
            DrawTargetArrow(cx, cy);
            DrawTurnDirectionArrow(cx, cy);
            DrawTankCockpitTelemetry();
            DrawInTankMinimap();

            GUI.color = old;
        }

        private bool ShouldDrawCockpitHud()
        {
            return showCockpitTargetOverlay && (_mode == InTankMode || _mode == DriveAssistMode || _mode == ZDriveMode);
        }

        private static string GetModeName(int mode)
        {
            switch (mode)
            {
                case InTankMode: return "IN_TANK";
                case StareDownMuzzleMode: return "STARE_DOWN_MUZZLE";
                case TopOfTankMode: return "TOP_OF_TANK";
                case OverheadMode: return "OVERHEAD_VIEW";
                case WorldExplorerMode: return "WORLD_EXPLORER";
                case SideInspectionMode: return "SIDE_INSPECTION";
                case DriveAssistMode: return "DRIVE_ASSIST";
                case ZDriveMode: return "Z_DRIVE";
                default: return "UNKNOWN";
            }
        }

        private void HandleModeInput()
        {
            if (zDriveKey != KeyCode.None && Input.GetKeyDown(zDriveKey))
            {
                SetMode(ZDriveMode);
                return;
            }

            bool tabPressed = Input.GetKeyDown(KeyCode.Tab);
            bool configuredSwitchPressed = allowModeToggle
                                           && switchKey != KeyCode.None
                                           && switchKey != zDriveKey
                                           && Input.GetKeyDown(switchKey);
            if (!tabPressed && !configuredSwitchPressed)
            {
                return;
            }

            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            CycleMode(reverse ? -1 : 1);
        }

        private void CycleMode(int delta)
        {
            SetMode(_mode + delta);
        }

        private static int WrapModeIndex(int mode)
        {
            int wrapped = mode % CameraModeCount;
            if (wrapped < 0)
            {
                wrapped += CameraModeCount;
            }

            return wrapped;
        }

        private void SyncWorldLookToTransform()
        {
            _worldYaw = transform.eulerAngles.y;
            _worldPitch = NormalizePitch(transform.eulerAngles.x);
            _worldLookInitialized = true;
        }

        private void EnsureMouseDriveLookInitialized()
        {
            if (_mouseDriveLookInitialized)
            {
                return;
            }

            SyncMouseDriveLookToCurrentView(_mode);
        }

        private void SyncMouseDriveLookToCurrentView(int previousMode)
        {
            Vector3 forward = transform.forward;
            bool viewTooVertical = forward.sqrMagnitude <= 0.0001f
                                   || Mathf.Abs(Vector3.Dot(forward.normalized, Vector3.up)) > 0.92f;
            if (viewTooVertical)
            {
                forward = GetStableTankForward();
                if (forward.sqrMagnitude <= 0.0001f)
                {
                    forward = Vector3.forward;
                }
            }

            Vector3 planarForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = GetStableTankForward();
            }
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = Vector3.forward;
            }

            _mouseDriveYaw = Mathf.Atan2(planarForward.x, planarForward.z) * Mathf.Rad2Deg;
            _mouseDrivePitch = viewTooVertical && previousMode == WorldExplorerMode
                ? Mathf.Clamp(_worldPitch, minPitch, maxPitch)
                : (viewTooVertical
                    ? Mathf.Clamp(zDriveDefaultPitch, minPitch, maxPitch)
                    : Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(forward.normalized.y, -1f, 1f)) * Mathf.Rad2Deg, minPitch, maxPitch));
            _mouseDriveLookInitialized = true;
        }

        private static void EnsureOverlayPixel()
        {
            if (_overlayPixel != null)
            {
                return;
            }

            _overlayPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _overlayPixel.SetPixel(0, 0, Color.white);
            _overlayPixel.Apply();
        }

        private static void DrawRect(float x, float y, float w, float h)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), _overlayPixel);
        }

        private void DrawTankCockpitFrame()
        {
            float w = Screen.width;
            float h = Screen.height;
            float edge = Mathf.Min(w, h) * 0.08f;
            float t = Mathf.Max(1f, overlayThickness);

            DrawRect(0f, 0f, edge, t);
            DrawRect(0f, 0f, t, edge);
            DrawRect(w - edge, 0f, edge, t);
            DrawRect(w - t, 0f, t, edge);
            DrawRect(0f, h - t, edge, t);
            DrawRect(0f, h - edge, t, edge);
            DrawRect(w - edge, h - t, edge, t);
            DrawRect(w - t, h - edge, t, edge);
        }

        private void DrawCockpitMask()
        {
            float w = Screen.width;
            float h = Screen.height;
            float maskX = Mathf.Clamp01(cockpitMaskPercent) * w;
            float maskY = Mathf.Clamp01(cockpitMaskPercent) * h;
            Color old = GUI.color;
            GUI.color = cockpitMaskColor;

            // Occlude a 10% border around the center viewport.
            DrawRect(0f, 0f, w, maskY);               // Top
            DrawRect(0f, h - maskY, w, maskY);        // Bottom
            DrawRect(0f, maskY, maskX, h - 2f * maskY);           // Left
            DrawRect(w - maskX, maskY, maskX, h - 2f * maskY);    // Right

            GUI.color = old;
        }

        private void DrawTargetArrow(float cx, float cy)
        {
            if (!showTargetArrow)
            {
                return;
            }

            GUIStyle arrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = cockpitOverlayColor }
            };

            GUI.Label(new Rect(cx - 14f, cy - 44f, 28f, 24f), "^", arrowStyle);
        }

        private void DrawTurnDirectionArrow(float cx, float cy)
        {
            if (!showTurnDirectionArrow || _playerTankController == null)
            {
                return;
            }

            float turn = _playerTankController.CurrentTurnInput;
            string glyph = "";
            if (turn > 0.2f) glyph = "<";
            else if (turn < -0.2f) glyph = ">";

            if (string.IsNullOrEmpty(glyph))
            {
                return;
            }

            GUIStyle arrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = cockpitOverlayColor }
            };
            GUI.Label(new Rect(cx - 18f, cy + 26f, 36f, 28f), glyph, arrowStyle);
        }

        private void DrawInTankMinimap()
        {
            if (!showInTankMinimap || playerTank == null)
            {
                return;
            }

            float size = Mathf.Max(80f, minimapSize);
            float x = Screen.width - size + minimapScreenOffset.x;
            float y = minimapScreenOffset.y;
            Rect rect = new Rect(x, y, size, size);

            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            DrawRect(rect.x, rect.y, rect.width, rect.height);
            GUI.color = cockpitOverlayColor;
            DrawRect(rect.x, rect.y, rect.width, 1.5f);
            DrawRect(rect.x, rect.y + rect.height - 1.5f, rect.width, 1.5f);
            DrawRect(rect.x, rect.y, 1.5f, rect.height);
            DrawRect(rect.x + rect.width - 1.5f, rect.y, 1.5f, rect.height);

            Vector2 center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
            GUI.color = Color.green;
            DrawRect(center.x - 2f, center.y - 2f, 4f, 4f);

            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float range = Mathf.Max(5f, minimapRange);
            for (int i = 0; i < enemies.Length; i++)
            {
                GameObject enemy = enemies[i];
                if (enemy == null || !enemy.activeInHierarchy)
                {
                    continue;
                }

                Vector3 delta = enemy.transform.position - playerTank.position;
                Vector2 flat = new Vector2(delta.x, delta.z);
                if (flat.magnitude > range)
                {
                    flat = flat.normalized * range;
                }

                Vector2 uv = flat / range;
                Vector2 point = center + new Vector2(uv.x, uv.y) * (size * 0.45f);
                GUI.color = Color.red;
                DrawRect(point.x - 2f, point.y - 2f, 4f, 4f);
            }

            GUI.color = Color.white;
        }

        private void DrawTankCockpitTelemetry()
        {
            if (_playerTankController == null)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = cockpitOverlayColor }
            };

            float speed = _playerTankController.CurrentSpeed;
            float slope = _playerTankController.CurrentSlopeAngle;

            GUI.Label(new Rect(18f, Screen.height - 56f, 260f, 22f), $"SPD {speed:0.0}", style);
            GUI.Label(new Rect(18f, Screen.height - 34f, 260f, 22f), $"SLOPE {slope:0.0}°", style);
        }

        private bool ShouldDrawStageTelemetry()
        {
            return playerTank != null
                   && (_mode == SideInspectionMode || DebugVisualSettings.ShowColliderBounds || DebugVisualSettings.ShowRaycasts);
        }

        private void DrawStageTelemetry()
        {
            if (_camera == null || playerTank == null)
            {
                return;
            }

            Transform reference = ResolveTelemetryReferenceRoot();
            Vector3 tankWorld = playerTank.position;
            float snappedGroundY = _playerTankController != null ? _playerTankController.CurrentGroundHeight : tankWorld.y;
            float supportY = _playerTankController != null ? _playerTankController.CurrentSupportSurfaceY : snappedGroundY;
            float capsuleBottomY = _playerTankController != null ? _playerTankController.CurrentCapsuleBottomY : tankWorld.y;
            float rendererBottomY = _playerTankController != null ? _playerTankController.CurrentRendererBottomY : tankWorld.y;
            float visualLocalY = _playerTankController != null ? _playerTankController.CurrentVisualRootLocalY : 0f;
            Vector3 supportPoint = new Vector3(tankWorld.x, supportY, tankWorld.z);
            Vector3 capsuleBottomPoint = new Vector3(tankWorld.x, capsuleBottomY, tankWorld.z);
            Vector3 rendererBottomPoint = new Vector3(tankWorld.x, rendererBottomY, tankWorld.z);
            float verticalVelocity = _playerTankController != null
                ? _playerTankController.CurrentVerticalVelocity
                : (_playerTankRigidbody != null ? _playerTankRigidbody.linearVelocity.y : 0f);
            string verticalMode = _playerTankController != null ? _playerTankController.VerticalMotionModeName : "Unknown";
            Vector3 relative = reference != null ? reference.InverseTransformPoint(tankWorld) : tankWorld;

            DrawWorldTelemetryLine(tankWorld, supportPoint, new Color(0.2f, 0.95f, 1f, 0.95f), 2f);
            DrawWorldTelemetryLine(capsuleBottomPoint, rendererBottomPoint, new Color(1f, 0.82f, 0.28f, 0.95f), 2f);
            DrawWorldTelemetryLabel(
                tankWorld + Vector3.up * 2.1f,
                "TANK\n"
                + $"W {FormatVector3(tankWorld)}\n"
                + $"LOCAL {FormatVector3(relative)}\n"
                + $"VY {verticalVelocity:0.00}\n"
                + $"MODE {verticalMode}\n"
                + $"VIS Y {visualLocalY:0.00}",
                stageTelemetryColor,
                220f);

            DrawWorldTelemetryLabel(
                supportPoint + Vector3.up * 0.18f,
                "GROUND\n"
                + $"SUP {supportY:0.00}\n"
                + $"SNAP {snappedGroundY:0.00}\n"
                + $"DY {(tankWorld.y - snappedGroundY):0.00}",
                new Color(0.2f, 0.95f, 1f, 1f),
                160f);

            DrawWorldTelemetryLabel(
                new Vector3(tankWorld.x + 0.55f, Mathf.Max(capsuleBottomY, rendererBottomY) + 0.7f, tankWorld.z),
                "ALIGN\n"
                + $"CAP {capsuleBottomY:0.00}\n"
                + $"REN {rendererBottomY:0.00}\n"
                + $"CAP-SUP {(capsuleBottomY - supportY):0.00}\n"
                + $"REN-SUP {(rendererBottomY - supportY):0.00}",
                new Color(1f, 0.82f, 0.28f, 1f),
                176f);

            if (reference != null)
            {
                DrawWorldTelemetryLabel(
                    reference.position + Vector3.up * 1.25f,
                    $"{reference.name}\nORIGIN {FormatVector3(reference.position)}",
                    new Color(1f, 0.82f, 0.28f, 1f),
                    210f);
            }
        }

        private void DrawWorldTelemetryLabel(Vector3 worldPosition, string text, Color color, float width)
        {
            Vector3 screen = _camera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f)
            {
                return;
            }

            float guiX = screen.x + 12f;
            float guiY = Screen.height - screen.y;
            int lineCount = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    lineCount++;
                }
            }

            float height = 8f + (lineCount * 18f);
            Rect rect = new Rect(guiX, guiY - (height * 0.5f), width, height);

            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.74f);
            DrawRect(rect.x, rect.y, rect.width, rect.height);
            GUI.color = old;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = color }
            };

            GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, rect.height - 8f), text, style);
        }

        private void DrawWorldTelemetryLine(Vector3 worldStart, Vector3 worldEnd, Color color, float thickness)
        {
            Vector3 startScreen = _camera.WorldToScreenPoint(worldStart);
            Vector3 endScreen = _camera.WorldToScreenPoint(worldEnd);
            if (startScreen.z <= 0f || endScreen.z <= 0f)
            {
                return;
            }

            Vector2 a = new Vector2(startScreen.x, Screen.height - startScreen.y);
            Vector2 b = new Vector2(endScreen.x, Screen.height - endScreen.y);
            DrawScreenLine(a, b, thickness, color);
        }

        private static string FormatVector3(Vector3 value)
        {
            return $"{value.x:0.00}, {value.y:0.00}, {value.z:0.00}";
        }

        private Transform ResolveTelemetryReferenceRoot()
        {
            if (_telemetryReferenceRoot != null)
            {
                return _telemetryReferenceRoot;
            }

            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform best = null;
            float bestDistance = float.MaxValue;
            Vector3 anchor = playerTank != null ? playerTank.position : Vector3.zero;

            for (int i = 0; i < all.Length; i++)
            {
                Transform candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                string name = candidate.name;
                if (name == "First Sandbox")
                {
                    _telemetryReferenceRoot = candidate;
                    return _telemetryReferenceRoot;
                }

                if (!name.ToLowerInvariant().Contains("sandbox"))
                {
                    continue;
                }

                float distance = (candidate.position - anchor).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            _telemetryReferenceRoot = best;
            return _telemetryReferenceRoot;
        }

        private void DrawScreenLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            if ((end - start).sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(start, end);

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            DrawRect(start.x, start.y - (thickness * 0.5f), length, thickness);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void OnDisable()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void EnsureFillLight()
        {
            if (!autoCreateFillLight)
            {
                EnsureSkyCornerLights();
                return;
            }

            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int directionalCount = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional && lights[i].enabled)
                {
                    directionalCount++;
                }
            }

            if (directionalCount >= 2)
            {
                return;
            }

            GameObject fillGo = new GameObject("RuntimeFillLight");
            Light fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = fillLightColor;
            fill.intensity = fillLightIntensity;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(fillLightEuler);

            EnsureSkyCornerLights();
        }

        private void EnsureSkyCornerLights()
        {
            if (!autoCreateSkyCornerLights)
            {
                return;
            }

            const string rootName = "RuntimeSkyCornerLights";
            if (GameObject.Find(rootName) != null)
            {
                return;
            }

            Bounds arenaBounds;
            bool hasBounds = TryGetArenaBounds(out arenaBounds);
            if (!hasBounds)
            {
                Vector3 center = playerTank != null ? playerTank.position : Vector3.zero;
                arenaBounds = new Bounds(center, new Vector3(60f, 8f, 60f));
            }

            Vector3 ext = arenaBounds.extents;
            ext.x = Mathf.Max(ext.x, 18f);
            ext.z = Mathf.Max(ext.z, 18f);
            float topY = arenaBounds.max.y + Mathf.Max(6f, skyCornerHeight);
            float range = Mathf.Max(ext.x, ext.z) * 1.55f + Mathf.Max(0f, skyCornerRangePadding);

            GameObject root = new GameObject(rootName);
            root.transform.position = arenaBounds.center;

            Vector3[] corners =
            {
                new Vector3(arenaBounds.center.x - ext.x, topY, arenaBounds.center.z - ext.z),
                new Vector3(arenaBounds.center.x - ext.x, topY, arenaBounds.center.z + ext.z),
                new Vector3(arenaBounds.center.x + ext.x, topY, arenaBounds.center.z - ext.z),
                new Vector3(arenaBounds.center.x + ext.x, topY, arenaBounds.center.z + ext.z)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                GameObject lightGo = new GameObject("SkyCornerLight_" + i);
                lightGo.transform.SetParent(root.transform, true);
                lightGo.transform.position = corners[i];

                Light l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = skyCornerLightColor;
                l.intensity = skyCornerLightIntensity;
                l.range = range;
                l.shadows = LightShadows.None;
            }
        }

        private static bool TryGetArenaBounds(out Bounds bounds)
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            bounds = default;
            bool initialized = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (r is ParticleSystemRenderer)
                {
                    continue;
                }

                string n = r.gameObject.name.ToLowerInvariant();
                if (n.Contains("paintsplat") || n.Contains("trajectoryline"))
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = r.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return initialized;
        }
    }
}
