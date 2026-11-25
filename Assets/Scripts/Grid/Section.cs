using UnityEngine;
using System;
using System.Threading;

public class Section
{
    public Vector2 Position { get; }

    public int ID { get; }

    public float Size { get; }

    public Node[] Nodes { get; private set; }

    private static int globalVersion;
    public static int CurrentVersion => Volatile.Read(ref globalVersion);
    public int BlockVersion { get; private set; }

    public event Action<Section, bool> OnCollidersChanged;

    public void MarkChanged(bool blocked)
    {
        if (blocked)
            BlockVersion = Interlocked.Increment(ref globalVersion);
    }

    public void RaiseCollidersChanged(bool blocked)
    {
        OnCollidersChanged?.Invoke(this, blocked);
    }

    public void SpawnGrid(float gridSize, Vector2 mapSize, float unitRadius)
    {
        if (gridSize <= 0) return;

        int gridCount = (int)Mathf.Round((Size * Size) / (gridSize * gridSize));

        Debug.Log($"grid count = {gridCount}, Size = {Size}, gridSize = {gridSize}");

        Nodes = new Node[gridCount];
        //Cells = new GameObject[gridCount];

        int sizeTable = (int)Mathf.Round(Size / gridSize);

        float halfSize = Size / 2;

        for (int i = 0; i < gridCount; i++)
        {
            int line = i / sizeTable;
            int column = i % sizeTable;

            Vector2 nodePosition = new Vector2(Position.x - halfSize + (column * gridSize) + gridSize / 2,
                                                Position.y - halfSize + (line * gridSize) + gridSize / 2);


            Nodes[i] = new Node(this, nodePosition, gridSize, 1, i + ID * gridCount, mapSize, unitRadius);
        }
        
        Debug.Log("Spawn grid end");
    }

    public bool IsInside(Vector2 point)
    {
        float halfSize = Size / 2;
        
        float padding = 0.01f;

        return point.x > Position.x - halfSize - padding && point.x < Position.x + halfSize + padding && 
                point.y > Position.y - halfSize - padding && point.y < Position.y + halfSize + padding;
    }

    public Section(Vector2 _position, int id, float _size, float gridSize, Vector2 mapSize, float unitRadius)
    {
        Position = _position;
        ID = id;
        Size = _size;

        SpawnGrid(gridSize, mapSize, unitRadius);
    }
}
