using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.Gameplay
{
    /// <summary>
    /// Spawns powerups at random empty positions on a timed schedule.
    /// Drop timing: initial burst on Start, then periodic respawns.
    /// </summary>
    public class PowerupSpawner : MonoBehaviour
    {
        [Header("Prefabs — assign in Inspector")]
        [SerializeField] private GameObject ricochetPrefab;
        [SerializeField] private GameObject armorPrefab;
        [SerializeField] private GameObject blockBreakerPrefab;
        [SerializeField] private GameObject healPrefab;         // Gear icon

        [Header("Spawn Settings")]
        [SerializeField] private int initialSpawnCount   = 3;  // On game start
        [SerializeField] private float respawnInterval   = 15f; // Seconds between new drops
        [SerializeField] private int maxActivePowerups   = 5;   // Cap live on field
        [SerializeField] private float gridSize          = 30f;
        [SerializeField] private float cellSize          = 1f;
        [SerializeField] private float spawnHeight       = 0.75f;
        [SerializeField] private float spawnCheckRadius  = 0.45f;

        // Drop weight table — heal is rarer, blockBreaker is rare
        private readonly (PowerupType type, int weight)[] _dropTable =
        {
            (PowerupType.Ricochet,    35),
            (PowerupType.Armor,       30),
            (PowerupType.Heal,        25),
            (PowerupType.BlockBreaker,10),
        };

        private readonly List<GameObject> _active = new List<GameObject>();

        private void Start()
        {
            for (int i = 0; i < initialSpawnCount; i++)
                TrySpawnOne();

            StartCoroutine(RespawnLoop());
        }

        private IEnumerator RespawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(respawnInterval);
                // Clean up destroyed entries
                _active.RemoveAll(go => go == null);
                if (_active.Count < maxActivePowerups)
                    TrySpawnOne();
            }
        }

        private void TrySpawnOne()
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float x = Random.Range(2f, gridSize - 2f);
                float z = Random.Range(2f, gridSize - 2f);
                Vector3 pos = new Vector3(x, spawnHeight, z);

                if (Physics.CheckSphere(pos, spawnCheckRadius)) continue;

                PowerupType type = WeightedRandom();
                GameObject prefab = GetPrefab(type);
                if (prefab == null) continue;

                GameObject go = Instantiate(prefab, pos, Quaternion.identity, transform);

                // Add PowerupPickup component with correct type
                var pickup = go.GetComponent<PowerupPickup>() ?? go.AddComponent<PowerupPickup>();
                pickup.powerupType = type;

                // Add trigger collider if missing
                if (go.GetComponent<Collider>() == null)
                {
                    var col = go.AddComponent<SphereCollider>();
                    col.isTrigger = true;
                    col.radius = 0.5f;
                }

                _active.Add(go);
                Debug.Log($"[PowerupSpawner] Spawned {type} at {pos}");
                return;
            }
        }

        private PowerupType WeightedRandom()
        {
            int total = 0;
            foreach (var entry in _dropTable) total += entry.weight;
            int roll = Random.Range(0, total);
            int cumulative = 0;
            foreach (var (type, weight) in _dropTable)
            {
                cumulative += weight;
                if (roll < cumulative) return type;
            }
            return PowerupType.Ricochet;
        }

        private GameObject GetPrefab(PowerupType type)
        {
            return type switch
            {
                PowerupType.Ricochet     => ricochetPrefab,
                PowerupType.Armor        => armorPrefab,
                PowerupType.BlockBreaker => blockBreakerPrefab,
                PowerupType.Heal         => healPrefab,
                _                        => ricochetPrefab
            };
        }
    }
}
