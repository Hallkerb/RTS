using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [SerializeField] private GameObject[] prefubs;

    private Dictionary<uint, GameObject> _assetIdToPrefab = new Dictionary<uint, GameObject>();

    private Dictionary<GameObject, Stack<GameObject>> _pools = new Dictionary<GameObject, Stack<GameObject>>();

    void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = this;
    }

    void Start()
    {
        if (prefubs != null)
        {
            foreach (var prefab in prefubs)
            {
                if (prefab.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    _assetIdToPrefab[identity.assetId] = prefab;
                    
                    NetworkClient.RegisterPrefab(prefab, SpawnHandler, UnspawnHandler);
                }
            }
        }
    }

    private GameObject SpawnHandler(SpawnMessage msg)
    {
        if (!_assetIdToPrefab.TryGetValue(msg.assetId, out GameObject prefab))
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

            if (obj.TryGetComponent(out IPoolable poolableObj))
                poolableObj.ResetState();
        }
        else
        {
            obj = Instantiate(prefab, position, quaternion, Floor.Instance.SpawnParent);

            var poolable = obj.GetComponent<PoolableObject>() ?? obj.AddComponent<PoolableObject>();
            poolable.OriginPrefab = prefab;
        }

        obj.SetActive(true);

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
