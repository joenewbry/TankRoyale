using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        private const int TopDownMode = 0;
        private const int ShoulderMode = 1;
        private const int CockpitMode = 2;

        [Header("References")]
        [SerializeField] private Transform playerTank;
        [SerializeField] private Transform playerTurret;

        [Header("Top Down")]
        [SerializeField] private float topDownHeight = 20f;
        [SerializeField] private float orthoSize = 12f;

        [Header("Over Shoulder")]
        [SerializeField] private float shoulderDistance = 2.8f;
        [SerializeField] private float shoulderHeight = 1.7f;
        [SerializeField] private float shoulderForwardOffset = 0.9f;

        [Header("Cockpit")]
        [SerializeField] private Vector3 cockpitLocalOffset = new Vector3(0f, 1.3f, 0.2f);
        [SerializeField] private bool showCockpitTargetOverlay = true;
        [SerializeField] private Color cockpitOverlayColor = new Color(0.2f, 1f, 0.5f, 0.9f);
        [SerializeField] private float overlaySize = 22f;
        [SerializeField] private float overlayThickness = 2f;

        [Header("Look")]
        [SerializeField] private float mouseLookSensitivity = 2f;
        [SerializeField] private float minPitch = -20f;
        [SerializeField] private float maxPitch = 45f;
        [SerializeField] private bool lockCursorInFirstPerson = true;

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
        private int _mode = ShoulderMode;
        private float _yaw;
        private float _pitch = 10f;
        private bool _lookInitialized;
        private static Texture2D _overlayPixel;

        public Vector3 AimForward
        {
            get
            {
                if (_mode == TopDownMode && playerTank != null)
                {
                    return playerTank.forward;
                }

                return Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward;
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
            if (allowModeToggle && switchKey != KeyCode.None && Input.GetKeyDown(switchKey))
            {
                _mode = (_mode + 1) % 3;
                ApplyModeSettings();
            }

            HandleMouseLook();

            if (playerTank == null || (_mode == ShoulderMode && playerTurret == null))
            {
                ResolveReferences();
            }

            Vector3 targetPosition = GetTargetPosition();
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

            if (_mode == TopDownMode)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                Quaternion targetRotation = GetFirstPersonRotation();
                if (targetRotation != Quaternion.identity)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * followSpeed);
                }
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
                }
            }

            if (playerTurret == null && playerTank != null)
            {
                Transform foundTurret = playerTank.Find("Turret");
                if (foundTurret == null)
                {
                    foundTurret = playerTank.Find("turret");
                }

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

        private void ApplyModeSettings()
        {
            if (_camera == null)
            {
                return;
            }

            if (_mode == TopDownMode)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = orthoSize;
            }
            else
            {
                _camera.orthographic = false;
                _camera.fieldOfView = 60f;
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (_mode == TopDownMode)
            {
                if (playerTank != null)
                {
                    return playerTank.position + Vector3.up * topDownHeight;
                }

                return transform.position;
            }

            if (_mode == CockpitMode)
            {
                if (playerTurret != null)
                {
                    return playerTurret.TransformPoint(cockpitLocalOffset);
                }

                if (playerTank != null)
                {
                    return playerTank.position + Vector3.up * cockpitLocalOffset.y;
                }

                return transform.position;
            }

            if (playerTank != null)
            {
                Quaternion lookRotation = Quaternion.Euler(_pitch, _yaw, 0f);
                Vector3 pivot = playerTank.position
                    + playerTank.forward * shoulderForwardOffset
                    + Vector3.up * shoulderHeight;
                return pivot - (lookRotation * Vector3.forward * shoulderDistance);
            }

            return transform.position;
        }

        private Quaternion GetFirstPersonRotation()
        {
            return Quaternion.Euler(_pitch, _yaw, 0f);
        }

        private void SnapToCurrentModeTarget()
        {
            transform.position = GetTargetPosition();

            if (_mode == TopDownMode)
            {
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                Quaternion targetRotation = GetFirstPersonRotation();
                if (targetRotation != Quaternion.identity)
                {
                    transform.rotation = targetRotation;
                }
            }
        }

        private void InitializeLookState()
        {
            if (_lookInitialized)
            {
                return;
            }

            if (playerTank != null)
            {
                _yaw = playerTank.eulerAngles.y;
            }
            else
            {
                _yaw = transform.eulerAngles.y;
            }

            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            _lookInitialized = true;
        }

        private void HandleMouseLook()
        {
            if (_mode != ShoulderMode && _mode != CockpitMode)
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

        private void OnGUI()
        {
            if (_mode != CockpitMode || !showCockpitTargetOverlay)
            {
                return;
            }

            EnsureOverlayPixel();

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            Color old = GUI.color;
            GUI.color = cockpitOverlayColor;

            DrawRect(cx - overlaySize * 0.5f, cy - overlayThickness * 0.5f, overlaySize, overlayThickness);
            DrawRect(cx - overlayThickness * 0.5f, cy - overlaySize * 0.5f, overlayThickness, overlaySize);

            GUI.color = old;
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

        private void OnDisable()
        {
            if (Cursor.lockState == CursorLockMode.Locked && lockCursorInFirstPerson)
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
