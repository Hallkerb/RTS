using Mirror;
using UnityEngine;

public class TownHall : NetworkBehaviour
{
    private Entity building;

    void Awake()
    {
        building = GetComponent<Entity>();
    }

    [Server]
    public void Initialize(Player player)
    {
        GetComponent<Entity>().Initialize(player.ID);

        building.OnDeath += player.Lose;
    }
}
