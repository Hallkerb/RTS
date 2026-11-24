using System.Collections.Generic;
using UnityEngine;

public static class NavigationHelper
{
    public static Node SetTargetNode(Grid grid, Vector2 targetPoint)
    {
        Node first = GetNode(grid, grid.GridSizeX, grid.GridSizeY, grid.GetGridSize(), targetPoint);

        if (first.IsSizeClear[0]) return first;

        var heap = new FastPriorityQueue<Node>();
        var visited = new HashSet<int> { first.ID };
        heap.Enqueue(0, first);

        int iteration = 0;

        while(heap.Count > 0 && iteration < 10000)
        {
            Node node = heap.Dequeue().element;

            if (node.IsSizeClear[0])
                return node;

            foreach (Node neighbor in node.NeighborsNode)
            {
                if (neighbor == null || !visited.Add(neighbor.ID)) continue;

                heap.Enqueue(Vector2.Distance(first.Position, neighbor.Position), neighbor);
            }

            iteration++;
        }

        return first;
    }

    public static bool IsPathClear(Node startNode, Vector2 endPoint, Unit unit, Queue<Node> openSet, HashSet<Node> closedSet, float stepSize = 0)
    {
        if(startNode.IsClear == false)
            return false;

        openSet.Clear();
        closedSet.Clear();

        openSet.Enqueue(startNode);

        while(openSet.Count > 0)
        {
            Node currentNode = openSet.Dequeue();

            closedSet.Add(currentNode);

            foreach(Node neighbor in currentNode.NeighborsNode)
            {
                if(neighbor == null)
                    return false;

                if (closedSet.Contains(neighbor))
                    continue;

                if (!openSet.Contains(neighbor) && IsInsidePath(startNode.Position, endPoint, neighbor.Position, unit, stepSize))
                {
                    if(neighbor.IsClear == false)
                        return false;
                        
                    openSet.Enqueue(neighbor);
                }
            }
        }

        return true;
    }

    private static bool IsInsidePath(Vector2 startPoint, Vector2 endPoint, Vector2 point, Unit unit, float stepSize = 0)
    {
        bool isInside = false;

        switch(unit.Shape.Type)
        {
            case EntityShape.ShapeType.Square:
                isInside = IsPointInsideSquare(startPoint, endPoint, point, unit.Shape.SizeX, unit.Shape.SizeY, stepSize);
                break;
            case EntityShape.ShapeType.Circle:
                isInside = IsPointInsideCapsule(startPoint, endPoint, point, unit.Shape.SizeX / 2, stepSize);
                break;
        }

        return isInside;
    }

    private static bool IsPointInsideSquare(Vector2 startPoint, Vector2 endPoint, Vector2 point, float sizeX, float sizeY, float stepSize = 0)
    {
        Vector2 axis = endPoint - startPoint;
        Vector2 dir = axis.normalized;

        Vector2 perpendicular = new Vector2(-dir.y, dir.x);

        float halfWidth = sizeX / 2f;
        float halfHeight = sizeY / 2f;

        Vector2 localPoint = point - startPoint;

        float projectionOnAxis = Vector2.Dot(localPoint, dir);

        float length = axis.magnitude;

        if (projectionOnAxis < -halfHeight || projectionOnAxis > length + halfHeight)
            return false;

        float projectionOnPerpendicular = Vector2.Dot(localPoint, perpendicular);
        
        if (Mathf.Abs(projectionOnPerpendicular) > halfWidth + stepSize)
            return false;

        return true;
    }

    private static bool IsPointInsideCapsule(Vector2 startPoint, Vector2 endPoint, Vector2 point, float radius, float stepSize = 0)
    {
        Vector2 axis = endPoint - startPoint;                  // Vector from start to end
        Vector2 dir = axis.normalized;               // Direction along capsule
        float length = axis.magnitude;               // Length capsule (without rounding)

        Vector2 toPoint = point - startPoint;             // Vector from start to point

        float projection = Vector2.Dot(toPoint, dir); // Projection of point on direction

        // Limitation projection — the most distant point on direction
        projection = Mathf.Clamp(projection, 0f, length);

        Vector2 closestPoint = startPoint + dir * projection; // the most closest point on axis capsule
        float distanceToCapsule = Vector2.Distance(point, closestPoint); // Distance to axis

        return distanceToCapsule <= radius + stepSize;
    }

    public static bool IsPosClear(Node startNode, Unit unit, Queue<Node> openSet, HashSet<Node> closedSet, float stepSize = 0)
    {
        if(startNode.IsClear == false)
            return false;

        openSet.Clear();
        closedSet.Clear();

        openSet.Enqueue(startNode);

        while(openSet.Count > 0)
        {
            Node currentNode = openSet.Dequeue();

            closedSet.Add(currentNode);

            foreach(Node neighbor in currentNode.NeighborsNode)
            {
                if(neighbor == null)
                    return false;

                if (closedSet.Contains(neighbor))
                    continue;
                else if (!openSet.Contains(neighbor) && unit.Shape.IsInside(startNode.Position, neighbor.Position, stepSize))
                {
                    if(neighbor.IsClear == false)
                        return false;

                    openSet.Enqueue(neighbor);
                }
            }
        }

        return true;
    }

    public static Node GetNode(Grid grid, int gridSizeX, int gridSizeY, float gridSize, Vector2 point)
    {
        int x = Mathf.FloorToInt((point.x - grid.SizeX.x) / gridSize);
        int y = Mathf.FloorToInt((point.y - grid.SizeY.x) / gridSize);

        x = Mathf.Clamp(x, 0, gridSizeX - 1);
        y = Mathf.Clamp(y, 0, gridSizeY - 1);

        return grid.Nodes[y * gridSizeX + x];
    }
}