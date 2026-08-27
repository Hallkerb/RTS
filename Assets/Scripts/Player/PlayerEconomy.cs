using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;

public enum ResourceType 
{ 
    Food, Materials, Iron 
}

public class PlayerEconomy : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnFoodChanged))] private int _food = 500;
    [SyncVar(hook = nameof(OnMaterialsChanged))] private int _materials = 500;
    [SyncVar(hook = nameof(OnIronChanged))] private int _iron = 0;

    public int Food => _food;
    public int Materials => _materials;
    public int Iron => _iron;

    public event Action<(ResourceType, int)[]> OnResourceChanged;

    private void OnFoodChanged(int oldValue, int newValue) => OnResourceChanged?.Invoke(new[] { (ResourceType.Food, newValue) });

    private void OnMaterialsChanged(int oldValue, int newValue) => OnResourceChanged?.Invoke(new[] { (ResourceType.Materials, newValue) });

    private void OnIronChanged(int oldValue, int newValue) => OnResourceChanged?.Invoke(new[] { (ResourceType.Iron, newValue) });

    public int GetResource(ResourceType resource)
    {
        return resource switch
        {
            ResourceType.Food => Food,
            ResourceType.Materials => Materials,
            ResourceType.Iron => Iron,
            _ => 0
        };
    }

    public bool EnoughResource(params (ResourceType type, int count)[] resources)
    {
        var totals = new Dictionary<ResourceType, int>();

        foreach (var r in resources)
        {
            if (!totals.ContainsKey(r.type)) 
                totals[r.type] = 0;

            totals[r.type] += r.count;
        }

        foreach (var pair in totals)
            if (GetResource(pair.Key) < pair.Value) return false;

        return true;
    }

    [Server]
    public bool AddResource(ResourceType resource, int count)
    {
        if (count < 0) return false;

        switch(resource)
        {
            case ResourceType.Food:
                _food += count;
                break;
            case ResourceType.Materials:
                _materials += count;
                break;
            case ResourceType.Iron:
                _iron += count;
                break;
        }

        return true;
    }

    [Server]
    public bool SpendResources(params (ResourceType type, int count)[] resources)
    {
        var totals = new Dictionary<ResourceType, int>();

        foreach (var r in resources)
        {
            if (!totals.ContainsKey(r.type)) 
                totals[r.type] = 0;

            totals[r.type] += r.count;
        }

        foreach (var pair in totals)
            if (GetResource(pair.Key) < pair.Value) return false;

        foreach (var r in resources)
            SpendResource((r.type, r.count));

        return true;
    }

    private void SpendResource((ResourceType type, int count) resource)
    {
        switch(resource.type)
        {
            case ResourceType.Food:
                _food -= resource.count;
                break;
            case ResourceType.Materials:
                _materials -= resource.count;
                break;
            case ResourceType.Iron:
                _iron -= resource.count;
                break;
        }
    }
}