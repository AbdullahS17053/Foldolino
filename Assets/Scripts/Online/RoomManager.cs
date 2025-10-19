using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;


public class RoomManager : NetworkBehaviour
{
    public void LeaveRoom()
    {
        Screen.orientation = ScreenOrientation.LandscapeRight;

        if (NetworkManager.Singleton == null)
        {
            SceneManager.LoadScene(0);
            return;
        }


        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    public void DisplayMessage()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.quitPanel.SetActive(false);
            if (NetworkManager.IsHost)
                UIManager.Instance.gamePlayManager.OnPlayerLeft(NetworkManager.Singleton.LocalClientId);
            else
            {
                UIManager.Instance.gamePlayManager.NotifyServerPlayerLeavingServerRpc(NetworkManager.Singleton.LocalClientId);
                UIManager.Instance.gamePlayManager.LocalMessage();
            }
        }
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
            NetworkManager.Singleton.OnServerStopped += HandleServerStopped;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
            NetworkManager.Singleton.OnServerStopped -= HandleServerStopped;
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        //if (UIManager.Instance != null)
        //{
        //    UIManager.Instance.gamePlayManager.OnPlayerLeft(clientId);
        //}

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Screen.orientation = ScreenOrientation.LandscapeRight;
        }
        else
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
                //SceneManager.LoadScene(0);
            }
        }
    }

    private void HandleServerStopped(bool _)
    {
        //SceneManager.LoadScene(0);
        Screen.orientation = ScreenOrientation.LandscapeRight;

    }
}
