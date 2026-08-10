using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

public abstract class Unit : Entity, IMovable, IAttackable
{
    private NavigationManager navigationManager;
    private AStar_Navigation navigation;
    private EnemyScanner enemyScanner = new EnemyScanner();

    protected Transform Body;
    protected UnitLineRenderer unitLineRenderer;

    protected Queue<(Action task, Entity target)> Tasks { get; private set; } = new Queue<(Action task, Entity target)>();

    private Entity target;
    public Entity Target
    {
        get => target;
        set
        {
            if (!isServer) return;

            var oldTarget = target;

            if (value == null || value.gameObject.activeSelf == false)
                target = null;
            else
                target = value;

            if (NetworkServer.connections.TryGetValue(PlayerID, out var conn))
                SetLocalTarget(conn, target, oldTarget);
        }
    }

    public event Action<Unit, (Entity oldTarget, Entity newTarget)> ChangeLocalTarget;
    
    private List<Unit> closeUnits = new List<Unit>();
    private List<Collider2D> closeWalls = new List<Collider2D>();

    [SerializeField] private float speed;
    [SerializeField] private float damage;
    [SerializeField] private float reloadTime;
    [SerializeField] protected float rangeAttack;

    protected float minDistanceToTarget;

    public int? _FieldIndex = null;
    public int? FieldIndex
    {
        get => _FieldIndex;
        set
        {
            _FieldIndex = value;
        }
    }

    private int moveLayerMask;

    private Vector2 targetPos = new Vector2(int.MinValue, int.MinValue);

    [SerializeField] private bool isWalking;
    public bool IsWalking
    {
        get => isWalking;
        private set => isWalking = value;
    }

    private bool isReloading;

    protected override void Awake()
    {
        base.Awake();

        navigationManager = FindFirstObjectByType<NavigationManager>();

        navigation = new AStar_Navigation(FindFirstObjectByType<Floor>(), this);

        Body = transform.Find("Body");
        unitLineRenderer = GetComponent<UnitLineRenderer>();

        moveLayerMask = GetLayerMask();
    }

    void Update()
    {
        if (isServer)
        {
            if (Tasks.TryPeek(out var result))
            {
                Target = result.target;
                result.task();
            }
            else if (enemyScanner.EnemySearch == false)
                enemyScanner.SearchCoroutine = StartCoroutine(enemyScanner.SearchEnemy(transform.position, ViewingRadius, PlayerID, (entity) => SetAttackTask(entity), Tasks));
        }
    }

    private void StopSearchEnemy()
    {
        if (enemyScanner.SearchCoroutine != null)
        {
            StopCoroutine(enemyScanner.SearchCoroutine);
            enemyScanner.SearchCoroutine = null;
            enemyScanner.EnemySearch = false;
        }
    }

    [Server]
    public void SearchNavigation(NavigationMode mode, Vector2 position, int? fieldIndex)
    {
        targetPos = position;

        switch(mode)
        {
            case NavigationMode.AStar:
                _ = SearchNavigation(position);
                break;
            case NavigationMode.FlowField:
                if (fieldIndex.HasValue)
                {
                    navigation.WayPoints.Clear();

                    if (NetworkServer.connections.TryGetValue(PlayerID, out NetworkConnectionToClient conn))
                        unitLineRenderer.ResetLineRendererPosition(conn);

                    if (FieldIndex.HasValue)
                        navigationManager.RemoveUnitInField(this, FieldIndex.Value);

                    navigationManager.AddUnitInField(this, fieldIndex.Value);
                }
                else
                    Debug.LogError($"FieldIndex is null!");
                break;
        }

        FieldIndex = fieldIndex;
    }

    [Server]
    public void SetMoveTask(NavigationMode mode, Entity target, Vector2 position, int? fieldIndex, bool resetTasks = true)
    {
        SearchNavigation(mode, position, fieldIndex);

        PreviousTask(resetTasks, Body.lossyScale.x / 2);

        Tasks.Enqueue((Move, target));
    }

