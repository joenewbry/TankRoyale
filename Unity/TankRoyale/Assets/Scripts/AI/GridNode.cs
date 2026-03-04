using UnityEngine;

namespace TankRoyale.AI
{
    /// <summary>
    /// One A* grid cell.
    /// Stores world position, walkability, path costs, and parent link.
    /// </summary>
    public sealed class GridNode
    {
        public Vector3 WorldPosition;
        public bool Walkable;

        public readonly int GridX;
        public readonly int GridY;

        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;

        public GridNode Parent;

        // Runtime pathfinding state (reused between searches; no per-search allocations).
        internal int HeapIndex = -1;
        internal bool IsOpen;
        internal bool IsClosed;

        public GridNode(bool walkable, Vector3 worldPosition, int gridX, int gridY)
        {
            Walkable = walkable;
            WorldPosition = worldPosition;
            GridX = gridX;
            GridY = gridY;

            ResetPathState();
        }

        public void ResetPathState()
        {
            GCost = int.MaxValue;
            HCost = 0;
            Parent = null;
            HeapIndex = -1;
            IsOpen = false;
            IsClosed = false;
        }
    }
}
