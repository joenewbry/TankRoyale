using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class DebugMenuController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleMenuKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode quickHitboxKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode quickRayKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode quickArcKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode quickWireframeKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode quickShadowKey = KeyCode.Alpha6;
        [SerializeField] private Rect menuRect = new Rect(18f, 18f, 360f, 340f);
        private ShadowQuality _defaultShadows = ShadowQuality.All;
        private bool _shadowQualityCached;

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
            GUILayout.Label("1 : Toggle Menu");
            GUILayout.Label("2 : Hitboxes");
            GUILayout.Label("3 : Rays");
            GUILayout.Label("4 : Arc");
            GUILayout.Label("5 : Wireframe");
            GUILayout.Label("6 : Shadows");
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
