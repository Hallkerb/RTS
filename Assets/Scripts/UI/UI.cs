using System.Collections.Generic;
using UnityEngine;

public class UI : MonoBehaviour
{
    public static string[] PanelKeys { get; } = new string[4]
    {
        "Buildings",
        "Barrack",
        "House",
        "Mill"
    };

    [HideInInspector] public BuildingsUI BuildingsUI { get; private set; }

    private Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

    public GameObject exitGamePanel { get; private set; }
    public GameObject lossPanel { get; private set; }
    public GameObject victoryPanel { get; private set; }

    void Awake()
    {
        BuildingsUI = transform.Find("Bottom_Panel").Find("Panels").GetComponentInChildren<BuildingsUI>();

        exitGamePanel = Camera.main.transform.Find("UI").Find("Exit_Panel").gameObject;
        lossPanel = transform.Find("Loss_Panel").gameObject;
        victoryPanel = transform.Find("Victory_Panel").gameObject;

        Transform panelsParent = transform.Find("Bottom_Panel").Find("Panels").Find("Content");

        for (int i = 0; i < panelsParent.childCount; i++)
        {
            GameObject panel = panelsParent.GetChild(i).gameObject;

            panels.Add(panel.name.Split('_')[0], panel);
        }
    }

    public void OpenPanel(string key = "Buildings")
    {
        foreach (var panel in panels)
        {
            if (panel.Key == key)
                panel.Value.SetActive(true);
            else
                panel.Value.SetActive(false);
        }
    }

    public void OpenWinUI()
    {
        transform.Find("Bottom_Panel").gameObject.SetActive(false);
        victoryPanel.SetActive(true);
    }

    public void OpenLoseUI()
    {
        transform.Find("Bottom_Panel").gameObject.SetActive(false);
        lossPanel.SetActive(true);
    }
}
