using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Grid : MonoBehaviour
{
    public static Grid Instance;

    [SerializeField] private float[] unitsRadius;

    [SerializeField] private int sectionsCount;
    [SerializeField] private float gridSize = 0.2f;

    public Vector2 SizeX { get; private set; }
    public Vector2 SizeY { get; private set; }

    public int GridSizeX { get; private set; }
    public int GridSizeY { get; private set; }

    public Section[] Sections { get; private set; }
    public Node[] Nodes { get; private set; }

    public bool IsInitialized { get; private set; }

    public static event Action<Grid> OnInstantiated;
    public static event Action<Grid> OnInitialized;

    void Awake()
    {
        Instance = this;
        OnInstantiated?.Invoke(this);

        if(!NetworkServer.active) return;

        initialize();

        Vector2 position = transform.position;
        Vector2 localScale = transform.localScale;

        _ = generateSections(position, localScale);
    }

    void Start()
    {
        if (!NetworkServer.active) return;

        SpawnerManager.Instance.OnSpawn += ReactOnSpawn;
    }

    public float GetGridSize() => gridSize;

    public float GetUnitRadius(int i) => unitsRadius[i];

    private void initialize()
    {
        Collider2D collider = GetComponent<Collider2D>();

        SizeX = new Vector2(collider.bounds.min.x, collider.bounds.max.x);
        SizeY = new Vector2(collider.bounds.min.y, collider.bounds.max.y);
    }

    private async Task generateSections(Vector2 position, Vector2 localScale)
    {
        try
        {
            await Task.Run(() =>
            {
                Sections = new Section[sectionsCount];

                float sectionSize = Mathf.Sqrt(localScale.x * localScale.y / sectionsCount);

                Nodes = new Node[sectionsCount * (int)Mathf.Round((sectionSize * sectionSize) / (gridSize * gridSize))];

                int columns = (int)Mathf.Round(localScale.x / sectionSize);
                int lines = (int)Mathf.Round(localScale.y / sectionSize);

                Vector2 constPos = new Vector2(position.x - localScale.x / 2, position.y - localScale.y / 2);

                Parallel.For(0, sectionsCount, i =>
                {
                    int line = i / columns;
                    int column = i % columns;

                    Vector2 position = new Vector2(constPos.x + (column * sectionSize) + sectionSize / 2,
                                                constPos.y + (line * sectionSize) + sectionSize / 2);

                    Sections[i] = new Section(position, i, sectionSize, gridSize, localScale, unitsRadius[0]);
                });

                int offset = 0;

                for (int i = 0; i < sectionsCount; i++)
                {
                    Array.Copy(Sections[i].Nodes, 0, Nodes, offset, Sections[i].Nodes.Length);
                    offset += Sections[i].Nodes.Length;
                }

                Nodes = Nodes.AsParallel()
                        .OrderBy(n => n.Position.y)
                        .ThenBy(n => n.Position.x)
                        .ToArray();

                Parallel.For(0, Nodes.Length, i =>
                {
                    Nodes[i].SetID(i);
                });

                float totalWidth = SizeX.y - SizeX.x;
                float totalHeight = SizeY.y - SizeY.x;
                
                GridSizeX = Mathf.RoundToInt(totalWidth / gridSize);
                GridSizeY = Mathf.RoundToInt(totalHeight / gridSize);

                Parallel.For(0, Nodes.Length, i =>
                {
                    CalculateNeighborsForNode(i);
                });
            });
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        Debug.Log("Generate sections end");
        IsInitialized = true;
        OnInitialized?.Invoke(this);
    }

    private void CalculateNeighborsForNode(int index)
    {
        int x = index % GridSizeX;
        int y = index / GridSizeX;

        Node currentNode = Nodes[index];

        int[] dx = { 0, 0, 1, -1, 1, -1, 1, -1 };
        int[] dy = { 1, -1, 0, 0, 1, 1, -1, -1 };

        for (int i = 0; i < 8; i++)
        {
            int neighborX = x + dx[i];
            int neighborY = y + dy[i];

            if (neighborX >= 0 && neighborX < GridSizeX && neighborY >= 0 && neighborY < GridSizeY)
            {
                int neighborIndex = neighborY * GridSizeX + neighborX;
                currentNode.NeighborsNode[i] = Nodes[neighborIndex];
                currentNode.VectorsToNeighbors[i] = new Vector2(dx[i], dy[i]);
            }
            else
            {
                currentNode.NeighborsNode[i] = null;
                currentNode.VectorsToNeighbors[i] = Vector2.zero;
            }
        }
    }

    private void ReactOnSpawn(GameObject obj)
    {
        if (obj.TryGetComponent(out Building building) || (obj.TryGetComponent(out Resource resource) && resource.Type != ResourceType.Food))
        {
            Entity entity = obj.GetComponent<Entity>();
            entity.OnDeath += ReactOnDestroy;

            AsyncEvaluateCollidersNode(entity, true).ContinueWith(t =>
            {
                if (t.Exception != null)
                    Debug.LogError(t.Exception);

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    private async Task AsyncEvaluateCollidersNode(Entity entity, bool isblock)
    {
        var collider = entity.GetComponent<Collider2D>();
        Vector2 position = collider.transform.TransformPoint(collider.offset);
        Vector2 size = Vector2.zero;

        switch(entity.Shape.Type)
        {
            case EntityShape.ShapeType.Square:
                var box = collider.GetComponent<BoxCollider2D>();
                size = Vector2.Scale(box.size, entity.transform.lossyScale);
                break;
            case EntityShape.ShapeType.Circle:
                var circle = collider.GetComponent<CircleCollider2D>();
                size = new Vector2(circle.radius * 2 * entity.transform.lossyScale.x, circle.radius * 2 * entity.transform.lossyScale.y);
                break;
        }

        await Task.Run(() => 
        {
            float unitRadius = unitsRadius[0];
            float padding = unitRadius;

            float minX = position.x - size.x / 2 - padding;
            float maxX = position.x + size.x / 2 + padding;
            float minY = position.y - size.y / 2 - padding;
            float maxY = position.y + size.y / 2 + padding;

            Vector2[] corners =
            {
                new Vector2(minX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY),
                new Vector2(maxX, minY)
            };

            List<Section> sections = new List<Section>();

            var uniqueSections = corners
                .Select(corner => Sections.FirstOrDefault(s => s.IsInside(corner)))
                .Where(s => s != null)
                .Distinct()
                .ToList();

            sections.AddRange(uniqueSections);

            foreach (var section in sections)
            {
                float halfSize = section.Size / 2;
                float gridSize = GetGridSize();

                float localMinX = (position.x - size.x / 2 - padding) - section.Position.x + halfSize;
                float localMaxX = (position.x + size.x / 2 + padding) - section.Position.x + halfSize;

                float localMinY = (position.y - size.y / 2 - padding) - (section.Position.y - halfSize);
                float localMaxY = (position.y + size.y / 2 + padding) - (section.Position.y - halfSize);

                int nodesPerSide = Mathf.RoundToInt(section.Size / gridSize);

                int startCol = Mathf.Clamp(Mathf.FloorToInt(localMinX / gridSize), 0, nodesPerSide - 1);
                int endCol = Mathf.Clamp(Mathf.FloorToInt(localMaxX / gridSize), 0, nodesPerSide - 1);

                int startRow = Mathf.Clamp(Mathf.FloorToInt(localMinY / gridSize), 0, nodesPerSide - 1);
                int endRow = Mathf.Clamp(Mathf.FloorToInt(localMaxY / gridSize), 0, nodesPerSide - 1);

                /*Debug.Log(@$"Node count = {(endRow - startRow + 1) * (endCol - startCol + 1)}
                Build position = {position}, Section position = {section.Position}
                Build size = {size}, unitRadius = {unitRadius}, padding = {padding}, Section size = {section.Size}, Section halfSize = {halfSize}

                gridSize = {gridSize}
                nodesPerSide = {nodesPerSide}

                localMinX = {localMinX}, localMaxX = {localMaxX}
                localMinY = {localMinY}, localMaxY = {localMaxY}
                startCol = {startCol}, endCol = {endCol}
                startRow = {startRow}, endRow = {endRow}");*/

                switch(entity.Shape.Type)
                {
                    case EntityShape.ShapeType.Square:
                        ExecuteBoxBlocking((section, position, size, startRow, endRow, startCol, endCol, nodesPerSide, isblock));
                        break;
                    case EntityShape.ShapeType.Circle:
                        ExecuteCircleBlocking((section, position, size.x / 2, unitsRadius[0], startRow, endRow, startCol, endCol, nodesPerSide, isblock));
                        break;
                }
            }
        });
    }

    private void ExecuteBoxBlocking((Section section, Vector2 position, Vector2 size, int startRow, int endRow, int startCol, int endCol, int nodesPerSide, bool isblock) data)
    {
        Parallel.For(data.startRow, data.endRow + 1, row =>
        {
            for (int col = data.startCol; col <= data.endCol; col++)
            {
                int index = row * data.nodesPerSide + col;

                Node node = data.section.Nodes[index];

                bool covers = GeometryUtils.IsInside(node.Position, node.Size, data.position, data.size);
                node.BlockNode(covers, true, data.isblock);

                NavigationManager.Instance.GetFlowField().SetFieldClearData(node.ID, node.IsClear, node.IsSizeClear);
            }
        });
    }

    private void ExecuteCircleBlocking((Section section, Vector2 center, float radius, float padding, int startRow, int endRow, int startCol, int endCol, int nodesPerSide, bool isblock) data)
    {
        float objR2 = data.radius * data.radius;
        float padR = data.radius + data.padding;
        float padR2 = padR * padR;

        Parallel.For(data.startRow, data.endRow + 1, row =>
        {
            for (int col = data.startCol; col <= data.endCol; col++)
            {
                Node node = data.section.Nodes[row * data.nodesPerSide + col];

                float distSqr = DistSqrToCell(node.Position, node.Size * 0.5f, data.center);

                node.BlockNode(distSqr <= objR2, distSqr <= padR2, data.isblock);

                NavigationManager.Instance.GetFlowField().SetFieldClearData(node.ID, node.IsClear, node.IsSizeClear);
            }
        });
    }

    private static float DistSqrToCell(Vector2 cellCenter, float halfCell, Vector2 point)
    {
        float dx = Mathf.Max(Mathf.Abs(point.x - cellCenter.x) - halfCell, 0f);
        float dy = Mathf.Max(Mathf.Abs(point.y - cellCenter.y) - halfCell, 0f);
        return dx * dx + dy * dy;
    }

    private void ReactOnDestroy(Entity entity)
    {
        if (entity.TryGetComponent(out Building building) || (entity.TryGetComponent(out Resource resource) && resource.Type != ResourceType.Food))
        {
            entity.OnDeath -= ReactOnDestroy;

            AsyncEvaluateCollidersNode(entity, false).ContinueWith(t =>
            {
                if (t.Exception != null)
                    Debug.LogError(t.Exception);

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    void OnDestroy()
    {
        OnInstantiated = null;
        OnInitialized = null;
    }
}
