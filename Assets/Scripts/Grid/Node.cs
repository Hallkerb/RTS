using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public struct BuildingData
{
    public Vector2 position;
    public Vector2 size;
    public Quaternion rotation;

    public BuildingData(Vector2 position, Vector2 size, Quaternion rotation)
    {
        this.position = position;
        this.size = size;
        this.rotation = rotation;
    }

    public BuildingData(Vector2 position, Vector2 size)
    {
        this.position = position;
        this.size = size;
        this.rotation = Quaternion.identity;
    }
}

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

public class Node
{
    //private GameObject SectionObject;

    public Node[] NeighborsNode { get; private set; } = new Node[8];
    public Vector2[] VectorsToNeighbors { get; private set; } = new Vector2[8];

    public Vector2 Position { get; }

    public int ID { get; private set; }

    public float Size { get; }

    public bool IsClear { get; private set; } = true;
    private int blockCount;

    public bool[] IsSizeClear { get; private set; }
    private int[] blockSizeCount;

    private bool borderBlocked;

    public bool IsInside(Vector2 point)
    {
        float halfSize = Size / 2;

        float padding = 0.01f;

        return point.x > Position.x - halfSize - padding && point.x < Position.x + halfSize + padding && 
                point.y > Position.y - halfSize - padding && point.y < Position.y + halfSize + padding;
    }

    public void SetID(int newID)
    {
        ID = newID;
    }

    /*public void SetNeighbors(Floor floor)
    {
        Vector2[] offsets = {
            new Vector2(0, Size),
            new Vector2(0,-Size),
            new Vector2(Size, 0),
            new Vector2(-Size, 0),

            new Vector2(Size, Size),
            new Vector2(-Size, Size),
            new Vector2(Size, -Size),
            new Vector2(-Size, -Size)
        };

        VectorsToNeighbors = new Vector2[]
        {
            offsets[0].normalized,
            offsets[1].normalized,
            offsets[2].normalized,
            offsets[3].normalized,
            offsets[4].normalized,
            offsets[5].normalized,
            offsets[6].normalized,
            offsets[7].normalized,
        };

        for(int i = 0; i < offsets.Length; i++)
        {
            Vector2 neighborPos = Position + offsets[i];

            Section section = floor.Sections.FirstOrDefault(s => s.IsInside(neighborPos));

            if(section == null) continue;

            Node neighborNode = section.Grid.FirstOrDefault(n => n.IsInside(neighborPos));

            if(neighborNode == null) continue;

            NeighborsNode[i] = neighborNode;
        }
    }*/

    public void BlockNode(bool coversPoint, bool coversSize, bool add)
    {
        int delta = add ? 1 : -1;

        if (coversPoint)
            Interlocked.Add(ref blockCount, delta);

        if (coversSize)
            Interlocked.Add(ref blockSizeCount[0], delta);

        IsClear = Volatile.Read(ref blockCount) <= 0;
        IsSizeClear[0] = !borderBlocked && Volatile.Read(ref blockSizeCount[0]) <= 0;
    }

    private void EvaluateStartColliders(Vector2 mapSize, float unitRadius)
    {
        float differenceX = mapSize.x / 2 - (Mathf.Abs(Position.x) + Size / 2);
        float differenceY = mapSize.y / 2 - (Mathf.Abs(Position.y) + Size / 2);

        borderBlocked = Mathf.Min(differenceX, differenceY) <= unitRadius;
        IsSizeClear[0] = !borderBlocked;
    }

    public Node(Vector2 _position, float gridSize, int sizeCount, int id, Vector2 mapSize, float unitRadius)
    {
        Position = _position;
        ID = id;
        Size = gridSize;
        IsSizeClear = new bool[sizeCount];
        blockSizeCount = new int[sizeCount];

        EvaluateStartColliders(mapSize, unitRadius);
    }
}
