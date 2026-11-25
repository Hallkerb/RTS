using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public struct AStarNodeData
{
    public int parent;
    public float g; // Distance from start

    public AStarNodeData(int parent, float g)
    {
        this.parent = parent;
        this.g = g;
    }
}

public class AStar_Navigation
{
    private Grid grid;
    private Unit unit;

    private List<Vector2> WayPoints { get; set; } = new List<Vector2>();
    private HashSet<Section> Sections { get; set; } = new HashSet<Section>();

    public Vector2 TargetPoint { get; private set; }

    public static float stepSize = 0.05f;

    private int searchVersion;

    public bool HasPath => WayPoints.Count > 0;

    public event Action OnNavigationChanged;

    public bool TryGetWayPoint(out Vector2 WayPoint)
    {
        if (WayPoints.Count > 0)
        {
            WayPoint = WayPoints[0];
            return true;
        }

        WayPoint = unit.transform.position;

        return false;
    }

    public bool TryRemoveWayPoint(int index = 0)
    {
        if (WayPoints.Count > index && index >= 0)
        {
            WayPoints.RemoveAt(index);
            OnNavigationChanged?.Invoke();
            return true;
        }

        return false;
    }

    public Vector2[] GetWayPoints()
    {
        return WayPoints.ToArray();
    }

    public async Task SearchWayToTarget(Vector2 startPos, Vector2 targetPoint)
    {
        TargetPoint = targetPoint;

        int myVersion = ++searchVersion;
        int versionAtStart = Section.CurrentVersion;

        var result = await Task.Run(() =>
        {
            Node startNode = NavigationHelper.GetNode(grid, grid.GridSizeX, grid.GridSizeY, grid.GetGridSize(), startPos);
            Node endNode = NavigationHelper.SetTargetNode(grid, targetPoint);

            var data = new Dictionary<int, AStarNodeData>();
            endNode = SearchWayPoints(startNode, endNode, data);

            return ReconstructPath(endNode, data);
        });

        if (myVersion != searchVersion) return;

        WayPoints = result.points;
        SetSections(result.sections);

        OnNavigationChanged?.Invoke();

        foreach (var s in result.sections)
        {
            if (s.BlockVersion > versionAtStart)
            {
                _ = SearchWayToTarget(unit.transform.position, TargetPoint);
                return;
            }
        }
    }

    private void SetSections(HashSet<Section> newSections)
    {
        UnsubscribeAll();

        Sections = newSections;

        foreach (var s in newSections)
            s.OnCollidersChanged += OnSectionCollidersChanged;
    }

    private void UnsubscribeAll()
    {
        foreach (var s in Sections)
            s.OnCollidersChanged -= OnSectionCollidersChanged;
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

    private (List<Vector2> points, HashSet<Section> sections) ReconstructPath(Node lastNode, Dictionary<int, AStarNodeData> data)
    {
        List<Node> pathNodes = new List<Node>();
        HashSet<Section> sections = new HashSet<Section>();

        int id = lastNode.ID;
        while (data.TryGetValue(id, out var d) && d.parent >= 0)
        {
            Node node = grid.Nodes[id];

            pathNodes.Add(node);
            id = d.parent;

            Section section = node.Section;

            if (!sections.Contains(section))
                sections.Add(section);
        }

        List<Vector2> path = new List<Vector2>();

        if (pathNodes.Count == 0)
            return (path, sections);

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

        return (path, sections);
    }

    private void OnSectionCollidersChanged(Section section, bool blocked)
    {
        if (!blocked) return;

        Debug.Log($"Section {section.ID} colliders changed, recalculating navigation");

        foreach (var sec in Sections)
        {
            sec.OnCollidersChanged -= OnSectionCollidersChanged;
        }

        _ = SearchWayToTarget(unit.transform.position, TargetPoint);
    }

    public void Stop()
    {
        searchVersion++;

        WayPoints = new List<Vector2>();

        UnsubscribeAll();

        Sections = new HashSet<Section>();

        OnNavigationChanged?.Invoke();
    }

    public AStar_Navigation(Grid grid, Unit unit)
    {
        this.grid = grid;
        this.unit = unit;
    }
}
