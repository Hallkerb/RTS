using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;

public class ResourcesUI : MonoBehaviour
{
    private TextMeshProUGUI food;
    private TextMeshProUGUI materials;
    private TextMeshProUGUI iron;

    public void ChangeResources(params (ResourceType type, int count)[] resources)
    {
        foreach(var resource in resources)
        {
            switch(resource.type)
            {
                case ResourceType.Food:
                    food.text = $"Food: <color={TextColors.Accent}>{resource.count}</color>";
                    break;
                case ResourceType.Materials:
                    materials.text = $"Materials: <color={TextColors.Accent}>{resource.count}</color>";
                    break;
                case ResourceType.Iron:
                    iron.text = $"Iron: <color={TextColors.Accent}>{resource.count}</color>";
                    break;
            }
        }
    }

    public void Initialize(PlayerEconomy playerEconomy)
    {
        food = transform.Find("Food_Text").GetComponent<TextMeshProUGUI>();
        materials = transform.Find("Materials_Text").GetComponent<TextMeshProUGUI>();
        iron = transform.Find("Iron_Text").GetComponent<TextMeshProUGUI>();

        ChangeResources((ResourceType.Food, playerEconomy.Food), (ResourceType.Materials, playerEconomy.Materials), (ResourceType.Iron, playerEconomy.Iron));

        playerEconomy.OnResourceChanged += ChangeResources;
    }
}
