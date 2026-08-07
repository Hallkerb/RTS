using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Section
{
    public Vector2 Position { get; }

    public int ID { get; }

    public float Size { get; }

    public Node[] Grid { get; private set; }

    //public GameObject[] Walls;
    //public GameObject[] Cells;

    /*private void SpawnWalls(float maxMaterialForWall, int maxWalls)
    {
        int countWalls = Random.Range(maxWalls / 2, maxWalls);

        Walls = new GameObject[countWalls];

        for(int i = 0; i < countWalls; i++)
        {
            float materialForWall = maxMaterialForWall / countWalls;

            Walls[i] = new GameObject("Wall");

            Walls[i].AddComponent<Canvas>();
            Walls[i].AddComponent<Image>().color = Color.black;
            Walls[i].AddComponent<BoxCollider2D>();

            float sizeX = Random.Range(materialForWall / 2, materialForWall);
            materialForWall -= sizeX;
            float sizeY = Mathf.Max(1f, materialForWall);

            Walls[i].GetComponent<RectTransform>().sizeDelta = new Vector2(1, 1);
            Walls[i].transform.localScale = new Vector2(sizeX, sizeY);
            Walls[i].transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));;

            float halfSize = Size / 2;

            float posX = Random.Range(Position.x - halfSize, Position.x + halfSize);
            float posY = Random.Range(Position.y - halfSize, Position.y + halfSize);

            Walls[i].transform.position = new Vector2(posX, posY);
        }
    }*/

    public void SpawnGrid(float gridSize, Vector2 mapSize, float unitRadius)
    {
        if (gridSize <= 0) return;

        int gridCount = (int)Mathf.Round((Size * Size) / (gridSize * gridSize));

        Debug.Log($"grid count = {gridCount}, Size = {Size}, gridSize = {gridSize}");

        Grid = new Node[gridCount];
        //Cells = new GameObject[gridCount];

        int sizeTable = (int)Mathf.Round(Size / gridSize);

        float halfSize = Size / 2;

        for (int i = 0; i < gridCount; i++)
        {
            int line = i / sizeTable;
            int column = i % sizeTable;

            Vector2 nodePosition = new Vector2(Position.x - halfSize + (column * gridSize) + gridSize / 2,
                                                Position.y - halfSize + (line * gridSize) + gridSize / 2);


            Grid[i] = new Node(nodePosition, gridSize, 1, i + ID * gridCount, mapSize, unitRadius);
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
