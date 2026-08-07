using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    private Dictionary<GameObject, Stack<GameObject>> _pools = new Dictionary<GameObject, Stack<GameObject>>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (NetworkManager.singleton != null)
        {
            foreach (var prefab in NetworkManager.singleton.spawnPrefabs)
            {
                if (prefab.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    NetworkClient.UnregisterPrefab(prefab);

                    NetworkClient.RegisterPrefab(prefab, SpawnHandler, UnspawnHandler);
                }
            }
        }

        /*foreach (var prefab in poolablePrefabs)
        {
            NetworkClient.RegisterPrefab(prefab, SpawnHandler, UnspawnHandler);
        }*/
    }

    private GameObject SpawnHandler(SpawnMessage msg)
    {
        if (!NetworkClient.GetPrefab(msg.assetId, out GameObject prefab))
            return null;

        if (NetworkServer.active && NetworkClient.spawned.TryGetValue(msg.netId, out NetworkIdentity identity))
            return identity.gameObject;

        return Get(prefab, msg.position, msg.rotation);
    }

    private void UnspawnHandler(GameObject spawned)
    {
        SetToPool(spawned);
    }

    public GameObject Get(GameObject prefab, Vector2 position, Quaternion quaternion)
    {
        if (!_pools.ContainsKey(prefab))
            _pools[prefab] = new Stack<GameObject>();

        GameObject obj;

        if (_pools[prefab].Count > 0)
        {
            obj = _pools[prefab].Pop();
            obj.transform.position = position;
            obj.transform.rotation = quaternion;
            obj.SetActive(true); 
        }
        else
        {
            obj = Instantiate(prefab, position, quaternion);
            
            var poolable = obj.GetComponent<PoolableObject>() ?? obj.AddComponent<PoolableObject>();
            poolable.OriginPrefab = prefab;
        }

        return obj;
    }

    public void SetToPool(GameObject obj)
    {
        var poolable = obj.GetComponent<PoolableObject>();
        if (poolable == null)
        {
            Debug.LogError($"Object {obj.name} is not poolable!");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        
        _pools[poolable.OriginPrefab].Push(obj);
    }
}
