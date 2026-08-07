using System.Net;
using System.Net.Sockets;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lobby : NetworkBehaviour
{
    private MyNetworkManager myNetworkManager;

    private GameObject[] playerSlots;

    private TextMeshProUGUI hostIPText;
    private string ip;

    private Button start;

    void Awake()
    {
        myNetworkManager = FindFirstObjectByType<MyNetworkManager>();

        Transform playerSlotsPanel = transform.Find("PlayerSlots");

        playerSlots = new GameObject[playerSlotsPanel.childCount];

        for (int i = 0; i < playerSlotsPanel.childCount; i++)
            playerSlots[i] = playerSlotsPanel.GetChild(i).gameObject;

        hostIPText = transform.Find("IP_Text").GetComponent<TextMeshProUGUI>();

        start = transform.Find("Start_Button").GetComponent<Button>();

        start.onClick.AddListener(myNetworkManager.StartGame);
    }

    void OnEnable()
    {
        start.gameObject.SetActive(NetworkServer.active);

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                string candidate = ip.ToString();

                if (candidate.StartsWith("26.")) // Radmin VPN
                {
                    this.ip = candidate;
                    hostIPText.text = $"Host IP: {ip.ToString()}";
                }
            }
        }

        MyNetworkManager.OnPlayersCountChanged += UpdatePlayersCount;
    }

    void OnDisable()
    {
        MyNetworkManager.OnPlayersCountChanged -= UpdatePlayersCount;
    }

    [ClientRpc]
    public void UpdatePlayersCount(int count)
    {
        for (int i = 0; i < playerSlots.Length; i++)
            playerSlots[i].SetActive(i < count);

        start.interactable = count >= 1;
    }

    public void UpdatePlayersName(int id, string newName)
    {
        Debug.Log($"id: {id}, new name: {newName}");
        
        TextMeshProUGUI nameText = playerSlots[id].GetComponentInChildren<TextMeshProUGUI>();
        nameText.text = newName;
    }

    public void CopyIP()
    {
        GUIUtility.systemCopyBuffer = ip;
    }
}