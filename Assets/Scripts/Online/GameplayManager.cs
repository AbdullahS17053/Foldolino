using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Gley.Localization;

public class GameplayManager : NetworkBehaviour
{
    [Header("Prefabs & Setup")]
    public GameObject drawingSurfacePrefab;
    public Transform parent;
    public float xSpacing = 3f;

    public List<DrawingSurface> allSurfaces = new();
    private List<PlayerDrawingState> playerStates = new();

    [Header("Camera")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 10, -10);
    [SerializeField] private float cameraMoveDuration = 0.2f;
    [SerializeField] private int totalRounds = 5;

    private List<Vector3> cameraPositions = new();

    private int roundCounter = 1;
    public int currentSurfaceIndex;

    private HashSet<ulong> completedClientIds = new HashSet<ulong>();
    private HashSet<int> completedSurfacesThisRound = new();

    private Dictionary<int, List<MultiLineChunk>> playerChunkBuffer = new();
    private Dictionary<int, List<MultiLineChunk>> broadcastChunkBuffer = new();
    private Dictionary<int, List<MultiLineChunk>> serverChunkBuffer = new();
    private const int MaxPointsPerChunk = 200;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Host will send its player list to all clients
            InitializeServerPlayerStates();

            if (PlayerDataStore.Instance.isHostPremium && PlayerDataStore.Instance.mCreatureIndex == 1)
            {
                AssignCreatureIndices();
            }
            var allData = PlayerDataStore.Instance.players.ToArray();
            var indices = PlayerDataStore.Instance.assignedCreatureIndices.ToArray();
            SendPlayerDataClientRpc(allData, indices);
        }

        if (IsClient)
        {
            StartCoroutine(WaitForPlayerDataAndSpawn());
        }
    }

    private void AssignCreatureIndices()
    {
        int count = UIManager.Instance.mysteryCreatureBgs.Count;
        int playerCount = PlayerDataStore.Instance.players.Count;

        if (count < playerCount)
        {
            Debug.LogError("[GameplayManager] Not enough creature indices for players!");
            return;
        }

        // create shuffled list of available indices
        List<int> available = Enumerable.Range(0, count).OrderBy(_ => Random.value).ToList();

        PlayerDataStore.Instance.assignedCreatureIndices.Clear();
        PlayerDataStore.Instance.assignedCreatureIndices.AddRange(available.Take(playerCount));

        // assign by spawnIndex ordering
        foreach (var player in PlayerDataStore.Instance.players)
        {
            player.CreatureIndex = PlayerDataStore.Instance.assignedCreatureIndices[player.SpawnIndex];
        }
    }

    [ClientRpc]
    private void SendPlayerDataClientRpc(PlayerData[] allPlayers, int[] assignedIndices)
    {
        PlayerDataStore.Instance.players.Clear();
        PlayerDataStore.Instance.players.AddRange(allPlayers);

        PlayerDataStore.Instance.assignedCreatureIndices.Clear();
        PlayerDataStore.Instance.assignedCreatureIndices.AddRange(assignedIndices);
    }
    private IEnumerator WaitForPlayerDataAndSpawn()
    {
        while (PlayerDataStore.Instance.players.Count == 0)
            yield return null;

        InitializePlayerStates();
        SpawnDrawingSurfaces();
    }
    private void InitializePlayerStates()
    {
        playerStates.Clear();
        foreach (var p in PlayerDataStore.Instance.players)
        {
            playerStates.Add(new PlayerDrawingState
            {
                SurfaceIndex = p.SpawnIndex,
                IsDone = false,
                DrawnPoints = null,
                DrawnColors = null,
                OwnerClientId = p.ClientId,
                PlayerName = p.Name.ToString(),
            });
        }
    }

    private void InitializeServerPlayerStates()
    {
        playerStates.Clear();

        if (PlayerDataStore.Instance == null || PlayerDataStore.Instance.players == null)
        {
            return;
        }

        foreach (var p in PlayerDataStore.Instance.players)
        {
            playerStates.Add(new PlayerDrawingState
            {
                SurfaceIndex = p.SpawnIndex,
                IsDone = false,
                DrawnPoints = null,
                DrawnColors = null,
                OwnerClientId = p.ClientId,
                PlayerName = p.Name.ToString(),
            });
        }
    }

    private void SpawnDrawingSurfaces()
    {
        var players = PlayerDataStore.Instance.players;
        int localIndex = GetLocalPlayerIndex();

        for (int i = 0; i < players.Count; i++)
        {
            float xPos = i * xSpacing;
            Vector3 spawnPos = new Vector3(xPos, 0, 0);

            GameObject surfaceObj = Instantiate(drawingSurfacePrefab, spawnPos, Quaternion.identity, parent);
            DrawingSurface surface = surfaceObj.GetComponent<DrawingSurface>();

            surface.OwnerIndex = players[i].SpawnIndex;
            cameraPositions.Add(spawnPos + cameraOffset);

            bool isLocal = (surface.OwnerIndex == localIndex);
            surface.gameObject.SetActive(true);
            surface.lineDrawer.canDraw = isLocal;

            allSurfaces.Add(surface);
        }

        currentSurfaceIndex = localIndex;
        MoveCameraToSurface(localIndex, instant: true);
        if (PlayerDataStore.Instance.assignedCreatureIndices.Count != 0)
            UIManager.Instance.UpdateTitleText(PlayerDataStore.Instance.assignedCreatureIndices[currentSurfaceIndex]);
    }

    private void MoveCameraToSurface(int surfaceIndex, bool instant = false)
    {
        Vector3 baseTarget = cameraPositions[surfaceIndex];

        // apply round-based vertical shift
        float totalYOffset = -4.5f * (roundCounter - 1);
        Vector3 targetPos = baseTarget + new Vector3(0, totalYOffset, 0);

        if (instant)
        {
            cameraRoot.position = targetPos;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(MoveCameraSmooth(targetPos));
        }
    }

    private IEnumerator MoveCameraSmooth(Vector3 targetPos)
    {
        float elapsed = 0;
        Vector3 startPos = cameraRoot.position;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            cameraRoot.position = Vector3.Lerp(startPos, targetPos, elapsed / cameraMoveDuration);
            yield return null;
        }

        cameraRoot.position = targetPos;
    }

    private int GetLocalPlayerIndex()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        var player = PlayerDataStore.Instance.players.FirstOrDefault(p => p.ClientId == localId);

        return player != null ? player.SpawnIndex : -1;
    }

    public void OnCompletePartPressed()
    {
        if (UIManager.Instance.isColorPalletEnabled)
        {
            UIManager.Instance.EnableColorPallet();
        }
        else
        {
            DrawingSurface mySurface = allSurfaces.Find(s => s.OwnerIndex == currentSurfaceIndex);
            if (mySurface == null)
            {
                return;
            }

            if (!mySurface.lineDrawer.hasDrawn)
            {
                UIManager.Instance.ShowDrawMessage();
                return;
            }

            mySurface.lineDrawer.hasDrawn = false;
            MultiLineData myData = mySurface.GetDrawnLinesData();
            var chunks = SplitIntoChunks(currentSurfaceIndex, myData);

            foreach (var chunk in chunks)
                SubmitDrawingChunkServerRpc(chunk);

            //if (!IsServer)
            //{
            mySurface.lineDrawer.ClearAllLines();
            mySurface.lineDrawer.RemoveSaves();
            mySurface.lineDrawer.lineColor = Color.black;
            //}
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitDrawingChunkServerRpc(MultiLineChunk chunk, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // Buffer this chunk for the surface
        if (!serverChunkBuffer.ContainsKey(chunk.SurfaceIndex))
            serverChunkBuffer[chunk.SurfaceIndex] = new List<MultiLineChunk>();

        serverChunkBuffer[chunk.SurfaceIndex].Add(chunk);

        // Only act when we've received all chunks for this surface submission
        if (serverChunkBuffer[chunk.SurfaceIndex].Count == chunk.TotalChunks)
        {
            // Merge chunks into full MultiLineData
            var merged = MergeChunks(serverChunkBuffer[chunk.SurfaceIndex]);
            serverChunkBuffer.Remove(chunk.SurfaceIndex);

            // Ensure we have a state entry for this surface
            var state = playerStates.Find(s => s.SurfaceIndex == chunk.SurfaceIndex);
            if (state == null)
            {
                state = new PlayerDrawingState { SurfaceIndex = chunk.SurfaceIndex };
                playerStates.Add(state);
            }

            // Convert merged data to points/colors and mark done
            var (lines, colors) = RebuildLinesFromData(merged);
            state.DrawnPoints = lines;
            state.DrawnColors = colors;
            state.IsDone = true;

            // Track who completed (used to notify them)
            completedClientIds.Add(senderId);

            // Send this player's drawing to all clients (in safe-sized chunks)
            var playerChunksToSend = SplitIntoChunks(chunk.SurfaceIndex, merged);

            var playerChunks = SplitIntoChunks(chunk.SurfaceIndex, merged);
            StartCoroutine(SendChunksWithDelay(playerChunks, false));


            // Check if the whole round is completed (everyone IsDone)
            bool haveAllStates = playerStates.Count == PlayerDataStore.Instance.players.Count;
            bool allDone = haveAllStates && playerStates.All(s => s.IsDone);

            if (allDone)
            {
                // Broadcast final drawings for every surface (in chunks)
                foreach (var s in playerStates)
                {
                    var fullData = ConvertLinesToData(s.DrawnPoints, s.DrawnColors);
                    var chunksForSurface = SplitIntoChunks(s.SurfaceIndex, fullData);

                    // Send broadcast chunks gradually
                    StartCoroutine(SendChunksWithDelay(chunksForSurface, true));
                }

                // Reset
                ResetRoundStates();
                //foreach (var s in playerStates)
                //    s.IsDone = false;

                //completedClientIds.Clear();
            }
            else
            {
                // Notify the players who have completed about who is still remaining
                string[] remaining = playerStates
                    .Where(s => !s.IsDone)
                    .Select(s => s.PlayerName)
                    .ToArray();

                string joined = string.Join("|", remaining);

                // If we have a set of completed client IDs, notify only them (so only they see the waiting message)
                if (completedClientIds.Count > 0)
                {
                    NotifyRemainingPlayersClientRpc(joined, new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = completedClientIds.ToArray()
                        }
                    });
                }
            }
        }
    }
    private IEnumerator SendChunksWithDelay(List<MultiLineChunk> chunks, bool broadcast)
    {
        foreach (var chunk in chunks)
        {
            if (broadcast)
                BroadcastAllDrawingsChunkClientRpc(chunk);
            else
                UpdatePlayerDrawingChunkClientRpc(chunk);

            yield return null;
        }
    }

    private List<MultiLineChunk> SplitIntoChunks(int surfaceIndex, MultiLineData data)
    {
        List<MultiLineChunk> chunks = new();
        int totalChunks = Mathf.CeilToInt((float)data.Points.Length / MaxPointsPerChunk);

        for (int i = 0; i < data.Points.Length; i += MaxPointsPerChunk)
        {
            int len = Mathf.Min(MaxPointsPerChunk, data.Points.Length - i);
            Vector3[] pointsChunk = new Vector3[len];
            System.Array.Copy(data.Points, i, pointsChunk, 0, len);

            chunks.Add(new MultiLineChunk
            {
                SurfaceIndex = surfaceIndex,
                ChunkIndex = chunks.Count,
                TotalChunks = totalChunks,
                Points = pointsChunk,
                LineLengths = data.LineLengths,
                LineColors = data.LineColors
            });
        }
        return chunks;
    }
    private MultiLineData ConvertLinesToData(Vector3[][] lines, uint[] colors)
    {
        List<Vector3> allPoints = new List<Vector3>();
        int[] lengths = new int[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            lengths[i] = lines[i].Length;
            allPoints.AddRange(lines[i]);
        }

        return new MultiLineData
        {
            Points = allPoints.ToArray(),
            LineLengths = lengths,
            LineColors = colors
        };
    }

    [ClientRpc]
    private void UpdatePlayerDrawingChunkClientRpc(MultiLineChunk chunk)
    {
        if (!playerChunkBuffer.ContainsKey(chunk.SurfaceIndex))
            playerChunkBuffer[chunk.SurfaceIndex] = new List<MultiLineChunk>();

        playerChunkBuffer[chunk.SurfaceIndex].Add(chunk);

        // When we have all chunks for this player's submission
        if (playerChunkBuffer[chunk.SurfaceIndex].Count == chunk.TotalChunks)
        {
            var merged = MergeChunks(playerChunkBuffer[chunk.SurfaceIndex]);
            playerChunkBuffer.Remove(chunk.SurfaceIndex);

            var state = playerStates.FirstOrDefault(s => s.SurfaceIndex == chunk.SurfaceIndex);
            if (state == null)
            {
                state = new PlayerDrawingState { SurfaceIndex = chunk.SurfaceIndex };
                playerStates.Add(state);
            }

            var (lines, colors) = RebuildLinesFromData(merged);
            state.DrawnPoints = lines;
            state.DrawnColors = colors;
            state.IsDone = true;
        }
    }
    private MultiLineData MergeChunks(List<MultiLineChunk> chunks)
    {
        chunks = chunks.OrderBy(c => c.ChunkIndex).ToList();

        List<Vector3> points = new();
        foreach (var c in chunks)
            points.AddRange(c.Points);

        return new MultiLineData
        {
            Points = points.ToArray(),
            LineLengths = chunks[0].LineLengths,
            LineColors = chunks[0].LineColors
        };
    }

    private (Vector3[][], uint[]) RebuildLinesFromData(MultiLineData data)
    {
        List<Vector3[]> lines = new List<Vector3[]>();
        int offset = 0;

        foreach (int len in data.LineLengths)
        {
            Vector3[] linePoints = new Vector3[len];
            System.Array.Copy(data.Points, offset, linePoints, 0, len);
            offset += len;
            lines.Add(linePoints);
        }

        return (lines.ToArray(), data.LineColors);
    }

    // bool cleared = false;

    [ClientRpc]
    private void BroadcastAllDrawingsChunkClientRpc(MultiLineChunk chunk)
    {
        if (!broadcastChunkBuffer.ContainsKey(chunk.SurfaceIndex))
            broadcastChunkBuffer[chunk.SurfaceIndex] = new List<MultiLineChunk>();

        broadcastChunkBuffer[chunk.SurfaceIndex].Add(chunk);

        // When all chunks for this surface are in
        if (broadcastChunkBuffer[chunk.SurfaceIndex].Count == chunk.TotalChunks)
        {
            var merged = MergeChunks(broadcastChunkBuffer[chunk.SurfaceIndex]);
            broadcastChunkBuffer.Remove(chunk.SurfaceIndex);

            var state = playerStates.FirstOrDefault(s => s.SurfaceIndex == chunk.SurfaceIndex);
            if (state == null)
            {
                state = new PlayerDrawingState { SurfaceIndex = chunk.SurfaceIndex };
                playerStates.Add(state);
            }

            var (lines, colors) = RebuildLinesFromData(merged);
            state.DrawnPoints = lines;
            state.DrawnColors = colors;

            var surface = allSurfaces.FirstOrDefault(s => s.OwnerIndex == chunk.SurfaceIndex);
            if (surface != null)
            {
                surface.gameObject.SetActive(true);
                surface.Redraw(lines, colors);
            }

            // mark surface done for this round
            completedSurfacesThisRound.Add(chunk.SurfaceIndex);
        }

        if (completedSurfacesThisRound.Count == allSurfaces.Count)
        {
            completedSurfacesThisRound.Clear();
            ResetRoundStates();

            UIManager.Instance.HideWaitingMessage();

            int nextIndex = (currentSurfaceIndex + 1) % allSurfaces.Count;
            currentSurfaceIndex = nextIndex;

            roundCounter++;
            UIManager.Instance.UpdateBodyPartTxt(roundCounter);

            if (roundCounter < 6)
            {
                MoveCameraToSurface(nextIndex, instant: true);

                foreach (var s in allSurfaces)
                    s.lineDrawer.canDraw = (s.OwnerIndex == nextIndex);

                RotatePlayerStates();
            }
            else
            {
                FinishDrawing();
            }
        }
    }

    private void ResetRoundStates()
    {
        foreach (var s in playerStates)
            s.IsDone = false;

        completedClientIds.Clear();
        //cleared = false;
        //completedSurfacesThisRound.Clear();
    }

    private void FinishDrawing()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        StartCoroutine(DoRemaining());
    }

    IEnumerator DoRemaining()
    {
        yield return new WaitForSeconds(0.4f);
        Vector3 pos = cameraRoot.transform.position;
        cameraRoot.transform.position = new Vector3(pos.x, pos.y / 2f, pos.z);

        //Debug.LogError(allSurfaces[currentSurfaceIndex].OwnerIndex + " Owner Index");
        //allSurfaces[currentSurfaceIndex].lineDrawer.canDraw = false;

        foreach (var surface in allSurfaces)
        {
            surface.lineDrawer.canDraw = false;
        }

        MoveInitialCameraOnComplete();
        UIManager.Instance.shareBtn.SetActive(true);
        UIManager.Instance.homeBtn.SetActive(true);

        UIManager.Instance.colorPallet.gameObject.SetActive(false);
        foreach (GameObject btn in UIManager.Instance.navigationBtns)
            btn.SetActive(true);

        cameraRoot.GetComponent<Camera>().orthographicSize = 12f;
        foreach (GameObject obj in UIManager.Instance.offObjs)
        {
            obj.SetActive(false);
        }
    }

    private void RotatePlayerStates()
    {
        foreach (var state in playerStates)
        {
            state.SurfaceIndex = (state.SurfaceIndex + 1) % allSurfaces.Count;
        }
        if (PlayerDataStore.Instance.assignedCreatureIndices.Count != 0)
            UIManager.Instance.UpdateTitleText(PlayerDataStore.Instance.assignedCreatureIndices[currentSurfaceIndex]);

    }
    private string GetPlayerNameBySurfaceIndex(int index)
    {
        var p = PlayerDataStore.Instance.players.FirstOrDefault(p => p.SpawnIndex == index);
        return p != null ? p.Name.Value.ToString() : $"Player {index}";
    }

    [ClientRpc]
    private void NotifyRemainingPlayersClientRpc(string remainingNamesJoined, ClientRpcParams clientRpcParams = default)
    {
        string[] remainingNames = remainingNamesJoined.Split('|');
        string t1 = API.GetText(WordIDs.Waiting_For_Id);
        string t2 = API.GetText(WordIDs.To_Complete_Id);
        string message = t1 + " " + string.Join(", ", remainingNames) + " " + t2;
        UIManager.Instance.ShowWaitingMessage(message);
    }

    private string GetPlayerNameBySpawnIndex(int index)
    {
        var p = PlayerDataStore.Instance.players.FirstOrDefault(p => p.SpawnIndex == index);
        return p != null ? p.Name.Value.ToString() : $"Player {index}";
    }

    public DrawingSurface CurrentDrawingSurface()
    {
        return allSurfaces.FirstOrDefault(s => s.OwnerIndex == currentSurfaceIndex);
    }

    void MoveInitialCameraOnComplete()
    {
        currentSurfaceIndex = 0;
        StopAllCoroutines();
        if (PlayerDataStore.Instance.assignedCreatureIndices.Count != 0)
        {
            UIManager.Instance.UpdateBgOnComplete(currentSurfaceIndex);
        }
        Vector3 pos = cameraPositions[currentSurfaceIndex];
        Vector3 movePos = new Vector3(pos.x, -9f, pos.z);
        StartCoroutine(MoveCameraSmooth(movePos));
    }

    public void MoveLeftOnComplete()
    {
        currentSurfaceIndex--;
        if (currentSurfaceIndex < 0)
            currentSurfaceIndex = allSurfaces.Count - 1;

        StopAllCoroutines();
        if (PlayerDataStore.Instance.assignedCreatureIndices.Count != 0)
        {
            UIManager.Instance.UpdateBgOnComplete(currentSurfaceIndex);
        }
        Vector3 pos = cameraPositions[currentSurfaceIndex];
        Vector3 movePos = new Vector3(pos.x, -9f, pos.z);
        StartCoroutine(MoveCameraSmooth(movePos));
    }

    public void MoveRightOnComplete()
    {
        currentSurfaceIndex++;
        if (currentSurfaceIndex >= allSurfaces.Count)
            currentSurfaceIndex = 0;

        StopAllCoroutines();
        if (PlayerDataStore.Instance.assignedCreatureIndices.Count != 0)
        {
            UIManager.Instance.UpdateBgOnComplete(currentSurfaceIndex);
        }
        Vector3 pos = cameraPositions[currentSurfaceIndex];
        Vector3 movePos = new Vector3(pos.x, -9f, pos.z);
        StartCoroutine(MoveCameraSmooth(movePos));

    }

    public void OnPlayerLeft(ulong clientId)
    {
        if (PlayerDataStore.Instance == null || PlayerDataStore.Instance.players == null)
            return;

        var leftPlayer = PlayerDataStore.Instance.players.FirstOrDefault(p => p.ClientId == clientId);

        string leftName = leftPlayer != null ? leftPlayer.Name.Value.ToString() : "A Player";
        
        StopDrawing();
        ShowPlayerLeftClientRpc(leftName);
    }

    string message = "";

    [ClientRpc]
    private void ShowPlayerLeftClientRpc(string playerName)
    {
        var me = PlayerDataStore.Instance.players.FirstOrDefault(p => p.ClientId == NetworkManager.Singleton.LocalClientId)?.Name;

        string myName = me.Value.ToString();

        string t1 = API.GetText(WordIDs.Game_Over_Id);
        string t2 = API.GetText(WordIDs.GoTo_Menu_Id);
        string t3 = API.GetText(WordIDs.Left_Game_Id);

        if (playerName == myName)
            message = t1 + " " + t2;
        else
            message = t1 + " " + playerName + " " + t3;

        UIManager.Instance.ShowPlayerLeftMessage(message);
    }

    public void LocalMessage()
    {
        var me = PlayerDataStore.Instance.players.FirstOrDefault(p => p.ClientId == NetworkManager.Singleton.LocalClientId)?.Name;

        string myName = me.Value.ToString();

        string t1 = API.GetText(WordIDs.Game_Over_Id);
        string t2 = API.GetText(WordIDs.GoTo_Menu_Id);

        message = t1 + " " + t2;
        UIManager.Instance.ShowPlayerLeftMessage(message);
    }

    public void StopDrawing()
    {
        foreach (var drawBoard in allSurfaces)
        {
            drawBoard.lineDrawer.canDraw = false;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyServerPlayerLeavingServerRpc(ulong leavingClientId, ServerRpcParams rpcParams = default)
    {
        OnPlayerLeft(leavingClientId);
    }
}

[System.Serializable]
public class PlayerDrawingState
{
    public int SurfaceIndex;
    public bool IsDone;

    public ulong OwnerClientId;
    public string PlayerName;

    public Vector3[][] DrawnPoints;
    public uint[] DrawnColors;
}

[System.Serializable]
public struct MultiLineData : INetworkSerializable
{
    public int SurfaceIndex;
    public int ChunkIndex;
    public int TotalChunks;
    public Vector3[] Points;
    public int[] LineLengths;
    public uint[] LineColors;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SurfaceIndex);
        serializer.SerializeValue(ref ChunkIndex);
        serializer.SerializeValue(ref TotalChunks);
        serializer.SerializeValue(ref Points);
        serializer.SerializeValue(ref LineLengths);
        serializer.SerializeValue(ref LineColors);
    }
}

[System.Serializable]
public struct MultiLineChunk : INetworkSerializable
{
    public int SurfaceIndex;
    public int ChunkIndex;
    public int TotalChunks;
    public Vector3[] Points;
    public int[] LineLengths;
    public uint[] LineColors;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SurfaceIndex);
        serializer.SerializeValue(ref ChunkIndex);
        serializer.SerializeValue(ref TotalChunks);
        serializer.SerializeValue(ref Points);
        serializer.SerializeValue(ref LineLengths);
        serializer.SerializeValue(ref LineColors);
    }
}
