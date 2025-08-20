using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.UI;
using TMPro;

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

    void Awake()
    {
        Instance = this;

        LoadLocalProfile();
        SetPhotonNickname();
        UpdateUI();
        UpdateNetworkAvatar();
    }

    public void UpdatePfp(int index)
    {
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

    public void UpdateUsername(TMP_InputField nameField_)
    {
        if (nameField_ == null) return;

        nameField = nameField_;

        defaultUsername = nameField_.text;
        SaveProfile(defaultUsername, defaultAvatarIndex);
        UpdateUI();
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
}