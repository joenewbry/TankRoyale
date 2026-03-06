using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class BuildModeController : MonoBehaviour
    {
        private struct BuildOption
        {
            public string name;
            public PrimitiveType primitive;
            public Vector3 scale;
            public Vector3 euler;
            public Color color;
        }

        [SerializeField] private KeyCode godModeKey = KeyCode.G;
        [SerializeField] private float maxBuildDistance = 18f;
        [SerializeField] private float godModeBuildDistance = 85f;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private bool snapToGrid = true;
        [SerializeField] private bool showBuildHud = true;

        private int _selectedIndex;
        private Transform _playerTank;
        private BuildOption[] _options;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureController()
        {
            if (Object.FindFirstObjectByType<BuildModeController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("BuildModeController");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildModeController>();
        }

        private void Awake()
        {
            _options = new[]
            {
                new BuildOption
                {
                    name = "Cube",
                    primitive = PrimitiveType.Cube,
                    scale = new Vector3(1f, 1f, 1f),
                    euler = Vector3.zero,
                    color = new Color(0.78f, 0.66f, 0.48f, 1f)
                },
                new BuildOption
                {
                    name = "Wall",
                    primitive = PrimitiveType.Cube,
                    scale = new Vector3(1f, 2f, 0.45f),
                    euler = Vector3.zero,
                    color = new Color(0.62f, 0.54f, 0.41f, 1f)
                },
                new BuildOption
                {
                    name = "Pad",
                    primitive = PrimitiveType.Cube,
                    scale = new Vector3(2f, 0.35f, 2f),
                    euler = Vector3.zero,
                    color = new Color(0.58f, 0.62f, 0.66f, 1f)
                },
                new BuildOption
                {
                    name = "Ramp",
                    primitive = PrimitiveType.Cube,
                    scale = new Vector3(2f, 0.5f, 2f),
                    euler = new Vector3(0f, 0f, -28f),
                    color = new Color(0.72f, 0.58f, 0.42f, 1f)
                }
            };
        }

        private void Update()
        {
            ResolvePlayer();

            if (Input.GetKeyDown(godModeKey))
            {
                GameCheatState.GodModeEnabled = !GameCheatState.GodModeEnabled;
                Debug.Log($"[Build] GodMode={(GameCheatState.GodModeEnabled ? "ON" : "OFF")}");
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                int delta = scroll > 0f ? 1 : -1;
                _selectedIndex = (_selectedIndex + delta + _options.Length) % _options.Length;
            }

            if (Input.GetMouseButtonDown(1))
            {
                TryPlaceBlock();
            }
        }

        private void ResolvePlayer()
        {
            if (_playerTank != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTank = player.transform;
            }
        }

        private void TryPlaceBlock()
        {
            Camera cam = Camera.main;
            if (cam == null || _options == null || _options.Length == 0)
            {
                return;
            }

            Vector3 rayPoint = Cursor.lockState == CursorLockMode.Locked
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;

            Ray ray = cam.ScreenPointToRay(rayPoint);
            float maxDistance = GameCheatState.GodModeEnabled ? godModeBuildDistance : maxBuildDistance;
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            BuildOption option = _options[_selectedIndex];
            Quaternion rotation = Quaternion.Euler(option.euler);
            if (option.name == "Wall")
            {
                Vector3 fwd = Vector3.ProjectOnPlane(-hit.normal, Vector3.up);
                if (fwd.sqrMagnitude > 0.001f)
                {
                    rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                }
            }

            float normalOffset = Mathf.Max(option.scale.x, option.scale.y, option.scale.z) * 0.5f;
            Vector3 position = hit.point + hit.normal * (normalOffset + 0.02f);
            if (snapToGrid && gridSize > 0.01f)
            {
                position = Snap(position, gridSize);
            }

            GameObject block = GameObject.CreatePrimitive(option.primitive);
            block.name = "BuildBlock_" + option.name;
            block.transform.SetPositionAndRotation(position, rotation);
            block.transform.localScale = option.scale;

            try { block.tag = "Block"; } catch { }

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material m = new Material(renderer.sharedMaterial);
                m.color = option.color;
                renderer.material = m;
            }
        }

        private void OnGUI()
        {
            if (!showBuildHud || _options == null || _options.Length == 0)
            {
                return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            string mode = GameCheatState.GodModeEnabled ? "ON" : "OFF";
            GUI.Label(new Rect(12f, 34f, 540f, 22f),
                $"BUILD: {_options[_selectedIndex].name} (MouseWheel) | Place: RMB | GodMode[G]: {mode}", style);
        }

        private static Vector3 Snap(Vector3 value, float step)
        {
            return new Vector3(
                Mathf.Round(value.x / step) * step,
                Mathf.Round(value.y / step) * step,
                Mathf.Round(value.z / step) * step);
        }
    }
}
