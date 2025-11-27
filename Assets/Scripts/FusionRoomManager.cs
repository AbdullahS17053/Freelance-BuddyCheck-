using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System.Linq;
using System.Collections;

public class FusionRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject clientPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject roomFullPanel;
    [SerializeField] private GameObject roomNotFoundPanel;
    [SerializeField] private GameObject playerDisconnectedPanel;
    [SerializeField] private Button shadowGameButton;

    [Header("Connection UI")]
    [SerializeField] public GameObject loadingPanel;
    [SerializeField] private GameObject connectingPanel;
    [SerializeField] private GameObject reconnectPanel;
    [SerializeField] private Button[] reconnectButton;

    [Header("Room Info UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text[] roomCodeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text logText;

    [SerializeField] private Toggle publicRoomToggle;
    [SerializeField] private GameObject[] textOfToggle;


    private const int MaxPlayers = 7;
    private string currentRoomCode;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);

        connectingPanel?.SetActive(true);
        reconnectPanel?.SetActive(false);

        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();

        foreach (Button btn in reconnectButton)
        {
            if (btn != null)
                btn.onClick.AddListener(TryReconnect);
        }
    }

    private void TryReconnect()
    {
        playerDisconnectedPanel?.SetActive(false);
        reconnectPanel?.SetActive(false);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            connectingPanel?.SetActive(true);
        }
    }

    public override void OnConnectedToMaster()
    {
        connectingPanel?.SetActive(false);
        reconnectPanel?.SetActive(false);

        MusicManager.instance.LobbyMusic();
        PhotonNetwork.JoinLobby();
    }

    

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;

            if (GameplayManager.instance != null)
                GameplayManager.instance.StartGame();
        }
    }

    public void CreateRoom()
    {

        if (!PhotonNetwork.IsConnectedAndReady) return;

        loadingPanel.SetActive(true);

        currentRoomCode = GenerateRoomCode();
        foreach (var t in roomCodeText) t.text = currentRoomCode;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = MaxPlayers,
            IsOpen = true,
            IsVisible = false,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "public", false } },
            CustomRoomPropertiesForLobby = new string[] { "public" }
        };

        PhotonNetwork.CreateRoom(currentRoomCode, options);
    }

    public void JoinRoomFromInput()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code) || code.Length < 3)
        {
            roomNotFoundPanel.SetActive(true);
            return;
        }


        loadingPanel.SetActive(true);

        joinCodeInput.text = string.Empty;
        JoinRoom(code);
    }

    public void JoinRoom(string inputCode)
    {
        currentRoomCode = inputCode.ToUpper();

        loadingPanel.SetActive(true);

        PhotonNetwork.JoinRoom(currentRoomCode);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedRoom()
    {
        loadingPanel.SetActive(false);
        MusicManager.instance.GameMusic();
        foreach (var t in roomCodeText) t.text = PhotonNetwork.CurrentRoom.Name;


        if (PhotonNetwork.IsMasterClient)
            ShowHostPanel();
        else
            ShowClientPanel();

        UpdatePlayerCount();
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        loadingPanel.SetActive(false);
        Debug.Log("No available room found. Creating a new room...");

        roomNotFoundPanel.SetActive(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string msg)
    {
        loadingPanel.SetActive(false);
        Debug.Log($"Join room failed: {returnCode} - {msg}");


        if (returnCode == ErrorCode.GameFull)
        {
            roomFullPanel.SetActive(true);
        }
        else
        {
            roomNotFoundPanel.SetActive(true);
        }
    }

    public override void OnCreatedRoom()
    {
        UpdatePlayerCount();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        loadingPanel.SetActive(false);
        UpdatePlayerCount();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCount();

        // If the game was ongoing, show player disconnected panel
        if (GameplayManager.instance != null && GameplayManager.instance.gameObject.activeInHierarchy)
        {
            publicRoomToggle.isOn = false;
            //playerDisconnectedPanel?.SetActive(true);
        }
    }

    public override void OnLeftRoom()
    {
        MusicManager.instance.LobbyMusic();
        ShowMenuPanel();

        UpdatePlayerCount();
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        loadingPanel.SetActive(false);
        Debug.LogWarning("Photon disconnected: " + cause);
        ShowMenuPanel();
        connectingPanel.SetActive(false); 
        playerDisconnectedPanel.SetActive(true); 
    }

    private void UpdatePlayerCount()
    {
        StopAllCoroutines();

        StartCoroutine(littleDelayedUpdatePlayer());
    }

    private IEnumerator littleDelayedUpdatePlayer()
    {
        yield return new WaitForSeconds(1f);

        int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        playerCountText.text = $"Players: {count}/{MaxPlayers}";

        // Auto-close room when full
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = count < MaxPlayers;
            PhotonNetwork.CurrentRoom.IsVisible = count < MaxPlayers;
        }

        if (shadowGameButton != null)
        {
            bool canStartGame = PhotonNetwork.IsMasterClient && count >= 2;
            shadowGameButton.gameObject.SetActive(!canStartGame);
        }
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string code = "";
        for (int i = 0; i < 5; i++)
            code += chars[Random.Range(0, chars.Length)];
        return code;
    }

    public void ShowHostPanel()
    {
        SetPanelActive(hostPanel, true);
        SetPanelActive(clientPanel, false);
        SetPanelActive(menuPanel, false);
        UpdatePlayerCount();
    }

    public void ShowClientPanel()
    {
        SetPanelActive(hostPanel, false);
        SetPanelActive(clientPanel, true);
        SetPanelActive(menuPanel, false);
    }

    private void ShowMenuPanel()
    {
        SetPanelActive(hostPanel, false);
        SetPanelActive(clientPanel, false);
        SetPanelActive(menuPanel, true);
    }
    public void OnPrivacyToggleChanged()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        bool makePublic = publicRoomToggle.isOn;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "public", makePublic } });

        // Update UI
        textOfToggle[0].SetActive(makePublic);
        textOfToggle[1].SetActive(!makePublic);
    }



    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private string LogMessage(string message)
    {
        Debug.Log("[PUNRoomManager] " + message);
        return message;
    }
}
