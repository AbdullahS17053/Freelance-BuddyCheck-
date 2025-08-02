using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using TMPro;

public class LocalPlayer : MonoBehaviourPunCallbacks
{
    [Header("Profile Settings")]
    public string defaultUsername = "Player";
    public int defaultAvatarIndex = 0;
    [SerializeField] private int maxAvatarIndex = 5; 

    [Header("UI References")]
    [SerializeField] private Image[] pfp;
    [SerializeField] private TextMeshProUGUI[] names;

    void Awake()
    {
        LoadLocalProfile();
        SetPhotonNickname();
        UpdateUI();
        UpdateNetworkAvatar();
    }

    public void UpdatePfp(int index)
    {
        // Clamp the index to valid range
        defaultAvatarIndex = Mathf.Clamp(index, 0, maxAvatarIndex);
        SaveProfile(defaultUsername, defaultAvatarIndex);
        UpdateUI();
        UpdateNetworkAvatar();
    }

    private void UpdateNetworkAvatar()
    {
        Hashtable props = new Hashtable();
        props["avatarIndex"] = defaultAvatarIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        // Force immediate UI update for all clients
        if (PlayerListUI.instance != null)
        {
            PlayerListUI.instance.RefreshPlayerList();
        }
    }

    private void UpdateUI()
    {
        string username = PlayerPrefs.GetString("username", defaultUsername);
        int avatarIndex = PlayerPrefs.GetInt("avatarIndex", defaultAvatarIndex);

        foreach (Image img in pfp)
        {
            if (img != null && PlayerListUI.instance != null)
            {
                img.sprite = PlayerListUI.instance.GetAvatarSprite(avatarIndex);
            }
        }

        foreach (TextMeshProUGUI text in names)
        {
            if (text != null) text.text = username;
        }
    }

    public void UpdateUsername(TMP_InputField nameField)
    {
        if (nameField == null) return;

        defaultUsername = nameField.text;
        SaveProfile(defaultUsername, defaultAvatarIndex);
        UpdateUI();

        // Update Photon nickname in real-time
        PhotonNetwork.NickName = defaultUsername;
        if (PlayerListUI.instance != null) PlayerListUI.instance.RefreshPlayerList();
    }

    private void SaveProfile(string username, int avatarIndex)
    {
        PlayerPrefs.SetString("username", username);
        PlayerPrefs.SetInt("avatarIndex", avatarIndex);
        PlayerPrefs.Save();
    }

    private void LoadLocalProfile()
    {
        if (!PlayerPrefs.HasKey("username"))
            PlayerPrefs.SetString("username", defaultUsername);

        if (!PlayerPrefs.HasKey("avatarIndex"))
            PlayerPrefs.SetInt("avatarIndex", defaultAvatarIndex);
    }

    private void SetPhotonNickname()
    {
        string username = PlayerPrefs.GetString("username", defaultUsername);
        PhotonNetwork.NickName = username;
    }

    public override void OnJoinedRoom()
    {

        int avatarIndex = PlayerPrefs.GetInt("avatarIndex", defaultAvatarIndex);

        Hashtable props = new Hashtable { { "avatarIndex", avatarIndex } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateNetworkAvatar();
    }

    private Sprite GetAvatarSprite(int index)
    {
        // Implementation depends on your avatar system
        return PlayerListUI.instance.avatarSprites[index];
    }
}