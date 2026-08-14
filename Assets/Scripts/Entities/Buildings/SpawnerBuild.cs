using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;
using UnityEngine.UI;
using Mirror;

public class SpawnerBuild : Building
{
    [SerializeField] private int indexUI;

    [SerializeField] private List<EntitySpawnData> entities = new List<EntitySpawnData>();

    private Image spawnImage;

    private Queue<(Entity entity, float timeToSpawn)> queueEntity = new Queue<(Entity entity, float timeToSpawn)>();

    private Coroutine timer;

    private Vector2 spawnPoint;

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

        if (player == null || player.GetPlayerEconomy().SpendResources(entities[index].GetPrice()) == false) return;

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

            float timeLeft = 0;

            while(timeLeft < queue.timeToSpawn)
            {
                timeLeft += Time.deltaTime;

                OnSpawnProgressChanged(connectionToClient, timeLeft / queue.timeToSpawn);

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
