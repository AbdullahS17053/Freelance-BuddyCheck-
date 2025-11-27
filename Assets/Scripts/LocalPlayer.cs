using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalPlayer : MonoBehaviourPunCallbacks
{
    public static LocalPlayer Instance;

    [Header("Profile Settings")]
    public string defaultUsername = "Player";
    public int defaultAvatarIndex = 0;
    [SerializeField] private int maxAvatarIndex = 7;

    [Header("UI References")]
    [SerializeField] private Image[] pfp;
    [SerializeField] private TextMeshProUGUI[] names;
    [HideInInspector] public TMP_InputField nameField;

    public FusionRoomManager fusionRoomManager;
    private List<RoomInfo> cachedRoomList = new List<RoomInfo>();

    void Awake()
    {
        Instance = this;


            LoadLocalProfile();     // Load saved profile
        ApplyProfileToPhoton(); // Set nickname + avatar to Photon
        UpdateUI();             // Update local UI
    }

    // -------------------------------
    // PROFILE LOADING & SAVING
    // -------------------------------
    private void LoadLocalProfile()
    {
        defaultUsername = PlayerPrefs.GetString("username", defaultUsername);
        defaultAvatarIndex = PlayerPrefs.GetInt("avatarIndex", defaultAvatarIndex);
    }

    private void SaveProfile()
    {
        PlayerPrefs.SetString("username", defaultUsername);
        PlayerPrefs.SetInt("avatarIndex", defaultAvatarIndex);
        PlayerPrefs.Save();
    }

    private void ApplyProfileToPhoton()
    {
        PhotonNetwork.NickName = defaultUsername;

        Hashtable props = new Hashtable();
        props["avatarIndex"] = defaultAvatarIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // -------------------------------
    // CHANGE AVATAR
    // -------------------------------
    public void UpdatePfp(int index)
    {
        defaultAvatarIndex = Mathf.Clamp(index, 0, maxAvatarIndex);

        SaveProfile();
        ApplyProfileToPhoton();
        UpdateUI();

        if (PlayerListUI.instance != null)
            PlayerListUI.instance.RefreshPlayerList();
    }

    // -------------------------------
    // CHANGE USERNAME
    // -------------------------------
    public void UpdateUsername(TMP_InputField field)
    {
        string input = field.text.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            if (PlayerPrefs.HasKey("username"))
            {
                defaultUsername = PlayerPrefs.GetString("username", defaultUsername);
            }
            else
            {
                defaultUsername = "Player" + Random.Range(10, 99);
            }
        }
        else
        {
            defaultUsername = input;
            field.text = ""; // Clear input field
        }

        SaveProfile();
        ApplyProfileToPhoton();
        UpdateUI();

        if (PlayerListUI.instance != null)
            PlayerListUI.instance.RefreshPlayerList();
    }


    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        cachedRoomList = roomList;
    }
    // -------------------------------
    // JOIN RANDOM ROOM (Auto-save name first)
    // -------------------------------
    public void JoinRandomRoom(TMP_InputField field)
    {
        fusionRoomManager.loadingPanel.SetActive(false);
        UpdateUsername(field);

        // Define the expected custom properties
        Hashtable expectedProps = new Hashtable { { "public", true } };

        PhotonNetwork.JoinRandomRoom(expectedProps, 0);
    }

    // -------------------------------
    // UI UPDATES
    // -------------------------------
    private void UpdateUI()
    {
        // Update images
        foreach (Image img in pfp)
        {
            if (img != null && PlayerListUI.instance != null)
            {
                img.sprite = PlayerListUI.instance.GetAvatarSprite(defaultAvatarIndex);
            }
        }

        // Update text
        foreach (TextMeshProUGUI txt in names)
        {
            if (txt != null)
                txt.text = defaultUsername;
        }
    }

    public override void OnJoinedRoom()
    {
        ApplyProfileToPhoton();
        if (PlayerListUI.instance != null)
            PlayerListUI.instance.RefreshPlayerList();
    }
}
