using UnityEngine;

namespace TankRoyale.AI
{
    /// <summary>
    /// Scene-agnostic A* grid that derives walkability from physics overlaps.
    /// </summary>
    [DisallowMultipleComponent]
    public class AStarGrid : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int gridWidth = 30;
        [SerializeField] private int gridHeight = 30;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector3 originPosition = Vector3.zero;

        [Header("Obstacle Detection")]
        // Default: everything except Ignore Raycast (layer 2).
        [SerializeField] private LayerMask obstacleLayer = ~(1 << 2);
        [SerializeField] private float obstacleCheckRadius = 0.4f;

        public GridNode[,] Grid { get; private set; }
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int MaxNodeCount => gridWidth * gridHeight;

        private void Awake()
        {
            BuildGrid();
        }

        private void OnValidate()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            cellSize = Mathf.Max(0.01f, cellSize);
            obstacleCheckRadius = Mathf.Max(0.01f, obstacleCheckRadius);
        }

        public void BuildGrid()
        {
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
            cellSize = Mathf.Max(0.01f, cellSize);
            obstacleCheckRadius = Mathf.Max(0.01f, obstacleCheckRadius);

            Grid = new GridNode[gridWidth, gridHeight];

            for (int z = 0; z < gridHeight; z++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector3 center = originPosition + new Vector3(
                        x * cellSize + (cellSize * 0.5f),
                        0.5f,
                        z * cellSize + (cellSize * 0.5f));

                    bool hitObstacle = Physics.CheckBox(
                        center,
                        Vector3.one * obstacleCheckRadius,
                        Quaternion.identity,
                        obstacleLayer);

                    bool walkable = !hitObstacle;
                    Grid[x, z] = new GridNode(walkable, center, x, z);
                }
            }
        }

        public GridNode GetNodeFromWorldPos(Vector3 worldPos)
        {
            if (Grid == null)
            {
                BuildGrid();
            }

            int x = Mathf.FloorToInt((worldPos.x - originPosition.x) / cellSize);
            int z = Mathf.FloorToInt((worldPos.z - originPosition.z) / cellSize);

            x = Mathf.Clamp(x, 0, gridWidth - 1);
            z = Mathf.Clamp(z, 0, gridHeight - 1);

            return Grid[x, z];
        }

        public void NotifyBlockDestroyed(Vector3 worldPos)
        {
            GridNode node = GetNodeFromWorldPos(worldPos);
            if (node != null)
            {
                node.Walkable = true;
            }
        }

        public void RebuildGrid()
        {
            BuildGrid();
        }
    }
}
