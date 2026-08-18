using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class PlayerBuildingsManager : NetworkBehaviour
{
    private Player player;

    private readonly List<BuildingPhantom> buildingPhantoms = new List<BuildingPhantom>();

    public IReadOnlyList<BuildingPhantom> BuildingPhantoms => buildingPhantoms;

    void Awake()
    {
        player = GetComponent<Player>();
    }

    [Server]
    public void RegisterConstruction(BuildingPhantom construction)
    {
        if (construction == null || buildingPhantoms.Contains(construction)) return;

        buildingPhantoms.Add(construction);
    }

    [Server]
    public void UnregisterConstruction(Entity construction)
    {
        if (buildingPhantoms.Contains(construction))
            buildingPhantoms.Remove(construction as BuildingPhantom);
    }

    [Server]
    public BuildingPhantom GetNearestConstruction(Vector3 workerPosition)
    {
        BuildingPhantom nearest = null;
        float minDistance = float.MaxValue;

        for (int i = buildingPhantoms.Count - 1; i >= 0; i--)
        {
            var construction = buildingPhantoms[i];

            if (construction == null)
            {
                buildingPhantoms.RemoveAt(i);
                continue;
            }

            float dist = Vector3.SqrMagnitude(construction.transform.position - workerPosition);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = construction;
            }
        }

        return nearest;
    }

    public void PlaceConstruct(bool resetTasks, int index, Vector2 pos) => CmdPlaceConstruct(player.Controller.UnitChoose.OfType<Unit>().ToList(), resetTasks, index, pos);

    [Command]
    private void CmdPlaceConstruct(List<Unit> unitChoose, bool resetTasks, int index, Vector2 pos)
    {
        var phantomPrefub = player.Controller.UserInterface.BuildingsUI.GetBuilding(index);
        BuildingPhantom newBuilding = SpawnerManager.Instance.Spawn(phantomPrefub, pos, Quaternion.identity, player.ID, connectionToClient) as BuildingPhantom;

        RegisterConstruction(newBuilding);
        newBuilding.OnDeath += UnregisterConstruction;

        player.UnitsController.SetUnitsBuilding(unitChoose, newBuilding, player.ID, resetTasks);
        newBuilding.StartConstruct();
    }
}