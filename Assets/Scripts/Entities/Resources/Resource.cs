using System.Linq;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class Resource : Entity, IResource
{
    [SerializeField] private ResourceType type;
    public ResourceType Type => type;

    private static float harvestMultiplier = 20;
    private static int harvestCount = 20;

    [Server]
    public void Harvesting()
    {
        HP -= harvestMultiplier * Time.deltaTime;
    }

    [Server]
    public void Harvest(int playerID)
    {
        PlayerEconomy playerEconomy = null;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null ) continue;

            var player = conn.identity.GetComponent<Player>();

            if (player.ID != playerID) continue;

            playerEconomy = conn.identity.GetComponent<Player>().Economy;

            break;
        }

        if (playerEconomy != null)
            playerEconomy.AddResource(Type, harvestCount);
    }
}