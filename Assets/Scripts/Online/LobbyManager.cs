using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("UI")]
    public TMP_InputField nameInput, hostNameInput;
    public TMP_InputField joinCodeInput;
    public TMP_Text joinCodeText;
    public Button startGameButton;

    [Header("Mystery Creature")]
    public AdvancedDropdown lobbyCreatureDropDown;

    [Header("Lobby UI")]
    public GameObject lobbyPlayerPrefab;
    public GameObject lobbyPanel;
    public GameObject mainMenuPanel;
    public GameObject hostPanel;
    public GameObject hostWaitPanel;
    public GameObject joinPanel;
    public GameObject joinWaitPanel;
    public GameObject copiedPopUp;
    public Transform prefabSpawnParent;

    [Header("Relay Settings")]
    public int maxPlayers = 8;

    private UnityTransport _transport;
    private int connectedPlayers = 0;
    private string currentCode = "";
    private Dictionary<ulong, GameObject> lobbyPlayers = new();

    #region UNITY LIFECYCLE

    private async void Start()
    {
        _transport = NetworkManager.Singleton.GetComponent<UnityTransport>() ??
                     FindFirstObjectByType<UnityTransport>();

        if (_transport == null)
        {
            return;
        }

        if (startGameButton != null)
            startGameButton.interactable = false;

        lobbyPanel.SetActive(false);
        PlayerDataStore.Instance.players.Clear();
        PlayerDataStore.Instance.assignedCreatureIndices.Clear();
        PlayerDataStore.Instance.mCreatureIndex = -1;

        await InitializeUnityServices();
    }

    private void OnEnable()
    {
        LobbyPlayer.OnLobbyPlayerSpawned += HandleLobbyPlayerSpawned;
        LobbyPlayer.OnLobbyPlayerDespawned += HandleLobbyPlayerDespawned;

        StartCoroutine(SubscribeNetworkCallbacksNextFrame());
    }

    private IEnumerator SubscribeNetworkCallbacksNextFrame()
    {
        yield return null;
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        LobbyPlayer.OnLobbyPlayerSpawned -= HandleLobbyPlayerSpawned;
        LobbyPlayer.OnLobbyPlayerDespawned -= HandleLobbyPlayerDespawned;
    }
    #endregion

    #region INITIALIZATION
    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    #endregion

    #region BUTTON ACTIONS
    public void HostButtonPressed() => _ = HostGameAsync();
    public void JoinButtonPressed()
    {
        string code = joinCodeInput != null ? joinCodeInput.text.Trim() : string.Empty;
        if (!string.IsNullOrEmpty(code)) _ = JoinGameAsync(code);
    }
    #endregion

    #region HOST / JOIN
    private async Task HostGameAsync()
    {
        try
        {
            hostWaitPanel.SetActive(true);
            Allocation hostAllocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(hostAllocation.AllocationId);

            joinCodeText.text = relayJoinCode;
            currentCode = joinCodeText.text;
            _transport.SetRelayServerData(AllocationUtils.ToRelayServerData(hostAllocation, "dtls"));

            if (!NetworkManager.Singleton.StartHost()) return;

            lobbyPanel.SetActive(true);
            hostPanel.SetActive(false);
            hostWaitPanel.SetActive(false);
            startGameButton.gameObject.SetActive(true);

            PlayerDataStore.Instance.isHostPremium = true;
            if (PlayerDataStore.Instance.isHostPremium)
                lobbyCreatureDropDown.gameObject.SetActive(true);

            connectedPlayers = 1;
            UpdateStartButtonState();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    private async Task JoinGameAsync(string joinCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(joinCode) || joinCode.Length < 6)
            {
                return;
            }
            joinWaitPanel.SetActive(true);

            JoinAllocation joinAllocation;
            try
            {
                joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            }
            catch (RelayServiceException re)
            {
                joinWaitPanel.SetActive(false);
                return;
            }

            if (joinCodeText != null)
                joinCodeText.text = joinCode;

            currentCode = joinCode;

            _transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

            if (!NetworkManager.Singleton.StartClient()) return;

            lobbyPanel.SetActive(true);
            joinPanel.SetActive(false);
            joinWaitPanel.SetActive(false);
            joinCodeInput.text = "";
            startGameButton.gameObject.SetActive(false); // clients never see start button
            lobbyCreatureDropDown.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void LeaveLobby()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();

            foreach (var kvp in lobbyPlayers)
                Destroy(kvp.Value);
            lobbyPlayers.Clear();

            connectedPlayers = 0;

            lobbyPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();

            foreach (var kvp in lobbyPlayers)
                Destroy(kvp.Value);
            lobbyPlayers.Clear();

            connectedPlayers = 0;

            lobbyPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
    }

    #endregion

    #region CLIENT CONNECTION EVENTS
    private void HandleClientConnected(ulong clientId)
    {
        connectedPlayers++;

        if (NetworkManager.Singleton.IsHost)
        {
            int index = 0;
            foreach (var kvp in NetworkManager.Singleton.ConnectedClientsList)
            {
                LobbyPlayer lobbyPlayer = kvp.PlayerObject.GetComponent<LobbyPlayer>();
                if (lobbyPlayer != null)
                    lobbyPlayer.SpawnIndex.Value = index++;
            }
        }

        UpdateStartButtonState();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (lobbyPlayers.TryGetValue(clientId, out var ui))
        {
            Destroy(ui);
            lobbyPlayers.Remove(clientId);
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            connectedPlayers = Mathf.Max(0, connectedPlayers - 1);
            UpdateStartButtonState();
            return;
        }
        StartCoroutine(DelayedCheckForLocalDisconnect(clientId));
    }

    private IEnumerator DelayedCheckForLocalDisconnect(ulong clientId)
    {
        yield return null;

        try
        {
            if (NetworkManager.Singleton == null)
            {
                HandleLocalClientLostConnection();
                yield break;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
            {
                HandleLocalClientLostConnection();
                yield break;
            }

            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                HandleLocalClientLostConnection();
                yield break;
            }

        }
        catch (Exception ex)
        {
            HandleLocalClientLostConnection();
        }
    }

    private void HandleLocalClientLostConnection()
    {
        try
        {
            if (NetworkManager.Singleton != null &&
                (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error shutting down NetworkManager during local disconnect cleanup: {ex}");
        }

        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

        foreach (var kvp in lobbyPlayers)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        lobbyPlayers.Clear();

        connectedPlayers = 0;
        UpdateStartButtonState();
    }
    #endregion

    #region LOBBY PLAYER HANDLERS
    private void HandleLobbyPlayerSpawned(LobbyPlayer lobbyPlayer)
    {
        GameObject uiObj = Instantiate(lobbyPlayerPrefab, prefabSpawnParent);
        uiObj.name = $"LobbyUI_{lobbyPlayer.OwnerClientId}";
        lobbyPlayers[lobbyPlayer.OwnerClientId] = uiObj;

        LobbyPlayerUI ui = uiObj.GetComponent<LobbyPlayerUI>();
        if (ui == null)
        {
            Debug.LogError("LobbyPlayerUI missing on UI prefab!");
            return;
        }

        ui.SetName(lobbyPlayer.PlayerName.Value.ToString());
        ui.SetAvatar(lobbyPlayer.AvatarIndex.Value < lobbyPlayer.avtars.Length
                     ? lobbyPlayer.avtars[lobbyPlayer.AvatarIndex.Value]
                     : null);

        lobbyPlayer.PlayerName.OnValueChanged += (oldVal, newVal) => ui.SetName(newVal.ToString());
        lobbyPlayer.AvatarIndex.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal >= 0 && newVal < lobbyPlayer.avtars.Length)
                ui.SetAvatar(lobbyPlayer.avtars[newVal]);
        };

        bool canKick = NetworkManager.Singleton.IsHost && lobbyPlayer.OwnerClientId != NetworkManager.Singleton.LocalClientId;
        ui.ShowKickButton(canKick, () => KickPlayer(lobbyPlayer.OwnerClientId));

        if (lobbyPlayer.IsOwner && nameInput != null)
            lobbyPlayer.PlayerName.Value = nameInput.text;
    }

    private void HandleLobbyPlayerDespawned(LobbyPlayer lobbyPlayer)
    {
        if (lobbyPlayers.TryGetValue(lobbyPlayer.OwnerClientId, out var ui))
        {
            Destroy(ui);
            lobbyPlayers.Remove(lobbyPlayer.OwnerClientId);
        }
    }
    public void StartGame()
    {
        PlayerDataStore.Instance.players.Clear();
        foreach (var lobbyPlayer in FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None))
        {
            PlayerDataStore.Instance.players.Add(new PlayerData
            {
                ClientId = lobbyPlayer.OwnerClientId,
                SpawnIndex = lobbyPlayer.SpawnIndex.Value,
                Name = lobbyPlayer.PlayerName.Value.ToString()
            });
        }

        foreach (var player in FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None))
        {
            if (player.NetworkObject != null && player.NetworkObject.IsSpawned)
                player.NetworkObject.Despawn(true);
        }

        if (PlayerDataStore.Instance.isHostPremium)
        {
            PlayerDataStore.Instance.mCreatureIndex = lobbyCreatureDropDown.value;
        }

        NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerGameplay", LoadSceneMode.Single);
    }


    #endregion


    #region HELPERS
    private void UpdateStartButtonState()
    {
        if (startGameButton != null)
            startGameButton.interactable = connectedPlayers >= 2;
    }

    public void KickPlayer(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
        else
        {
            Debug.Log($"No player with ClientId {clientId} found.");
        }
    }

    public void CopyToClipBoard()
    {
        GUIUtility.systemCopyBuffer = currentCode;
        StartCoroutine(DisplayMessage());
    }
    bool isDisplaying = false;
    IEnumerator DisplayMessage()
    {
        if (!isDisplaying)
        {
            isDisplaying = true;
            copiedPopUp.SetActive(true);
            yield return new WaitForSeconds(1);
            copiedPopUp.SetActive(false);
            isDisplaying = false;
        }
    }
    #endregion
}

