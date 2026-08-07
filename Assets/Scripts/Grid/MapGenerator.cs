using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Floor floor;

    [Header("Generation Settings")]
    [SerializeField] private float _noiseScale = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float _densityThreshold = 0.6f;
    [SerializeField] private int _mapSeed = -1;
    private float _offsetX;
    private float _offsetY;

    [Space(10)]
    [SerializeField] private Entity ironPrefab;
    [SerializeField] private int _ironCount = 10;
    [SerializeField] private float _minDistToOtherIron = 5f;
    [SerializeField] private float _treeFreeZoneAroundIron = 1f;

    [Space(10)]
    [SerializeField] private Entity treePrefab;
    [SerializeField] private float _treeSpacing = 0.5f; 
    [SerializeField] private float _minDistanceBetweenTrees = 0.5f;

    [Header("Player Settings")]
    [SerializeField] private float _safetyRadius = 3f;
    [SerializeField] private float _falloffRadius = 6f;

    private List<Vector2> _spawnedIronPositions = new List<Vector2>();
    private List<Entity> _spawnedIron = new List<Entity>();

    private List<Vector2> _spawnedTreePositions = new List<Vector2>();
    private List<Entity> _spawnedTrees = new List<Entity>();

    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        ClearForest();
        ClearIron(); 

        GenerateIron(); 
        GenerateForest();
    }

    [ContextMenu("Generate Iron")]
    public void GenerateIron()
    {
        int attempts = 0;

        while (_spawnedIronPositions.Count < _ironCount && attempts < 500)
        {
            attempts++;
            
            float x = Random.Range(floor.SizeX.x + 1f, floor.SizeX.y - 1f);
            float y = Random.Range(floor.SizeY.x + 1f, floor.SizeY.y - 1f);
            Vector2 pos = new Vector2(x, y);

            if (CalculateSafetyFactor2D(pos) < 1) continue;

            if (CanPlaceIron(pos))
                _spawnedIronPositions.Add(pos);
        }

        Debug.Log("Iron count: " + _spawnedIronPositions.Count);

        for (int i = 0; i < _spawnedIronPositions.Count; i++)
            SpawnIron(_spawnedIronPositions[i]);
    }

    [ContextMenu("Generate Forest")]
    public void GenerateForest()
    {
        var treeCollider = treePrefab.GetComponent<CircleCollider2D>();

        if (_mapSeed >= 0)
            Random.InitState(_mapSeed);

        _offsetX = Random.Range(0f, 100000f);
        _offsetY = Random.Range(0f, 100000f);

        float paddingX = treeCollider.radius * treePrefab.transform.localScale.x;
        float paddingY = treeCollider.radius * treePrefab.transform.localScale.y;

        float maxJitter = _treeSpacing * 0.3f;
    
        float safePaddingX = paddingX + maxJitter;
        float safePaddingY = paddingY + maxJitter;

        float minX = floor.SizeX.x + safePaddingX;
        float maxX = floor.SizeX.y - safePaddingX;
        float minY = floor.SizeY.x + safePaddingY;
        float maxY = floor.SizeY.y - safePaddingY;

        int stepsX = Mathf.FloorToInt((maxX - minX) / _treeSpacing);
        int stepsY = Mathf.FloorToInt((maxY - minY) / _treeSpacing);

        for(int x = 0; x < stepsX; x++)
        {
            for (int y = 0; y < stepsY; y++)
            {
                float xCoord = (x * _noiseScale) + _offsetX;
                float yCoord = (y * _noiseScale) + _offsetY;
                float perlinValue = Mathf.PerlinNoise(xCoord, yCoord);

                float posX = minX + (x * _treeSpacing) + Random.Range(-_treeSpacing * 0.3f, _treeSpacing * 0.3f);
                float posY = minY + (y * _treeSpacing) + Random.Range(-_treeSpacing * 0.3f, _treeSpacing * 0.3f);
                Vector2 spawnPos = new Vector2(posX, posY);

                float safetyFactor = CalculateSafetyFactor2D(spawnPos);

                if (perlinValue * safetyFactor > _densityThreshold)
                {
                    if (CanPlaceTree(spawnPos))
                        _spawnedTreePositions.Add(spawnPos);
                }
            }
        }

        for (int i = 0; i < _spawnedTreePositions.Count; i++)
            SpawnTree(_spawnedTreePositions[i]);
    }

    private bool CanPlaceIron(Vector2 position)
    {
        foreach (var ironPos in _spawnedIronPositions)
        {
            if (Vector2.Distance(position, ironPos) < _minDistToOtherIron) 
                return false;
        }

        return true;
    }

    private bool CanPlaceTree(Vector2 position)
    {
        foreach (var otherPos in _spawnedTreePositions)
        {
            if (Vector2.Distance(position, otherPos) < _minDistanceBetweenTrees)
                return false;
        }

        foreach (var ironPos in _spawnedIronPositions)
        {
            if (Vector2.Distance(position, ironPos) < _treeFreeZoneAroundIron) 
                return false;
        }

        return true;
    }

    private float CalculateSafetyFactor2D(Vector2 position)
    {
        float minFactor = 1f;

        foreach (Transform spawnPoint in floor.PlayersSpawnPos)
        {
            if (spawnPoint == null) continue;

            float distanceSqr = (position - (Vector2)spawnPoint.position).sqrMagnitude;
            
            float coreRadiusSqr = _safetyRadius * _safetyRadius;
            float totalRadiusSqr = (_safetyRadius + _falloffRadius) * (_safetyRadius + _falloffRadius);

            if (distanceSqr < coreRadiusSqr) return 0f;

            if (distanceSqr < totalRadiusSqr)
            {
                float dist = Mathf.Sqrt(distanceSqr);
                float factor = (dist - _safetyRadius) / _falloffRadius;
                if (factor < minFactor) minFactor = factor;
            }
        }
        return minFactor;
    }

    private void SpawnTree(Vector2 position)
    {
        var tree = SpawnerManager.Singlton.Spawn(treePrefab, position, Quaternion.identity);
        _spawnedTrees.Add(tree);
    }

    private void SpawnIron(Vector2 position)
    {
        var iron = SpawnerManager.Singlton.Spawn(ironPrefab, position, Quaternion.identity);
        _spawnedIron.Add(iron);
    }

    [ContextMenu("Clear Forest")]
    public void ClearForest()
    {
        _spawnedTreePositions.Clear();

        foreach(var tree in _spawnedTrees)
            tree.Destruction();
    }

    [ContextMenu("Clear Iron")]
    public void ClearIron()
    {
        _spawnedIronPositions.Clear();

        foreach(var iron in _spawnedIron)
            iron.Destruction();
    }
}
