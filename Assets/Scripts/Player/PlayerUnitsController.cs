using Mirror;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class PlayerUnitsController : NetworkBehaviour
{
    private NavigationManager navigationManager;
    private Player player;

    void Awake()
    {
        player = GetComponent<Player>();
        navigationManager = FindFirstObjectByType<NavigationManager>();
    }

    [Command]
    public void CmdSetUnitsTasks(Unit[] unitChoose, Vector2 targetPosition, int playerID)
    {
        var safeUnits = unitChoose.Where(u => u != null).ToArray();

        if (safeUnits.Length > 0)
        {
            int floorLayer = 1 << 3; // 3 - Floor
            int unitLayer = 1 << 6; // 6 - Units
            int buildingLayer = 1 << 7; // 7 - Buildings
            int constructionLayer = 1 << 8; // 8 - Constructions
            int resourceLayer = 1 << 13; // 13 - Resources
            int wheatLayer = 1 << 14; // 14 - Wheat

            int combinedLayerMask = floorLayer | unitLayer | buildingLayer | constructionLayer | resourceLayer | wheatLayer;

            Collider2D[] hit;

            hit = Physics2D.OverlapPointAll(targetPosition, combinedLayerMask);

            if (hit != null && hit.Length > 0)
            {
                hit = hit.OrderByDescending(c => c.TryGetComponent(out Entity target) && target.PlayerID != playerID)
                        .ThenByDescending(c => c.gameObject.layer == 8).ThenByDescending(c => c.gameObject.layer == 13)
                        .ThenByDescending(c => c.gameObject.layer == 14).ThenByDescending(c => c.gameObject.layer == 3).ToArray();

                switch (hit[0].gameObject)
                {
                    case GameObject go when go.TryGetComponent(out Entity target):
                        Vector2 targetPos = target.transform.position;

                        Debug.Log("Clicked on entity");

                        if (target.PlayerID >= 0 && target.PlayerID != playerID)
                            _ = AssignTaskToUnitsAsync(safeUnits, target, targetPos, unit => {if (unit is IAttackable attackable) attackable.SetAttackTask(target, false);}, playerID);
                        else if (target.TryGetComponent(out BuildingPhantom construct))
                            SetUnitsBuilding(safeUnits, construct, playerID, true);
                        else if (target.TryGetComponent(out Resource resource))
                            _ = AssignTaskToUnitsAsync(safeUnits, target, targetPos, unit => {if (unit is IHarvester harvester) harvester.SetHarvestTask(resource, false);}, playerID);
                        else
                            _ = navigationManager.SetNavigationAsync(safeUnits, target, targetPosition, player.ID);
                        break;
                    default:
                            _ = navigationManager.SetNavigationAsync(safeUnits, null, targetPosition, player.ID);
                        break;
                }
            }
        }
    }

    private async Task AssignTaskToUnitsAsync(IReadOnlyList<Unit> unitChoose, Entity target, Vector2 targetPosition, Action<Unit> taskAssigner, int playerID, bool resetTasks = true)
    {
        if (resetTasks)
            await navigationManager.SetNavigationAsync(unitChoose, target, targetPosition, player.ID, resetTasks);

        foreach (Unit unit in unitChoose)
        {
            if (playerID != unit.PlayerID) continue;

            taskAssigner(unit);
        }
    }

    [Server]
    public void SetUnitsBuilding(IReadOnlyList<Unit> unitChoose, BuildingPhantom construct, int playerID, bool resetTasks = false)
    {
        var safeUnits = unitChoose.Where(u => u != null).ToArray();

        _ = AssignTaskToUnitsAsync(safeUnits, construct, construct.transform.position, unit => {if (unit is IBuilder builder) builder.SetBuildTask(construct, false);}, playerID, resetTasks);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        DisposeNavigationManager();
    }

    public void DisposeNavigationManager()
    {
        if (navigationManager != null)
            navigationManager.Dispose();
    }
}