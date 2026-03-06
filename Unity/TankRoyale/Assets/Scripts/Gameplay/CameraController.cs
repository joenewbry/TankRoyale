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

        [Header("STARE_DOWN_MUZZLE")]
        [SerializeField] private float muzzleViewDistance = 7f;
        [SerializeField] private float muzzleViewHeight = 0.45f;

        [Header("TOP_OF_TANK")]
        [SerializeField] private Vector3 topOfTankLocalOffset = new Vector3(0f, 2.4f, -0.2f);
        [SerializeField] private float topOfTankPitch = 75f;

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

        private Camera _camera;
        private int _mode = OverheadMode;
        private float _yaw;
        private float _pitch = 10f;
        private bool _lookInitialized;

        private float _worldYaw;
        private float _worldPitch;
        private bool _worldLookInitialized;

        private TankController _playerTankController;

        private static Texture2D _overlayPixel;

        public Vector3 AimForward
        {
            get
            {
                if (_mode == InTankMode)
                {
                    return Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward;
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

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            if (targetRotation != Quaternion.identity)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
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
                if (playerTurret != null)
                {
                    return playerTurret.TransformPoint(cockpitLocalOffset);
                }

                return playerTank != null ? playerTank.position + Vector3.up * cockpitLocalOffset.y : transform.position;
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
                    return playerTank.TransformPoint(topOfTankLocalOffset);
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
                return Quaternion.Euler(_pitch, _yaw, 0f);
            }

            if (_mode == StareDownMuzzleMode)
            {
                Transform muzzle = GetMuzzleTransform();
                if (muzzle != null)
                {
                    return Quaternion.LookRotation(muzzle.forward, Vector3.up);
                }

                return playerTank != null ? playerTank.rotation : transform.rotation;
            }

            if (_mode == TopOfTankMode)
            {
                float yaw = playerTank != null ? playerTank.eulerAngles.y : transform.eulerAngles.y;
                return Quaternion.Euler(topOfTankPitch, yaw, 0f);
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

            _yaw = playerTank != null ? playerTank.eulerAngles.y : transform.eulerAngles.y;
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

            float speed = worldMoveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? worldFastMultiplier : 1f);

            int right = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1 : 0;
            int left = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? 1 : 0;
            int forward = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1 : 0;
            int back = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1 : 0;
            int up = (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.Space)) ? 1 : 0;
            int down = (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.PageDown)) ? 1 : 0;

            Vector3 move = transform.forward * (forward - back)
                         + transform.right * (right - left)
                         + Vector3.up * (up - down);

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            transform.position += move * (speed * Time.deltaTime);

            bool allowLook = Input.GetMouseButton(1) || lockCursorInFirstPerson;
            if (allowLook)
            {
                if (lockCursorInFirstPerson)
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
            DrawTankCockpitTelemetry();

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
        }
    }
}
