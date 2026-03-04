using System;
using System.Reflection;
using UnityEngine;

namespace TankRoyale.AI
{
    /// <summary>
    /// 30x30 navigation grid for AI pathfinding.
    /// Builds from an ArenaSpawner block map and supports runtime walkability updates.
    /// </summary>
    [DisallowMultipleComponent]
    public class AStarGrid : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int width = 30;
        [SerializeField] private int height = 30;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private bool useTransformAsCenter = true;
        [SerializeField] private Vector3 manualOrigin = Vector3.zero;

        [Header("Arena Source")]
        [Tooltip("Drag the ArenaSpawner component here.")]
        [SerializeField] private MonoBehaviour arenaSpawner;
        [Tooltip("ArenaSpawner member containing occupancy map (bool[,] / int[,]).")]
        [SerializeField] private string blockMapMemberName = "BlockMap";
        [Tooltip("If true, map value=true/1 means blocked. If false, true/1 means walkable.")]
        [SerializeField] private bool mapValueMeansBlocked = true;

        private GridNode[,] _nodes;
        private Vector3 _gridOrigin;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public int MaxNodeCount => width * height;
        public Vector3 GridOrigin => _gridOrigin;

        private void Awake()
        {
            BuildGrid();
            RefreshFromArenaSpawner();
        }

        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cellSize = Mathf.Max(0.01f, cellSize);
        }

        public void BuildGrid()
        {
            ResolveOrigin();

            bool mustRecreate = _nodes == null || _nodes.GetLength(0) != width || _nodes.GetLength(1) != height;
            if (mustRecreate)
            {
                _nodes = new GridNode[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector3 worldPos = GetCellCenterWorld(x, y);
                        _nodes[x, y] = new GridNode(true, worldPos, x, y);
                    }
                }

                return;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GridNode node = _nodes[x, y];
                    node.WorldPosition = GetCellCenterWorld(x, y);
                    node.Walkable = true;
                    node.ResetPathState();
                }
            }
        }

        public void RefreshFromArenaSpawner()
        {
            if (_nodes == null)
            {
                BuildGrid();
            }

            object mapObject;
            if (!TryGetBlockMapObject(out mapObject))
            {
                return;
            }

            bool[,] boolMap = mapObject as bool[,];
            if (boolMap != null)
            {
                ApplyBoolMap(boolMap);
                return;
            }

            int[,] intMap = mapObject as int[,];
            if (intMap != null)
            {
                ApplyIntMap(intMap);
            }
        }

        public GridNode GetNodeFromWorldPos(Vector3 worldPosition)
        {
            if (_nodes == null)
            {
                BuildGrid();
            }

            int x = Mathf.FloorToInt((worldPosition.x - _gridOrigin.x) / cellSize);
            int y = Mathf.FloorToInt((worldPosition.z - _gridOrigin.z) / cellSize);

            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);

            return _nodes[x, y];
        }

        public bool TryGetNode(int x, int y, out GridNode node)
        {
            if (!IsInBounds(x, y))
            {
                node = null;
                return false;
            }

            if (_nodes == null)
            {
                BuildGrid();
            }

            node = _nodes[x, y];
            return true;
        }

        public GridNode GetNode(int x, int y)
        {
            GridNode node;
            return TryGetNode(x, y, out node) ? node : null;
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsWalkable(int x, int y)
        {
            GridNode node;
            if (!TryGetNode(x, y, out node))
            {
                return false;
            }

            return node.Walkable;
        }

        public void SetWalkable(int x, int y, bool walkable)
        {
            GridNode node;
            if (TryGetNode(x, y, out node))
            {
                node.Walkable = walkable;
            }
        }

        public void SetWalkable(Vector3 worldPosition, bool walkable)
        {
            GridNode node = GetNodeFromWorldPos(worldPosition);
            if (node != null)
            {
                node.Walkable = walkable;
            }
        }

        /// <summary>
        /// Hook this from destructible-block callbacks when a crate is removed.
        /// </summary>
        public void NotifyBlockDestroyed(Vector3 blockWorldPosition)
        {
            SetWalkable(blockWorldPosition, true);
        }

        /// <summary>
        /// Hook this from destructible-block callbacks when a crate is removed.
        /// </summary>
        public void NotifyBlockDestroyed(int x, int y)
        {
            SetWalkable(x, y, true);
        }

        // UnityEvent-friendly aliases (ArenaSpawner/destructible callbacks).
        public void OnBlockDestroyed(Vector3 blockWorldPosition)
        {
            NotifyBlockDestroyed(blockWorldPosition);
        }

        public void OnBlockDestroyed(int x, int y)
        {
            NotifyBlockDestroyed(x, y);
        }

        public void OnArenaMapChanged()
        {
            RefreshFromArenaSpawner();
        }

        public Vector3 GetCellCenterWorld(int x, int y)
        {
            return _gridOrigin + new Vector3((x + 0.5f) * cellSize, 0f, (y + 0.5f) * cellSize);
        }

        private void ResolveOrigin()
        {
            if (useTransformAsCenter)
            {
                _gridOrigin = transform.position - new Vector3(width * cellSize, 0f, height * cellSize) * 0.5f;
                return;
            }

            _gridOrigin = manualOrigin;
        }

        private void ApplyBoolMap(bool[,] map)
        {
            int mapWidth = map.GetLength(0);
            int mapHeight = map.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool blocked = false;
                    if (x < mapWidth && y < mapHeight)
                    {
                        bool value = map[x, y];
                        blocked = mapValueMeansBlocked ? value : !value;
                    }

                    _nodes[x, y].Walkable = !blocked;
                }
            }
        }

        private void ApplyIntMap(int[,] map)
        {
            int mapWidth = map.GetLength(0);
            int mapHeight = map.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool blocked = false;
                    if (x < mapWidth && y < mapHeight)
                    {
                        int value = map[x, y];
                        bool occupied = value != 0;
                        blocked = mapValueMeansBlocked ? occupied : !occupied;
                    }

                    _nodes[x, y].Walkable = !blocked;
                }
            }
        }

        private bool TryGetBlockMapObject(out object mapObject)
        {
            mapObject = null;
            if (arenaSpawner == null)
            {
                return false;
            }

            Type spawnerType = arenaSpawner.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (!string.IsNullOrEmpty(blockMapMemberName))
            {
                FieldInfo namedField = spawnerType.GetField(blockMapMemberName, flags);
                if (namedField != null)
                {
                    mapObject = namedField.GetValue(arenaSpawner);
                    return mapObject != null;
                }

                PropertyInfo namedProperty = spawnerType.GetProperty(blockMapMemberName, flags);
                if (namedProperty != null)
                {
                    mapObject = namedProperty.GetValue(arenaSpawner, null);
                    return mapObject != null;
                }
            }

            // Fallback: first bool[,] or int[,] member.
            FieldInfo[] fields = spawnerType.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                Type fieldType = fields[i].FieldType;
                if (fieldType == typeof(bool[,]) || fieldType == typeof(int[,]))
                {
                    mapObject = fields[i].GetValue(arenaSpawner);
                    return mapObject != null;
                }
            }

            PropertyInfo[] properties = spawnerType.GetProperties(flags);
            for (int i = 0; i < properties.Length; i++)
            {
                Type propertyType = properties[i].PropertyType;
                if (propertyType == typeof(bool[,]) || propertyType == typeof(int[,]))
                {
                    mapObject = properties[i].GetValue(arenaSpawner, null);
                    return mapObject != null;
                }
            }

            return false;
        }
    }
}
