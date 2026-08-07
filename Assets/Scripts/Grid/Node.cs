using System.Collections.Generic;
using System.Linq;
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

public struct FieldData
{
    public int Cost;
    public Vector2 Vector;

    public FieldData(int cost, Vector2 vector)
    {
        Cost = cost;
        Vector = vector;
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

    public Node parent;
    public float g; // Distance from start
    public float h; // Distance to target
    public float f => g + h; // General cost (g + h)

    //public List<FieldData> FieldDatas { get; private set; } = new List<FieldData>();

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

    public void BlockNode(bool isBlock, bool isSizeBlock)
    {
        if (isBlock)
            Interlocked.Increment(ref blockCount);
        else if (blockCount > 0)
            Interlocked.Decrement(ref blockCount);

        if (isSizeBlock)
            Interlocked.Increment(ref blockSizeCount[0]);
        else if (blockSizeCount[0] > 0)
            Interlocked.Decrement(ref blockSizeCount[0]);

        IsClear = blockCount == 0;
        IsSizeClear[0] = blockSizeCount[0] == 0;
    }

    public (bool pointClear, bool sizeClear) TryBlockNode(BuildingData buildingData)
    {
        Vector2 localPos = Quaternion.Inverse(buildingData.rotation) * (Position - buildingData.position);
        Vector2 halfSize = buildingData.size * 0.5f;

        IsClear = Mathf.Abs(localPos.x) > halfSize.x ||
            Mathf.Abs(localPos.y) > halfSize.y;

        IsSizeClear[0] = Mathf.Abs(localPos.x) > halfSize.x + 0.5f ||
            Mathf.Abs(localPos.y) > halfSize.y + 0.5f;

        return (IsClear, IsSizeClear[0]);
    }

    private void EvaluateStartColliders(Vector2 mapSize, float unitRadius)
    {
        float differenceX = mapSize.x / 2 - (Mathf.Abs(Position.x) + Size / 2);
        float differenceY = mapSize.y / 2 - (Mathf.Abs(Position.y) + Size / 2);

        IsSizeClear[0] = Mathf.Min(differenceX, differenceY) > unitRadius;
    }

    public void EvaluateColliders()
    {
        IsClear = EvaluateBoxClear(Size);
        IsSizeClear[0] = EvaluateCircleClear(0.5f);
    }

    private bool EvaluateCircleClear(float radius)
    {
        Collider2D hit = Physics2D.OverlapCircle(Position, radius, ~GetCombinedLayerMask());

        return hit == null;
    }

    private bool EvaluateBoxClear(float size)
    {
        Collider2D hit = Physics2D.OverlapBox(Position, new Vector2(size / 2, size / 2), 0, ~GetCombinedLayerMask());

        return hit == null;
    }

    private int GetCombinedLayerMask()
    {
        int layerToIgnore = 1 << 3; // 3 - Floor
        int layerToIgnore2 = 1 << 6; // 6 - Units
        int layerToIgnore3 = 1 << 5; // 5 - UI

        return layerToIgnore | layerToIgnore2 | layerToIgnore3;
    }

    public Node(Vector2 _position, float gridSize, int sizeCount, int id, Vector2 mapSize, float unitRadius)
    {
        Position = _position;
        ID = id;
        Size = gridSize;
        IsSizeClear = new bool[sizeCount];
        blockSizeCount = new int[sizeCount];

        parent = null;
        g = Mathf.Infinity;
        h = Mathf.Infinity;

        EvaluateStartColliders(mapSize, unitRadius);
    }
}
