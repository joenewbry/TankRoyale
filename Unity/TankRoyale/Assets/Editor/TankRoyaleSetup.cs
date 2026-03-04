using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TankRoyale.Gameplay;
using TankRoyale.Audio;
using TankRoyale.AI;

public static class TankRoyaleSetup
{
    private const int ArenaSize = 30;
    private const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    private const string GroundTilePath   = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/3D Tile/3D_Tile_Ground_01.prefab";
    private const string CratePath        = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Prop/Prop_Crate_01.prefab";
    private const string PlayerTankPath   = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Player_Tank _GO-07 v01.prefab";
    private const string EnemyTank01Path  = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Enemy_Tank_01.prefab";
    private const string EnemyTank02Path  = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Enemy_Tank_02.prefab";
    private const string EnemyTank03Path  = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Enemy_Tank_03.prefab";
    private const string RicochetPath     = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Collectible/Collectible_Lightning_01.prefab";
    private const string ArmorPath        = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Collectible/Collectible_Heart_01.prefab";
    private const string BlockBreakerPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Collectible/Collectible_Bomb_01.prefab";
    private const string ShellPath        = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Weapon/Weapon_Tank_Shell_01.prefab";

    [MenuItem("TankRoyale/Setup Scene")]
    public static void SetupScene()
    {
        // ── Load required prefabs ──────────────────────────────────────────
        var groundTile   = Load<GameObject>(GroundTilePath);
        var crate        = Load<GameObject>(CratePath);
        var playerPrefab = Load<GameObject>(PlayerTankPath);
        var enemy01      = Load<GameObject>(EnemyTank01Path);
        var enemy02      = Load<GameObject>(EnemyTank02Path);
        var enemy03      = Load<GameObject>(EnemyTank03Path);
        var ricochet     = Load<GameObject>(RicochetPath);
        var armor        = Load<GameObject>(ArmorPath);
        var blockBreaker = Load<GameObject>(BlockBreakerPath);
        var shell        = Load<GameObject>(ShellPath);

        if (groundTile == null || crate == null || playerPrefab == null ||
            enemy01 == null || enemy02 == null || enemy03 == null)
        {
            Debug.LogError("[TankRoyaleSetup] One or more required prefabs missing. Run TankRoyale > Verify Assets for details.");
            return;
        }

        // ── Create fresh scene ────────────────────────────────────────────
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ────────────────────────────────────────────────────────
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 18f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.transform.position = new Vector3(15f, 25f, 15f);
        camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camGO.AddComponent<AudioListener>();

        // ── Lighting ──────────────────────────────────────────────────────
        GameObject lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightGO.transform.position = new Vector3(15f, 10f, 15f);
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f);

        // ── Arena floor (30x30 tiles) ─────────────────────────────────────
        GameObject floorRoot = new GameObject("ArenaFloor");
        for (int x = 0; x < ArenaSize; x++)
        {
            for (int z = 0; z < ArenaSize; z++)
            {
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(groundTile);
                tile.transform.position = new Vector3(x, 0f, z);
                tile.transform.SetParent(floorRoot.transform, true);
            }
        }
        Debug.Log($"[TankRoyaleSetup] Arena floor built: {ArenaSize}x{ArenaSize} tiles.");

