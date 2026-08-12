using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AStar_Navigation
{
    private Floor floor;
    private Unit unit;

    public List<Vector2> WayPoints = new List<Vector2>();

    public Vector2 TargetPoint { get; private set; }

    public static float stepSize = 0.05f;

    public async Task SearchWayToTarget(Vector2 startPos, Vector2 targetPoint)
    {
        TargetPoint = targetPoint;    

        await Task.Run(() =>
        {
            WayPoints.Clear();

            HashSet<Node> openSet = new HashSet<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();

            Node startNode = NavigationHelper.GetNode(floor, startPos);

            Node endNode = NavigationHelper.SetTargetNode(floor, unit, targetPoint, startNode.Size);

            endNode = SearchWayPoints(openSet, closedSet, startNode, endNode);

            WayPoints.AddRange(ReconstructPath(endNode));
        });
    }

    private Node SearchWayPoints(HashSet<Node> openSet, HashSet<Node> closedSet, Node startNode, Node endNode)
    {
        int iteration = 0;
        // int minIterationsToProgressCounter = 100;
        // int noProgressCounter = 0;
        // int maxNoProgressIterations = 2000;

        startNode.g = 0;
        startNode.parent = null;

        endNode.parent = null;

        startNode.h = Vector2.Distance(startNode.Position, endNode.Position);

        Node lowestHNode = startNode;

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = GetNodeWithLowestF(openSet);

            if (currentNode == endNode)
            {
                endNode.parent = currentNode.parent;
                return endNode;
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            //if(iteration > minIterationsToProgressCounter)
            //{
                if (currentNode.h < lowestHNode.h)
                {
                    lowestHNode = currentNode;
                    //noProgressCounter = 0;
                }
                //else
                //{
                    //noProgressCounter++;

                    //if (noProgressCounter >= maxNoProgressIterations)
                    //{
                        //break;
                    //}
                //}
            //}

            foreach (Node neighbor in currentNode.NeighborsNode)
            {
                if (closedSet.Contains(neighbor))
                    continue;
                
                if(neighbor.IsSizeClear[0])
                {
                    float tentativeG = currentNode.g + Vector2.Distance(currentNode.Position, neighbor.Position);

                    if (!openSet.Contains(neighbor))
                    {
                        neighbor.g = tentativeG;
                        neighbor.h = Vector2.Distance(neighbor.Position, endNode.Position);
                        neighbor.parent = currentNode;
                        openSet.Add(neighbor);
                    }
                    else if (tentativeG < neighbor.g)
                    {
                        neighbor.g = tentativeG;
                        neighbor.parent = currentNode;
                    }
                }
            }

            iteration++;
        }

        Debug.Log("ASTAR FORCIBLY END");

        return lowestHNode;
    }

    private Node GetNodeWithLowestF(HashSet<Node> openSet)
    {
        Node lowestFNode = null;
        float nodef = Mathf.Infinity;

        foreach (Node node in openSet)
        {
            if (node.f < nodef)
            {
                lowestFNode = node;
                nodef = node.f;
            }
        }

        return lowestFNode;
    }

    private List<Vector2> ReconstructPath(Node currentNode)
    {
        List<Node> pathNodes = new List<Node>();

        Queue<Node> openTargetSet = new Queue<Node>();
        HashSet<Node> closeTargetdSet = new HashSet<Node>();

        while (currentNode != null)
        {
            if(currentNode.parent != null)
            {
                pathNodes.Add(currentNode);

                if(currentNode.parent == currentNode)
                    break;

                currentNode = currentNode.parent;
            }
            else
                currentNode = null;
        }

        if(pathNodes.Count > 0)
        {
            Node node = pathNodes[pathNodes.Count - 1];

            for(int i = pathNodes.Count - 2; i >= 1; i--)
            {
                if(NavigationHelper.IsPathClear(node, pathNodes[i - 1].Position, unit, openTargetSet, closeTargetdSet))
                    pathNodes.RemoveAt(i);
                else
                    node = pathNodes[i];
            }

            pathNodes.Reverse();
        }

        List<Vector2> path = new List<Vector2>();

        for(int i = 0; i < pathNodes.Count; i++)
            path.Add(pathNodes[i].Position);

        return path;
    }

    public AStar_Navigation(Floor floor, Unit unit)
    {
        this.floor = floor;
        this.unit = unit;
    }
}