    [Server]
    public void SetAttackTask(Entity enemy, bool resetTasks = true)
    {
        if (enemy == null) return;

        PreviousTask(resetTasks, rangeAttack);

        enemy.OnDeath += UnsetTarget;

        Tasks.Enqueue((PerformAttack, enemy));
    }

    [TargetRpc]
    private void SetLocalTarget(NetworkConnection conn, Entity newTarget, Entity oldTarget)
    {
        target = newTarget;

        ChangeLocalTarget?.Invoke(this, (oldTarget, newTarget));
    }

    [Server]
    protected void PreviousTask(bool resetTasks, float minDistanceToTarget)
    {
        StopSearchEnemy();

        if (resetTasks)
            ResetTasks();

        this.minDistanceToTarget = minDistanceToTarget;
    }

    [Server]
    protected void ResetTasks() => Tasks.Clear();

    [Server]
    protected void UnsetTarget(Entity entity = null)
    {
        if (entity != null)
            entity.OnDeath -= UnsetTarget;

        Target = null;
    }

    [Server]
    protected void Move()
    {
        IsWalking = true;

        Vector2 finalMove = Vector2.zero;
        Vector2 direction = Vector2.zero;

        if (navigation.WayPoints.Count > 0)
        {
            Vector2 currentTarget = navigation.WayPoints[0];
            direction = (currentTarget - (Vector2)transform.position).normalized;

            NetworkConnectionToClient client = null;

            if (NetworkServer.connections.TryGetValue(PlayerID, out NetworkConnectionToClient conn))
                client = conn;

            if (client != null)
                unitLineRenderer.SetLineRendererPosition(client);

            if (Vector2.Distance(transform.position, currentTarget) < 0.1f)
            {
                navigation.WayPoints.RemoveAt(0);

                if (client != null)
                    unitLineRenderer.ReductionLineRendererPosition(client);
            }
        }
        else if (FieldIndex.HasValue)
            direction = navigationManager.GetNormalizedVectorToNextNode(transform.position, FieldIndex.Value);

        finalMove = Repulsion(finalMove, direction);

        RotateRpc(finalMove);

        transform.position += (Vector3)finalMove;

        bool targetClose = false;

        if (Target != null && Target.gameObject.activeSelf)
        {
            Vector2 closetPoint = Target.GetComponent<Collider2D>().ClosestPoint(transform.position);

            targetClose = (Target.PlayerID != PlayerID && Vector2.Distance(transform.position, closetPoint) <= rangeAttack)
                            || (Vector2.Distance(transform.position, closetPoint) <= minDistanceToTarget);
        }

        if (targetClose || Vector2.Distance(targetPos, transform.position) <= minDistanceToTarget)
        {
            if (FieldIndex.HasValue)
                navigationManager.RemoveUnitInField(this, FieldIndex.Value);
            else
            {
                navigation.WayPoints.Clear();

                if (NetworkServer.connections.TryGetValue(PlayerID, out NetworkConnectionToClient conn))
                    unitLineRenderer.ResetLineRendererPosition(conn);
            }

            FieldIndex = null;
            targetPos = new Vector2(int.MinValue, int.MinValue);
            IsWalking = false;

            if (Tasks.Count > 0)
            {
                var task = Tasks.Peek();

                if (task.Item1.Method.Name == nameof(Move))
                    Tasks.Dequeue();
            }
        }
    }

    [ClientRpc]
    private void RotateRpc(Vector2 move)
    {
        Body.rotation = Quaternion.Euler(0, 0, (Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg) - 90);
    }

