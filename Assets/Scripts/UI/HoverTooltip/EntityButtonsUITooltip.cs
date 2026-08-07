using UnityEngine;
using UnityEngine.EventSystems;

public class EntityButtonUITooltip : UIHoverTooltip
{
    public override void OnPointerEnter(PointerEventData eventData)
    {
        EntitySpawnData entitySpawnData = null;

        if (playerController.BuildingChoose.Count > 0)
        {
            if (playerController.BuildingChoose[0].TryGetComponent(out SpawnerBuild spawnerBuild))
                entitySpawnData = spawnerBuild.GetEntitySpawnData(eventData.pointerEnter.transform.GetSiblingIndex());
        }

        if (entitySpawnData == null) return;

        int foodCost = entitySpawnData.GetPrice(ResourceType.Food);
        int materialsCost = entitySpawnData.GetPrice(ResourceType.Materials);
        int ironCost = entitySpawnData.GetPrice(ResourceType.Iron);
        float timeToSpawn = entitySpawnData.TimeToSpawn;

        SetInfo(entitySpawnData.Entity.Data.Name, $"Cost: Food = <color={TextColors.Accent}>{foodCost}</color>\n" +
                                                  $"         Materials = <color={TextColors.Accent}>{materialsCost}</color>\n" +
                                                  $"         Iron = <color={TextColors.Accent}>{ironCost}</color>\n" +
                                                  $"Time to spawn = <color={TextColors.Accent}>{timeToSpawn}s</color>");

        base.OnPointerEnter(eventData);
    }
}
