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
    
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (buildingsUI == null) return;

        BuildingPhantom building = null;

        building = buildingsUI.GetBuilding(eventData.pointerEnter.transform.GetSiblingIndex());

        if (building == null) return;

        SetInfo(building.Data.Name, $"Cost: Food = <color={TextColors.Accent}>{0}</color>\n" +
                                                  $"         Materials = <color={TextColors.Accent}>{0}</color>\n" +
                                                  $"         Iron = <color={TextColors.Accent}>{0}</color>\n");

        base.OnPointerEnter(eventData);
    }
}