    [Server]
    private Vector2 Repulsion(Vector2 finalMove, Vector2 direction)
    {
        Vector2 repelForce = Vector2.zero;
        float minDistance = transform.Find("SecondCollider").GetComponent<CircleCollider2D>().radius * 2; // Minimal allowed distance

        foreach (Collider2D wall in closeWalls)
        {
            Vector2 closePos = wall.ClosestPoint(transform.position);

            Vector2 toWall = (Vector2)transform.position - closePos;
            float distance = toWall.magnitude;

            if (distance < minDistance)
            {
                Vector2 pushDir = toWall.normalized;
                float pushStrength = (minDistance - distance) * (speed / 2);
                repelForce += pushDir * pushStrength;
            }
        }

        foreach (Unit otherUnit in closeUnits)
        {
            if (otherUnit.IsWalking == false) continue;

            Vector2 toOtherUnit = (Vector2)transform.position - (Vector2)otherUnit.transform.position;
            float distance = toOtherUnit.magnitude;

            if (distance < minDistance)
            {
                Vector2 pushDir = toOtherUnit.normalized;
                float pushStrength = (minDistance - distance) * (speed / 2);
                repelForce += pushDir * pushStrength;
            }
        }

        finalMove = (direction + repelForce).normalized * speed * Time.deltaTime;

        return finalMove;
    }

    [Server]
    protected void SearchNavigationAndMove(Vector2 targetPos)
    {
        if (Vector2.Distance(navigation.TargetPoint, targetPos) > 0.5f)
            _ = SearchNavigation(targetPos);

        Move();
    }

    [Server]
    protected async Task SearchNavigation(Vector2 targetPos)
    {
        if (FieldIndex.HasValue)
            navigationManager.RemoveUnitInField(this, FieldIndex.Value);

        await navigation.SearchWayToTarget(transform.position, targetPos);

        if (NetworkServer.connections.TryGetValue(PlayerID, out NetworkConnectionToClient conn))
            unitLineRenderer.ReconstructLineRenderer(conn, navigation.WayPoints.ToArray());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out Unit unit))
            closeUnits.Add(unit);
        else if ((moveLayerMask & (1 << collision.gameObject.layer)) == 0)
            closeWalls.Add(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out Unit unit))
            closeUnits.Remove(unit);
        else if ((moveLayerMask & (1 << collision.gameObject.layer)) == 0)
            closeWalls.Remove(collision);
    }

    private int GetLayerMask()
    {
        int layer = 1 << 3; // 3 - floor
        int layer2 = 1 << 5; // 5 - UI
        int layer3 = 1 << 6; // 6 - Units
        int layer4 = 1 << 8; // 8 - Constructions
        int layer5 = 1 << 10; // 10 - Map
        int layer6 = 1 << 14; // 14 - Wheat

        int combinedLayerMask = layer | layer2 | layer3 | layer4 | layer5 | layer6;

        return combinedLayerMask;
    }

    [Server]
    protected void PerformAttack()
    {
        if (Target == null || !Target.gameObject.activeSelf)
        {
            Tasks.Dequeue();
            return;
        }

        if (isReloading == false)
        {
            Vector2 closePoint = Target.GetComponent<Collider2D>().ClosestPoint(transform.position);

            if (Vector2.Distance(transform.position, closePoint) <= rangeAttack)
            {
                RpcAttackAnimate();
                Attack(Target, damage, this);
                StartCoroutine(Reload());
            }
            else
                SearchNavigationAndMove(closePoint);
        }
    }

    [Server]
    protected virtual void Attack(Entity target, float damage, Entity source)
    {
        target.InflictDamage(damage, source);
    }

    [ClientRpc]
    protected virtual void RpcAttackAnimate() { }

    [ClientRpc]
    protected virtual void RpcReloadAnimate(float t) { }

    [Server]
    private IEnumerator Reload()
    {
        isReloading = true;

        float timeLeft = 0;

        while (timeLeft < reloadTime)
        {
            timeLeft += Time.deltaTime;

            RpcReloadAnimate(timeLeft / reloadTime);

            yield return null;
        }

        isReloading = false;
    }

    [Server]
    public override void InflictDamage(float enemyDamage, Entity enemy)
    {
        base.InflictDamage(enemyDamage, enemy);

        if (Tasks.Count <= 0 && Target == null)
            SetAttackTask(enemy, false);
    }

    public override void ResetState()
    {
        base.ResetState();

        target = null;
        _FieldIndex = null;
        targetPos = new Vector2(int.MinValue, int.MinValue);
        isWalking = false;
        isReloading = false;
        
        Tasks.Clear();
        closeUnits.Clear();
        closeWalls.Clear();
    }
}
