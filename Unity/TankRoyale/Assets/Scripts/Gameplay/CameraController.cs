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
        private int _mode = InTankMode;
        private float _yaw;
        private float _pitch = 10f;
        private bool _lookInitialized;

        private float _worldYaw;
        private float _worldPitch;
        private bool _worldLookInitialized;

        private TankController _playerTankController;
        private Rigidbody _playerTankRigidbody;

        private static Texture2D _overlayPixel;
        public bool IsInTankMode => _mode == InTankMode;

        public Vector3 AimForward
        {
            get
            {
                if (_mode == InTankMode)
                {
                    return transform.forward;
                }

                if (_mode == WorldExplorerMode)
                {
                    return transform.forward;
                }

                Transform muzzle = GetMuzzleTransform();
                if (muzzle != null)
                {
                    return muzzle.forward;
                }

                return playerTank != null ? playerTank.forward : transform.forward;
            }
        }

        private void Start()
        {
            _camera = GetComponent<Camera>();
            ResolveReferences();
            InitializeLookState();
            EnsureFillLight();
            ApplyModeSettings();
            SnapToCurrentModeTarget();
        }

        private void LateUpdate()
        {
            bool tabPressed = Input.GetKeyDown(KeyCode.Tab);
            bool configuredSwitchPressed = allowModeToggle && switchKey != KeyCode.None && Input.GetKeyDown(switchKey);
            if (tabPressed || configuredSwitchPressed)
            {
                SetMode((_mode + 1) % 5);
            }

            ResolveReferences();

            if (_mode == WorldExplorerMode)
            {
                HandleWorldExplorerInput();
                return;
            }

            HandleMouseLook();

            Vector3 targetPosition = GetTargetPosition();
            Quaternion targetRotation = GetTargetRotation();
            float activeFollowSpeed = _mode == TopOfTankMode ? topOfTankFollowSpeed : followSpeed;

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

            if (_playerTankController == null && playerTank != null)
            {
                _playerTankController = playerTank.GetComponent<TankController>();
            }

            if (_playerTankRigidbody == null && playerTank != null)
            {
                _playerTankRigidbody = playerTank.GetComponent<Rigidbody>();
            }

            if (playerTurret == null && playerTank != null)
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
            _mode = Mathf.Clamp(newMode, 0, 4);
            ApplyModeSettings();

            if (_mode == WorldExplorerMode)
            {
                _worldYaw = transform.eulerAngles.y;
                _worldPitch = NormalizePitch(transform.eulerAngles.x);
                _worldLookInitialized = true;
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
                return playerTank != null ? playerTank.position + Vector3.up * overheadHeight : transform.position;
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
                Transform muzzle = GetMuzzleTransform();
                if (muzzle != null)
                {
                    return muzzle.position - muzzle.forward * muzzleViewDistance + Vector3.up * muzzleViewHeight;
                }

                return playerTank != null ? playerTank.position + Vector3.up * 2f : transform.position;
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

                    Vector3 anchor = playerTank.position;
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
                return Quaternion.Euler(90f, 0f, 0f);
            }

            if (_mode == InTankMode)
            {
                Vector3 up = GetCameraUpVector();
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

            if (_mode == StareDownMuzzleMode)
            {
                Transform muzzle = GetMuzzleTransform();
                if (muzzle != null)
                {
                    return Quaternion.LookRotation(muzzle.forward, GetCameraUpVector());
                }

                return playerTank != null ? playerTank.rotation : transform.rotation;
            }

            if (_mode == TopOfTankMode)
            {
                Vector3 forward = GetStableTankForward();
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                Vector3 lookForward = Quaternion.Euler(topOfTankPitch, yaw, 0f) * Vector3.forward;
                return Quaternion.LookRotation(lookForward.normalized, GetCameraUpVector());
            }

            return transform.rotation;
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
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            _lookInitialized = true;
        }

        private void HandleMouseLook()
        {
            if (_mode != InTankMode)
            {
                if (Cursor.lockState == CursorLockMode.Locked && lockCursorInFirstPerson)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            if (!_lookInitialized)
            {
                InitializeLookState();
            }

            if (lockCursorInFirstPerson)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            float yawDelta = Input.GetAxisRaw("Mouse X") * mouseLookSensitivity;
            float pitchDelta = -Input.GetAxisRaw("Mouse Y") * mouseLookSensitivity;

            _yaw += yawDelta;
            _pitch = Mathf.Clamp(_pitch + pitchDelta, minPitch, maxPitch);
        }

        private void HandleWorldExplorerInput()
        {
            if (!_worldLookInitialized)
            {
                _worldYaw = transform.eulerAngles.y;
                _worldPitch = NormalizePitch(transform.eulerAngles.x);
                _worldLookInitialized = true;
            }

            bool moveModifierHeld = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            float speed = worldMoveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? worldFastMultiplier : 1f);

            // Keep WASD/Arrows available for tank control unless a camera modifier is held.
            int right = (Input.GetKey(KeyCode.L) ? 1 : 0) + (moveModifierHeld && Input.GetKey(KeyCode.D) ? 1 : 0);
            int left = (Input.GetKey(KeyCode.J) ? 1 : 0) + (moveModifierHeld && Input.GetKey(KeyCode.A) ? 1 : 0);
            int forward = (Input.GetKey(KeyCode.I) ? 1 : 0) + (moveModifierHeld && Input.GetKey(KeyCode.W) ? 1 : 0);
            int back = (Input.GetKey(KeyCode.K) ? 1 : 0) + (moveModifierHeld && Input.GetKey(KeyCode.S) ? 1 : 0);
            int up = (Input.GetKey(KeyCode.U) ? 1 : 0) + (moveModifierHeld && (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.PageUp)) ? 1 : 0);
            int down = (Input.GetKey(KeyCode.O) ? 1 : 0) + (moveModifierHeld && (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.PageDown)) ? 1 : 0);

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

        private Vector3 GetCameraUpVector()
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
                    return Vector3.Slerp(Vector3.up, bodyUp.normalized, 0.8f).normalized;
                }

                Vector3 groundNormal = _playerTankController.CurrentGroundNormal;
                if (groundNormal.sqrMagnitude > 0.0001f)
                {
                    return Vector3.Slerp(Vector3.up, groundNormal.normalized, 0.8f).normalized;
                }
            }

            Vector3 tankUp = playerTank.up.sqrMagnitude > 0.0001f ? playerTank.up.normalized : Vector3.up;
            return Vector3.Slerp(Vector3.up, tankUp, 0.75f).normalized;
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
            GUI.Label(new Rect(12f, 10f, 520f, 24f), "CAMERA: " + GetModeName(_mode) + " (Tab to cycle)", labelStyle);

            if (_mode != InTankMode || !showCockpitTargetOverlay)
            {
                return;
            }

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            Color old = GUI.color;
            DrawCockpitMask();

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

        private static string GetModeName(int mode)
        {
            switch (mode)
            {
                case InTankMode: return "IN_TANK";
                case StareDownMuzzleMode: return "STARE_DOWN_MUZZLE";
                case TopOfTankMode: return "TOP_OF_TANK";
                case OverheadMode: return "OVERHEAD_VIEW";
                case WorldExplorerMode: return "WORLD_EXPLORER";
                default: return "UNKNOWN";
            }
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
