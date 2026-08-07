using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EntityButtonUITooltip))]
public class SpawnerBuildUI : MonoBehaviour
{
    private PlayerController playerController;

    private int buildingIndex;

    void Awake()
    {
        playerController = Camera.main.GetComponent<PlayerController>();

        foreach(Transform child in transform)
        {
            if(child.TryGetComponent(out Button button))
                button.onClick.AddListener(() => SetSpawnEntity(child.GetSiblingIndex()));
        }
    }

    public void SetSpawnEntity(int index)
    {
        if(playerController.BuildingChoose.Count > 0)
        {
            if(buildingIndex >= playerController.BuildingChoose.Count)
                buildingIndex = 0;

            if(playerController.BuildingChoose[buildingIndex].TryGetComponent(out SpawnerBuild spawnerBuild))
                spawnerBuild.SetEntityToSpawn(index);
        }

        buildingIndex++;
    }
}
