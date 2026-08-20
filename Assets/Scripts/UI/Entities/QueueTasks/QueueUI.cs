using System.Collections.Generic;
using UnityEngine;

public class QueueUI : MonoBehaviour
{
    public static string[] PanelKeys { get; } = new string[3]
    {
        "Barrack",
        "House",
        "Mill"
    };

    private Dictionary<string, QueuePanel> panels = new Dictionary<string, QueuePanel>();

    void Awake()
    {
        Transform content = transform.Find("Content");

        for (int i = 0; i < content.childCount; i++)
        {
            GameObject panel = content.GetChild(i).gameObject;
            string key = panel.name.Split('_')[2].Split(' ')[0];

            panels.Add(key, panel.GetComponent<QueuePanel>());
        }
    }

    public void OpenPanel(string key, List<QueueTaskData> queue)
    {
        foreach (var panel in panels)
        {
            if (panel.Key == key)
            {
                panel.Value.gameObject.SetActive(true);
                panel.Value.UpdatePanel(queue);

                return;
            }
        }
    }

    public void ClosePanel(string key)
    {
        foreach (var panel in panels)
        {
            if (panel.Key == key)
            {
                panel.Value.gameObject.SetActive(false);
                
                return;
            }
        }
    }
}
