using System.Collections.Generic;
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
    private const string DemoSceneSourcePath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Demo Scene/Demo Scene 02.unity";

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
        camGO.AddComponent<CameraController>();

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

    [MenuItem("TankRoyale/Setup Demo Scene")]
    public static void SetupDemoScene()
    {
        EnsureTagExists("Player");
        EnsureTagExists("Enemy");
        EnsureTagExists("Block");

        EnsureAssetFolder(Path.GetDirectoryName(ArenaScenePath));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoSceneSourcePath) == null)
        {
            Debug.LogError($"[TankRoyaleSetup] Demo scene not found at: {DemoSceneSourcePath}");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaScenePath) != null)
        {
            AssetDatabase.DeleteAsset(ArenaScenePath);
        }

        if (!AssetDatabase.CopyAsset(DemoSceneSourcePath, ArenaScenePath))
        {
            Debug.LogError($"[TankRoyaleSetup] Failed to copy demo scene from '{DemoSceneSourcePath}' to '{ArenaScenePath}'.");
            return;
        }

        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

        Camera mainCam = FindMainCameraInScene(scene);
        GameObject cameraObject;
        if (mainCam == null)
        {
            cameraObject = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            mainCam = cameraObject.AddComponent<Camera>();
        }
        else
        {
            cameraObject = mainCam.gameObject;
            cameraObject.tag = "MainCamera";
        }

        mainCam.orthographic = true;
        mainCam.orthographicSize = 18f;
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = Color.black;
        cameraObject.transform.position = new Vector3(15f, 25f, 15f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        if (cameraObject.GetComponent<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }

        if (cameraObject.GetComponent<CameraController>() == null)
        {
            cameraObject.AddComponent<CameraController>();
        }

        GameObject powerupManagerGO = GetOrCreateSceneObject(scene, "PowerupManager");
        if (powerupManagerGO.GetComponent<PowerupManager>() == null)
        {
            powerupManagerGO.AddComponent<PowerupManager>();
        }

        GameObject audioManagerGO = GetOrCreateSceneObject(scene, "AudioManager");
        if (audioManagerGO.GetComponent<AudioManager>() == null)
        {
            audioManagerGO.AddComponent<AudioManager>();
        }

        GameObject gridGO = GetOrCreateSceneObject(scene, "AStarGrid");
        var grid = gridGO.GetComponent<AStarGrid>() ?? gridGO.AddComponent<AStarGrid>();
        SetPrivateField(grid, "width", ArenaSize);
        SetPrivateField(grid, "height", ArenaSize);
        SetPrivateField(grid, "cellSize", 1f);
        SetPrivateField(grid, "useTransformAsCenter", false);
        SetPrivateField(grid, "manualOrigin", Vector3.zero);

        GetOrCreateSceneObject(scene, "GameManager");

        GameObject spawnerGO = GetOrCreateSceneObject(scene, "PowerupSpawner");
        var spawner = spawnerGO.GetComponent<PowerupSpawner>() ?? spawnerGO.AddComponent<PowerupSpawner>();
        SetPrivateField(spawner, "ricochetPrefab", Load<GameObject>(RicochetPath));
        SetPrivateField(spawner, "armorPrefab", Load<GameObject>(ArmorPath));
        SetPrivateField(spawner, "blockBreakerPrefab", Load<GameObject>(BlockBreakerPath));

        if (FindTaggedObjectsInScene(scene, "Player").Count == 0)
        {
            GameObject playerPrefab = Load<GameObject>(PlayerTankPath);
            if (playerPrefab != null)
            {
                GameObject playerTank = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
                playerTank.transform.position = new Vector3(2f, 0.5f, 2f);
                playerTank.tag = "Player";
                playerTank.name = "PlayerTank";

                if (playerTank.GetComponent<TankController>() == null)
                {
                    playerTank.AddComponent<TankController>();
                }

                var weaponController = playerTank.GetComponent<WeaponController>() ?? playerTank.AddComponent<WeaponController>();
                SetPrivateField(weaponController, "projectilePrefab", Load<GameObject>(ShellPath));
            }
        }

        if (FindTaggedObjectsInScene(scene, "Enemy").Count < 3)
        {
            var enemyData = new (GameObject prefab, Vector3 pos, string name)[]
            {
                (Load<GameObject>(EnemyTank01Path), new Vector3(27f, 0.5f, 27f), "EnemyTank_01"),
                (Load<GameObject>(EnemyTank02Path), new Vector3(27f, 0.5f, 2f), "EnemyTank_02"),
                (Load<GameObject>(EnemyTank03Path), new Vector3(2f, 0.5f, 27f), "EnemyTank_03"),
            };

            foreach (var (prefab, pos, enemyName) in enemyData)
            {
                GameObject enemyObject = GetOrCreateSceneObject(scene, enemyName);

                if (enemyObject.GetComponent<AITankController>() == null)
                {
                    if (enemyObject.transform.childCount == 0 && prefab != null)
                    {
                        Object.DestroyImmediate(enemyObject);
                        enemyObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    }

                    enemyObject.AddComponent<AITankController>();
                }

                enemyObject.transform.position = pos;
                enemyObject.tag = "Enemy";
                enemyObject.name = enemyName;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ArenaScenePath))
        {
            Debug.LogError($"[TankRoyaleSetup] Failed to save scene at {ArenaScenePath}");
            return;
        }

        Debug.Log($"[TankRoyaleSetup] ✅ Demo scene setup complete! Scene saved to {ArenaScenePath}");
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

    private static void EnsureAssetFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static Camera FindMainCameraInScene(Scene scene)
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null || cam.gameObject.scene != scene)
            {
                continue;
            }

            if (cam.gameObject.CompareTag("MainCamera") || cam.name == "Main Camera")
            {
                return cam;
            }
        }

        return null;
    }

    private static GameObject GetOrCreateSceneObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
            {
                return roots[i];
            }
        }

        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static List<GameObject> FindTaggedObjectsInScene(Scene scene, string tag)
    {
        var matches = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < transforms.Length; j++)
            {
                if (transforms[j].CompareTag(tag))
                {
                    matches.Add(transforms[j].gameObject);
                }
            }
        }

        return matches;
    }
}
