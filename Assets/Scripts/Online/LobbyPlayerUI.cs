using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A simple UI card that displays a lobby player's info (name, avatar, kick button).
/// This is NOT networked — it just mirrors data from a networked LobbyPlayer.
/// </summary>
public class LobbyPlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text playerNameText;
    public Image avatarImage;
    public Button kickButton;

    // Updates the displayed name.
    public void SetName(string name)
    {
        if (playerNameText != null)
            playerNameText.text = name;
    }

    // Updates the displayed avatar.
    public void SetAvatar(Sprite sprite)
    {
        if (avatarImage != null)
            avatarImage.sprite = sprite;
    }

    // Shows/hides the kick button and assigns its callback.
    public void ShowKickButton(bool show, System.Action onClick)
    {
        if (kickButton == null)
            return;

        kickButton.gameObject.SetActive(show);
        kickButton.onClick.RemoveAllListeners();

        if (show && onClick != null)
            kickButton.onClick.AddListener(() => onClick());
    }
}
