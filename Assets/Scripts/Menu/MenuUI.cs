using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public Lobby Lobby { get; private set; }

    private Button hostButton;
    private Button joinButton;
    private Button leaveButton;
    private Button exitButton;

    private TMP_InputField ipInputField;
    public TMP_InputField NameInputField { get; private set; }

    void Awake()
    {
        Lobby = transform.Find("Lobby_Panel").GetComponent<Lobby>();

        Transform buttons = transform.Find("Buttons");

        hostButton = buttons.Find("CreateLobby_Button").GetComponent<Button>();
        joinButton = buttons.Find("JoinLobby_Button").GetComponent<Button>();
        leaveButton = buttons.Find("LeaveLobby_Button").GetComponent<Button>();
        exitButton = buttons.Find("Exit_Button").GetComponent<Button>();

        ipInputField = joinButton.transform.Find("Background").Find("SearchLobby_InputField").GetComponent<TMP_InputField>();
        NameInputField = buttons.Find("Name_Background").Find("SearchLobby_InputField").GetComponent<TMP_InputField>();

        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
        leaveButton.onClick.AddListener(UpdateMenu);
        exitButton.onClick.AddListener(Exit);
    }

    public void StartHost()
    {
        NetworkManager.singleton.StartHost();
        Debug.Log("Host started");
    }

    public void StartClient()
    {
        if (ipInputField.text.Length > 0)
        {
            NetworkManager.singleton.networkAddress = ipInputField.text;

            NetworkManager.singleton.StartClient();

            Debug.Log("Client started connecting to " + ipInputField.text);
        }
    }

    public void UpdateMenu()
    {
        Lobby.gameObject.SetActive(NetworkManager.singleton.isNetworkActive);
        
        SwitchButtons();
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void ConnectLobby(UnityAction stopclient)
    {
        leaveButton.onClick.AddListener(stopclient);

        UpdateMenu();
    }

    private void SwitchButtons()
    {
        hostButton.gameObject.SetActive(!hostButton.gameObject.activeSelf);
        joinButton.gameObject.SetActive(!joinButton.gameObject.activeSelf);
        leaveButton.gameObject.SetActive(!leaveButton.gameObject.activeSelf);
    }
}
