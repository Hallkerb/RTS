using System;
using System.Collections;
using Mirror;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Floor : MonoBehaviour
{
    public static Floor Instance;

    public MapGenerator MapGenerator { get; private set; }
    private NavigationManager navigationManager; 
    private Grid grid;

    [SerializeField] private Transform spawnParent;

    public Transform SpawnParent => spawnParent;


    [SerializeField] private Transform playersSpawnPosPanel;
    public Transform[] PlayersSpawnPos { get; private set; }

    void Awake()
    {
        Instance = this;

        if(!NetworkServer.active) return;

        MapGenerator = GetComponent<MapGenerator>();

        PlayersSpawnPos = new Transform[playersSpawnPosPanel.childCount];

        initialize();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game" && NetworkServer.active)
        {
            if (Grid.Instance == null)
            {
                Debug.LogError("Grid.Instance == null!");
                return;
            }

            if (NavigationManager.Instance == null)
            {
                Debug.LogError("NavigationManager.Instance == null!");
                return;
            }

            grid = Grid.Instance;
            navigationManager = NavigationManager.Instance;

            if (grid.IsInitialized)
                GridInitialized(grid);
            else
                Grid.OnInitialized += GridInitialized;
        }
    }

    private void initialize()
    {
        for (int i = 0; i < playersSpawnPosPanel.childCount; i++)
            PlayersSpawnPos[i] = playersSpawnPosPanel.GetChild(i);
    }

    private void GridInitialized(Grid grid)
    {
        MapGenerator.GenerateMap();
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
    
    #if UNITY_EDITOR

    public void OnDrawGizmos()
    {
        if (grid == null || grid.Sections == null) return;

        foreach(var section in grid.Sections)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(section.Position, new Vector2(section.Size, section.Size));
        }

        var flowField = navigationManager.GetFlowField();

        for(int i = 0; i < grid.Nodes.Length; i++)
        {
            var node = grid.Nodes[i];

            if (!grid.Nodes[i].IsClear)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawCube(node.Position, new Vector2(node.Size * 0.9f, node.Size * 0.9f));
            }

            if (!flowField.FieldClearData.IsClear.IsSet(i))
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawCube(node.Position, new Vector2(node.Size * 0.6f, node.Size * 0.6f));
            }

            if (!grid.Nodes[i].IsSizeClear[0])
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
