using System;
using Mirror;
using UnityEngine;

public class SpawnerManager : NetworkBehaviour
{
    public static SpawnerManager Singlton;

    public event Action<Entity> OnSpawn;

    void Awake()
    {
        Singlton = this;
    }

    [Server]
    public Entity Spawn(Entity entity, Vector2 position, Quaternion quaternion, int playerID, NetworkConnectionToClient conn)
    {
        Entity obj = ObjectPooler.Instance.Get(entity.gameObject, position, quaternion).GetComponent<Entity>();
        NetworkServer.Spawn(obj.gameObject, conn);
        obj.Initialize(playerID);

        OnSpawn?.Invoke(obj);

        return obj;
    }

    [Server]
    public Entity Spawn(Entity entity, Vector2 position, Quaternion quaternion, NetworkConnectionToClient conn)
    {
        Entity obj = ObjectPooler.Instance.Get(entity.gameObject, position, quaternion).GetComponent<Entity>();
        NetworkServer.Spawn(obj.gameObject, conn);

        OnSpawn?.Invoke(obj);

        return obj;
    }

    [Server]
    public Entity Spawn(Entity entity, Vector2 position, Quaternion quaternion)
    {
        Entity obj = ObjectPooler.Instance.Get(entity.gameObject, position, quaternion).GetComponent<Entity>();
        NetworkServer.Spawn(obj.gameObject);

        OnSpawn?.Invoke(obj);

        return obj;
    }
}
