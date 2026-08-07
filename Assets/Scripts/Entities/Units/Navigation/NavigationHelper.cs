using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class NavigationHelper
{
    public static Node SetTargetNode(Floor floor, Unit unit, Vector2 targetPoint, float stepSize = 0)
    {
        HashSet<Node> openTargetSet = new HashSet<Node>();
        HashSet<Node> closedTargetSet = new HashSet<Node>();

        int iteration = 0;

        targetPoint.x = Mathf.Clamp(targetPoint.x, floor.SizeX.x, floor.SizeX.y);
        targetPoint.y = Mathf.Clamp(targetPoint.y, floor.SizeY.x, floor.SizeY.y);

        Node firstNode = GetNode(floor, targetPoint);
        Node node = firstNode;
        node.g = 0;
        node.h = 0;

        openTargetSet.Add(node);

        while(node.IsSizeClear[0] == false && openTargetSet.Count > 0 && iteration < 1000)
        {
            openTargetSet.Remove(node);
            closedTargetSet.Add(node);

            foreach (Node neighbor in node.NeighborsNode)
            {
                if (neighbor == null || closedTargetSet.Contains(neighbor))
                    continue;
                else if (!openTargetSet.Contains(neighbor))
                {
                    openTargetSet.Add(neighbor);

                    neighbor.g = Vector2.Distance(firstNode.Position, neighbor.Position);

                    neighbor.h = 0;
                }
            }

            node = openTargetSet.OrderBy(n => node.IsSizeClear[0] == false).ThenBy(n => n.f).FirstOrDefault();

            iteration++;
        }

        return node;
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

        if (projectionOnAxis < -halfHeight || projectionOnAxis > halfHeight)
            return false;

        float projectionOnPerpendicular = Vector2.Dot(localPoint, perpendicular);
        if (Mathf.Abs(projectionOnPerpendicular) > halfWidth)
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

    public static Node GetNode(Floor floor, Vector2 point)
    {
        Section section = floor.Sections.FirstOrDefault(s => s.IsInside(point));

        if(section == null) return null;

        Node node = section.Grid.FirstOrDefault(n => n.IsInside(point));

        return node;
    }
}
