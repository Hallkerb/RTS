using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class Worker : Unit, IBuilder, IHarvester
{
    private Transform fork;
    [SerializeField] private Vector2 startPosSpear;
    [SerializeField] private Vector2 endPosSpear;

    private static float harvestTime = 3;

    private Coroutine harvest;

    protected override void Awake()
    {
        base.Awake();

        fork = Body.Find("Fork");
    }

    [ClientRpc]
    protected override void RpcAttackAnimate()
    {
        fork.localPosition = endPosSpear;
    }

    [ClientRpc]
    protected override void RpcReloadAnimate(float t)
    {
        fork.localPosition = Vector2.Lerp(endPosSpear, startPosSpear, t);
    }

    [Server]
    public void SetBuildTask(BuildingPhantom construct, bool resetTasks = true)
    {
        if (construct == null) return;

        PreviousTask(resetTasks, rangeAttack);

        construct.OnDeath += UnsetTarget;

        Tasks.Enqueue((Build, construct));
    }

    [Server]
    public void SetHarvestTask(Entity resource, bool resetTasks = true)
    {
        if (resource == null) return;

        PreviousTask(resetTasks, 0.1f);

        resource.OnDeath += UnsetTarget;

        Tasks.Enqueue((Harvest, resource));
    }

    [Server]
    public void Build()
    {
        if (Target == null || !Target.gameObject.activeSelf)
        {
            DequeueTask();
            return;
        }

        if (Target.TryGetComponent(out BuildingPhantom construct) == false)
        {
            Debug.LogError($"Target is not {"BuildingPhantom"}, worker can`t build this");
            return;
        }

        Vector2 closePoint = construct.GetComponent<Collider2D>().ClosestPoint(transform.position);

        if (Vector2.Distance(transform.position, closePoint) < rangeAttack)
            construct.Build();
        else
            SearchNavigationAndMove(closePoint);
    }

    [Server]
    public void Harvest()
    {
        if (Target == null || !Target.gameObject.activeSelf)
        {
            DequeueTask();
            return;
        }

        if (Target.TryGetComponent(out Resource resource) == false)
        {
            Debug.LogError($"Target is not {"Resource"}, worker can`t harvest this");
            return;
        }

        Vector2 closePoint = resource.GetComponent<Collider2D>().ClosestPoint(transform.position);

        if (Vector2.Distance(transform.position, closePoint) < rangeAttack)
        {
            if (harvest == null)
                harvest = StartCoroutine(HarvestCoroutine(resource));
        }
        else
            SearchNavigationAndMove(closePoint);
    }

    [Server]
    private IEnumerator HarvestCoroutine(Resource resource)
    {
        float timeLeft = 0;

        while(timeLeft < harvestTime)
        {
            timeLeft += Time.deltaTime;

            if (resource != null)
                resource.Harvesting();
            else
            {
                harvest = null;
                yield break;
            }
            
            yield return null;
        }

        if (resource != null)
            resource.Harvest(PlayerID);

        harvest = null;
    }

    protected override void TryHandleCustomTask((Action task, Entity target) completedTask, string methodName)
    {
        base.TryHandleCustomTask(completedTask, methodName);

        Player player = null;

        if (NetworkServer.connections.TryGetValue(PlayerID, out var conn) && conn.identity != null)
            player = conn.identity.GetComponent<Player>();

        switch (methodName)
        {
            case nameof(Build):
                SetBuildTask(player.BuildingsManager.GetNearestConstruction(transform.position));
                break;
            case nameof(Harvest):
                Entity resource = FindNearestResourceInViewingCircle(completedTask.target.GetComponent<Resource>().Type);
                SetHarvestTask(resource);
                break;
            default:
                break;
        }
    }

    protected Entity FindNearestResourceInViewingCircle(ResourceType type)
    {
        float radius = ViewingRadius;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius * 1.1f, 1 << 13);

        Entity nearestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out Resource resource) && resource.Type == type)
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestEnemy = resource;
                }
            }
        }

        return nearestEnemy;
    }

    public override void ResetState()
    {
        base.ResetState();

        harvest = null;
    }
}
