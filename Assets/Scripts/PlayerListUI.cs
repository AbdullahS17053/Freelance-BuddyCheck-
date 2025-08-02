using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class PlayerListUI : MonoBehaviourPunCallbacks, IInRoomCallbacks
{
    public static PlayerListUI instance;

    [Header("UI References")]
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private Sprite[] avatarSprites;
    [SerializeField] private Sprite defaultAvatar;

    private Dictionary<int, GameObject> playerSlots = new Dictionary<int, GameObject>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => RefreshPlayerList();
    public override void OnPlayerLeftRoom(Player otherPlayer) => RefreshPlayerList();
    public override void OnJoinedRoom() => RefreshPlayerList();

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Only refresh if avatar index changed
        if (changedProps.ContainsKey("avatarIndex"))
        {
            RefreshPlayerList();
        }
    }

    public void RefreshPlayerList()
    {
        if (playerSlotPrefab == null || playerListContainer == null)
        {
            Debug.LogWarning("Player list UI references not set!");
            return;
        }

        // Clear existing slots
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }
        playerSlots.Clear();

        if (PhotonNetwork.CurrentRoom == null) return;

        // Create slots for all players
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            CreatePlayerSlot(player);
        }
    }

    private void CreatePlayerSlot(Player player)
    {
        GameObject slot = Instantiate(playerSlotPrefab, playerListContainer);

        // Get UI components
        TMP_Text nameText = slot.transform.Find("UsernameText")?.GetComponent<TMP_Text>();
        Image avatarImage = slot.transform.Find("Mask/AvatarImage")?.GetComponent<Image>();

        // Set player name
        if (nameText != null)
        {
            nameText.text = player.NickName ?? "Unknown Player";
        }

        // Set avatar
        if (avatarImage != null)
        {
            avatarImage.sprite = GetPlayerAvatar(player);
        }

        playerSlots[player.ActorNumber] = slot;
    }

    private Sprite GetPlayerAvatar(Player player)
    {
        int avatarIndex = 0;

        // Get avatar index from player properties
        if (player.CustomProperties.TryGetValue("avatarIndex", out object indexObj))
        {
            avatarIndex = (int)indexObj;
        }

        // Return appropriate sprite
        if (avatarSprites != null && avatarIndex >= 0 && avatarIndex < avatarSprites.Length)
        {
            return avatarSprites[avatarIndex];
        }

        return defaultAvatar;
    }

    public Sprite GetAvatarSprite(int index)
    {
        if (avatarSprites == null || avatarSprites.Length == 0)
        {
            Debug.LogError("Avatar sprites array not set up!");
            return defaultAvatar;
        }

        index = Mathf.Clamp(index, 0, avatarSprites.Length - 1);
        return avatarSprites[index];
    }
}