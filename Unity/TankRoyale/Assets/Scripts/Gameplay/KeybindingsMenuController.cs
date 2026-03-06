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
            GUILayout.Label("Space or Mouse Left: Fire");
            GUILayout.Label("R: Reset player position to spawn");

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
            GUILayout.Label("K: Toggle this settings menu");
            GUILayout.Label("P: Toggle asset palette menu");
            GUILayout.Label("Right Mouse: Place selected build block");
            GUILayout.Label("Mouse Wheel: Cycle build block options");
            GUILayout.Label("G: Toggle god mode");
            GUILayout.Label("` : Toggle debug menu");
            GUILayout.Label("F2: Hitboxes");
            GUILayout.Label("F3: Rays");
            GUILayout.Label("F4: Arc");
            GUILayout.Label("F5: Wireframe");
            GUILayout.Label("F6: Shadows");
            GUILayout.Label("T: Target balance mode");

            GUI.DragWindow(new Rect(0f, 0f, 5000f, 22f));
        }
    }
}
