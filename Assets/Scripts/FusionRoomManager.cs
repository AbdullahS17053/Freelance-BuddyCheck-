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
    public GameObject hostPanel;
    public GameObject clientPanel;
    public GameObject menuPanel;
    public GameObject shadowButton;
    public TMP_InputField joinCodeInput;
    public TMP_Text roomCodeText;
    public TMP_Text playerCountText;
    public TMP_Text logText;

    private const int MaxPlayers = 6;
    private string currentRoomCode;
    private List<string> messageLog = new List<string>();
    private bool gameStarted = false;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Log("Connecting to Photon...");
        }
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
            gameStarted = true;
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
        hostPanel.SetActive(true);
        clientPanel.SetActive(false);
        menuPanel.SetActive(false);
    }

    public void ShowClientPanel()
    {
        hostPanel.SetActive(false);
        clientPanel.SetActive(true);
        menuPanel.SetActive(false);
    }

    private void ShowMenuPanel()
    {
        hostPanel.SetActive(false);
        clientPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    private void Log(string message)
    {
        Debug.Log("[PUNRoomManager] " + message);
        messageLog.Add(message);
        if (logText != null)
            logText.text = string.Join("\n", messageLog);
    }
}