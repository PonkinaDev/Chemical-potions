using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

// SRP: solo maneja conexión y ciclo de vida de la red
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef _playerPrefab;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    private void Awake()
    {
        // Singleton simple
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─── Métodos públicos que llama la UI ────────────────────────────────────

    public async void StartHost()
    {
        await LaunchRunner(GameMode.Host);
    }

    public async void StartClient()
    {
        await LaunchRunner(GameMode.Client);
    }

    // ─── Launch ──────────────────────────────────────────────────────────────

private async System.Threading.Tasks.Task LaunchRunner(GameMode mode)
{
    // Si ya existe un runner, lo apagamos primero
    if (_runner != null)
    {
        await _runner.Shutdown();
        // Esperamos un frame para que Unity limpie
        await System.Threading.Tasks.Task.Delay(100);
    }

    // Verificamos que el gameObject siga vivo
    if (this == null || gameObject == null)
    {
        Debug.LogError("[NetworkManager] gameObject destruido durante Shutdown.");
        return;
    }

    // Creamos el runner limpio
    _runner = gameObject.AddComponent<NetworkRunner>();
    _runner.ProvideInput = true;

    // Creamos el SceneManager por separado antes del StartGameArgs
    var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

    var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

    var args = new StartGameArgs
    {
        GameMode     = mode,
        SessionName  = "ProtocolPanic",
        Scene        = scene,
        SceneManager = sceneManager
    };

    var result = await _runner.StartGame(args);

    if (result.Ok)
    {
        Debug.Log($"[NetworkManager] Conectado como: {mode}");
    }
    else
    {
        Debug.LogError($"[NetworkManager] Falló la conexión: {result.ErrorMessage}");
    }
}

    // ─── INetworkRunnerCallbacks ─────────────────────────────────────────────
    // IMPORTANTE: firma exacta requerida por Fusion 2

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Solo el Host hace spawn de jugadores
        if (!runner.IsServer) return;

        Vector3 spawnPos = player.RawEncoded == runner.LocalPlayer.RawEncoded
            ? new Vector3(0, 1, 0)
            : new Vector3(2, 1, 0);

        NetworkObject obj = runner.Spawn(
            _playerPrefab,
            spawnPos,
            Quaternion.identity,
            player  // <-- asigna InputAuthority al jugador
        );

        _spawnedPlayers[player] = obj;
        Debug.Log($"[NetworkManager] Jugador {player} hizo spawn.");
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
        {
            runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
    }

    // Fusion 2 requiere TODOS estos métodos aunque estén vacíos
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}