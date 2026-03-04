using System.Collections.Generic;
using UnityEngine;

namespace TankRoyale.AI
{
    /// <summary>
    /// Static A* pathfinding over AStarGrid using a binary min-heap for the open set.
    /// </summary>
    public static class AStarPathfinder
    {
        private const int StraightCost = 10;
        private const int DiagonalCost = 14;

        private static readonly BinaryHeap OpenSet = new BinaryHeap(900);
        private static readonly List<GridNode> NodeTraceBuffer = new List<GridNode>(128);

        /// <summary>
        /// Required API: finds a path and returns waypoint world positions.
        /// </summary>
        public static List<Vector3> FindPath(Vector3 start, Vector3 end, AStarGrid grid)
        {
            List<Vector3> waypoints = new List<Vector3>(64);
            FindPath(start, end, grid, waypoints);
            return waypoints;
        }

        /// <summary>
        /// Non-alloc overload for runtime AI loops.
        /// </summary>
        public static bool FindPath(Vector3 start, Vector3 end, AStarGrid grid, List<Vector3> waypointsBuffer)
        {
            waypointsBuffer.Clear();

            if (grid == null)
            {
                return false;
            }

            GridNode startNode = grid.GetNodeFromWorldPos(start);
            GridNode endNode = grid.GetNodeFromWorldPos(end);

            if (startNode == null || endNode == null)
            {
                return false;
            }

            ResetNodes(grid);
            OpenSet.EnsureCapacity(grid.MaxNodeCount);
            OpenSet.Clear();

            startNode.GCost = 0;
            startNode.HCost = GetDistanceCost(startNode, endNode);
            startNode.Parent = null;
            startNode.IsOpen = true;
            OpenSet.Add(startNode);

            while (OpenSet.Count > 0)
            {
                GridNode current = OpenSet.RemoveFirst();
                current.IsClosed = true;

                if (current == endNode)
                {
                    BuildWaypoints(startNode, endNode, waypointsBuffer);
                    return waypointsBuffer.Count > 0;
                }

                int baseX = current.GridX;
                int baseY = current.GridY;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        int neighborX = baseX + x;
                        int neighborY = baseY + y;
                        GridNode neighbor;
                        if (!grid.TryGetNode(neighborX, neighborY, out neighbor))
                        {
                            continue;
                        }

                        if (neighbor.IsClosed)
                        {
                            continue;
                        }

                        // Allow destination selection even if it is currently occupied,
                        // so AI can still converge near target tanks.
                        if (!neighbor.Walkable && neighbor != endNode)
                        {
                            continue;
                        }

                        bool diagonalMove = x != 0 && y != 0;
                        if (diagonalMove && !CanTraverseDiagonal(grid, baseX, baseY, x, y))
                        {
                            continue;
                        }

                        int stepCost = diagonalMove ? DiagonalCost : StraightCost;
                        int tentativeG = current.GCost + stepCost;

                        if (tentativeG < neighbor.GCost || !neighbor.IsOpen)
                        {
                            neighbor.Parent = current;
                            neighbor.GCost = tentativeG;
                            neighbor.HCost = GetDistanceCost(neighbor, endNode);

                            if (!neighbor.IsOpen)
                            {
                                neighbor.IsOpen = true;
                                OpenSet.Add(neighbor);
                            }
                            else
                            {
                                OpenSet.UpdateItem(neighbor);
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static void BuildWaypoints(GridNode startNode, GridNode endNode, List<Vector3> waypointsBuffer)
        {
            waypointsBuffer.Clear();
            NodeTraceBuffer.Clear();

            GridNode current = endNode;
            while (current != null && current != startNode)
            {
                NodeTraceBuffer.Add(current);
                current = current.Parent;
            }

            if (NodeTraceBuffer.Count == 0)
            {
                return;
            }

            int lastDirX = 0;
            int lastDirY = 0;

            for (int i = NodeTraceBuffer.Count - 1; i >= 0; i--)
            {
                GridNode node = NodeTraceBuffer[i];
                GridNode nextNode = i > 0 ? NodeTraceBuffer[i - 1] : null;

                int dirX = 0;
                int dirY = 0;
                if (nextNode != null)
                {
                    dirX = nextNode.GridX - node.GridX;
                    dirY = nextNode.GridY - node.GridY;
                }

                bool directionChanged = waypointsBuffer.Count == 0 || dirX != lastDirX || dirY != lastDirY;
                if (directionChanged)
                {
                    waypointsBuffer.Add(node.WorldPosition);
                }

                lastDirX = dirX;
                lastDirY = dirY;
            }

            Vector3 targetPoint = endNode.WorldPosition;
            int finalIndex = waypointsBuffer.Count - 1;
            if (finalIndex < 0 || (waypointsBuffer[finalIndex] - targetPoint).sqrMagnitude > 0.0001f)
            {
                waypointsBuffer.Add(targetPoint);
            }
        }

        private static bool CanTraverseDiagonal(AStarGrid grid, int currentX, int currentY, int stepX, int stepY)
        {
            // Prevent corner cutting through blocked tiles.
            bool horizontalWalkable = grid.IsWalkable(currentX + stepX, currentY);
            bool verticalWalkable = grid.IsWalkable(currentX, currentY + stepY);
            return horizontalWalkable && verticalWalkable;
        }

        private static void ResetNodes(AStarGrid grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    GridNode node = grid.GetNode(x, y);
                    if (node != null)
                    {
                        node.ResetPathState();
                    }
                }
            }
        }

        private static int GetDistanceCost(GridNode a, GridNode b)
        {
            int dx = Mathf.Abs(a.GridX - b.GridX);
            int dy = Mathf.Abs(a.GridY - b.GridY);

            int diagonalSteps = Mathf.Min(dx, dy);
            int straightSteps = Mathf.Abs(dx - dy);
            return diagonalSteps * DiagonalCost + straightSteps * StraightCost;
        }

        /// <summary>
        /// Binary min-heap keyed by node f-cost, then h-cost.
        /// </summary>
        private sealed class BinaryHeap
        {
            private GridNode[] _items;

            public int Count { get; private set; }

            public BinaryHeap(int initialCapacity)
            {
                _items = new GridNode[Mathf.Max(1, initialCapacity)];
                Count = 0;
            }

            public void EnsureCapacity(int required)
            {
                if (required <= _items.Length)
                {
                    return;
                }

                int newCapacity = _items.Length;
                while (newCapacity < required)
                {
                    newCapacity *= 2;
                }

                GridNode[] expanded = new GridNode[newCapacity];
                for (int i = 0; i < Count; i++)
                {
                    expanded[i] = _items[i];
                }

                _items = expanded;
            }

            public void Clear()
            {
                for (int i = 0; i < Count; i++)
                {
                    _items[i] = null;
                }

                Count = 0;
            }

            public void Add(GridNode node)
            {
                node.HeapIndex = Count;
                _items[Count] = node;
                Count++;
                SortUp(node);
            }

            public GridNode RemoveFirst()
            {
                GridNode firstItem = _items[0];
                Count--;

                if (Count > 0)
                {
                    GridNode lastItem = _items[Count];
                    _items[0] = lastItem;
                    lastItem.HeapIndex = 0;
                    SortDown(lastItem);
                }

                _items[Count] = null;
                firstItem.HeapIndex = -1;
                firstItem.IsOpen = false;
                return firstItem;
            }

            public void UpdateItem(GridNode node)
            {
                SortUp(node);
            }

            private void SortDown(GridNode node)
            {
                while (true)
                {
                    int leftIndex = node.HeapIndex * 2 + 1;
                    int rightIndex = leftIndex + 1;
                    int swapIndex = 0;

                    if (leftIndex < Count)
                    {
                        swapIndex = leftIndex;

                        if (rightIndex < Count && Compare(_items[rightIndex], _items[leftIndex]) < 0)
                        {
                            swapIndex = rightIndex;
                        }

                        if (Compare(_items[swapIndex], node) < 0)
                        {
                            Swap(node, _items[swapIndex]);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            private void SortUp(GridNode node)
            {
                while (true)
                {
                    int parentIndex = (node.HeapIndex - 1) / 2;
                    if (parentIndex < 0)
                    {
                        return;
                    }

                    GridNode parentItem = _items[parentIndex];
                    if (Compare(node, parentItem) < 0)
                    {
                        Swap(node, parentItem);
                    }
                    else
                    {
                        return;
                    }
                }
            }

            private static int Compare(GridNode a, GridNode b)
            {
                int fCompare = a.FCost.CompareTo(b.FCost);
                if (fCompare != 0)
                {
                    return fCompare;
                }

                return a.HCost.CompareTo(b.HCost);
            }

            private void Swap(GridNode a, GridNode b)
            {
                _items[a.HeapIndex] = b;
                _items[b.HeapIndex] = a;

                int aIndex = a.HeapIndex;
                a.HeapIndex = b.HeapIndex;
                b.HeapIndex = aIndex;
            }
        }
    }
}
