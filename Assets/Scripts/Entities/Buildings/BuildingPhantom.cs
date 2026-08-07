using System.Collections;
using System.Collections.Generic;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPhantom : Entity
{
    private Floor floor;

    [SerializeField] private Building building;

    [SerializeField] private Color[] green;
    [SerializeField] private Color[] red;

    [SerializeField] private SpriteRenderer[] buildingComponents;

    private Collider2D buildingCollider;

    public bool PosClear { get; private set; }
    [SyncVar]
    private bool isPlaced;

    private static float constructMultiplier = 20;

    protected override void Awake()
    {
        base.Awake();

        buildingCollider = GetComponent<Collider2D>();

        floor = FindFirstObjectByType<Floor>();

        HP = 1;
    }

    void Update()
    {
        if (isPlaced == false)
        {
            PosClear = IsPosClear();

            transform.position = SetPosInside();

            if (buildingComponents != null)
            {
                for(int i = 0; i < buildingComponents.Length; i++)
                {
                    if (i < green.Length && i < red.Length)
                        buildingComponents[i].color = PosClear ? green[i] : red[i];
                }
            }
        }
    }

    private bool IsPosClear()
    {
        int layerToIgnore = 1 << floor.gameObject.layer;
        int layerToIgnore3 = 1 << 5; // 5 - UI

        int combinedLayerMask = layerToIgnore | layerToIgnore3;

        Collider2D otherCollider;

        float width = buildingCollider.bounds.size.x;
        float height = buildingCollider.bounds.size.y;

        float x1 = transform.position.x - width / 2 + buildingCollider.offset.x;
        float y1 = transform.position.y - height / 2 + buildingCollider.offset.y;
        float x2 = transform.position.x + width / 2 + buildingCollider.offset.x;
        float y2 = transform.position.y + height / 2 + buildingCollider.offset.y;

        Vector2 pointA = new Vector2(x1, y1);
        Vector2 pointB = new Vector2(x2, y2);

        otherCollider = Physics2D.OverlapArea(pointA, pointB, ~combinedLayerMask);

        return otherCollider == null;
    }

    /*private bool IsFullyInside()
    {
        Bounds buildingBounds = buildingCollider.bounds;
        buildingBounds.center = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) + buildingCollider.offset;

        return floor.bounds.min.x <= buildingBounds.min.x && 
               floor.bounds.max.x >= buildingBounds.max.x &&
               floor.bounds.min.y <= buildingBounds.min.y && 
               floor.bounds.max.y >= buildingBounds.max.y;
    }*/

    private Vector2 SetPosInside()
    {
        float halfsizeX = buildingCollider.bounds.size.x / 2;
        float halfsizeY = buildingCollider.bounds.size.y / 2;

        float stepSize = 0.05f;

        Collider2D floorCollider = floor.GetComponent<Collider2D>();

        float minX = floorCollider.bounds.min.x + halfsizeX - buildingCollider.offset.x + stepSize;
        float maxX = floorCollider.bounds.max.x - halfsizeX - buildingCollider.offset.x - stepSize;
        float minY = floorCollider.bounds.min.y + halfsizeY - buildingCollider.offset.y + stepSize;
        float maxY = floorCollider.bounds.max.y - halfsizeY - buildingCollider.offset.y - stepSize;

        float posX = Mathf.Clamp(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, minX, maxX);
        float posY = Mathf.Clamp(Camera.main.ScreenToWorldPoint(Input.mousePosition).y, minY, maxY);

        return new Vector2(posX, posY);
    }

    public void Place(Player player, bool resetTasks, int index)
    {
        if(PosClear)
        {
            player.PlaceConstruct(resetTasks, index, transform.position);

            if (TryGetComponent<NetworkIdentity>(out var netIdentity))
            {
                if (netIdentity.netId == 0)
                {
                    LocalDestruction();
                    return;
                }
            }

            UnSpawn();
        }
    }

    [ClientRpc]
    private void RpcStartConstruct()
    {
        gameObject.layer = 8;
    }

    [Server]
    public void StartConstruct()
    {
        isPlaced = true;

        gameObject.layer = 8;

        RpcStartConstruct();
        StartCoroutine(Construct());
    }

    private IEnumerator Construct()
    {
        while (HP < building.HP)
            yield return null;

        ConstructBuilding();
    }

    [Server]
    public void Build()
    {
        HP += constructMultiplier * Time.deltaTime;
    }

    private void ConstructBuilding()
    {
        SpawnerManager.Singlton.Spawn(building, transform.position, Quaternion.identity, PlayerID, connectionToClient);

        Destruction();
    }

    public override void ResetState()
    {
        base.ResetState();

        isPlaced = false;
        HP = 1;
    }
}