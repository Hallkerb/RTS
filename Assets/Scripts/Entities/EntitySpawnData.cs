using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntitySpawnData
{
    [Header("Prefub")]
    [SerializeField]private Entity _entity;
    public Entity Entity 
    {
        get => _entity;
    }

    [Space(20)]
    [Min(0)] [SerializeField] private float _timeToSpawn;
    public float TimeToSpawn 
    { 
        get => _timeToSpawn; 
        set
        {
            _timeToSpawn = value;
        }
    }

    [Header("Price")]
    [Min(0)] [SerializeField] private int _foodPrice;
    [Min(0)] [SerializeField] private int _materialsPrice;
    [Min(0)] [SerializeField] private int _ironPrice;

    public int GetPrice(ResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceType.Food => _foodPrice,
            ResourceType.Materials => _materialsPrice,
            ResourceType.Iron => _ironPrice,
            _ => 0
        };
    }

    public (ResourceType type, int price)[] GetPrice()
    {
        return new (ResourceType, int)[]
        {
            (ResourceType.Food, _foodPrice),
            (ResourceType.Materials, _materialsPrice),
            (ResourceType.Iron, _ironPrice)
        };
    }

    public void SetPrice(ResourceType resourceType, int price)
    {
        switch(resourceType)
        {
            case ResourceType.Food:
                _foodPrice = price;
                break;
            case ResourceType.Materials:
                _materialsPrice = price;
                break;
            case ResourceType.Iron:
                _ironPrice = price;
                break;
        }
    }

    public bool SetPrices(params (ResourceType type, int price)[] resources)
    {
        var totals = new Dictionary<ResourceType, int>();

        foreach (var r in resources)
        {
            if (!totals.ContainsKey(r.type)) 
                totals[r.type] = 0;

            totals[r.type] += r.price;
        }

        foreach (var t in totals)
            SetPrice(t.Key, t.Value);

        return true;
    }
}