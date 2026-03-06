using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class DebugMenuController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleMenuKey = KeyCode.BackQuote;
        [SerializeField] private KeyCode quickHitboxKey = KeyCode.F2;
        [SerializeField] private KeyCode quickRayKey = KeyCode.F3;
        [SerializeField] private KeyCode quickArcKey = KeyCode.F4;
        [SerializeField] private Rect menuRect = new Rect(18f, 18f, 320f, 290f);

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

            GL.wireframe = DebugVisualSettings.Wireframe;
        }

        private void OnDisable()
        {
            GL.wireframe = false;
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
            DebugVisualSettings.ShowColliderBounds = GUILayout.Toggle(DebugVisualSettings.ShowColliderBounds, "Show Hitboxes / Colliders");
            DebugVisualSettings.ShowRaycasts = GUILayout.Toggle(DebugVisualSettings.ShowRaycasts, "Show Ground / Trace Rays");
            DebugVisualSettings.ShowProjectileArc = GUILayout.Toggle(DebugVisualSettings.ShowProjectileArc, "Show Projectile Arc");
            DebugVisualSettings.ShowBounceNormals = GUILayout.Toggle(DebugVisualSettings.ShowBounceNormals, "Show Bounce Normals");
            DebugVisualSettings.Wireframe = GUILayout.Toggle(DebugVisualSettings.Wireframe, "Wireframe Rendering");

            GUILayout.Space(10f);
            GUILayout.Label("Hotkeys");
            GUILayout.Label("` : Toggle Menu");
            GUILayout.Label("F2: Hitboxes");
            GUILayout.Label("F3: Rays");
            GUILayout.Label("F4: Arc");
            GUILayout.Label("Tab: Cycle Camera (Top/Shoulder/Front)");

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
    }
}
