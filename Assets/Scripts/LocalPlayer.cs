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

    public Image[] pfp;
    public TextMeshProUGUI[] names;

    void Awake()
    {
        LoadLocalProfile();
        SetPhotonNickname();
    }

    public void UpdatePfp(int index)
    {
        defaultAvatarIndex = index;
        SaveProfile(defaultUsername, defaultAvatarIndex);
    }
    public void UpdatePfpLocal(Sprite pfp_)
    {
        foreach(Image PFP in pfp)
        {
            PFP.sprite = pfp_;
        }
    }
    public void UpdateUsername(string name_)
    {
        defaultUsername = name_;
        SaveProfile(defaultUsername, defaultAvatarIndex);
    }
    public void UpdateUsernameLocal(string usn)
    {
        foreach (TextMeshProUGUI texts in names)
        {
            texts.text = usn;
        }
    }

    // 🚀 Call this when player chooses their name and avatar
    public void SaveProfile(string username, int avatarIndex)
    {
        PlayerPrefs.SetString("username", username);
        PlayerPrefs.SetInt("avatarIndex", avatarIndex);
        PlayerPrefs.Save();
    }

    // 🌱 Loads saved data or uses default values
    private void LoadLocalProfile()
    {
        if (!PlayerPrefs.HasKey("username"))
            PlayerPrefs.SetString("username", defaultUsername);

        if (!PlayerPrefs.HasKey("avatarIndex"))
            PlayerPrefs.SetInt("avatarIndex", defaultAvatarIndex);
    }

    // 🧠 Tells Photon about your username
    private void SetPhotonNickname()
    {
        string username = PlayerPrefs.GetString("username", defaultUsername);
        PhotonNetwork.NickName = username;
    }

    // 🌐 Called when you join a room
    public override void OnJoinedRoom()
    {
        int avatarIndex = PlayerPrefs.GetInt("avatarIndex", defaultAvatarIndex);

        // 🎈 Send your avatar index to others via custom properties
        Hashtable props = new Hashtable
        {
            { "avatarIndex", avatarIndex }
        };

        PhotonNetwork.NickName = defaultUsername;
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        Debug.Log($"[LocalProfile] Set avatar index to {avatarIndex}");
    }

    
}
