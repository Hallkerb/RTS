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

    private Queue<EntitySpawnData> queueEntity { get; } = new Queue<EntitySpawnData>();

    private Coroutine timer;

    float timeLeftToSpawn = 0;

    private Vector2 spawnPoint;

    public Action<string, QueueTaskData> OnAddQueue;

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
                queueTaskDatas.Add(new QueueTaskData(task.Entity.Data.Name, task.TimeToSpawn, timeLeftToSpawn, true));
                isFirst = false;
            }
            else
            {
                queueTaskDatas.Add(new QueueTaskData(task.Entity.Data.Name, task.TimeToSpawn, 0, false));
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

    public void SetEntityToSpawn(int index)
    {
        if (isServer || ClientSetEntityToSpawn(index))
            CmdSetEntityToSpawn(index);
    }

    private bool ClientSetEntityToSpawn(int index)
    {
        Player player = NetworkClient.localPlayer != null 
        ? NetworkClient.localPlayer.GetComponent<Player>() 
        : null;

        EntitySpawnData spawnData = entities[index];

        if (player == null || player.Economy.EnoughResource(spawnData.GetPrice()) == false) return false;

        queueEntity.Enqueue(spawnData);

        OnAddQueue?.Invoke(UI.PanelKeys[IndexUI], new QueueTaskData(spawnData.Entity.Data.Name, spawnData.TimeToSpawn, 0, timer == null));

        return true;
    }

    [Command]
    public void CmdSetEntityToSpawn(int index)
    {
        Player player = null;

        if (NetworkServer.connections.TryGetValue(PlayerID, out var conn))
            player = conn.identity.GetComponent<Player>();

        EntitySpawnData spawnData = entities[index];

        if (player == null || player.Economy.SpendResources(spawnData.GetPrice()) == false) return;

        queueEntity.Enqueue(spawnData);

        OnAddQueue?.Invoke(UI.PanelKeys[IndexUI], new QueueTaskData(spawnData.Entity.Data.Name, spawnData.TimeToSpawn, 0, timer == null));

        if (timer == null)
            timer = StartCoroutine(TimerToSpawn());
    }

    private void RepositionSpawn(Vector2 endPos)
    {
        Collider2D collider = GetComponent<Collider2D>();

        spawnPoint = collider.ClosestPoint(endPos);
    }

    [Command]
    public void CmdRepositionSpawn(Vector2 endPos) => RepositionSpawn(endPos);

    public void SpawnEntity(EntitySpawnData spawnData)
    {
        Vector2 direction = (spawnPoint - (Vector2)transform.position).normalized;
        Vector2 spawnPos = spawnPoint + direction * spawnData.SpawnOffset;

        SpawnerManager.Instance.Spawn(spawnData.Entity, spawnPos , Quaternion.identity, PlayerID, connectionToClient);
    }

    private IEnumerator TimerToSpawn()
    {
        if (isServer)
            ClientStartTimer(connectionToClient);

        while(queueEntity.Count > 0)
        {
            var queue = queueEntity.Peek();

            timeLeftToSpawn = 0;

            while(timeLeftToSpawn < queue.TimeToSpawn)
            {
                timeLeftToSpawn += Time.deltaTime;

                OnSpawnProgressChanged(timeLeftToSpawn / queue.TimeToSpawn);

                yield return null;
            }

            queueEntity.Dequeue();

            OnSpawnProgressChanged(0);

            if (isServer)
                SpawnEntity(queue);
        }

        timer = null;
    }

    [TargetRpc]
    private void ClientStartTimer(NetworkConnection connection)
    {
        if (isServer == false && timer == null)
            timer = StartCoroutine(TimerToSpawn());
    }

    private void OnSpawnProgressChanged(float newValue) => spawnImage.fillAmount = newValue;

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
