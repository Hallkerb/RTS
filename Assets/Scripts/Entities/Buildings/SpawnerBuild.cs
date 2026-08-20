using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class SpawnerBuild : Building, ISpawner
{
    [SerializeField] private int indexUI;

    [SerializeField] private List<EntitySpawnData> entities = new List<EntitySpawnData>();

    private Image spawnImage;

    private Queue<(Entity entity, float timeToSpawn)> queueEntity { get; } = new Queue<(Entity entity, float timeToSpawn)>();

    private Coroutine timer;

    float timeLeftToSpawn = 0;

    private Vector2 spawnPoint;

    public int IndexUI => indexUI;

    public List<QueueTaskData> GetQueueData()
    {
        List<QueueTaskData> queueTaskDatas = new List<QueueTaskData>();

        if (queueEntity.Count < 1) return queueTaskDatas;

        bool isFirst = true;

        foreach (var task in queueEntity)
        {
            if (isFirst)
            {
                queueTaskDatas.Add(new QueueTaskData(task.entity.Data.Name, task.timeToSpawn, timeLeftToSpawn, true));
                isFirst = false;
            }
            else
            {
                queueTaskDatas.Add(new QueueTaskData(task.entity.Data.Name, task.timeToSpawn, 0, false));
            }
        }
        
        return queueTaskDatas;
    }

    protected override void Awake()
    {
        base.Awake();

        spawnImage = transform.Find("UI").Find("Spawn_Panel").Find("Spawn_Image").GetComponent<Image>();
    }

    public EntitySpawnData GetEntitySpawnData(int index)
    {
        return entities[index];
    }

    [Command]
    public void SetEntityToSpawn(int index)
    {
        Player player = null;

        if (NetworkServer.connections.TryGetValue(PlayerID, out var conn))
            player = conn.identity.GetComponent<Player>();

        if (player == null || player.Economy.SpendResources(entities[index].GetPrice()) == false) return;

        queueEntity.Enqueue((entities[index].Entity, entities[index].TimeToSpawn));

        if (timer == null)
            timer = StartCoroutine(timerToSpawn());
    }

    private void RepositionSpawn(Vector2 endPos)
    {
        Collider2D collider = GetComponent<Collider2D>();

        spawnPoint = collider.ClosestPoint(endPos);
    }

    [Command]
    public void CmdRepositionSpawn(Vector2 endPos) => RepositionSpawn(endPos);

    public void SpawnEntity(Entity entity)
    {
        SpawnerManager.Instance.Spawn(entity, spawnPoint, Quaternion.identity, PlayerID, connectionToClient);
    }

    private IEnumerator timerToSpawn()
    {
        while(queueEntity.Count > 0)
        {
            var queue = queueEntity.Dequeue();

            timeLeftToSpawn = 0;

            while(timeLeftToSpawn < queue.timeToSpawn)
            {
                timeLeftToSpawn += Time.deltaTime;

                OnSpawnProgressChanged(connectionToClient, timeLeftToSpawn / queue.timeToSpawn);

                yield return null;
            }

            SpawnEntity(queue.entity);

            OnSpawnProgressChanged(connectionToClient, 0);
        }

        timer = null;
    }

    [TargetRpc]
    private void OnSpawnProgressChanged(NetworkConnectionToClient target, float newValue) => spawnImage.fillAmount = newValue;

    public override void Initialize(int playerID)
    {
        base.Initialize(playerID);

        RepositionSpawn(new Vector2(transform.position.x, GetComponent<Collider2D>().bounds.max.y));

        Debug.Log($"spawnPoint = {spawnPoint}");
    }

    protected override void OpenUI(bool choose)
    {
        playerController.UserInterface.OpenPanel(choose ? UI.PanelKeys[indexUI] : UI.PanelKeys[0]);
    }
}
