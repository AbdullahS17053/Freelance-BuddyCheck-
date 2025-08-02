using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class PUNRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject hostPanel;
    [SerializeField] private GameObject clientPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject shadowButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text logText;

    private const int MaxPlayers = 6;
    private string currentRoomCode;
    private List<string> messageLog = new List<string>();

    void Start()
    {
        if (!ValidateUIReferences()) return;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Log("Connecting to Photon...");
        }
    }

    private bool ValidateUIReferences()
    {
        bool valid = true;
        if (hostPanel == null) { Debug.LogError("Host Panel reference missing!"); valid = false; }
        if (clientPanel == null) { Debug.LogError("Client Panel reference missing!"); valid = false; }
        if (menuPanel == null) { Debug.LogError("Menu Panel reference missing!"); valid = false; }
        if (shadowButton == null) { Debug.LogError("Shadow Button reference missing!"); valid = false; }
        if (joinCodeInput == null) { Debug.LogError("Join Code Input reference missing!"); valid = false; }
        if (roomCodeText == null) { Debug.LogError("Room Code Text reference missing!"); valid = false; }
        if (playerCountText == null) { Debug.LogError("Player Count Text reference missing!"); valid = false; }
        if (logText == null) { Debug.LogError("Log Text reference missing!"); valid = false; }
        return valid;
    }

    public override void OnConnectedToMaster()
    {
        Log("Connected to Master Server.");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Log("Joined Photon Lobby. You can now create or join rooms.");
    }

    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Log("Not connected to Photon yet. Please wait...");
            return;
        }

        currentRoomCode = GenerateRoomCode();
        roomCodeText.text = currentRoomCode;
        Log("Creating room...");

        RoomOptions options = new RoomOptions { MaxPlayers = MaxPlayers, IsOpen = true, IsVisible = true };
        PhotonNetwork.CreateRoom(currentRoomCode, options, TypedLobby.Default);
    }

    public void JoinRoomFromInput()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code) || code.Length < 3)
        {
            Log("Invalid room code.");
            return;
        }

        JoinRoom(code);
    }

    public void JoinRoom(string inputCode)
    {
        currentRoomCode = inputCode.ToUpper();
        roomCodeText.text = $"Joining Room: {currentRoomCode}";
        Log($"Attempting to join room: {currentRoomCode}");

        PhotonNetwork.JoinRoom(currentRoomCode);
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Log("Leaving room...");
            PhotonNetwork.LeaveRoom();
        }
    }

    public void CancelRoomAsHost()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Log("Host cancelling room...");
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.LeaveRoom();
            ShowMenuPanel();
        }
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false; // Prevent more players from joining
            Log("Game started. Room is now closed to new players.");
        }
    }

    public override void OnJoinedRoom()
    {
        Log("Joined room successfully.");
        UpdatePlayerCount();

        if (PhotonNetwork.IsMasterClient)
            ShowHostPanel();
        else
            ShowClientPanel();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Log($"Player joined: {newPlayer.NickName}");
        UpdatePlayerCount();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Log($"Player left: {otherPlayer.NickName}");
        UpdatePlayerCount();
    }

    public override void OnLeftRoom()
    {
        Log("Left room.");
        roomCodeText.text = "Not in a room.";
        playerCountText.text = "Players: 0/6";
        ShowMenuPanel();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Log("Failed to create room: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Log("Failed to join room: " + message);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Log("Disconnected: " + cause);
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
        int count = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        playerCountText.text = $"Players: {count}/{MaxPlayers}";
        Log($"Updated player count: {count}");

        if (shadowButton != null)
            shadowButton.SetActive(count <= 1);
    }

    public void ShowHostPanel()
    {
        if (hostPanel != null) hostPanel.SetActive(true);
        if (clientPanel != null) clientPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void ShowClientPanel()
    {
        if (hostPanel != null) hostPanel.SetActive(false);
        if (clientPanel != null) clientPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void ShowMenuPanel()
    {
        if (hostPanel != null) hostPanel.SetActive(false);
        if (clientPanel != null) clientPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(true);
    }


    private void Log(string message)
    {
        Debug.Log("[PUNRoomManager] " + message);
        messageLog.Add(message);
        if (logText != null)
            logText.text = string.Join("\n", messageLog.TakeLast(5).ToArray()); // Show only last 5 messages
    }
}