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
    private Floor floor;

    void Awake()
    {
        player = GetComponent<Player>();
        navigationManager = FindFirstObjectByType<NavigationManager>();
    }

    void Start()
    {
        floor = Floor.Instance;
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

            int combinedLayerMask = floorLayer | unitLayer | buildingLayer | constructionLayer | resourceLayer;

            Collider2D[] hit;

            hit = Physics2D.OverlapPointAll(targetPosition, combinedLayerMask);

            if (hit != null && hit.Length > 0)
            {
                hit = hit.OrderByDescending(c => c.TryGetComponent(out Entity target) && target.PlayerID != playerID)
                        .ThenByDescending(c => c.gameObject.layer == 8).ThenByDescending(c => c.gameObject.layer == 13)
                        .ThenByDescending(c => c.gameObject.layer == 3).ToArray();

                switch (hit[0].gameObject)
                {
                    case GameObject go when go.TryGetComponent(out Entity target):
                        Vector2 targetPos = target.transform.position;

                        //Debug.Log("Clicked on entity");

                        if (target.PlayerID >= 0 && target.PlayerID != playerID)
                            _ = AssignTaskToUnitsAsync(safeUnits, target, targetPos, unit => {if (unit is IAttackable attackable) attackable.SetAttackTask(target, false);}, playerID);
                        else if (target.TryGetComponent(out BuildingPhantom construct))
                            SetUnitsBuilding(safeUnits, construct, playerID, true);
                        else if (target.TryGetComponent(out Resource resource))
                        {
                            if (resource.Type == ResourceType.Materials)
                                target = GetAccessibleTree(resource, hit[0], safeUnits[0].transform.position);

                            Debug.Log("Target: " + target);

                            _ = AssignTaskToUnitsAsync(safeUnits, target, targetPos, unit => {if (unit is IHarvester harvester) harvester.SetHarvestTask(target, false);}, playerID);
                        }
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

    [Server]
    public Entity GetAccessibleTree(Resource resource, Collider2D resourceCollider, Vector3 unitPos)
    {
        if (IsTreeAccessible(resource.transform, resourceCollider, resource.transform.position, resource.Shape))
            return resource;

        var circle = resourceCollider as CircleCollider2D;

        float stepSize = circle.radius * 3 * resource.transform.lossyScale.x;
        Vector3 direction = (unitPos - resource.transform.position).normalized;
        float maxDistance = Vector3.Distance(resource.transform.position, unitPos);

        for (float currentDist = 0; currentDist < maxDistance; currentDist += stepSize)
        {
            Vector2 checkPoint = resource.transform.position + direction * currentDist;

            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPoint, stepSize, 1 << 13);

            foreach(var hit in hits)
            {
                if (hit.TryGetComponent<Resource>(out var currentResource) && currentResource.Type == ResourceType.Materials)
                {
                    if (IsTreeAccessible(currentResource.transform, hit, currentResource.transform.position, currentResource.Shape))
                        return currentResource;
                }
            }
        }

        return null;
    }

    [Server]
    private bool IsTreeAccessible(Transform tree, Collider2D treeCollider, Vector2 treePos, EntityShape treeShape)
    {
        Vector2 treeSize = Vector2.zero;

        Vector2[] offsets = {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
        };

        // switch(treeShape.Type)
        // {
        //     case EntityShape.ShapeType.Square:
        //         var box = treeCollider as BoxCollider2D;
        //         treeSize = Vector2.Scale(box.size, tree.lossyScale);
        //         break;
        //     case EntityShape.ShapeType.Circle:
        //         var circle = treeCollider as CircleCollider2D;
        //         treeSize = new Vector2(circle.radius * 2 * tree.lossyScale.x, circle.radius * 2 * tree.lossyScale.y);
        //         break;
        // }

        var circle = treeCollider as CircleCollider2D;
        treeSize = new Vector2(circle.radius * 2 * tree.lossyScale.x, circle.radius * 2 * tree.lossyScale.y);

        for(int i = 0; i < offsets.Length; i++)
        {
            Vector2 offset = treePos + offsets[i] * treeSize / 2 + offsets[i] * floor.GetUnitRadius(0) + offsets[i] * floor.GetGridSize();

            Node node = NavigationHelper.GetNode(floor, offset);

            if (node != null && node.IsSizeClear[0])
                return true;
        }

        return false;
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