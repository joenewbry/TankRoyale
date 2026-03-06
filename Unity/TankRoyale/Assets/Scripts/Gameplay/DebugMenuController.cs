using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class DebugMenuController : MonoBehaviour
    {
        private static readonly Color PrimitiveColliderColor = new Color(0.25f, 1f, 0.45f, 0.95f);
        private static readonly Color FallbackBoundsColor = new Color(1f, 0.8f, 0.2f, 0.9f);

        [SerializeField] private KeyCode toggleMenuKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode quickHitboxKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode quickRayKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode quickArcKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode quickWireframeKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode quickShadowKey = KeyCode.Alpha6;
        [SerializeField] private Rect menuRect = new Rect(18f, 18f, 360f, 340f);
        private static Material _lineMaterial;
        private ShadowQuality _defaultShadows = ShadowQuality.All;
        private bool _shadowQualityCached;

        private void Awake()
        {
            // Force requested debug defaults at startup (Mac-friendly numeric toggles).
            DebugVisualSettings.ResetDefaults();
            GL.wireframe = DebugVisualSettings.Wireframe;
            ApplyShadowDebugState();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDebugMenu()
        {
            if (Object.FindFirstObjectByType<DebugMenuController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("DebugMenuController");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugMenuController>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleMenuKey))
            {
                DebugVisualSettings.ShowMenu = !DebugVisualSettings.ShowMenu;
            }

            if (Input.GetKeyDown(quickHitboxKey))
            {
                DebugVisualSettings.ShowColliderBounds = !DebugVisualSettings.ShowColliderBounds;
            }

            if (Input.GetKeyDown(quickRayKey))
            {
                DebugVisualSettings.ShowRaycasts = !DebugVisualSettings.ShowRaycasts;
            }

            if (Input.GetKeyDown(quickArcKey))
            {
                DebugVisualSettings.ShowProjectileArc = !DebugVisualSettings.ShowProjectileArc;
            }

            if (Input.GetKeyDown(quickWireframeKey))
            {
                DebugVisualSettings.Wireframe = !DebugVisualSettings.Wireframe;
            }

            if (Input.GetKeyDown(quickShadowKey))
            {
                DebugVisualSettings.DisableShadows = !DebugVisualSettings.DisableShadows;
            }

            GL.wireframe = DebugVisualSettings.Wireframe;
            ApplyShadowDebugState();
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
                _lineMaterial = null;
            }
        }

        private void OnDisable()
        {
            GL.wireframe = false;
            if (_shadowQualityCached)
            {
                QualitySettings.shadows = _defaultShadows;
            }
        }

        private void OnGUI()
        {
            if (!DebugVisualSettings.ShowMenu)
            {
                return;
            }

            menuRect = GUILayout.Window(97412, menuRect, DrawMenuWindow, "Tank Debug");
        }

        private void DrawMenuWindow(int windowId)
        {
            GUILayout.Label("Views + Visual Debug");
            DebugVisualSettings.ShowColliderBounds = GUILayout.Toggle(DebugVisualSettings.ShowColliderBounds, "Show Hitboxes / Colliders (2)");
            DebugVisualSettings.ShowRaycasts = GUILayout.Toggle(DebugVisualSettings.ShowRaycasts, "Show Ground / Trace Rays (3)");
            DebugVisualSettings.ShowProjectileArc = GUILayout.Toggle(DebugVisualSettings.ShowProjectileArc, "Show Projectile Arc (4)");
            DebugVisualSettings.ShowTrajectoryLine = GUILayout.Toggle(DebugVisualSettings.ShowTrajectoryLine, "Show Trajectory Line");
            DebugVisualSettings.ShowBounceNormals = GUILayout.Toggle(DebugVisualSettings.ShowBounceNormals, "Show Bounce Normals");
            DebugVisualSettings.Wireframe = GUILayout.Toggle(DebugVisualSettings.Wireframe, "Wireframe Rendering (5)");
            DebugVisualSettings.DisableShadows = GUILayout.Toggle(DebugVisualSettings.DisableShadows, "Disable Shadows (6)");

            GUILayout.Space(10f);
            GUILayout.Label("Hotkeys");
            GUILayout.Label("Toggle Menu (1)");
            GUILayout.Label("Hitboxes (2)");
            GUILayout.Label("Rays (3)");
            GUILayout.Label("Arc (4)");
            GUILayout.Label("Wireframe (5)");
            GUILayout.Label("Shadows (6)");
            GUILayout.Label("Tab: Cycle Camera (IN_TANK/MUZZLE/TOP/OVERHEAD/WORLD)");

            GUI.DragWindow(new Rect(0f, 0f, 5000f, 22f));
        }

        private void OnDrawGizmos()
        {
            if (!DebugVisualSettings.ShowColliderBounds)
            {
                return;
            }

            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            Gizmos.color = Color.green;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider c = colliders[i];
                if (c == null || !c.enabled)
                {
                    continue;
                }

                DrawColliderGizmo(c);
            }
        }

        private void OnRenderObject()
        {
            if (!DebugVisualSettings.ShowColliderBounds)
            {
                return;
            }

            Camera currentCamera = Camera.current;
            if (currentCamera == null || currentCamera.cameraType != CameraType.Game)
            {
                return;
            }

            EnsureLineMaterial();
            if (_lineMaterial == null)
            {
                return;
            }

            Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            if (colliders == null || colliders.Length == 0)
            {
                return;
            }

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                DrawColliderLines(collider);
            }

            GL.End();
            GL.PopMatrix();
        }

        private static void DrawColliderGizmo(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = old;
                return;
            }

            if (collider is SphereCollider sphere)
            {
                Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
                float scale = Mathf.Max(
                    Mathf.Abs(sphere.transform.lossyScale.x),
                    Mathf.Abs(sphere.transform.lossyScale.y),
                    Mathf.Abs(sphere.transform.lossyScale.z));
                Gizmos.DrawWireSphere(worldCenter, sphere.radius * scale);
                return;
            }

            // Fallback for capsule/mesh/terrain colliders.
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }

        private static void EnsureLineMaterial()
        {
            if (_lineMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
            {
                return;
            }

            _lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            _lineMaterial.SetInt("_ZWrite", 0);
            _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        private static void DrawColliderLines(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                DrawWorldBox(box.transform, box.center, box.size, PrimitiveColliderColor);
                return;
            }

            if (collider is SphereCollider sphere)
            {
                Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
                float scale = Mathf.Max(
                    Mathf.Abs(sphere.transform.lossyScale.x),
                    Mathf.Abs(sphere.transform.lossyScale.y),
                    Mathf.Abs(sphere.transform.lossyScale.z));
                DrawWireSphere(worldCenter, sphere.radius * scale, PrimitiveColliderColor);
                return;
            }

            DrawWireBounds(collider.bounds, FallbackBoundsColor);
        }

        private static void DrawWorldBox(Transform source, Vector3 localCenter, Vector3 localSize, Color color)
        {
            Vector3 half = localSize * 0.5f;
            Vector3 c000 = source.TransformPoint(localCenter + new Vector3(-half.x, -half.y, -half.z));
            Vector3 c001 = source.TransformPoint(localCenter + new Vector3(-half.x, -half.y, half.z));
            Vector3 c010 = source.TransformPoint(localCenter + new Vector3(-half.x, half.y, -half.z));
            Vector3 c011 = source.TransformPoint(localCenter + new Vector3(-half.x, half.y, half.z));
            Vector3 c100 = source.TransformPoint(localCenter + new Vector3(half.x, -half.y, -half.z));
            Vector3 c101 = source.TransformPoint(localCenter + new Vector3(half.x, -half.y, half.z));
            Vector3 c110 = source.TransformPoint(localCenter + new Vector3(half.x, half.y, -half.z));
            Vector3 c111 = source.TransformPoint(localCenter + new Vector3(half.x, half.y, half.z));

            DrawLine(c000, c001, color);
            DrawLine(c000, c010, color);
            DrawLine(c000, c100, color);
            DrawLine(c001, c011, color);
            DrawLine(c001, c101, color);
            DrawLine(c010, c011, color);
            DrawLine(c010, c110, color);
            DrawLine(c011, c111, color);
            DrawLine(c100, c101, color);
            DrawLine(c100, c110, color);
            DrawLine(c101, c111, color);
            DrawLine(c110, c111, color);
        }

        private static void DrawWireBounds(Bounds bounds, Color color)
        {
            Vector3 center = bounds.center;
            Vector3 ext = bounds.extents;

            Vector3 c000 = center + new Vector3(-ext.x, -ext.y, -ext.z);
            Vector3 c001 = center + new Vector3(-ext.x, -ext.y, ext.z);
            Vector3 c010 = center + new Vector3(-ext.x, ext.y, -ext.z);
            Vector3 c011 = center + new Vector3(-ext.x, ext.y, ext.z);
            Vector3 c100 = center + new Vector3(ext.x, -ext.y, -ext.z);
            Vector3 c101 = center + new Vector3(ext.x, -ext.y, ext.z);
            Vector3 c110 = center + new Vector3(ext.x, ext.y, -ext.z);
            Vector3 c111 = center + new Vector3(ext.x, ext.y, ext.z);

            DrawLine(c000, c001, color);
            DrawLine(c000, c010, color);
            DrawLine(c000, c100, color);
            DrawLine(c001, c011, color);
            DrawLine(c001, c101, color);
            DrawLine(c010, c011, color);
            DrawLine(c010, c110, color);
            DrawLine(c011, c111, color);
            DrawLine(c100, c101, color);
            DrawLine(c100, c110, color);
            DrawLine(c101, c111, color);
            DrawLine(c110, c111, color);
        }

        private static void DrawWireSphere(Vector3 center, float radius, Color color)
        {
            const int segmentCount = 24;
            DrawCircle(center, radius, Vector3.right, Vector3.up, color, segmentCount);
            DrawCircle(center, radius, Vector3.right, Vector3.forward, color, segmentCount);
            DrawCircle(center, radius, Vector3.up, Vector3.forward, color, segmentCount);
        }

        private static void DrawCircle(Vector3 center, float radius, Vector3 axisA, Vector3 axisB, Color color, int segmentCount)
        {
            Vector3 previous = center + (axisA * radius);
            for (int i = 1; i <= segmentCount; i++)
            {
                float angle = (i / (float)segmentCount) * Mathf.PI * 2f;
                Vector3 next = center + ((axisA * Mathf.Cos(angle)) + (axisB * Mathf.Sin(angle))) * radius;
                DrawLine(previous, next, color);
                previous = next;
            }
        }

        private static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            GL.Color(color);
            GL.Vertex(start);
            GL.Vertex(end);
        }

        private void ApplyShadowDebugState()
        {
            if (!_shadowQualityCached)
            {
                _defaultShadows = QualitySettings.shadows;
                _shadowQualityCached = true;
            }

            QualitySettings.shadows = DebugVisualSettings.DisableShadows ? ShadowQuality.Disable : _defaultShadows;
        }
    }
}
