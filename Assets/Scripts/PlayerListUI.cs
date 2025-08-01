using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class PlayerListUI : MonoBehaviourPunCallbacks
{
    public static PlayerListUI instance;

    public GameObject playerSlotPrefab;
    public Transform playerListContainer;
    public Sprite[] avatarSprites;

    private Dictionary<int, GameObject> playerSlots = new Dictionary<int, GameObject>();

    void Start()
    {
        instance = this;
        RefreshPlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnJoinedRoom()
    {
        RefreshPlayerList();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        RefreshPlayerList();
    }

    public void RefreshPlayerList()
    {
        // Clear all current slots
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        playerSlots.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject slot = Instantiate(playerSlotPrefab, playerListContainer);
            TMP_Text nameText = slot.transform.Find("UsernameText").GetComponent<TMP_Text>();
            Image avatarImage = slot.transform.Find("Mask/AvatarImage").GetComponent<Image>();

            nameText.text = player.NickName;

            // Get avatar index from custom properties
            int avatarIndex = 0;
            if (player.CustomProperties.ContainsKey("avatarIndex"))
            {
                avatarIndex = (int)player.CustomProperties["avatarIndex"];
            }

            if (avatarIndex >= 0 && avatarIndex < avatarSprites.Length)
            {
                avatarImage.sprite = avatarSprites[avatarIndex];
            }

            playerSlots[player.ActorNumber] = slot;
        }
    }
}
