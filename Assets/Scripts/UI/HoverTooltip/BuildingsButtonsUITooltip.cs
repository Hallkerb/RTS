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

        BuildingPhantom building = null;

        building = buildingsUI.GetBuilding(eventData.pointerCurrentRaycast.gameObject.transform.GetSiblingIndex());

        if (building == null) return false;

        SetInfo(building.Data.Name, $"Cost: Food = <color={TextColors.Accent}>{0}</color>\n" +
                                                  $"         Materials = <color={TextColors.Accent}>{0}</color>\n" +
                                                  $"         Iron = <color={TextColors.Accent}>{0}</color>\n");

        return true;
    }
}
