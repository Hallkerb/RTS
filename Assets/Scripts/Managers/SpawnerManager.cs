using System;
using Mirror;
using UnityEngine;

public class SpawnerManager : NetworkBehaviour
{
    public static SpawnerManager Instance;

    public event Action<GameObject> OnSpawn;

    void Awake()
    {
        Instance = this;
    }

    [Server]
    public Entity Spawn(Entity entity, Vector2 position, Quaternion quaternion, int playerID, NetworkConnectionToClient conn)
    {
        Entity obj = ObjectPooler.Instance.Get(entity.gameObject, position, quaternion).GetComponent<Entity>();
        obj.Initialize(playerID);
        NetworkServer.Spawn(obj.gameObject, conn);

        OnSpawn?.Invoke(obj.gameObject);

        return obj;
    }

    [Server]
    public Entity Spawn(Entity entity, Vector2 position, Quaternion quaternion, NetworkConnectionToClient conn)
    {
        GameObject obj = ObjectPooler.Instance.Get(entity.gameObject, position, quaternion);
        NetworkServer.Spawn(obj, conn);

        OnSpawn?.Invoke(obj);

        return obj.GetComponent<Entity>();
    }

    [Server]
    public Entity Spawn(Entity entity, Vector2 position, Quaternion quaternion)
    {
        GameObject obj = ObjectPooler.Instance.Get(entity.gameObject, position, quaternion);
        NetworkServer.Spawn(obj);

        OnSpawn?.Invoke(obj);

        return obj.GetComponent<Entity>();
    }
}
