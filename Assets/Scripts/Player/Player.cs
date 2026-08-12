using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [SerializeField] private MenuUI menuUI;

    private PlayerController controller;
    private PlayerUnitsController unitsController;
    private PlayerEconomy economy;

    [SyncVar] private TownHall townHall;

    public event Action OnWin;
    public event Action OnLose;
    public event Action<NetworkConnectionToClient, string> OnNameChanged;

    [SyncVar]
    private int _id;
    public int ID
    {
        get => _id;
        private set => _id = value;
    }

    [SyncVar(hook = nameof(UpdatePlayerName))]
    private string _name;
    public string Name
    {
        get => _name;
        private set
        {
            if (!string.IsNullOrWhiteSpace(value))
                _name = value;
            else
                _name = $"Player {ID + 1}";
        }
    }

    [SyncVar]
    private bool _isWon;
    public bool IsWon
    {
        get => _isWon;
        private set => _isWon = value;
    }

    [SyncVar]
    private bool _isLoss;
    public bool IsLoss
    {
        get => _isLoss;
        private set => _isLoss = value;
    }

    [SyncVar]
    private bool _IsInitialized;
    public bool IsInitialized
    {
        get => _IsInitialized;
        private set => _IsInitialized = value;
    }

    public PlayerUnitsController GetPlayerUnitsController() => unitsController;

    public PlayerEconomy GetPlayerEconomy() => economy;

    void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
            menuUI = FindFirstObjectByType<MenuUI>();
        else
        {
            unitsController = GetComponent<PlayerUnitsController>();
            economy = GetComponent<PlayerEconomy>();
            controller = FindFirstObjectByType<PlayerController>();
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        if (SceneManager.GetActiveScene().name == "Game")
            controller.Initialize(this, unitsController);
    }

    private void SetCameraPosition(Vector2 pos)
    {
        if (controller != null)
            controller.SetCameraPosition(pos);
        else
            Debug.LogWarning($"controller == null");
    }

    [Server]
    public void SetID(int id) => ID = id;

    [Server]
    public void SetName(string newName) => Name = newName;

    [Command]
    public void CmdSetName(string newName) => Name = newName;

    private void UpdatePlayerName(string oldName, string newName)
    {
        if (menuUI != null && menuUI.Lobby != null)
            menuUI.Lobby.UpdatePlayersName(ID, newName);

        if (isServer)
            OnNameChanged?.Invoke(connectionToClient, newName);
    }

    [TargetRpc]
    public void Connect(NetworkConnection target)
    {
        if (menuUI != null)
        {
            CmdSetName(menuUI.NameInputField.text);

            menuUI.NameInputField.onEndEdit.AddListener(CmdSetName);

            menuUI.ConnectLobby(StopClient);
        }
    }

    public void PlaceConstruct(bool resetTasks, int index, Vector2 pos) => CmdPlaceConstruct(controller.UnitChoose.OfType<Unit>().ToList(), resetTasks, index, pos);

    [Command]
    private void CmdPlaceConstruct(List<Unit> unitChoose, bool resetTasks, int index, Vector2 pos)
    {
        var phantomPrefub = controller.UserInterface.BuildingsUI.GetBuilding(index);
        BuildingPhantom newBuilding = SpawnerManager.Singlton.Spawn(phantomPrefub, pos, Quaternion.identity, ID, connectionToClient) as BuildingPhantom;

        unitsController.SetUnitsBuilding(unitChoose, newBuilding, ID, resetTasks);
        newBuilding.StartConstruct();
    }

    public void DeleteEntity() => CmdDeleteEntity(controller.UnitChoose, controller.BuildingChoose, controller.ConstructionChoose);

    [Command]
    private void CmdDeleteEntity(List<Entity> unitChoose, List<Entity> buildingChoose, List<Entity> ConstructionChoose)
    {
        var safeUnits = unitChoose.Where(u => u != null).ToArray();
        var safeBuildings = buildingChoose.Where(b => b != null).ToArray();
        var safeConstruction = ConstructionChoose.Where(c => c != null).ToArray();

        foreach (var unit in safeUnits)
            unit.Destruction();

        foreach (var building in safeBuildings)
            building.Destruction();

        foreach (var construction in safeConstruction)
            construction.Destruction();
    }

    [TargetRpc]
    public void SetTown(NetworkConnection target, TownHall th)
    {
        townHall = th;

        SetCameraPosition(townHall.transform.position);
    }

    [Server]
    public void Initialize(int id, string name)
    {
        SetID(id);
        SetName(name);
        Connect(connectionToClient);

        IsInitialized = true;
    }

    [Server]
    public void Win()
    {
        IsWon = true;

        RpcWin();
    }

    [ClientRpc]
    void RpcWin()
    {
        OnWin?.Invoke();
    }

    [Server]
    public void Lose(Entity entity)
    {
        IsLoss = true;

        RpcLose();
    }

    [ClientRpc]
    void RpcLose()
    {
        OnLose?.Invoke();
    }

    public void StopClient()
    {
        if (NetworkManager.singleton.isNetworkActive)
        {
            if (NetworkServer.active)
                NetworkManager.singleton.StopHost();
            else
                NetworkManager.singleton.StopClient();
        }
    }

    public void ExitFromServer()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
            SceneManager.LoadScene("MainMenu");
    }
}