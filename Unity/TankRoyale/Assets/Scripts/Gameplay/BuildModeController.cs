using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class BuildModeController : MonoBehaviour
    {
        private struct BuildOption
        {
            public string name;
            public string modelAssetPath;
            public Vector3 scaleMultiplier;
            public Color color;
        }

        [SerializeField] private KeyCode godModeKey = KeyCode.G;
        [SerializeField] private KeyCode rotateBuildKey = KeyCode.R;
        [SerializeField] private float maxBuildDistance = 18f;
        [SerializeField] private float godModeBuildDistance = 85f;
        [SerializeField] private float gridSize = 2f;
        [SerializeField] private float verticalGridStep = 1f;
        [SerializeField] private bool snapToGrid = true;
        [SerializeField] private bool showBuildHud = true;
        [SerializeField] private float previewAlpha = 0.35f;
        [SerializeField] private float normalSurfaceOffset = 0.02f;
        [SerializeField] private bool ensurePlacedBlockColliders = true;

        private int _selectedIndex;
        private Transform _playerTank;
        private BuildOption[] _options;
        private int _rotationQuarterTurns;
        private GameObject _previewInstance;
        private BuildOption? _previewOption;
        private string _previewAssetPath;

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
                    name = "Ground_02",
                    modelAssetPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Source File/3D_Tile_Ground_02.fbx",
                    scaleMultiplier = Vector3.one,
                    color = new Color(0.78f, 0.66f, 0.48f, 1f)
                },
                new BuildOption
                {
                    name = "Ground_Slope_02",
                    modelAssetPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Source File/3D_Tile_Ground_Slope_02.fbx",
                    scaleMultiplier = Vector3.one,
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
                _rotationQuarterTurns = 0;
            }

            if (Input.GetKeyDown(rotateBuildKey))
            {
                _rotationQuarterTurns = (_rotationQuarterTurns + 1) % 4;
            }

            if (Input.GetMouseButtonDown(1))
            {
                TryPlaceBlock();
            }

            UpdatePreview();
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
            Quaternion rotation = GetPlacementRotation(hit.normal);

            Vector3 position = hit.point + hit.normal * normalSurfaceOffset;
            if (snapToGrid)
            {
                position = Snap(position, Mathf.Max(0.01f, gridSize), Mathf.Max(0.01f, verticalGridStep));
            }

            GameObject block = CreateBlockInstance(option);
            if (block == null)
            {
                return;
            }

            block.name = "BuildBlock_" + option.name;
            block.transform.SetPositionAndRotation(position, rotation);
            block.transform.localScale = Vector3.Scale(block.transform.localScale, option.scaleMultiplier);

            try { block.tag = "Block"; } catch { }
            EnsureBlockColliders(block);

            ApplyColorToRenderers(block, option.color, 1f);
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
                $"BUILD: {_options[_selectedIndex].name} (MouseWheel) | Rotate[R] | Place[RMB] | GodMode[G]: {mode}", style);
        }

        private void UpdatePreview()
        {
            Camera cam = Camera.main;
            if (cam == null || _options == null || _options.Length == 0)
            {
                DestroyPreview();
                return;
            }

            Vector3 rayPoint = Cursor.lockState == CursorLockMode.Locked
                ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f)
                : Input.mousePosition;
            Ray ray = cam.ScreenPointToRay(rayPoint);
            float maxDistance = GameCheatState.GodModeEnabled ? godModeBuildDistance : maxBuildDistance;
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                DestroyPreview();
                return;
            }

            BuildOption option = _options[_selectedIndex];
            EnsurePreview(option);
            if (_previewInstance == null)
            {
                return;
            }

            Quaternion rotation = GetPlacementRotation(hit.normal);
            Vector3 position = hit.point + hit.normal * normalSurfaceOffset;
            if (snapToGrid)
            {
                position = Snap(position, Mathf.Max(0.01f, gridSize), Mathf.Max(0.01f, verticalGridStep));
            }

            _previewInstance.transform.SetPositionAndRotation(position, rotation);
            _previewInstance.transform.localScale = Vector3.Scale(Vector3.one, option.scaleMultiplier);
            ApplyColorToRenderers(_previewInstance, option.color, Mathf.Clamp01(previewAlpha));
        }

        private void EnsurePreview(BuildOption option)
        {
            bool needsNew = _previewInstance == null
                            || _previewAssetPath != option.modelAssetPath
                            || !_previewOption.HasValue
                            || _previewOption.Value.name != option.name;
            if (!needsNew)
            {
                return;
            }

            DestroyPreview();
            _previewInstance = CreateBlockInstance(option);
            if (_previewInstance == null)
            {
                return;
            }

            _previewInstance.name = "BuildPreview_" + option.name;
            Collider[] colliders = _previewInstance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            _previewAssetPath = option.modelAssetPath;
            _previewOption = option;
        }

        private void DestroyPreview()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }
        }

        private GameObject CreateBlockInstance(BuildOption option)
        {
#if UNITY_EDITOR
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(option.modelAssetPath);
            if (prefab != null)
            {
                return Instantiate(prefab);
            }
#endif
            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }

        private Quaternion GetPlacementRotation(Vector3 hitNormal)
        {
            bool isSlope = _options[_selectedIndex].name.Contains("Slope");
            if (!isSlope)
            {
                return Quaternion.identity;
            }

            Quaternion baseRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            Quaternion quarter = Quaternion.Euler(0f, _rotationQuarterTurns * 90f, 0f);
            if (hitNormal.y < 0.5f)
            {
                Vector3 facing = Vector3.ProjectOnPlane(-hitNormal, Vector3.up);
                if (facing.sqrMagnitude > 0.0001f)
                {
                    baseRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
                }
            }

            return quarter * baseRotation;
        }

        private static void ApplyColorToRenderers(GameObject go, Color color, float alpha)
        {
            if (go == null)
            {
                return;
            }

            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    continue;
                }

                Material m = new Material(renderer.sharedMaterial);
                Color c = color;
                c.a = alpha;
                if (m.HasProperty("_Color"))
                {
                    m.color = c;
                }

                if (alpha < 0.999f)
                {
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    m.renderQueue = 3000;
                }

                renderer.material = m;
            }
        }

        private void EnsureBlockColliders(GameObject block)
        {
            if (!ensurePlacedBlockColliders || block == null)
            {
                return;
            }

            Collider[] existing = block.GetComponentsInChildren<Collider>(true);
            if (existing != null && existing.Length > 0)
            {
                for (int i = 0; i < existing.Length; i++)
                {
                    if (existing[i] != null)
                    {
                        existing[i].enabled = true;
                    }
                }
                return;
            }

            MeshFilter[] meshes = block.GetComponentsInChildren<MeshFilter>(true);
            bool addedAny = false;
            for (int i = 0; i < meshes.Length; i++)
            {
                MeshFilter mf = meshes[i];
                if (mf == null || mf.sharedMesh == null)
                {
                    continue;
                }

                MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mc = mf.gameObject.AddComponent<MeshCollider>();
                }

                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
                addedAny = true;
            }

            if (!addedAny)
            {
                BoxCollider fallback = block.GetComponent<BoxCollider>();
                if (fallback == null)
                {
                    block.AddComponent<BoxCollider>();
                }
            }
        }

        private static Vector3 Snap(Vector3 value, float horizontalStep, float verticalStep)
        {
            return new Vector3(
                Mathf.Round(value.x / horizontalStep) * horizontalStep,
                Mathf.Round(value.y / verticalStep) * verticalStep,
                Mathf.Round(value.z / horizontalStep) * horizontalStep);
        }
    }
}
