
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FusionRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject clientPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button shadowGameButton;

    [Header("Room Info UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text logText;

    private const int MaxPlayers = 6;
    private string currentRoomCode;
    private List<string> messageLog = new List<string>();

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);

        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        currentRoomCode = GenerateRoomCode();
        roomCodeText.text = currentRoomCode;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = MaxPlayers,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(currentRoomCode, options);
    }

    public void JoinRoomFromInput()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code) || code.Length < 3) return;

        joinCodeInput.text = string.Empty;
        JoinRoom(code);
    }

    public void JoinRoom(string inputCode)
    {
        currentRoomCode = inputCode.ToUpper();
        roomCodeText.text = $"Joining: {currentRoomCode}";
        PhotonNetwork.JoinRoom(currentRoomCode);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;

            // Start the actual game
            if (GameplayManager.instance != null)
            {
                GameplayManager.instance.StartGame();
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedRoom()
    {
        UpdatePlayerCount();

        if (PhotonNetwork.IsMasterClient)
            ShowHostPanel();
        else
            ShowClientPanel();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerCount();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerCount();
    }

    public override void OnLeftRoom()
    {
        roomCodeText.text = "Not in a room";
        playerCountText.text = "Players: 0/6";
        ShowMenuPanel();
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string code = "";
        for (int i = 0; i < 5; i++)
            code += chars[Random.Range(0, chars.Length)];
        return code;
    }

    private void UpdatePlayerCount()
    {
        int count = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        playerCountText.text = $"Players: {count}/{MaxPlayers}";

        // FIX: Enable start button when there are 2 or more players AND we are the host
        if (shadowGameButton != null)
        {
            // Only enable if we're the master client AND there are at least 2 players
            bool canStartGame = PhotonNetwork.IsMasterClient && count >= 2;
            shadowGameButton.gameObject.SetActive(!canStartGame);
        }
    }

    public void ShowHostPanel()
    {
        SetPanelActive(hostPanel, true);
        SetPanelActive(clientPanel, false);
        SetPanelActive(menuPanel, false);
        UpdatePlayerCount(); // Update button state when showing host panel
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

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    private void Log(string message)
    {
        Debug.Log("[PUNRoomManager] " + message);
        messageLog.Add(message);

        if (logText != null)
            logText.text = string.Join("\n", messageLog.TakeLast(5).ToArray());
    }
}