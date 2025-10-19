using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerDataStore : MonoBehaviour
{
    public static PlayerDataStore Instance;

    public List<PlayerData> players = new List<PlayerData>();
    public List<int> assignedCreatureIndices = new();
    public bool isHostPremium = false;
    public int mCreatureIndex = -1;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveFromLobby(List<LobbyPlayer> lobbyPlayers)
    {
        players.Clear();
        foreach (var p in lobbyPlayers)
        {
            players.Add(new PlayerData
            {
                ClientId = p.OwnerClientId,
                Name = p.PlayerName.Value.ToString(),
                AvatarIndex = p.AvatarIndex.Value,
                SpawnIndex = p.SpawnIndex.Value
            });
        }
    }
}

[System.Serializable]
public class PlayerData: INetworkSerializable
{
    public ulong ClientId;
    public FixedString32Bytes Name;
    public int AvatarIndex;
    public int SpawnIndex;
    public int CreatureIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref SpawnIndex);
        serializer.SerializeValue(ref CreatureIndex);
        serializer.SerializeValue(ref Name);
    }
}
