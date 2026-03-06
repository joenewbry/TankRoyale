using UnityEngine;

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class KeybindingsMenuController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.K;
        [SerializeField] private Rect menuRect = new Rect(380f, 18f, 420f, 460f);
        [SerializeField] private bool showMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMenu()
        {
            if (Object.FindFirstObjectByType<KeybindingsMenuController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("KeybindingsMenuController");
            DontDestroyOnLoad(go);
            go.AddComponent<KeybindingsMenuController>();
        }

        private void Update()
        {
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                showMenu = !showMenu;
            }
        }

        private void OnGUI()
        {
            if (!showMenu)
            {
                return;
            }

            menuRect = GUILayout.Window(74621, menuRect, DrawWindow, "Settings / Keybindings");
        }

        private static void DrawWindow(int id)
        {
            GUILayout.Label("Core Movement");
            GUILayout.Label("W/S or Up/Down: Forward / Backward");
            GUILayout.Label("A/D or Left/Right: Turn Left / Right");
            GUILayout.Label("Space (Jump): Jump");
            GUILayout.Label("Mouse Left (Fire): Shoot");
            GUILayout.Label("R (Reset): Reset player position to spawn");

            GUILayout.Space(8f);
            GUILayout.Label("Camera");
            GUILayout.Label("Tab: Cycle camera modes");
            GUILayout.Label("IN_TANK, STARE_DOWN_MUZZLE, TOP_OF_TANK, OVERHEAD_VIEW, WORLD_EXPLORER");

            GUILayout.Space(8f);
            GUILayout.Label("WORLD_EXPLORER Freecam");
            GUILayout.Label("Hold Right Mouse: Look + unlock WASD for camera move");
            GUILayout.Label("I/J/K/L: Move camera F/L/B/R");
            GUILayout.Label("U/O: Move camera Up/Down");
            GUILayout.Label("Shift: Fast camera movement");

            GUILayout.Space(8f);
            GUILayout.Label("Debug / Settings");
            GUILayout.Label("K (Settings): Toggle this settings menu");
            GUILayout.Label("P (Palette): Toggle asset palette menu");
            GUILayout.Label("Right Mouse (Build): Place selected build block");
            GUILayout.Label("Mouse Wheel (Build): Cycle block options");
            GUILayout.Label("R (Rotate Build): Rotate slope/block preview");
            GUILayout.Label("G (God Mode): Toggle god mode");
            GUILayout.Label("1 (Tank Debug): Toggle debug menu");
            GUILayout.Label("2 (Hitboxes): Toggle collider bounds");
            GUILayout.Label("3 (Rays): Toggle debug rays");
            GUILayout.Label("4 (Arc): Toggle projectile arc");
            GUILayout.Label("5 (Wireframe): Toggle wireframe");
            GUILayout.Label("6 (Shadows): Toggle shadow disable");
            GUILayout.Label("T (Target Balance): Toggle cactus balance mode");

            GUI.DragWindow(new Rect(0f, 0f, 5000f, 22f));
        }
    }
}
