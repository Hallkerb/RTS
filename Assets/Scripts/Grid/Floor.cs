using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class Floor : MonoBehaviour
{
    public MapGenerator MapGenerator { get; private set; }
    private NavigationManager navigationManager; 
    private Collider2D сollider;
    private Transform spawnParent;

    [SerializeField] private float[] unitsRadius;

    [SerializeField] private int sectionsCount;
    [SerializeField] private float gridSize = 0.2f;

    public Vector2 SizeX { get; private set; }
    public Vector2 SizeY { get; private set; }

    public Section[] Sections { get; private set; }
    public Node[] Grid { get; private set; }

    [SerializeField] private Transform playersSpawnPosPanel;
    public Transform[] PlayersSpawnPos { get; private set; }

    public bool GridActive { get; private set; }

    void Awake()
    {
        if(!NetworkServer.active) return;

        MapGenerator = GetComponent<MapGenerator>();
        navigationManager = FindFirstObjectByType<NavigationManager>();
        сollider = GetComponent<Collider2D>();
        spawnParent = transform.Find("EntityManager");

        PlayersSpawnPos = new Transform[playersSpawnPosPanel.childCount];

        initialize();

        Vector2 position = transform.position;
        Vector2 localScale = transform.localScale;

        _ = generateSections(position, localScale);
    }

    void Start()
    {
        if (!NetworkServer.active) return;
        
        SpawnerManager.Singlton.OnSpawn += ReactOnSpawn;

        StartCoroutine(GenerateMap());
    }

    public float GetGridSize() => gridSize;

    private void initialize()
    {
        for (int i = 0; i < playersSpawnPosPanel.childCount; i++)
            PlayersSpawnPos[i] = playersSpawnPosPanel.GetChild(i);

        SizeX = new Vector2(сollider.bounds.min.x, сollider.bounds.max.x);
        SizeY = new Vector2(сollider.bounds.min.y, сollider.bounds.max.y);
    }

    private IEnumerator GenerateMap()
    {
        while(GridActive == false || navigationManager?.GetFlowField()?.Initialized == false)
        {
            Debug.Log("FlowField not initialized");

            yield return null;
        }

        MapGenerator.GenerateMap();
    }

    private async Task generateSections(Vector2 position, Vector2 localScale)
    {
        try
        {
            await Task.Run(() =>
            {
                Sections = new Section[sectionsCount];

                float sectionSize = Mathf.Sqrt(localScale.x * localScale.y / sectionsCount);

                Grid = new Node[sectionsCount * (int)Mathf.Round((sectionSize * sectionSize) / (gridSize * gridSize))];

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
                    Array.Copy(Sections[i].Grid, 0, Grid, offset, Sections[i].Grid.Length);
                    offset += Sections[i].Grid.Length;
                }

                Grid = Grid.AsParallel()
                        .OrderBy(n => n.Position.y)
                        .ThenBy(n => n.Position.x)
                        .ToArray();

                Parallel.For(0, Grid.Length, i =>
                {
                    Grid[i].SetID(i);
                });

                float totalWidth = SizeX.y - SizeX.x;
                float totalHeight = SizeY.y - SizeY.x;
                
                int nodesX = Mathf.RoundToInt(totalWidth / gridSize);
                int nodesY = Mathf.RoundToInt(totalHeight / gridSize);

                Parallel.For(0, Grid.Length, i =>
                {
                    CalculateNeighborsForNode(i, nodesX, nodesY);
                });
            });
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        Debug.Log("Generate sections end");
        GridActive = true;
    }

    private void CalculateNeighborsForNode(int index, int nodesX, int nodesY)
    {
        int x = index % nodesX;
        int y = index / nodesX;

        Node currentNode = Grid[index];

        int[] dx = { 0, 0, 1, -1, 1, -1, 1, -1 };
        int[] dy = { 1, -1, 0, 0, 1, 1, -1, -1 };

        for (int i = 0; i < 8; i++)
        {
            int neighborX = x + dx[i];
            int neighborY = y + dy[i];

            if (neighborX >= 0 && neighborX < nodesX && neighborY >= 0 && neighborY < nodesY)
            {
                int neighborIndex = neighborY * nodesX + neighborX;
                currentNode.NeighborsNode[i] = Grid[neighborIndex];
                currentNode.VectorsToNeighbors[i] = new Vector2(dx[i], dy[i]);
            }
            else
            {
                currentNode.NeighborsNode[i] = null;
                currentNode.VectorsToNeighbors[i] = Vector2.zero;
            }
        }
    }

    public (Vector3 pos, bool inside) IsCameraInside(Vector3 point)
    {
        float halfScaleX = transform.localScale.x / 2;
        float halfScaleY = transform.localScale.y / 2;

        float minX = transform.position.x - halfScaleX;
        float maxX = transform.position.x + halfScaleX;

        float minY = transform.position.y - halfScaleY;
        float maxY = transform.position.y + halfScaleY;

        bool inside = Mathf.Clamp(point.x, minX, maxX) == point.x && Mathf.Clamp(point.y, minY, maxY) == point.y;

        point.x = Mathf.Clamp(point.x, minX, maxX);
        point.y = Mathf.Clamp(point.y, minY, maxY);

        return (point, inside);
    }

    private void ReactOnSpawn(Entity entity)
    {
        entity.transform.SetParent(spawnParent);

        if (entity.TryGetComponent(out Building building) || (entity.TryGetComponent(out Resource resource) && resource.Type != ResourceType.Food))
        {
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

        Debug.Log("AsyncEvaluateCollidersNode start");

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

            Debug.Log($"sections count = {sections.Count}");

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
                        ExecuteBoxBlocking(section, position, size, startRow, endRow, startCol, endCol, nodesPerSide, isblock);
                        break;
                    case EntityShape.ShapeType.Circle:
                        ExecuteCircleBlocking(section, position, size.x / 2, unitsRadius[0], startRow, endRow, startCol, endCol, nodesPerSide, isblock);
                        break;
                }
            }
        });

        Debug.Log("AsyncEvaluateCollidersNode end");
    }

    private void ExecuteBoxBlocking(Section section, Vector2 position, Vector2 size, int startRow, int endRow, int startCol, int endCol, int nodesPerSide, bool isblock)
    {
        var fieldClearData = navigationManager.GetFlowField().FieldClearData;

        Parallel.For(startRow, endRow + 1, row =>
        {
            for (int col = startCol; col <= endCol; col++)
            {
                int index = row * nodesPerSide + col;

                Node node = section.Grid[index];

                if (isblock)
                    node.BlockNode(node.IsClear == false || GeometryUtils.IsInside(node.Position, node.Size, position, size), true);
                else
                    node.BlockNode(false, false);

                int globalIndex = node.ID;

                fieldClearData.IsClear.Set(globalIndex, node.IsClear);
                fieldClearData.IsSizeClear.Set(globalIndex, node.IsSizeClear[0]);
            }
        });
    }

    private void ExecuteCircleBlocking(Section section, Vector2 center, float radius, float padding, int startRow, int endRow, int startCol, int endCol, int nodesPerSide, bool isblock)
    {
        var fieldClearData = navigationManager.GetFlowField().FieldClearData;

        float objectRadiusSqr = radius * radius;
        float paddedRadiusSqr = (radius + padding) * (radius + padding);

        Parallel.For(startRow, endRow + 1, row =>
        {
            for (int col = startCol; col <= endCol; col++)
            {
                int index = row * nodesPerSide + col;
                Node node = section.Grid[index];

                if (isblock)
                {
                    float distSqr = (node.Position - center).sqrMagnitude;

                    bool isInsideObject = GeometryUtils.IsInsideCircle(distSqr, radius);
                    bool isInsidePadding = GeometryUtils.IsInsideCircle(distSqr, radius + padding);

                    node.BlockNode(node.IsClear == false || isInsideObject, node.IsSizeClear[0] == false || isInsidePadding);
                }
                else
                    node.BlockNode(false, false);

                fieldClearData.IsClear.Set(node.ID, node.IsClear);
                fieldClearData.IsSizeClear.Set(node.ID, node.IsSizeClear[0]);
            }
        });
    }

    private void ReactOnDestroy(Entity entity)
    {
        if (entity.TryGetComponent(out Building building))
        {
            building.OnDeath -= ReactOnDestroy;

            AsyncEvaluateCollidersNode(building, false).ContinueWith(t =>
            {
                if (t.Exception != null)
                    Debug.LogError(t.Exception);

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
    
    #if UNITY_EDITOR

    public void OnDrawGizmos()
    {
        if (Sections == null) return;

        foreach(var section in Sections)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(section.Position, new Vector2(section.Size, section.Size));
        }

        var flowField = navigationManager.GetFlowField();

        for(int i = 0; i < Grid.Length; i++)
        {
            var node = Grid[i];

            if (!Grid[i].IsClear)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(node.Position, new Vector2(node.Size * 0.9f, node.Size * 0.9f));
            }

            if (!flowField.FieldClearData.IsClear.IsSet(i))
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawCube(node.Position, new Vector2(node.Size * 0.6f, node.Size * 0.6f));
            }

            if (!Grid[i].IsSizeClear[0])
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(node.Position, new Vector2(node.Size * 0.9f, node.Size * 0.9f));
            }

            if (!flowField.FieldClearData.IsSizeClear.IsSet(i))
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(node.Position, new Vector2(node.Size * 0.6f, node.Size * 0.6f));
            }

            if (flowField.FieldDatas.Count > 0 && math.any(flowField.FieldDatas[0].Vector[i] != float2.zero))
                DrawArrow(Color.green, node.Size / 2, node.Position, new Vector3(flowField.FieldDatas[0].Vector[i].x, flowField.FieldDatas[0].Vector[i].y, 0).normalized);
        }
    }

    private void DrawArrow(Color color, float halfSize, Vector2 position, Vector3 vector)
    {
        Gizmos.color = color;

        float arrowLength = halfSize + halfSize * 0.7f;
        float wingLength = arrowLength / 2;

        Vector3 dir = vector;
        Vector3 start = (Vector3)position - (dir * (arrowLength / 2));

        Vector3 tip = start + dir * arrowLength;

        Vector3 perp = new Vector3(-dir.y, dir.x, 0);

        Vector3 leftWing  = tip - dir * wingLength + perp * (wingLength / 2);
        Vector3 rightWing = tip - dir * wingLength - perp * (wingLength / 2);

        Vector3[] drawList =
        {
            start, tip,
            tip, leftWing,
            tip, rightWing
        };

        Gizmos.DrawLineList(drawList);
    }

    #endif
}
