using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TankRoyaleSetup
{
    private const int ArenaSize = 30;
    private const string ArenaScenePath = "Assets/Scenes/Arena.unity";

    private const string GroundTilePath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/3D Tile/3D_Tile_Ground_01.prefab";
    private const string CratePath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Prop/Prop_Crate_01.prefab";
    private const string PlayerTankPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Player_Tank _GO-07 v01.prefab";
    private const string EnemyTank01Path = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Enemy_Tank_01.prefab";
    private const string EnemyTank02Path = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Enemy_Tank_02.prefab";
    private const string EnemyTank03Path = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Tank/Enemy_Tank_03.prefab";
    private const string RicochetPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Collectible/Collectible_Lightning_01.prefab";
    private const string ArmorPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Collectible/Collectible_Heart_01.prefab";
    private const string BlockBreakerPath = "Assets/AssetHunts!/GameDev Starter Kit - Tanks/Asset/Collectible/Collectible_Bomb_01.prefab";

    private static readonly string[] AllPrefabPaths =
    {
        GroundTilePath,
        CratePath,
        PlayerTankPath,
        EnemyTank01Path,
        EnemyTank02Path,
        EnemyTank03Path,
        RicochetPath,
        ArmorPath,
        BlockBreakerPath
    };

    private static readonly Vector2Int[] CornerCells =
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, ArenaSize - 1),
        new Vector2Int(ArenaSize - 1, 0),
        new Vector2Int(ArenaSize - 1, ArenaSize - 1)
    };

    [MenuItem("TankRoyale/Setup Scene")]
    public static void SetupScene()
    {
        try
        {
            EnsureFolderExists("Assets/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupCamera();
            SetupLighting();

            GameObject groundTilePrefab = LoadPrefabOrThrow(GroundTilePath);
            GameObject cratePrefab = LoadPrefabOrThrow(CratePath);
            GameObject playerTankPrefab = LoadPrefabOrThrow(PlayerTankPath);
            GameObject enemyTank01Prefab = LoadPrefabOrThrow(EnemyTank01Path);
            GameObject enemyTank02Prefab = LoadPrefabOrThrow(EnemyTank02Path);
            GameObject enemyTank03Prefab = LoadPrefabOrThrow(EnemyTank03Path);

            GameObject ricochetPrefab = LoadPrefabOrThrow(RicochetPath);
            GameObject armorPrefab = LoadPrefabOrThrow(ArmorPath);
            GameObject blockBreakerPrefab = LoadPrefabOrThrow(BlockBreakerPath);

            BuildArenaFloor(groundTilePrefab);
            BuildArenaBlocks(cratePrefab);
            SetupPlayerTank(playerTankPrefab);
            SetupEnemyTanks(enemyTank01Prefab, enemyTank02Prefab, enemyTank03Prefab);
            SetupPowerupSpawner(ricochetPrefab, armorPrefab, blockBreakerPrefab);
            SetupAudioManager();
            SetupAStarGrid();
            _ = new GameObject("GameManager");

            if (!EditorSceneManager.SaveScene(scene, ArenaScenePath))
            {
                Debug.LogError($"[TankRoyaleSetup] Failed to save scene to '{ArenaScenePath}'.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TankRoyaleSetup] Arena scene created successfully at '{ArenaScenePath}'.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TankRoyaleSetup] Setup Scene failed: {ex.Message}\n{ex}");
        }
    }

    [MenuItem("TankRoyale/Verify Assets")]
    public static void VerifyAssets()
    {
        int found = 0;
        int missing = 0;

        foreach (string path in AllPrefabPaths.Distinct())
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                found++;
                Debug.Log($"[TankRoyaleSetup] FOUND: {path}");
            }
            else
            {
                missing++;
                Debug.LogError($"[TankRoyaleSetup] MISSING: {path}");
            }
        }

        Debug.Log($"[TankRoyaleSetup] Verify complete. Found: {found}, Missing: {missing}.");
    }

    private static void SetupCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(15f, 20f, 15f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 18f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;

        if (cameraObject.GetComponent<AudioListener>() == null)
        {
            cameraObject.AddComponent<AudioListener>();
        }
    }

    private static void SetupLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;

        lightObject.transform.position = new Vector3(0f, 10f, 0f);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f, 1f);
    }

    private static void BuildArenaFloor(GameObject groundTilePrefab)
    {
        GameObject arenaFloorRoot = new GameObject("ArenaFloor");

        for (int x = 0; x < ArenaSize; x++)
        {
            for (int z = 0; z < ArenaSize; z++)
            {
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(groundTilePrefab);
                tile.transform.SetParent(arenaFloorRoot.transform);
                tile.transform.position = new Vector3(x, 0f, z);
            }
        }
    }

    private static void BuildArenaBlocks(GameObject cratePrefab)
    {
        GameObject arenaBlocksRoot = new GameObject("ArenaBlocks");

        UnityEngine.Random.InitState(42);

        for (int x = 0; x < ArenaSize; x++)
        {
            for (int z = 0; z < ArenaSize; z++)
            {
                if (IsSpawnProtectedCell(x, z))
                {
                    continue;
                }

                if (UnityEngine.Random.Range(0f, 1f) > 0.25f)
                {
                    continue;
                }

                GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab);
                block.transform.SetParent(arenaBlocksRoot.transform);
                block.transform.position = new Vector3(x, 0f, z);

                Component destructible = AddComponentByName(block, "DestructibleBlock", true);
                if (destructible != null)
                {
                    TrySetMemberValue(destructible, new[] { "hitPoints" }, 2);
                }
            }
        }
    }

    private static void SetupPlayerTank(GameObject playerTankPrefab)
    {
        GameObject playerTank = (GameObject)PrefabUtility.InstantiatePrefab(playerTankPrefab);
        playerTank.transform.position = new Vector3(2f, 0f, 2f);
        SetTag(playerTank, "Player");

        _ = AddComponentByName(playerTank, "TankController", true);
        _ = AddComponentByName(playerTank, "WeaponController", true);
    }

    private static void SetupEnemyTanks(GameObject enemyTank01Prefab, GameObject enemyTank02Prefab, GameObject enemyTank03Prefab)
    {
        EnsureTagExists("Enemy");

        var enemies = new List<(GameObject prefab, Vector3 position)>
        {
            (enemyTank01Prefab, new Vector3(27f, 0f, 27f)),
            (enemyTank02Prefab, new Vector3(27f, 0f, 2f)),
            (enemyTank03Prefab, new Vector3(2f, 0f, 27f))
        };

        foreach ((GameObject prefab, Vector3 position) enemy in enemies)
        {
            GameObject enemyTank = (GameObject)PrefabUtility.InstantiatePrefab(enemy.prefab);
            enemyTank.transform.position = enemy.position;
            SetTag(enemyTank, "Enemy");
            _ = AddComponentByName(enemyTank, "AITankController", true);
        }
    }

    private static void SetupPowerupSpawner(GameObject ricochetPrefab, GameObject armorPrefab, GameObject blockBreakerPrefab)
    {
        GameObject powerupSpawner = new GameObject("PowerupSpawner");
        Component spawnerComponent = AddComponentByName(powerupSpawner, "PowerupSpawner", true);

        if (spawnerComponent == null)
        {
            return;
        }

        // Best-effort assignment by common field/property names.
        TrySetMemberValue(spawnerComponent, new[] { "ricochetPrefab", "ricochetPowerupPrefab" }, ricochetPrefab);
        TrySetMemberValue(spawnerComponent, new[] { "armorPrefab", "armorPowerupPrefab" }, armorPrefab);
        TrySetMemberValue(spawnerComponent, new[] { "blockBreakerPrefab", "blockBreakerPowerupPrefab", "bombPrefab" }, blockBreakerPrefab);
    }

    private static void SetupAudioManager()
    {
        GameObject audioManager = new GameObject("AudioManager");
        _ = AddComponentByName(audioManager, "AudioManager", true);
    }

    private static void SetupAStarGrid()
    {
        GameObject gridObject = new GameObject("AStarGrid");
        Component gridComponent = AddComponentByName(gridObject, "AStarGrid", true);

        if (gridComponent == null)
        {
            return;
        }

        TrySetMemberValue(gridComponent, new[] { "gridWidth", "width" }, 30);
        TrySetMemberValue(gridComponent, new[] { "gridHeight", "height" }, 30);
        TrySetMemberValue(gridComponent, new[] { "cellSize" }, 1f);
        TrySetMemberValue(gridComponent, new[] { "originPosition", "manualOrigin" }, Vector3.zero);
        TrySetMemberValue(gridComponent, new[] { "useTransformAsCenter" }, false);
    }

    private static bool IsSpawnProtectedCell(int x, int z)
    {
        Vector2 cell = new Vector2(x, z);

        foreach (Vector2Int corner in CornerCells)
        {
            if (Vector2.Distance(cell, corner) <= 3f)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject LoadPrefabOrThrow(string assetPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab != null)
        {
            return prefab;
        }

        string message = $"[TankRoyaleSetup] Prefab not found at path: {assetPath}";
        Debug.LogError(message);
        throw new FileNotFoundException(message, assetPath);
    }

    private static Component AddComponentByName(GameObject target, string typeName, bool required)
    {
        Type componentType = FindTypeByName(typeName);
        if (componentType == null)
        {
            string message = $"[TankRoyaleSetup] Component type '{typeName}' not found. Ensure {typeName}.cs exists and compiles.";
            if (required)
            {
                Debug.LogError(message);
            }
            else
            {
                Debug.LogWarning(message);
            }

            return null;
        }

        Component existing = target.GetComponent(componentType);
        if (existing != null)
        {
            return existing;
        }

        return target.AddComponent(componentType);
    }

    private static Type FindTypeByName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        Type direct = Type.GetType(typeName);
        if (direct != null)
        {
            return direct;
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            for (int i = 0; i < types.Length; i++)
            {
                Type candidate = types[i];
                if (candidate.Name == typeName || candidate.FullName == typeName)
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static bool TrySetMemberValue(object target, string[] memberNames, object value)
    {
        if (target == null || memberNames == null || memberNames.Length == 0)
        {
            return false;
        }

        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type targetType = target.GetType();

        foreach (string memberName in memberNames)
        {
            if (string.IsNullOrWhiteSpace(memberName))
            {
                continue;
            }

            FieldInfo field = targetType.GetField(memberName, Flags);
            if (field != null)
            {
                if (!CanAssignValue(field.FieldType, value))
                {
                    continue;
                }

                field.SetValue(target, value);
                MarkDirtyIfUnityObject(target);
                return true;
            }

            PropertyInfo property = targetType.GetProperty(memberName, Flags);
            if (property == null || !property.CanWrite)
            {
                continue;
            }

            if (!CanAssignValue(property.PropertyType, value))
            {
                continue;
            }

            property.SetValue(target, value, null);
            MarkDirtyIfUnityObject(target);
            return true;
        }

        return false;
    }

    private static bool CanAssignValue(Type memberType, object value)
    {
        if (value == null)
        {
            return !memberType.IsValueType || Nullable.GetUnderlyingType(memberType) != null;
        }

        return memberType.IsInstanceOfType(value);
    }

    private static void SetTag(GameObject target, string tag)
    {
        if (target == null || string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        try
        {
            target.tag = tag;
        }
        catch (UnityException)
        {
            EnsureTagExists(tag);
            target.tag = tag;
        }
    }

    private static void EnsureTagExists(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || IsBuiltInTag(tag))
        {
            return;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProperty = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProperty.arraySize; i++)
        {
            SerializedProperty existingTag = tagsProperty.GetArrayElementAtIndex(i);
            if (existingTag.stringValue == tag)
            {
                return;
            }
        }

        tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
        SerializedProperty newTag = tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1);
        newTag.stringValue = tag;

        tagManager.ApplyModifiedProperties();
        tagManager.Update();

        Debug.Log($"[TankRoyaleSetup] Added missing tag '{tag}'.");
    }

    private static bool IsBuiltInTag(string tag)
    {
        switch (tag)
        {
            case "Untagged":
            case "Respawn":
            case "Finish":
            case "EditorOnly":
            case "MainCamera":
            case "Player":
            case "GameController":
                return true;
            default:
                return false;
        }
    }

    private static void EnsureFolderExists(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string[] parts = assetFolder.Split('/');
        if (parts.Length == 0 || parts[0] != "Assets")
        {
            throw new ArgumentException($"Folder must start with 'Assets'. Got: {assetFolder}");
        }

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

    private static void MarkDirtyIfUnityObject(object target)
    {
        if (target is UnityEngine.Object unityObject)
        {
            EditorUtility.SetDirty(unityObject);
        }
    }
}
