using UnityEngine;

public interface IResource
{
    ResourceType Type { get; }
    
    void Harvesting();
    void Harvest(int playerID);
}