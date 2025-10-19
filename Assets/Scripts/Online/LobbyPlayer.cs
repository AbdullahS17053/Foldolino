using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{
    public static event System.Action<LobbyPlayer> OnLobbyPlayerSpawned;
    public static event System.Action<LobbyPlayer> OnLobbyPlayerDespawned;

    public NetworkVariable<FixedString32Bytes> PlayerName = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> AvatarIndex = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public NetworkVariable<int> SpawnIndex = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public Button kickButton;
    public Image icon;
    public Sprite[] avtars;

    public override void OnNetworkSpawn()
    {
        OnLobbyPlayerSpawned?.Invoke(this);

        if (IsOwner)
        {
            AvatarIndex.Value = Random.Range(0, avtars.Length);

            var lobbyManager = FindFirstObjectByType<LobbyManager>();
            if (NetworkManager.Singleton.IsHost)
                PlayerName.Value = lobbyManager.hostNameInput.text;
            else
                PlayerName.Value = lobbyManager.nameInput.text;
        }

        // Apply avatar immediately and on change
        AvatarIndex.OnValueChanged += (oldIndex, newIndex) => ApplyAvatar(newIndex);
        ApplyAvatar(AvatarIndex.Value);
    }

    private void ApplyAvatar(int index)
    {
        if (icon != null && avtars != null && index >= 0 && index < avtars.Length)
            icon.sprite = avtars[index];
    }

    public override void OnNetworkDespawn()
    {
        OnLobbyPlayerDespawned?.Invoke(this);
    }
}
