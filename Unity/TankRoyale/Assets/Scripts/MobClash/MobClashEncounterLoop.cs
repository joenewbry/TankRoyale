using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankRoyale.MobClash
{
    public enum MobClashTeam
    {
        Friendly,
        Enemy
    }

    /// <summary>
    /// Spawns two squads and runs a lightweight auto-battle loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class MobClashEncounterLoop : MonoBehaviour
    {
        [Header("Encounter")]
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField, Min(1)] private int friendlyCount = 4;
        [SerializeField, Min(1)] private int enemyCount = 4;

        [Header("Spawn")]
        [SerializeField] private GameObject creaturePrefab;
        [SerializeField] private Vector3 friendlySpawnCenter = new Vector3(-8f, 0.5f, 0f);
        [SerializeField] private Vector3 enemySpawnCenter = new Vector3(8f, 0.5f, 0f);
        [SerializeField, Min(0.5f)] private float laneSpacing = 1.75f;

        [Header("Advance Directions")]
        [SerializeField] private Vector3 friendlyAdvanceDirection = Vector3.right;
        [SerializeField] private Vector3 enemyAdvanceDirection = Vector3.left;

        [Header("Visuals")]
        [SerializeField] private Color friendlyColor = new Color(0.2f, 0.8f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.35f, 0.35f);

        private readonly List<MobClashCreature> _friendlies = new List<MobClashCreature>();
        private readonly List<MobClashCreature> _enemies = new List<MobClashCreature>();

        public bool EncounterActive { get; private set; }
        public event Action<MobClashTeam> EncounterEnded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureEncounterLoopExists()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            // Keep this MobClash slice self-running in the default gameplay scene.
            if (!string.Equals(activeScene.name, "Arena", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (FindObjectOfType<MobClashEncounterLoop>() != null)
            {
                return;
            }

            GameObject encounterRoot = new GameObject("MobClash Encounter Loop");
            encounterRoot.AddComponent<MobClashEncounterLoop>();
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                StartEncounter();
            }
        }

        private void Update()
        {
            if (!EncounterActive)
            {
                return;
            }

            if (_friendlies.Count == 0 || _enemies.Count == 0)
            {
                MobClashTeam winner = _friendlies.Count > 0 ? MobClashTeam.Friendly : MobClashTeam.Enemy;
                EndEncounter(winner);
            }
        }

        public void StartEncounter()
        {
            ClearEncounter();

            SpawnSquad(MobClashTeam.Friendly, friendlyCount, friendlySpawnCenter, friendlyAdvanceDirection.normalized);
            SpawnSquad(MobClashTeam.Enemy, enemyCount, enemySpawnCenter, enemyAdvanceDirection.normalized);

            EncounterActive = _friendlies.Count > 0 && _enemies.Count > 0;

            if (EncounterActive)
            {
                Debug.Log($"[MobClash] Encounter started: friendly={_friendlies.Count}, enemy={_enemies.Count}");
            }
            else
            {
                Debug.LogWarning("[MobClash] Encounter did not start. Check squad sizes.");
            }
        }

        public MobClashCreature FindNearestEnemy(MobClashCreature seeker)
        {
            List<MobClashCreature> enemyList = seeker.Team == MobClashTeam.Friendly ? _enemies : _friendlies;

            MobClashCreature nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = enemyList.Count - 1; i >= 0; i--)
            {
                MobClashCreature candidate = enemyList[i];
                if (candidate == null || !candidate.IsAlive)
                {
                    enemyList.RemoveAt(i);
                    continue;
                }

                Vector3 offset = candidate.transform.position - seeker.transform.position;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;

                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        public void NotifyCreatureDefeated(MobClashCreature creature)
        {
            if (creature == null)
            {
                return;
            }

            if (creature.Team == MobClashTeam.Friendly)
            {
                _friendlies.Remove(creature);
            }
            else
            {
                _enemies.Remove(creature);
            }
        }

        private void EndEncounter(MobClashTeam winner)
        {
            if (!EncounterActive)
            {
                return;
            }

            EncounterActive = false;
            Debug.Log($"[MobClash] Encounter finished. Winner: {winner}");
            EncounterEnded?.Invoke(winner);
        }

        private void SpawnSquad(MobClashTeam team, int count, Vector3 center, Vector3 advanceDirection)
        {
            int half = count / 2;

            for (int i = 0; i < count; i++)
            {
                float laneOffset = (i - half) * laneSpacing;
                Vector3 spawnPosition = center + new Vector3(0f, 0f, laneOffset);

                GameObject creatureObject = SpawnCreatureObject(team, i, spawnPosition);
                MobClashCreature creature = creatureObject.GetComponent<MobClashCreature>();
                if (creature == null)
                {
                    creature = creatureObject.AddComponent<MobClashCreature>();
                }

                creature.Initialize(this, team, advanceDirection);

                if (team == MobClashTeam.Friendly)
                {
                    _friendlies.Add(creature);
                }
                else
                {
                    _enemies.Add(creature);
                }
            }
        }

        private GameObject SpawnCreatureObject(MobClashTeam team, int index, Vector3 position)
        {
            GameObject creatureObject;

            if (creaturePrefab != null)
            {
                creatureObject = Instantiate(creaturePrefab, position, Quaternion.identity, transform);
            }
            else
            {
                creatureObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                creatureObject.transform.SetParent(transform);
                creatureObject.transform.position = position;
            }

            creatureObject.name = $"{team} Creature {index + 1}";
            ApplyTeamColor(creatureObject, team == MobClashTeam.Friendly ? friendlyColor : enemyColor);
            return creatureObject;
        }

        private void ApplyTeamColor(GameObject creatureObject, Color color)
        {
            Renderer[] renderers = creatureObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rendererComponent = renderers[i];
                if (rendererComponent == null || rendererComponent.material == null)
                {
                    continue;
                }

                rendererComponent.material.color = color;
            }
        }

        private void ClearEncounter()
        {
            for (int i = _friendlies.Count - 1; i >= 0; i--)
            {
                if (_friendlies[i] != null)
                {
                    Destroy(_friendlies[i].gameObject);
                }
            }

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                if (_enemies[i] != null)
                {
                    Destroy(_enemies[i].gameObject);
                }
            }

            _friendlies.Clear();
            _enemies.Clear();
            EncounterActive = false;
        }
    }
}
