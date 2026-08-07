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
            Tasks.Dequeue();
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
            Tasks.Dequeue();
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

    public override void ResetState()
    {
        base.ResetState();

        harvest = null;
    }
}
