using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AStar_Navigation
{
    private Grid grid;
    private Unit unit;

    public List<Vector2> WayPoints { get; private set; } = new List<Vector2>();

    public Vector2 TargetPoint { get; private set; }

    public static float stepSize = 0.05f;

    public async Task SearchWayToTarget(Vector2 startPos, Vector2 targetPoint)
    {
        TargetPoint = targetPoint;

        await Task.Run(() =>
        {
            Node startNode = NavigationHelper.GetNode(grid, grid.GridSizeX, grid.GridSizeY, grid.GetGridSize(), startPos);
            Node endNode = NavigationHelper.SetTargetNode(grid, targetPoint);

            var data = new Dictionary<int, AStarNodeData>();
            endNode = SearchWayPoints(startNode, endNode, data);

            WayPoints = ReconstructPath(endNode, data);
        });
    }

    private Node SearchWayPoints(Node startNode, Node endNode, Dictionary<int, AStarNodeData> data)
    {
        var closed = new HashSet<int>();
        var heap = new FastPriorityQueue<Node>();

        data[startNode.ID] = new AStarNodeData(-1, 0);
        heap.Enqueue(Heuristic(startNode, endNode), startNode);

        Node lowestHNode = startNode;
        float lowestH = Heuristic(startNode, endNode);

        while (heap.Count > 0)
        {
            Node current = heap.Dequeue().element;

            if (!closed.Add(current.ID))
                continue;

            if (current == endNode)
                return endNode;

            float h = Heuristic(current, endNode);
            if (h < lowestH)
            {
                lowestH = h;
                lowestHNode = current;
            }

            float currentG = data[current.ID].g;

            foreach (Node neighbor in current.NeighborsNode)
            {
                if (neighbor == null || closed.Contains(neighbor.ID) || !neighbor.IsSizeClear[0])
                    continue;
                
                float tentativeG = currentG  + Vector2.Distance(current.Position, neighbor.Position);

                if (!data.TryGetValue(neighbor.ID, out var nd) || tentativeG < nd.g)
                {
                    data[neighbor.ID] = new AStarNodeData(current.ID, tentativeG);
                    heap.Enqueue(tentativeG + Heuristic(neighbor, endNode), neighbor);
                }
            }
        }

        Debug.Log("ASTAR FORCIBLY END");

        return lowestHNode;
    }

    private static float Heuristic(Node a, Node b) => Vector2.Distance(a.Position, b.Position);

    private List<Vector2> ReconstructPath(Node lastNode, Dictionary<int, AStarNodeData> data)
    {
        List<Node> pathNodes = new List<Node>();

        int id = lastNode.ID;
        while (data.TryGetValue(id, out var d) && d.parent >= 0)
        {
            pathNodes.Add(grid.Nodes[id]);
            id = d.parent;
        }

        List<Vector2> path = new List<Vector2>();

        if (pathNodes.Count == 0)
            return path;

        pathNodes.Reverse();

        Queue<Node> openSet = new Queue<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Node anchor = pathNodes[0];
        path.Add(anchor.Position);

        for (int i = 1; i < pathNodes.Count - 1; i++)
        {
            Node next = pathNodes[i + 1];

            if(!NavigationHelper.IsPathClear(anchor, next.Position, unit, openSet, closedSet))
            {
                anchor = pathNodes[i];
                path.Add(anchor.Position);
            }
        }

        if (pathNodes.Count > 1)
            path.Add(pathNodes[pathNodes.Count - 1].Position);

        return path;
    }

    public AStar_Navigation(Grid grid, Unit unit)
    {
        this.grid = grid;
        this.unit = unit;
    }
}
