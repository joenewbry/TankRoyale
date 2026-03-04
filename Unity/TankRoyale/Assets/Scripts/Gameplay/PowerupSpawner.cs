using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.Gameplay
{
    public class PowerupSpawner : MonoBehaviour
    {
        [Header("Powerup Prefabs")]
        [SerializeField] private GameObject ricochetPrefab;
        [SerializeField] private GameObject armorPrefab;
        [SerializeField] private GameObject blockBreakerPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private int spawnCount = 3;
        [SerializeField] private float gridSize = 30f;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float spawnHeight = 0.5f;

        private readonly List<GameObject> prefabs = new List<GameObject>();

        private void Start()
        {
            if (ricochetPrefab != null) prefabs.Add(ricochetPrefab);
            if (armorPrefab != null) prefabs.Add(armorPrefab);
            if (blockBreakerPrefab != null) prefabs.Add(blockBreakerPrefab);
            SpawnPowerups();
        }

        private void SpawnPowerups()
        {
            if (prefabs.Count == 0)
            {
                Debug.LogWarning("[PowerupSpawner] No powerup prefabs assigned.");
                return;
            }

            int spawned = 0;
            int attempts = 0;
            int maxAttempts = 200;

            while (spawned < spawnCount && attempts < maxAttempts)
            {
                attempts++;
                float x = Mathf.Floor(Random.Range(2f, gridSize - 2f)) * cellSize + cellSize * 0.5f;
                float z = Mathf.Floor(Random.Range(2f, gridSize - 2f)) * cellSize + cellSize * 0.5f;
                Vector3 spawnPos = new Vector3(x, spawnHeight, z);

                // Check nothing is already at this position
                Collider[] hits = Physics.OverlapSphere(spawnPos, 0.4f);
                if (hits.Length == 0)
                {
                    GameObject prefab = prefabs[spawned % prefabs.Count];
                    Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                    spawned++;
                }
            }

            Debug.Log($"[PowerupSpawner] Spawned {spawned} powerups.");
        }
    }
}
