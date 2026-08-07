using UnityEngine;
using Mirror;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Mathematics;

public enum NavigationMode
{
    AStar,
    FlowField
}

public class NavigationManager : MonoBehaviour
{
    private FlowField_Navigation flowField;
    private Floor floor;

    public FlowField_Navigation GetFlowField() => flowField;

    private void Awake()
    {
        if(!NetworkServer.active) return;

        floor = FindFirstObjectByType<Floor>();

        StartCoroutine(InitializeFlowField());
    }

    private IEnumerator InitializeFlowField()
    {
        if (floor == null) 
        {
            Debug.LogError($"Floor == null");
            yield break;
        }

        while (floor.GridActive == false) yield return null;

        flowField = new FlowField_Navigation(floor);

        Debug.Log("FlowField Initialized");
    }

    [Server]
    public async Task SetNavigationAsync(IReadOnlyList<Unit> unitChoose, Entity target, Vector2 point, int playerID, bool resetTasks = true)
    {
        try
        {
            int? fieldIndex = null;

            if (unitChoose.Count == 1 && playerID == unitChoose[0].PlayerID && unitChoose[0] is IMovable movable)
                movable.SetMoveTask(NavigationMode.AStar, target, point, fieldIndex, resetTasks);
            else if (unitChoose.Count > 1)
            {
                fieldIndex = await flowField.CreateFieldToTarget(unitChoose[0], point);

                foreach (Unit unit in unitChoose)
                {
                    if (playerID != unit.PlayerID) continue;

                    if (unit is IMovable movableUnit)
                        movableUnit.SetMoveTask(NavigationMode.FlowField, target, point, fieldIndex, resetTasks);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    [Server]
    public void AddUnitInField(Unit unit, int index) => flowField.AddUnitInField(unit, index);

    [Server]
    public void RemoveUnitInField(Unit unit, int index) => flowField.RemoveUnitInField(unit, index);

    [Server]
    public Vector2 GetVectorToNextNode(Vector2 position, int fieldIndex)
    {
        float minX = floor.SizeX.x;
        float maxX = floor.SizeX.y;
        float minY = floor.SizeY.x;
        float maxY = floor.SizeY.y;

        if (position.x < minX || position.x >= maxX || position.y < minY || position.y >= maxY)
            return Vector2.zero;

        float cellSize = floor.GetGridSize();
        int gridWidth = Mathf.RoundToInt((maxX - minX) / cellSize);

        int x = Mathf.FloorToInt((position.x - minX) / cellSize);
        int y = Mathf.FloorToInt((position.y - minY) / cellSize);

        int nodeIndex = y * gridWidth + x;

        if (fieldIndex >= 0 && flowField.FieldDatas.Count > fieldIndex)
        {
            float2 nodeVector = flowField.FieldDatas[fieldIndex].Vector[nodeIndex];
            Vector2 vector = new Vector2(nodeVector.x, nodeVector.y);

            return vector;
        }

        /*foreach(Section section in floor.Sections)
        {
            if(section.IsInside(position))
            {
                foreach(Node node in section.Grid)
                {
                    if(node.IsInside(position) && node.FieldDatas.Count > fieldIndex)
                        return node.FieldDatas[fieldIndex].Vector;
                }
            }
        }*/

        return Vector2.zero;
    }

    [Server]
    public Vector2 GetNormalizedVectorToNextNode(Vector2 position, int fieldIndex)
    {
        float minX = floor.SizeX.x;
        float maxX = floor.SizeX.y;
        float minY = floor.SizeY.x;
        float maxY = floor.SizeY.y;

        if (position.x < minX || position.x >= maxX || position.y < minY || position.y >= maxY)
            return Vector2.zero;

        float cellSize = floor.GetGridSize();
        int gridWidth = Mathf.RoundToInt((maxX - minX) / cellSize);

        int x = Mathf.FloorToInt((position.x - minX) / cellSize);
        int y = Mathf.FloorToInt((position.y - minY) / cellSize);

        int nodeIndex = y * gridWidth + x;

        if (fieldIndex >= 0 && flowField.FieldDatas.Count > fieldIndex)
        {
            float2 nodeVector = flowField.FieldDatas[fieldIndex].Vector[nodeIndex];
            Vector2 normalizedVector = new Vector2(nodeVector.x, nodeVector.y).normalized;

            return normalizedVector;
        }
        
        /*foreach(Section section in floor.Sections)
        {
            if(section.IsInside(position))
            {
                foreach(Node node in section.Grid)
                {
                    if(node.IsInside(position) && node.FieldDatas.Count > fieldIndex)
                        return node.FieldDatas[fieldIndex].Vector.normalized;
                }
            }
        }*/

        return Vector2.zero;
    }

    public void Dispose()
    {
        flowField.Dispose();
    }
}