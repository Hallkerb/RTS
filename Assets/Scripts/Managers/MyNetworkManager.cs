using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    private class PlayerData
    {
        public int ID = 0;
        public string Name = "Player";

        public PlayerData(int id, string name)
        {
            ID = id;
            Name = name;
        }
    }

    private Dictionary<NetworkConnectionToClient, PlayerData> playersData = new Dictionary<NetworkConnectionToClient, PlayerData>();

    [SerializeField] private SpawnerManager spawnerManagerPrefub;
    [SerializeField] private TownHall townHallPrefub;
    
    private TownHall[] spawnedTownHalls;

    public static event Action<int> OnPlayersCountChanged;

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public override void OnStopServer()
    {
        foreach(var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;

            var player = conn.identity.GetComponent<Player>();
            player.OnLose -= PlayerLoss;
            player.OnNameChanged -= HandlePlayerNameChanged;
        }

        base.OnStopServer();

        playersData = new Dictionary<NetworkConnectionToClient, PlayerData>();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            var player = conn.identity.GetComponent<Player>();

            player.Initialize(conn.connectionId, $"Player {conn.connectionId + 1}");
            playersData.Add(conn, new PlayerData(player.ID, player.Name));
            player.OnNameChanged += HandlePlayerNameChanged;

            Debug.Log($"Add Player, player name: {player.Name}; player ID: {player.ID}");
        }
        else
        {
            var player = conn.identity.GetComponent<Player>();

            if (playersData.TryGetValue(conn, out PlayerData data))
                player.Initialize(data.ID, data.Name);
            else
            {
                player.Initialize(conn.connectionId, $"Player {conn.connectionId + 1}");
                playersData.Add(conn, new PlayerData(player.ID, player.Name));
            }

            player.OnLose += PlayerLoss;

            Debug.Log($"Add Player, player name: {player.Name}; player ID: {player.ID}");
        }

        OnPlayersCountChanged?.Invoke(NetworkServer.connections.Count);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        var player = conn.identity.GetComponent<Player>();

        if (player != null)
        {
            player.OnLose -= PlayerLoss;
            player.OnNameChanged -= HandlePlayerNameChanged;
        }
        
        base.OnServerDisconnect(conn);

        PlayerLoss();

        OnPlayersCountChanged?.Invoke(NetworkServer.connections.Count);
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        var localPlayer = NetworkClient.localPlayer?.GetComponent<Player>();

        if (localPlayer != null)
            localPlayer.ExitFromServer();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            if (NetworkServer.active && SpawnerManager.Instance == null)
            {
                var sm = Instantiate(spawnerManagerPrefub, transform.position, Quaternion.identity);
                NetworkServer.Spawn(sm.gameObject);
            }

            InitializeTowns();
        }
    }

    [Server]
    public void StartGame()
    {
        ServerChangeScene("Game");
    }

    [Server]
    private void InitializeTowns()
    {
        Floor floor = Floor.Instance;

        if (floor == null)
        {
            Debug.LogError("Floor == null!");
            return;
        }

        Debug.Log($"InitializeTowns, players: {NetworkServer.connections.Count}");

        spawnedTownHalls = new TownHall[NetworkServer.connections.Count];

        StartCoroutine(SpawnTownsCor(floor));
        StartCoroutine(InitializeTownsCor());
    }

    private IEnumerator SpawnTownsCor(Floor floor)
    {
        while (floor.GridActive == false) yield return null;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null) continue;

            Transform playersSpawnPos = floor.PlayersSpawnPos[conn.connectionId];
            
            var th = SpawnerManager.Instance.Spawn(townHallPrefub.GetComponent<Entity>(), playersSpawnPos.position, playersSpawnPos.rotation, conn).GetComponent<TownHall>();
            spawnedTownHalls[conn.connectionId] = th;
        }
    }

    private IEnumerator InitializeTownsCor()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn == null) continue;

            while (conn.identity == null || spawnedTownHalls[conn.connectionId] == null) yield return null;

            var player = conn.identity.GetComponent<Player>();
            spawnedTownHalls[conn.connectionId].Initialize(player);
            player.SetTown(conn, spawnedTownHalls[conn.connectionId]);
        }
    }

    public void HandlePlayerNameChanged(NetworkConnectionToClient conn, string newName)
    {
        playersData[conn].Name = newName;
    }

    private void PlayerLoss()
    {
        Player winner = null;
        int lossCount = 0;
        int activePlayers = 0;

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null) continue;

            var player = conn.identity.GetComponent<Player>();
            activePlayers++;

            if (player.IsLoss)
                lossCount++;
            else
                winner = player;
        }

        if (winner != null && lossCount == activePlayers - 1)
            winner.Win();
    }
}
