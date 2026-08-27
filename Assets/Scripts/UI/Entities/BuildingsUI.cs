using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BuildingsButtonsUITooltip))]
public class BuildingsUI : MonoBehaviour
{
    [SerializeField] private List<EntitySpawnData> buildings = new List<EntitySpawnData>();

    private PlayerController playerController;

    void Awake()
    {
        playerController = Camera.main.GetComponent<PlayerController>();

        foreach(Transform child in transform)
        {
            if(child.TryGetComponent(out Button button))
                button.onClick.AddListener(() => SpawnBuilding(child.GetSiblingIndex()));
        }
    }

    public BuildingPhantom GetBuilding(int index) => buildings[index].Entity as BuildingPhantom;

    public EntitySpawnData GetSpawnData(int index) =>  buildings[index];

    public void SpawnBuilding(int index)
    {
        EntitySpawnData spawnData = buildings[index];

        if (spawnData == null || playerController.Player.Economy.EnoughResource(spawnData.GetPrice()) == false) return;

        BuildingPhantom phantom = ObjectPooler.Instance.Get(buildings[index].Entity.gameObject, (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition), Quaternion.identity).GetComponent<BuildingPhantom>();

        playerController.SetСonstructionBuilding(phantom, index);
    }
}