        // ── Destructible blocks (~25% fill, corner protection) ────────────
        GameObject blockRoot = new GameObject("ArenaBlocks");
        EnsureTagExists("Block");
        Random.InitState(42);
        int blockCount = 0;
        for (int x = 0; x < ArenaSize; x++)
        {
            for (int z = 0; z < ArenaSize; z++)
            {
                // Skip corners (spawn protection radius 3)
                if (Vector2.Distance(new Vector2(x, z), new Vector2(1, 1)) < 3f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(ArenaSize-2, ArenaSize-2)) < 3f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(ArenaSize-2, 1)) < 3f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(1, ArenaSize-2)) < 3f) continue;

                if (Random.value < 0.25f)
                {
                    GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(crate);
                    block.transform.position = new Vector3(x, 0.5f, z);
                    block.tag = "Block";
                    block.transform.SetParent(blockRoot.transform, true);
                    if (block.GetComponent<DestructibleBlock>() == null)
                        block.AddComponent<DestructibleBlock>();
                    blockCount++;
                }
            }
        }
        Debug.Log($"[TankRoyaleSetup] Placed {blockCount} destructible blocks.");

        // ── Player tank ───────────────────────────────────────────────────
        EnsureTagExists("Player");
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(2f, 0f, 2f);
        player.tag = "Player";
        player.name = "PlayerTank";
        if (player.GetComponent<TankController>() == null)
            player.AddComponent<TankController>();
        var weapon = player.GetComponent<WeaponController>() ?? player.AddComponent<WeaponController>();
        if (shell != null)
        {
            var wField = typeof(WeaponController).GetField("projectilePrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            wField?.SetValue(weapon, shell);
        }
        Debug.Log("[TankRoyaleSetup] Player tank placed.");

        // ── Enemy tanks ───────────────────────────────────────────────────
        EnsureTagExists("Enemy");
        var enemyData = new (GameObject prefab, Vector3 pos, string name)[]
        {
            (enemy01, new Vector3(27f, 0f, 27f), "EnemyTank_01"),
            (enemy02, new Vector3(27f, 0f,  2f), "EnemyTank_02"),
            (enemy03, new Vector3( 2f, 0f, 27f), "EnemyTank_03"),
        };
        foreach (var (prefab, pos, ename) in enemyData)
        {
            GameObject e = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            e.transform.position = pos;
            e.tag = "Enemy";
            e.name = ename;
            if (e.GetComponent<AITankController>() == null)
                e.AddComponent<AITankController>();
        }
        Debug.Log("[TankRoyaleSetup] 3 enemy tanks placed.");

        // ── Powerup spawner ───────────────────────────────────────────────
        GameObject spawnerGO = new GameObject("PowerupSpawner");
        var spawner = spawnerGO.AddComponent<PowerupSpawner>();
        SetPrivateField(spawner, "ricochetPrefab",     ricochet);
        SetPrivateField(spawner, "armorPrefab",        armor);
        SetPrivateField(spawner, "blockBreakerPrefab", blockBreaker);
        Debug.Log("[TankRoyaleSetup] PowerupSpawner created.");

        // ── PowerupManager ────────────────────────────────────────────────
        GameObject pmGO = new GameObject("PowerupManager");
        pmGO.AddComponent<PowerupManager>();

        // ── Audio manager ─────────────────────────────────────────────────
        GameObject audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();
        Debug.Log("[TankRoyaleSetup] AudioManager created.");

        // ── A* grid ───────────────────────────────────────────────────────
        GameObject gridGO = new GameObject("AStarGrid");
        var grid = gridGO.AddComponent<AStarGrid>();
        SetPrivateField(grid, "gridWidth",       ArenaSize);
        SetPrivateField(grid, "gridHeight",      ArenaSize);
        SetPrivateField(grid, "cellSize",        1f);
        SetPrivateField(grid, "originPosition",  Vector3.zero);
        Debug.Log("[TankRoyaleSetup] AStarGrid created.");

        // ── Game Manager ──────────────────────────────────────────────────
        new GameObject("GameManager");

        // ── Save scene ────────────────────────────────────────────────────
        string dir = Path.GetDirectoryName(ArenaScenePath);
        if (!AssetDatabase.IsValidFolder(dir))
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "../" + dir));

        EditorSceneManager.SaveScene(scene, ArenaScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[TankRoyaleSetup] ✅ Setup complete! Scene saved to {ArenaScenePath}");
    }

    [MenuItem("TankRoyale/Verify Assets")]
    public static void VerifyAssets()
    {
        string[] paths = {
            GroundTilePath, CratePath, PlayerTankPath,
            EnemyTank01Path, EnemyTank02Path, EnemyTank03Path,
            RicochetPath, ArmorPath, BlockBreakerPath, ShellPath
        };
        bool allOk = true;
        foreach (string p in paths)
        {
            bool found = AssetDatabase.LoadAssetAtPath<GameObject>(p) != null;
            if (found) Debug.Log($"[VerifyAssets] ✅ FOUND: {p}");
            else { Debug.LogError($"[VerifyAssets] ❌ MISSING: {p}"); allOk = false; }
        }
        Debug.Log(allOk ? "[VerifyAssets] All assets verified OK." : "[VerifyAssets] Some assets are missing — check paths above.");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogError($"[TankRoyaleSetup] Could not load: {path}");
        return asset;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(target, value);
        else Debug.LogWarning($"[TankRoyaleSetup] Field '{fieldName}' not found on {target.GetType().Name}");
    }

    private static void EnsureTagExists(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProperty = tagManager.FindProperty("tags");
        for (int i = 0; i < tagsProperty.arraySize; i++)
            if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tag) return;
        tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
        tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[TankRoyaleSetup] Tag '{tag}' added.");
    }
}
