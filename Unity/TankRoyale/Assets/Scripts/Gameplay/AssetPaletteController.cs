using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TankRoyale.Gameplay
{
    [DisallowMultipleComponent]
    public class AssetPaletteController : MonoBehaviour
    {
        [SerializeField] private KeyCode toggleKey = KeyCode.P;
        [SerializeField] private Rect windowRect = new Rect(820f, 18f, 420f, 560f);
        [SerializeField] private bool showPalette;

        private readonly Dictionary<string, List<string>> _itemsByCategory = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, Vector2> _scrollByCategory = new Dictionary<string, Vector2>();
        private static readonly string[] Categories =
        {
            "font", "building", "cloud", "collectible", "ground", "icon",
            "obstacle", "plant", "prop", "rock", "tank", "vehicle", "weapon"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureController()
        {
            if (Object.FindFirstObjectByType<AssetPaletteController>() != null)
            {
                return;
            }

            GameObject go = new GameObject("AssetPaletteController");
            DontDestroyOnLoad(go);
            go.AddComponent<AssetPaletteController>();
        }

        private void Awake()
        {
            RefreshPaletteData();
        }

        private void Update()
        {
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            {
                showPalette = !showPalette;
                if (showPalette)
                {
                    RefreshPaletteData();
                }
            }
        }

        private void RefreshPaletteData()
        {
            _itemsByCategory.Clear();
            _scrollByCategory.Clear();
            for (int i = 0; i < Categories.Length; i++)
            {
                _itemsByCategory[Categories[i]] = new List<string>();
                _scrollByCategory[Categories[i]] = Vector2.zero;
            }

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:Model");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                string category = ResolveCategory(filename);
                if (category == null)
                {
                    continue;
                }

                _itemsByCategory[category].Add(System.IO.Path.GetFileName(path));
            }

            foreach (KeyValuePair<string, List<string>> kv in _itemsByCategory)
            {
                kv.Value.Sort(System.StringComparer.OrdinalIgnoreCase);
            }
#endif
        }

        private static string ResolveCategory(string filename)
        {
            if (filename.StartsWith("font_")) return "font";
            if (filename.StartsWith("building_")) return "building";
            if (filename.StartsWith("cloud_")) return "cloud";
            if (filename.StartsWith("collectible_")) return "collectible";
            if (filename.StartsWith("ground_") || filename.StartsWith("3d_tile_")) return "ground";
            if (filename.StartsWith("icon_")) return "icon";
            if (filename.StartsWith("obstacle_")) return "obstacle";
            if (filename.StartsWith("plant_")) return "plant";
            if (filename.StartsWith("prop_")) return "prop";
            if (filename.StartsWith("rock_")) return "rock";
            if (filename.Contains("tank")) return "tank";
            if (filename.StartsWith("vehicle_")) return "vehicle";
            if (filename.StartsWith("weapon_")) return "weapon";
            return null;
        }

        private void OnGUI()
        {
            if (!showPalette)
            {
                return;
            }

            windowRect = GUILayout.Window(85231, windowRect, DrawWindow, "Asset Palette (P)");
        }

        private void DrawWindow(int id)
        {
#if !UNITY_EDITOR
            GUILayout.Label("Palette listing is editor-only (uses AssetDatabase).");
#else
            for (int i = 0; i < Categories.Length; i++)
            {
                string cat = Categories[i];
                List<string> items = _itemsByCategory.TryGetValue(cat, out List<string> list) ? list : null;
                int count = items != null ? items.Count : 0;

                GUILayout.Label(cat.ToUpperInvariant() + " (" + count + ")");
                _scrollByCategory[cat] = GUILayout.BeginScrollView(_scrollByCategory[cat], GUILayout.Height(74f));
                if (items != null)
                {
                    for (int j = 0; j < items.Count; j++)
                    {
                        GUILayout.Label("- " + items[j]);
                    }
                }
                GUILayout.EndScrollView();
            }
#endif
            GUI.DragWindow(new Rect(0f, 0f, 5000f, 22f));
        }
    }
}
