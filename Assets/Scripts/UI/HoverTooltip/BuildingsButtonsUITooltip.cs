using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingsButtonsUITooltip : UIHoverTooltip
{
    private BuildingsUI buildingsUI;

    protected override void Awake()
    {
        base.Awake();

        buildingsUI = GetComponent<BuildingsUI>();
    }

    protected override bool TrySetInfo(PointerEventData eventData)
    {
        if (base.TrySetInfo(eventData) == false || buildingsUI == null) return false;

        int index = eventData.pointerCurrentRaycast.gameObject.transform.GetSiblingIndex();

        EntitySpawnData spawnData = buildingsUI.GetSpawnData(index);

        if (spawnData == null) return false;

        BuildingPhantom building = spawnData.Entity as BuildingPhantom;

        int foodCost = spawnData.GetPrice(ResourceType.Food);
        int materialsCost = spawnData.GetPrice(ResourceType.Materials);
        int ironCost = spawnData.GetPrice(ResourceType.Iron);

        SetInfo(building.Data.Name, $"Cost: Food = <color={TextColors.Accent}>{foodCost}</color>\n" +
                                                  $"         Materials = <color={TextColors.Accent}>{materialsCost}</color>\n" +
                                                  $"         Iron = <color={TextColors.Accent}>{ironCost}</color>\n");

        return true;
    }
}
